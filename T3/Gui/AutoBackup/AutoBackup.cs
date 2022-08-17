﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using T3.Core.Logging;
using T3.Gui.UiHelpers;

namespace T3.Gui.AutoBackup
{
    public class AutoBackup
    {
        public int SecondsBetweenSaves { get; set; }

        public bool IsEnabled { get; set; }

        public AutoBackup()
        {
            SecondsBetweenSaves = 3*60;
            IsEnabled = false;
        }

        /// <summary>
        /// Should be call after all frame operators are completed
        /// </summary>
        public void CheckForSave()
        {
            if (!IsEnabled || _isSaving || Stopwatch.ElapsedMilliseconds < SecondsBetweenSaves * 1000)
                return;

            _isSaving = true;
            Task.Run(CreateBackupCallback);
            Stopwatch.Restart();
        }

        private static void CreateBackupCallback()
        {
            if (T3Ui.IsCurrentlySaving)
            {
                Log.Debug("Skipped backup because saving is in progress.");
                return;
            }
            
            T3Ui.SaveModified();

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            var index = GetIndexOfLastBackup();
            index++;
            ReduceNumberOfBackups();

            var zipFilePath = Path.Join(BackupDirectory, $"#{index:D5}-{DateTime.Now:yyyy_MM_dd-HH_mm_ss_fff}.zip");
            var tempPath = TempDirectory + Guid.NewGuid() + @"\";

            try
            {
                var directoryWithFiles = new Dictionary<string, string[]>();
                foreach (var sourcePath in _sourcePaths)
                {
                    var tempTargetPath = Path.Combine(tempPath, sourcePath);

                    if (!Directory.Exists(tempTargetPath))
                        Directory.CreateDirectory(tempTargetPath);

                    CopyDirectory(sourcePath, tempTargetPath, "*");
                    directoryWithFiles[sourcePath] = Directory.GetFiles(tempTargetPath, "*");
                }

                var zipPath = Path.GetDirectoryName(zipFilePath);
                if (!string.IsNullOrEmpty(zipPath) && !Directory.Exists(zipPath))
                    Directory.CreateDirectory(zipPath);

                using ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);

                foreach (var (directory, value) in directoryWithFiles)
                {
                    foreach (var file in value)
                    {
                        archive.CreateEntryFromFile(file,
                                                    Path.Join(directory, Path.GetFileName(file)),
                                                    CompressionLevel.Fastest);
                    }
                }
            }
            catch (Exception ex)
            {
                DeleteFile(zipFilePath);
                Log.Error("auto backup failed: {0}", ex.Message);
            }
            finally
            {
                Log.Debug($"Deleting {tempPath}");
                DeletePath(tempPath);
            }

            _isSaving = false;
        }

        private static void CopyDirectory(string sourcePath, string destPath, string searchPattern)
        {
            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            foreach (string file in Directory.GetFiles(sourcePath, searchPattern))
            {
                string dest = Path.Combine(destPath, Path.GetFileName(file));
                File.Copy(file, dest);
            }

            foreach (string folder in Directory.GetDirectories(sourcePath, searchPattern))
            {
                string dest = Path.Combine(destPath, Path.GetFileName(folder));
                CopyDirectory(folder, dest, searchPattern);
            }
        }

