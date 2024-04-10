﻿using System.IO;
using T3.Core.SystemUi;
using T3.Core.UserData;
using T3.Editor.Gui.UiHelpers;
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
                ShowLastStartupFailedMessageBox();
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

        private static void ShowLastStartupFailedMessageBox()
        {
            var isThereABackup = !string.IsNullOrEmpty(AutoBackup.AutoBackup.GetLatestArchiveFilePath());
            if (!isThereABackup)
            {
                var result2 = BlockingWindow.Instance.Show("It looks like the last startup failed.\nSadly there is no backup yet.", "Startup Failed", "Retry", "Cancel");
                if (result2 != "Retry")
                {
                    Log.Info("User cancelled startup.");
                    EditorUi.Instance.ExitApplication();
                }

                return;
            }

            Log.Debug("StartUpProgress lock file exists?");

            var timeOfLastBackup = AutoBackup.AutoBackup.GetTimeOfLastBackup();
            var timeSpan = THelpers.GetReadableRelativeTime(timeOfLastBackup);

            const string caption = "Oh no! Start up problems...";
            string message = "It looks like last start up was incomplete.\n\n" +
                             "You can press...\n\n" +
                             $"  YES to restore latest backup {timeSpan}\n" +
                             "  NO to open a link to documentation\n" +
                             "  CANCEL to attempt starting anyways.\n";

            const string restore = "Restore backup";
            const string openDoc = "Open documentation";
            const string startup = "I don't care do it anyway!!!!";
            var result = BlockingWindow.Instance.Show(message, caption, restore, openDoc, startup);
            switch (result)
            {
                case restore:
                {
                    var wasSuccessful = AutoBackup.AutoBackup.RestoreLast();
                    if (wasSuccessful)
                    {
                        FlagStartupSequenceComplete();
                        BlockingWindow.Instance.Show("Backup restored. Click OK to restart.\nFingers crossed.", "Complete", "Ok");
                        //Application.Exit();
                        Environment.Exit(0);
                    }
                    else
                    {
                        BlockingWindow.Instance.Show("Restoring backup failed.\nYou might want to try an earlier archive in .t3\\backup\\...", "Failed",
                            "Ok");
                        Environment.Exit(0);
                    }

                    break;
                }
                case openDoc:
                    CoreUi.Instance.OpenWithDefaultApplication(HelpUrl);
                    Environment.Exit(0);
                    break;
                
                case startup:
                    break;
            }
        }

        private const string HelpUrl = "https://github.com/tooll3/t3/wiki/installation#setup-and-installation";
        private static string StartUpLockFilePath => Path.Combine(UserData.SettingsFolder, "startingUp");
    }
}