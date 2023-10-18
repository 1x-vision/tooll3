using ImGuiNET;
using SharpDX.Direct3D11;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.App;
using T3.Editor.Compilation;
using T3.Editor.Gui;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction.Camera;
using T3.Editor.Gui.Interaction.StartupCheck;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.SystemUi;
using T3.MsForms;
using T3.SystemUi;

namespace T3.Editor
{
    public static class Program
    {
        public static IUiContentDrawer<SharpDX.Direct3D11.Device, ImDrawDataPtr> UiContentContentDrawer;
        public static Device Device { get; private set; }

        public static readonly bool IsStandAlone = File.Exists("StartT3.exe");
        public const string Version = "3.7.1";

        /// <summary>
        /// Generate a release string with 
        /// </summary>
        public static string GetReleaseVersion(bool indicateDebugBuild = true)
        {
            var isDebug = "";
            #if DEBUG
            if (indicateDebugBuild)
            {
                isDebug = "Debug";
            }
            #endif

            var dev = IsStandAlone ? string.Empty : "Dev";
            return $"v{Version} {dev} {isDebug}";
        }

        [STAThread]
        private static void Main()
        {
            // Not calling this first will cause exceptions...
            EditorUi.Instance = new MsFormsEditor();
            EditorUi.Instance.EnableDpiAwareScaling();
            
            var startupStopWatch = new Stopwatch();
            startupStopWatch.Start();
            
            CrashReporting.InitializeCrashReporting();

            ISplashScreen splashScreen = new SplashScreen.SplashScreen();
            splashScreen.Show("Resources/t3-editor/images/t3-SplashScreen.png");
            Log.AddWriter(splashScreen);
            Log.AddWriter(new ConsoleWriter());
            Log.AddWriter(FileWriter.CreateDefault());
            Log.AddWriter(StatusErrorLine);
            Log.AddWriter(ConsoleLogWindow);
            Log.Debug($"Starting {Version}");

            if (IsStandAlone)
            {
                // Validate that operators.dll has been updated to warn users if they started "T3Editor.exe"
                var fiveMinutes = new TimeSpan(0, 2, 0);
                const string operatorFilePath = "Operators.dll";
                if (!File.Exists(operatorFilePath) || (DateTime.Now - File.GetLastWriteTime(operatorFilePath)) > fiveMinutes)
                {
                    EditorUi.Instance
                            .ShowMessageBox($"Operators.dll is outdated.\nPlease use StartT3.exe to run Tooll.",
                                            @"Error", PopUpButtons.Ok);
                    EditorUi.Instance.ExitApplication();
                }
            }
            else
            {
                StartupValidation.CheckInstallation();
            }

            StartUp.FlagBeginStartupSequence();

            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            var userSettings = new UserSettings(saveOnQuit: true);
            var projectSettings = new ProjectSettings(saveOnQuit: true);

            ProgramWindows.InitializeMainWindow(GetReleaseVersion(), out var device);

            Device = device;

            UiContentContentDrawer = new WindowsUiContentDrawer();
            UiContentContentDrawer.Initialize(device, ProgramWindows.Main.Width, ProgramWindows.Main.Height);

            var spaceMouse = new SpaceMouse(ProgramWindows.Main.HwndHandle);
            CameraInteraction.ManipulationDevices = new ICameraManipulator[] { spaceMouse };
            ProgramWindows.SetInteractionDevices(spaceMouse);

            ResourceManager.Init(device);
            ResourceManager resourceManager = ResourceManager.Instance();
            SharedResources.Initialize(resourceManager);

            // Initialize UI and load complete symbol model
            try
            {
                T3Ui = new T3Ui();
            }
            catch (Exception e)
            {
                Log.Error(e.Message + "\n\n" + e.StackTrace);
                var innerException = e.InnerException != null ? e.InnerException.Message.Replace("\\r", "\r") : string.Empty;
                EditorUi.Instance
                        .ShowMessageBox($"Loading Operators failed:\n\n{e.Message}\n{innerException}\n\nThis is liked caused by a corrupted operator file.\nPlease try restarting and restore backup.",
                                        @"Error", PopUpButtons.Ok);
                EditorUi.Instance.ExitApplication();
                return;
            }

            SymbolAnalysis.UpdateUsagesOnly();

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DpiEnableScaleFonts;
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DpiEnableScaleViewports;

            UiContentUpdate.GenerateFontsWithScaleFactor(UserSettings.Config.UiScaleFactor);

            // Setup file watching the operator source
            resourceManager.OperatorsAssembly = T3Ui.UiSymbolData.OperatorsAssembly;
            foreach (var (_, symbol) in SymbolRegistry.Entries)
            {
                var sourceFilePath = SymbolData.BuildFilepathForSymbol(symbol, SymbolData.SourceExtension);
                ResourceManager.Instance().CreateOperatorEntry(sourceFilePath, symbol.Id.ToString(), OperatorUpdating.ResourceUpdateHandler);
            }

            unsafe
            {
                // Disable ImGui ini file settings
                ImGui.GetIO().NativePtr->IniFilename = null;
            }

            Log.RemoveWriter(splashScreen);
            splashScreen.Close();
            splashScreen.Dispose();

            // Initialize optional Viewer Windows
            ProgramWindows.InitializeSecondaryViewerWindow("T3 Viewer", 640, 360);

            StartUp.FlagStartupSequenceComplete();

            startupStopWatch.Stop();
            Log.Debug($"Startup took {startupStopWatch.ElapsedMilliseconds}ms.");

            UiContentUpdate.StartMeasureFrame();
            
            T3Style.Apply();

            ProgramWindows.Main.RunRenderLoop(UiContentUpdate.RenderCallback);

            try
            {
                UiContentContentDrawer.Dispose();
            }
            catch (Exception e)
            {
                Log.Warning("Exception during shutdown: " + e.Message);
            }

            // Release all resources
            try
            {
                ProgramWindows.Release();
            }
            catch (Exception e)
            {
                Log.Warning("Exception freeing resources: " + e.Message);
            }

            Log.Debug("Shutdown complete");
        }

        // Main loop

        public static readonly StatusErrorLine StatusErrorLine = new();
        public static readonly ConsoleLogWindow ConsoleLogWindow = new();
        public static T3Ui T3Ui;
        public static string RequestImGuiLayoutUpdate;
    }
}