        private static void DeletePath(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception e)
            {
                Log.Info("Failed to delete path:" + e.Message);
            }
        }

        private static void DeleteFile(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception e)
            {
                Log.Info("Failed to delete file:" + e.Message);
            }
        }

        public static void RestoreLast()
        {
            var lastFile = GetLatestArchiveFile();
            if (lastFile == null)
                return;

            var latestArchiveName = lastFile.FullName;

            using ZipArchive archive = ZipFile.Open(latestArchiveName, ZipArchiveMode.Read);

            const string destinationDirectoryName = ".";

            foreach (var file in archive.Entries)
            {
                var completeFileName = Path.Combine(destinationDirectoryName, file.FullName);

                if (file.Name == "")
                {
                    // Assuming Empty for Directory
                    var directoryName = Path.GetDirectoryName(completeFileName);
                    if (string.IsNullOrEmpty(directoryName))
                        continue;

                    Directory.CreateDirectory(directoryName);
                    continue;
                }

                if (File.Exists(completeFileName))
                {
                    File.Delete(completeFileName);
                }

                file.ExtractToFile(completeFileName, true);
            }
        }

        public static DateTime? GetTimeOfLastBackup()
        {
            var lastFile = GetLatestArchiveFile();
            if (lastFile == null)
                return null;

            var result = Regex.Match(lastFile.Name, @"(#\d\d\d\d\d)?-(\d\d\d\d)_(\d\d)_(\d\d)-(\d\d)_(\d\d)_(\d\d)_(\d\d\d)");

            if (!result.Success)
                return null;

            var year = result.Groups[2].Value;
            var month = result.Groups[3].Value;
            var day = result.Groups[4].Value;
            var hour = result.Groups[5].Value;
            var min = result.Groups[6].Value;
            var second = result.Groups[7].Value;

            var timeFromName = year + "-" + month + "-" + day + " " + hour + ":" + min + ":" + second;

            var date = DateTime.Parse(timeFromName);
            return date;
        }

        private static int GetIndexOfLastBackup()
        {
            var lastFile = GetLatestArchiveFile();
            if (lastFile == null)
                return -1;

            var result = Regex.Match(lastFile.Name, @"#(\d\d\d\d\d)-(\d\d\d\d)_(\d\d)_(\d\d)-(\d\d)_(\d\d)_(\d\d)_(\d\d\d)");

            if (!result.Success)
                return -1;

            var index = int.Parse(result.Groups[1].Value);
            return index;
        }

        private static FileInfo GetLatestArchiveFile()
        {
            if (!Directory.Exists(BackupDirectory))
                return null;

            var backupDirectory = new DirectoryInfo(BackupDirectory);
            return backupDirectory.GetFiles().OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
        }

        /*
         * Reduce the number of backups by removing some of the older backups. The older the backup the
         * less versions are kept. We're using the binary representation of the backup index to separate
         * the deleted versions from the ones we keep.
         * 
         * This algorithm is a hard to describe in words, but it basically thins out the backup-copies 
         * according to their respective binary code:
         * 
         *     bits    significant bit
         *     43210   bit         threshold for 2 saves per generation
         *     ------- ----------- ------------------------------------   
         * 10. 01011 - 1           +0 keep(level0/1st)          <- example of 10 saved versions
         *  9. 01001 - 0           +0 keep(level0/2nd)      
         *  8. 01000 - 4           +1 keep(level1/1st)
         *  7. 00111 - 0            1 remove
         *  6. 00110 - 1           +1 keep(level1/2nd)
         *  5. 00101 - 0            2 remove
         *  4. 00100 - 2           +2 Keep(level2/1st)
         *  3. 00011 - 0            2 remove
         *  2. 00010 - 1            2 remove
         *  1. 00001 - 0            2 remove
         *  0. 00000   inf         +2 keep(level2/2nd)
         * 
         * This means that we're keeping N*log2 backups (e.g. 3*16 out of 65536 saved versions) where N is the backup density.
         */
        private static void ReduceNumberOfBackups(int backupDensity = 3)
        {
            // Gather list of backups with indexes and find latest index
            var regexMatchIndex = new Regex(@"#(\d\d\d\d\d)-(\d\d\d\d)_(\d\d)_(\d\d)-(\d\d)_(\d\d)_(\d\d)_(\d\d\d)");
            var backupFilePathsByIndex = new Dictionary<int, string>();
            var highestIndex = int.MinValue;

            if (!Directory.Exists(BackupDirectory))
                return;

            foreach (var filename in Directory.GetFiles(BackupDirectory))
            {
                var result = regexMatchIndex.Match(filename);
                if (!result.Success)
                    continue;

                var index = int.Parse(result.Groups[1].Value);
                if (index > highestIndex)
                    highestIndex = index;

                backupFilePathsByIndex[index] = filename;
            }

            // Iterate over all files and thin out the backups
            var limit = 0;
            var limitCount = 0;
            for (var i = highestIndex - 1; i >= 0; i--)
            {
                var b = GetSignificantBit(0xffffff - i) + 1;

                // Keep
                if (b > limit)
                {
                    limitCount++;
                    if (limitCount >= backupDensity)
                    {
                        limitCount = 0;
                        limit++;
                    }
                }
                // Remove
                else
                {
                    if (!backupFilePathsByIndex.ContainsKey(i))
                        continue;

                    //Log.Debug($"removing... old backup {backupFilePathsByIndex[i]} (level 2^{b})...");
                    File.Delete(backupFilePathsByIndex[i]);
                }
            }
        }

        /**
         * Get the significant bit in an integer
         */
        private static int GetSignificantBit(int n)
        {
            var a = new bool[32];
            var rest = n;

            // Break down integer into bits
            while (rest > 0)
            {
                var h = (int)Math.Floor(Math.Log(rest, 2));
                rest = rest - (int)Math.Pow(2, h);
                a[h] = true;
            }

            rest = n + 1;
            while (rest > 0)
            {
                var h = (int)Math.Floor(Math.Log(rest, 2));
                rest = rest - (int)Math.Pow(2, h);
                if (a[h] == false)
                {
                    return h;
                }
            }

            return 0;
        }

        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        private static bool _isSaving;

        private const string TempDirectory = @".t3\temp\";
        private const string BackupDirectory = @".t3\backup";

        private static readonly string[] _sourcePaths =
            {
                @"Operators\Types",
                @".t3\layouts",
            };
    }
}