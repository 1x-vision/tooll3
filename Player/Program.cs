#define FORCE_D3D_DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CommandLine;
using CommandLine.Text;
using ManagedBass;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.Compilation;
using T3.Core.DataTypes.Vector;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.SystemUi;
using T3.Editor.App;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;
using Vector2 = System.Numerics.Vector2;
using SharpDX.Windows;
using T3.Core.Utils;
using T3.Serialization;
using T3.SystemUi;
using DeviceContext = SharpDX.Direct3D11.DeviceContext;
using Factory = SharpDX.DXGI.Factory;
using FillMode = SharpDX.Direct3D11.FillMode;
using ResourceManager = T3.Core.Resource.ResourceManager;

namespace T3.Player
{
    internal static partial class Program
    {
        private class Options
        {
            [Option(Default = false, Required = false, HelpText = "Disable vsync")]
            public bool NoVsync { get; set; }

            [Option(Default = 1920, Required = false, HelpText = "Defines the width")]
            public int Width { get; set; }

            [Option(Default = 1080, Required = false, HelpText = "Defines the height")]
            public int Height { get; set; }

            [Option(Default = false, Required = false, HelpText = "Run in windowed mode")]
            public bool Windowed { get; set; }

            [Option(Default = false, Required = false, HelpText = "Loops the demo")]
            public bool Loop { get; set; }

            [Option(Default = true, Required = false, HelpText = "Show log messages.")]
            public bool Logging { get; set; }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            CoreUi.Instance = new MsForms.MsForms();
            
            var settingsPath = Path.Combine(RuntimeAssemblies.CoreDirectory, "exportSettings.json");
            if (!JsonUtils.TryLoadingJson(settingsPath, out ExportSettings exportSettings))
            {
                var message = $"Failed to load export settings from \"{settingsPath}\". Exiting!";
                Log.Error(message);
                CoreUi.Instance.ShowMessageBox(message);
                return;
            }

            ProjectSettings.Config = exportSettings!.ConfigData;
            
