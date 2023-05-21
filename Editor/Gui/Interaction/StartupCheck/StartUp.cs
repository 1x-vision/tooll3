﻿using System;
using System.IO;
using T3.Core.Logging;
using T3.Editor.SystemUi;
using T3.SystemUi;

namespace T3.Editor.Gui.Interaction.StartupCheck
{
    /// <summary>
    /// A helper that provides an option use resort backup with startup failed
    /// </summary>
    public static class StartUp
    {
        public static void FlagBeginStartupSequence()
        {
            if (File.Exists(StartUpLockFilePath))
            {
                ShowMessageBox();
            }

            File.WriteAllText(StartUpLockFilePath, "Startup " + DateTime.Now);
        }

        public static void FlagStartupSequenceComplete()
        {
            if (!File.Exists(StartUpLockFilePath))
            {
                Log.Debug("Strange. Startup file missing?");
            }
            else
            {
                File.Delete(StartUpLockFilePath);
            }
        }

        private static void ShowMessageBox()
        {
            var isThereABackup = !string.IsNullOrEmpty(AutoBackup.AutoBackup.GetLatestArchiveFilePath());
            if (!isThereABackup)
            {
                var result2 = EditorUi.Instance.ShowMessageBox("It looks like the last startup failed.\nSadly there is no backup yet.", "Startup Failed", PopUpButtons.RetryCancel);
                if (result2 != PopUpResult.Retry)
                {
                    EditorUi.Instance.ExitApplication();
                }

                return;
            }

            Log.Debug("StartUpProgress lock file exists?");

            var timeOfLastBackup = AutoBackup.AutoBackup.GetTimeOfLastBackup();
            var timeSpan = GetReadableRelativeTime(timeOfLastBackup);

            const string caption = "Oh no! Start up problems...";
            string message = "It looks like last start up was incomplete.\n\n" +
                             "You can press...\n\n" +
                             $"  YES to restore latest backup {timeSpan}\n" +
                             "  NO to open a link to documentation\n" +
                             "  CANCEL to attempt starting anyways.\n";

            var result = EditorUi.Instance.ShowMessageBox(message, caption, PopUpButtons.YesNoCancel);
            switch (result)
            {
                case PopUpResult.Yes:
                {
                    var wasSuccessful = AutoBackup.AutoBackup.RestoreLast();
                    if (wasSuccessful)
                    {
                        FlagStartupSequenceComplete();
                        EditorUi.Instance.ShowMessageBox("Backup restored. Click OK to restart.\nFingers crossed.", "Complete", PopUpButtons.Ok);
                        //Application.Exit();
                        Environment.Exit(0);
                    }
                    else
                    {
                        EditorUi.Instance.ShowMessageBox("Restoring backup failed.\nYou might want to try an earlier archive in .t3\\backup\\...", "Failed",
                                        PopUpButtons.Ok);
                        Environment.Exit(0);
                    }

                    break;
                }
                case PopUpResult.No:
                    StartupValidation.OpenUrl(HelpUrl);
                    Environment.Exit(0);
                    break;
                
                case PopUpResult.Cancel:
                    break;
            }
        }

        private static string GetReadableRelativeTime(DateTime? timeOfLastBackup)
        {
            if (timeOfLastBackup == null)
                return "Unknown time";

            var timeSinceLastBack = DateTime.Now - timeOfLastBackup;
            var minutes = timeSinceLastBack.Value.TotalMinutes;
            if (minutes < 120)
            {
                return $"{minutes:0} minutes ago";
            }

            var hours = timeSinceLastBack.Value.TotalHours;
            if (hours < 30)
            {
                return $"{hours:0.0} hours ago";
            }

            var days = timeSinceLastBack.Value.TotalDays;
            return $"{days:0.0} days ago";
        }

        private const string HelpUrl = "https://github.com/still-scene/t3/wiki/installation#setup-and-installation";
        private const string StartUpLockFilePath = @".t3\startingUp";
    }
}