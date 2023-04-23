using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
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
using Device = SharpDX.Direct3D11.Device;
using Message = System.Windows.Forms.Message;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor
{
    public static class Program
    {
        private static T3RenderForm _t3RenderForm;
        public static Device Device { get; private set; }
        public static SpaceMouse SpaceMouse { get; private set; }

        public static readonly bool IsStandAlone = File.Exists("StartT3.exe");
        private const string Version = "v3.5.1";

        [STAThread]
        private static void Main(string[] args)
        {
            var startupStopWatch = new Stopwatch();
            startupStopWatch.Start();

            // Enable DPI aware scaling
            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.PerMonitor);
            Application.SetCompatibleTextRenderingDefault(false);

            var logWriter = new ConsoleWriter();
            Log.AddWriter(logWriter);
            Log.AddWriter(FileWriter.CreateDefault());
            Log.Debug($"Starting {Version}");

            StartUp.FlagBeginStartupSequence();

            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            SplashScreen.SplashScreen.OpenSplashScreen("C:/Users/Dom/Downloads/t3-splash-example.png");

            new UserSettings(saveOnQuit: true);
            new ProjectSettings(saveOnQuit: true);

            if (!IsStandAlone && UserSettings.Config.EnableStartupConsistencyCheck)
                StartupValidation.CheckInstallation();

            _main.CreateRenderForm("T3 " + Version, false);

            // Create Device and SwapChain
            SharpDX.Direct3D11.Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, _main.SwapChainDescription, out var device,
                                                          out _main.SwapChain);
            _deviceContext = device.ImmediateContext;
            Device = device;
            Factory factory = _main.SwapChain.GetParent<Factory>();

            // Ignore all windows events
            factory.MakeWindowAssociation(_main.Form.Handle, WindowAssociationFlags.IgnoreAll);

            _t3RenderForm = new T3RenderForm(device, _main.Form.Width, _main.Form.Height);

            // Initialize T3 main window
            _main.InitRenderTargetsAndEventHandlers(device);
            _main.Form.KeyDown += HandleKeyDown;
            _main.Form.KeyUp += HandleKeyUp;
            _main.Form.Closing += (sender, args) =>
                                  {
                                      if (T3Ui.UiModel.IsSaving)
                                      {
                                          args.Cancel = true;
                                          Log.Debug($"Cancel closing because save-operation is in progress.");
                                      }
                                      else
                                      {
                                          Log.Debug("Shutting down");
                                      }
                                  };

            _main.Form.WindowState = FormWindowState.Maximized;
            SpaceMouse = new SpaceMouse(_main.Form.Handle);

            // Initialize optional Viewer Windows
            Viewer.CreateRenderForm("T3 Viewer", true);
            Viewer.InitViewSwapChain(factory, device);
            Viewer.InitRenderTargetsAndEventHandlers(device);
            Viewer.Form.Show();

            ResourceManager.Init(device);
            ResourceManager resourceManager = ResourceManager.Instance();
            SharedResources.Initialize(resourceManager);

            // Initialize UI and load complete symbol model
            try
            {
                _t3ui = new T3Ui();
            }
            catch (Exception e)
            {
                Log.Error(e.Message + "\n\n" + e.StackTrace);
                var innerException = e.InnerException != null ? e.InnerException.Message.Replace("\\r", "\r") : string.Empty;
                MessageBox.Show($"Loading Operators failed:\n\n{e.Message}\n{innerException}\n\nThis is liked caused by a corrupted operator file.\nPlease try restarting and restore backup.",@"Error", MessageBoxButtons.OK);
                Application.Exit();
                return;
            }

            SymbolAnalysis.UpdateUsagesOnly();

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DpiEnableScaleFonts;
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DpiEnableScaleViewports;

            GenerateFontsWithScaleFactor(UserSettings.Config.UiScaleFactor);

            // Setup file watching the operator source
            resourceManager.OperatorsAssembly = T3Ui.UiModel.OperatorsAssembly;
            foreach (var (_, symbol) in SymbolRegistry.Entries)
            {
                var sourceFilePath = Model.BuildFilepathForSymbol(symbol, Model.SourceExtension);
                ResourceManager.Instance().CreateOperatorEntry(sourceFilePath, symbol.Id.ToString(), OperatorUpdating.ResourceUpdateHandler);
            }

            ShaderResourceView viewWindowBackgroundSrv = null;

            unsafe
            {
                // Disable ImGui ini file settings
                ImGui.GetIO().NativePtr->IniFilename = null;
            }

            SplashScreen.SplashScreen.CloseSplashScreen();
            StartUp.FlagStartupSequenceComplete();

            startupStopWatch.Stop();
            Log.Debug($"Startup took {startupStopWatch.ElapsedMilliseconds}ms.");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Int64 lastElapsedTicks = stopwatch.ElapsedTicks;

            T3Style.Apply();

            var p = Cursor.Position;
            // Main loop
            void RenderCallback()
            {
                CursorPosOnScreen = new Vector2(Cursor.Position.X, Cursor.Position.Y);
                IsCursorInsideAppWindow= _main.Form.Bounds.Contains(Cursor.Position);

                // Update font atlas texture if UI-Scale changed
                if (Math.Abs(UserSettings.Config.UiScaleFactor - _lastUiScale) > 0.005f)
                {
                    GenerateFontsWithScaleFactor(UserSettings.Config.UiScaleFactor);
                    _lastUiScale = UserSettings.Config.UiScaleFactor;
                }

                if (_main.Form.WindowState == FormWindowState.Minimized == true)
                {
                    Thread.Sleep(100);
                    return;
                }

                Int64 ticks = stopwatch.ElapsedTicks;
                Int64 ticksDiff = ticks - lastElapsedTicks;
                ImGui.GetIO().DeltaTime = (float)((double)(ticksDiff) / Stopwatch.Frequency);
                lastElapsedTicks = ticks;
                ImGui.GetIO().DisplaySize = new Vector2(_main.Form.ClientSize.Width, _main.Form.ClientSize.Height);

                HandleFullscreenToggle();

                NodeOperations.UpdateChangedOperators();

                DirtyFlag.IncrementGlobalTicks();
                T3Metrics.UiRenderingStarted();

                if (!string.IsNullOrEmpty(RequestImGuiLayoutUpdate))
                {
                    ImGui.LoadIniSettingsFromMemory(RequestImGuiLayoutUpdate);
                    RequestImGuiLayoutUpdate = null;
                }

                ImGui.NewFrame();
                _main.PrepareRenderingFrame(_deviceContext);

                // Render 2nd view
                Viewer.Form.Visible = T3Ui.ShowSecondaryRenderWindow;
                if (T3Ui.ShowSecondaryRenderWindow)
                {
                    Viewer.PrepareRenderingFrame(_deviceContext);

                    if (ResourceManager.ResourcesById[SharedResources.FullScreenVertexShaderId] is VertexShaderResource vsr)
                        _deviceContext.VertexShader.Set(vsr.VertexShader);

                    if (ResourceManager.ResourcesById[SharedResources.FullScreenPixelShaderId] is PixelShaderResource psr)
                        _deviceContext.PixelShader.Set(psr.PixelShader);

                    if (resourceManager.SecondRenderWindowTexture != null && !resourceManager.SecondRenderWindowTexture.IsDisposed)
                    {
                        //Log.Debug($"using TextureId:{resourceManager.SecondRenderWindowTexture}, debug name:{resourceManager.SecondRenderWindowTexture.DebugName}");
                        if (viewWindowBackgroundSrv == null ||
                            viewWindowBackgroundSrv.Resource.NativePointer != resourceManager.SecondRenderWindowTexture.NativePointer)
                        {
                            viewWindowBackgroundSrv?.Dispose();
                            viewWindowBackgroundSrv = new ShaderResourceView(device, resourceManager.SecondRenderWindowTexture);
                        }

                        _deviceContext.Rasterizer.State = SharedResources.ViewWindowRasterizerState;
                        _deviceContext.PixelShader.SetShaderResource(0, viewWindowBackgroundSrv);
                    }
                    else if (ResourceManager.ResourcesById[SharedResources.ViewWindowDefaultSrvId] is ShaderResourceViewResource srvr)
                    {
                        _deviceContext.PixelShader.SetShaderResource(0, srvr.ShaderResourceView);
                        //Log.Debug($"using Default TextureId:{srvr.TextureId}, debug name:{srvr.ShaderResourceView.DebugName}");
                    }
                    else
                    {
                        Log.Debug("invalid srv for 2nd render view");
                    }

                    _deviceContext.Draw(3, 0);
                    _deviceContext.PixelShader.SetShaderResource(0, null);
                }

                _t3ui.ProcessFrame();

                _deviceContext.Rasterizer.SetViewport(new Viewport(0, 0, _main.Form.ClientSize.Width, _main.Form.ClientSize.Height, 0.0f, 1.0f));
                _deviceContext.OutputMerger.SetTargets(_main.RenderTargetView);

                ImGui.Render();
                _t3RenderForm.RenderImDrawData(ImGui.GetDrawData());

                T3Metrics.UiRenderingCompleted();

                _main.SwapChain.Present(T3Ui.UseVSync ? 1 : 0, PresentFlags.None);

                if (T3Ui.ShowSecondaryRenderWindow)
                    Viewer.SwapChain.Present(T3Ui.UseVSync ? 1 : 0, PresentFlags.None);
            }

            RenderLoop.Run(_main.Form, RenderCallback);

            try
            {
                _t3RenderForm.Dispose();
            }
            catch (Exception e)
            {
                Log.Warning("Exception during shutdown: " + e.Message);
            }

            // Release all resources
            try
            {
                _main.RenderTargetView.Dispose();
                _main.BackBufferTexture.Dispose();
                _deviceContext.ClearState();
                _deviceContext.Flush();
                device.Dispose();
                _deviceContext.Dispose();
                _main.SwapChain.Dispose();
                factory.Dispose();
            }
            catch (Exception e)
            {
                Log.Warning("Exception freeing resources: " + e.Message);
            }

            Log.Debug("Shutdown complete");
        }

        

        private static void HandleFullscreenToggle()
        {
            var isBorderStyleFullScreen = _main.Form.FormBorderStyle == FormBorderStyle.None;
            if (isBorderStyleFullScreen == UserSettings.Config.FullScreen)
                return;

            if (UserSettings.Config.FullScreen)
            {
                _main.Form.FormBorderStyle = FormBorderStyle.Sizable;
                _main.Form.WindowState = FormWindowState.Normal;
                _main.Form.FormBorderStyle = FormBorderStyle.None;

                var screenCount = Screen.AllScreens.Length;
                var hasSecondScreen = screenCount > 1;
                var secondScreenIndex = hasSecondScreen ? 1 : 0;

                var screenIndexForMainScreen = UserSettings.Config.SwapMainAnd2ndWindowsWhenFullscreen ? secondScreenIndex : 0;
                var screenIndexForSecondScreen = UserSettings.Config.SwapMainAnd2ndWindowsWhenFullscreen ? 0 : secondScreenIndex;

                var formBounds = Screen.AllScreens[screenIndexForMainScreen].Bounds;
                formBounds.Width = formBounds.Width;
                formBounds.Height = formBounds.Height;
                _main.Form.Bounds = formBounds;

                if (T3Ui.ShowSecondaryRenderWindow)
                {
                    Viewer.Form.WindowState = FormWindowState.Normal;
                    Viewer.Form.FormBorderStyle = FormBorderStyle.None;
                    Viewer.Form.Bounds = Screen.AllScreens[screenIndexForSecondScreen].Bounds;
                }
                else
                {
                    Viewer.Form.WindowState = FormWindowState.Normal;
                    Viewer.Form.FormBorderStyle = FormBorderStyle.None;
                    Viewer.Form.Bounds = Screen.AllScreens[screenIndexForSecondScreen].Bounds;
                }
            }
            else
            {
                _main.Form.FormBorderStyle = FormBorderStyle.Sizable;
                Viewer.Form.FormBorderStyle = FormBorderStyle.Sizable;
            }
        }

        private static void HandleKeyDown(object sender, KeyEventArgs e)
        {
            var keyIndex = (int)e.KeyCode;
            if (keyIndex >= KeyHandler.PressedKeys.Length)
            {
                Log.Warning($"Ignoring out of range key code {e.KeyCode} with index {keyIndex}");
            }
            else
            {
                KeyHandler.PressedKeys[keyIndex] = true;
            }
        }

        private static void HandleKeyUp(object sender, KeyEventArgs e)
        {
            var keyIndex = (int)e.KeyCode;
            if (keyIndex < KeyHandler.PressedKeys.Length)
            {
                KeyHandler.PressedKeys[keyIndex] = false;
            }
        }

        public static void GenerateFontsWithScaleFactor(float scaleFactor)
        {
            // See https://stackoverflow.com/a/5977638
            Graphics graphics = _main.Form.CreateGraphics();
            T3Ui.DisplayScaleFactor = graphics.DpiX / 96f;
            var dpiAwareScale = scaleFactor * T3Ui.DisplayScaleFactor;

            T3Ui.UiScaleFactor = dpiAwareScale;

            var fontAtlasPtr = ImGui.GetIO().Fonts;
            fontAtlasPtr.Clear();
            Fonts.FontNormal = fontAtlasPtr.AddFontFromFileTTF(@"Resources/t3-editor/fonts/Roboto-Regular.ttf", 18f * dpiAwareScale);
            Fonts.FontBold = fontAtlasPtr.AddFontFromFileTTF(@"Resources/t3-editor/fonts/Roboto-Medium.ttf", 18f * dpiAwareScale);
            Fonts.FontSmall = fontAtlasPtr.AddFontFromFileTTF(@"Resources/t3-editor/fonts/Roboto-Regular.ttf", 13f * dpiAwareScale);
            Fonts.FontLarge = fontAtlasPtr.AddFontFromFileTTF(@"Resources/t3-editor/fonts/Roboto-Light.ttf", 30f * dpiAwareScale);

            _t3RenderForm.CreateDeviceObjects();
        }
        

        private static float _lastUiScale = 1;

        private static readonly AppWindow _main = new();
        public static readonly AppWindow Viewer = new(); // Required it distinguish 2nd render view in mouse handling   

        private static T3Ui _t3ui = null;
        private static DeviceContext _deviceContext;
        public static Vector2 CursorPosOnScreen  {get; private set;}
        public static bool IsCursorInsideAppWindow { get; private set; }
        public static string RequestImGuiLayoutUpdate;
    }
}