            var logDirectory = Path.Combine(Core.UserData.UserData.SettingsFolder, exportSettings.Author, exportSettings.ApplicationTitle);
            var fileWriter = FileWriter.CreateDefault(logDirectory, out var logPath);
            try
            {
                Log.AddWriter(new ConsoleWriter());
                Log.AddWriter(fileWriter);

                if (!TryResolveOptions(args, exportSettings!, out _resolvedOptions))
                    return;
                
                Log.Info($"Starting {exportSettings.ApplicationTitle} with id {exportSettings.OperatorId} by {exportSettings.Author}.");
                Log.Info($"Build: {exportSettings.BuildId}, Editor: {exportSettings.EditorVersion}");
                
                ShaderCompiler.ShaderCacheSubdirectory = Path.Combine("Player", 
                                                                      exportSettings.EditorVersion, 
                                                                      exportSettings.Author,
                                                                      exportSettings.ApplicationTitle, 
                                                                      exportSettings.OperatorId.ToString(), 
                                                                      exportSettings.BuildId.ToString());

                var resolution = new Int2(_resolvedOptions.Width, _resolvedOptions.Height);
                _vsyncInterval = Convert.ToInt16(!_resolvedOptions.NoVsync);
                Log.Debug($": {_vsyncInterval}, windowed: {_resolvedOptions.Windowed}, size: {resolution}, loop: {_resolvedOptions.Loop}, logging: {_resolvedOptions.Logging}");

                var iconPath = Path.Combine(SharedResources.Directory, "t3-editor", "images", "t3.ico");
                var gotIcon = File.Exists(iconPath);

                Icon icon;
                if (!gotIcon)
                {
                    Log.Warning("Failed to load icon");
                    icon = null;
                }
                else
                {
                    icon = new Icon(iconPath);
                }

                _renderForm = new RenderForm(exportSettings!.ApplicationTitle)
                                  {
                                      ClientSize = new Size(resolution.X, resolution.Y),
                                      AllowUserResizing = false,
                                      Icon = icon,
                                  };

                var handle = _renderForm.Handle;

                // SwapChain description
                var desc = new SwapChainDescription
                               {
                                   BufferCount = 3,
                                   ModeDescription = new ModeDescription(resolution.Width, resolution.Height,
                                                                         new Rational(60, 1), Format.R8G8B8A8_UNorm),
                                   IsWindowed = _resolvedOptions.Windowed,
                                   OutputHandle = handle,
                                   SampleDescription = new SampleDescription(1, 0),
                                   SwapEffect = SwapEffect.FlipDiscard,
                                   Flags = SwapChainFlags.AllowModeSwitch,
                                   Usage = Usage.RenderTargetOutput
                               };

                // Create Device and SwapChain
                #if DEBUG || FORCE_D3D_DEBUG
                var deviceCreationFlags = DeviceCreationFlags.Debug;
                #else
                var deviceCreationFlags = DeviceCreationFlags.None;
                #endif
                Device.CreateWithSwapChain(DriverType.Hardware, deviceCreationFlags, desc, out _device, out _swapChain);
                ResourceManager.Instance().Init(_device);
                _deviceContext = _device.ImmediateContext;

                var cursor = CoreUi.Instance.Cursor;

                if (_swapChain.IsFullScreen)
                {
                    cursor.SetVisible(false);
                }

                // Ignore all windows events
                var factory = _swapChain.GetParent<Factory>();
                factory.MakeWindowAssociation(_renderForm.Handle, WindowAssociationFlags.IgnoreAll);

                // initialize input
                InitializeInput(_renderForm);

                // New RenderTargetView from the backbuffer
                _backBuffer = Resource.FromSwapChain<Texture2D>(_swapChain, 0);
                _renderView = new RenderTargetView(_device, _backBuffer);

                var shaderCompiler = new DX11ShaderCompiler
                                         {
                                             Device = _device
                                         };
                ShaderCompiler.Instance = shaderCompiler;
                
                SharedResources.Initialize();
                
                _fullScreenPixelShaderResource = SharedResources.FullScreenPixelShaderResource;
                _fullScreenVertexShaderResource = SharedResources.FullScreenVertexShaderResource;

                LoadOperators();

                if(!SymbolRegistry.TryGetSymbol(exportSettings.OperatorId, out var demoSymbol))
                {
                    CloseApplication(true, $"Failed to find [{exportSettings.ApplicationTitle}] with id {exportSettings.OperatorId}");
                    return;
                }

                var playbackSettings = demoSymbol.PlaybackSettings;
                _playback = new Playback
                                {
                                    Settings = playbackSettings
                                };

                // Create instance of project op, all children are create automatically

                if (!demoSymbol.TryCreateParentlessInstance(out _project))
                {
                    CloseApplication(true, $"Failed to create instance of project op {demoSymbol}");
                }
                
                _evalContext = new EvaluationContext();

                var prerenderRequired = false;

                Bass.Free();
                Bass.Init();

                _resolution = new Int2(_resolvedOptions.Width, _resolvedOptions.Height);

                // Init wasapi input if required
                if (playbackSettings is { AudioSource: PlaybackSettings.AudioSources.ProjectSoundTrack } && playbackSettings.GetMainSoundtrack(out _soundtrack))
                {
                    if (_soundtrack.TryGetAbsoluteFilePath(out _))
                    {
                        _playback.Bpm = _soundtrack.Bpm;
                        // Trigger loading clip
                        AudioEngine.UseAudioClip(_soundtrack, 0);
                        AudioEngine.CompleteFrame(_playback, Playback.LastFrameDuration); // Initialize
                        prerenderRequired = true;
                    }
                    else
                    {
                        Log.Warning($"Can't find soundtrack {_soundtrack.FilePath}");
                        _soundtrack = null;
                    }
                }

                var rasterizerDesc = new RasterizerStateDescription
                                         {
                                             FillMode = FillMode.Solid,
                                             CullMode = CullMode.None,
                                             IsScissorEnabled = false,
                                             IsDepthClipEnabled = false
                                         };
                _rasterizerState = new RasterizerState(_device, rasterizerDesc);

                foreach (var output in _project.Outputs)
                {
                    if (output is Slot<Texture2D> textureSlot)
                    {
                        if (_textureOutput == null)
                            _textureOutput = textureSlot;
                        else
                        {
                            var message = "Multiple texture outputs found. Only the first one will be used.";
                            Log.Warning(message);
                            break;
                        }
                    }
                }

                // TODO - implement proper shader pre-compilation as an option to instance instantiation
                // move this to core?
                // Sample some frames to preload all shaders and resources
                if (prerenderRequired)
                {
                    PreloadShadersAndResources(_soundtrack.LengthInSeconds, _resolution, _playback, _deviceContext, _evalContext, _textureOutput, _swapChain,
                                               _renderView);
                }

                // Start playback           
                _playback.Update();
                _playback.TimeInBars = 0;
                _playback.PlaybackSpeed = 1.0;

                try
                {
                    // Main loop
                    RenderLoop.Run(_renderForm, RenderCallback);
                }
                catch (TimelineEndedException)
                {
                    Log.Info($"Program ended at the end of the timeline: {_playback.TimeInSecs:0.00}s / {_playback.TimeInBars:0.00} bars");
                    CloseApplication(false, null);
                }
                catch (Exception e)
                {
                    var errorMessage = "Exception in main loop:\n" + e;
                    CloseApplication(true, errorMessage);
                    Log.Error(errorMessage);
                    fileWriter.Dispose(); // flush and close
                    CoreUi.Instance.ShowMessageBox(errorMessage);
                }

            }
            catch (Exception e)
            {
                CloseApplication(true, "Exception in initialization:\n" + e);
            }
            
            return;

            void CloseApplication(bool error, string message)
            {
                CoreUi.Instance.Cursor.SetVisible(true);
                bool openLogs = false;
                if (!string.IsNullOrWhiteSpace(message))
                {
                    Log.Error(message);

                    const int maxLines = 10;
                    message = StringUtils.TrimStringToLineCount(message, maxLines).ToString();

                    if (error)
                    {
                        message += "\n\nDo you want to open the log file?";

                        var result = CoreUi.Instance.ShowMessageBox(message, $"{exportSettings.ApplicationTitle} crashed /:", PopUpButtons.YesNo);
                        openLogs = result == PopUpResult.Yes;
                    }
                }
                    
                fileWriter.Dispose(); // flush and close

                // Release all resources
                _renderView?.Dispose();
                _backBuffer?.Dispose();
                _deviceContext?.ClearState();
                _deviceContext?.Flush();
                _device?.Dispose();
                _deviceContext?.Dispose();
                
                if (openLogs)
                {
                    CoreUi.Instance.OpenWithDefaultApplication(logPath);
                }
                
                CoreUi.Instance.ExitApplication();
            }
        }

        private static void RebuildBackBuffer(RenderForm form, Device device, ref RenderTargetView rtv, ref Texture2D buffer, SwapChain swapChain)
        {
            rtv.Dispose();
            buffer.Dispose();
            swapChain.ResizeBuffers(3, form.ClientSize.Width, form.ClientSize.Height, Format.Unknown, SwapChainFlags.AllowModeSwitch);
            buffer = Resource.FromSwapChain<Texture2D>(swapChain, 0);
            rtv = new RenderTargetView(device, buffer);
        }

        private static bool TryResolveOptions(string[] args, ExportSettings exportSettings, out Options resolvedOptions)
        {
            var parser = new Parser(config =>
                                    {
                                        config.HelpWriter = null;
                                        config.AutoVersion = false;
                                    });
            var parserResult = parser.ParseArguments<Options>(args);
            var helpText = HelpText.AutoBuild(parserResult,
                                              h =>
                                              {
                                                  h.AdditionalNewLineAfterOption = false;

                                                  // Todo: This should use information from the main operator
                                                  h.Heading = exportSettings.ApplicationTitle;

                                                  h.Copyright = exportSettings.Author;
                                                  h.AutoVersion = false;
                                                  return h;
                                              },
                                              e => e);

            Options parsedOptions = null;
            parserResult.WithParsed(o => { parsedOptions = o; })
                        .WithNotParsed(_ => { Log.Debug(helpText); });

            resolvedOptions = parsedOptions;
            if (resolvedOptions == null)
                return false;
            
            // use windowed status _only_ when explicitly set, the Options struct doesn't know about this
            if (!args.Any(s => "--windowed".Contains(s)))
            {
                parsedOptions.Windowed = exportSettings.WindowMode == WindowMode.Windowed;
            }

            return true;
        }

        private readonly struct PackageLoadInfo(
            PlayerSymbolPackage package,
            List<SymbolJson.SymbolReadResult> newlyLoadedSymbols)
        {
            public readonly PlayerSymbolPackage Package = package;
            public readonly List<SymbolJson.SymbolReadResult> NewlyLoadedSymbols = newlyLoadedSymbols;
        }

        // Private static bool _inResize;
        private static int _vsyncInterval;
        private static SwapChain _swapChain;
        private static RenderTargetView _renderView;
        private static Texture2D _backBuffer;
        private static Instance _project;
        private static EvaluationContext _evalContext;
        private static Playback _playback;
        private static AudioClip _soundtrack;
        private static DeviceContext _deviceContext;
        private static Options _resolvedOptions;
        private static RenderForm _renderForm;
        private static Texture2D _outputTexture;
        private static ShaderResourceView _outputTextureSrv;
        private static RasterizerState _rasterizerState;
        private static ShaderResource<VertexShader> _fullScreenVertexShaderResource;
        private static ShaderResource<PixelShader> _fullScreenPixelShaderResource;
        private static Device _device;
        private static Int2 _resolution;
        private static Slot<Texture2D> _textureOutput;
    }
}