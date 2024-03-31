﻿using System.IO;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Layouts;
using T3.Editor.Gui.Windows.RenderExport;
using T3.Editor.UiModel;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Windows.Output
{
    internal class OutputWindow : Window
    {
        #region Window implementation
        public OutputWindow()
        {
            Config.Title = LayoutHandling.OutputPrefix + _instanceCounter;
            Config.Visible = true;

            AllowMultipleInstances = true;
            Config.Visible = true;
            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

            _instanceCounter++;
            _camSelectionHandling = new CameraSelectionHandling();
            OutputWindowInstances.Add(this);
        }

        public static IEnumerable<OutputWindow> GetVisibleInstances()
        {
            foreach (var i in OutputWindowInstances)
            {
                if (!(i is OutputWindow outputWindow))
                    continue;

                if (!i.Config.Visible)
                    continue;

                yield return outputWindow;
            }
        }

        public static OutputWindow GetPrimaryOutputWindow()
        {
            return GetVisibleInstances().FirstOrDefault();
        }

        public Texture2D GetCurrentTexture()
        {
            return _imageCanvas?.LastTexture;
        }

        protected override void Close()
        {
            OutputWindowInstances.Remove(this);
        }

        protected override void AddAnotherInstance()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new OutputWindow();
        }

        public override List<Window> GetInstances()
        {
            return OutputWindowInstances;
        }
        #endregion

        protected override void DrawContent()
        {
            ImGui.BeginChild("##content", new Vector2(0, ImGui.GetWindowHeight()), false,
                             ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollWithMouse);
            {
                // Very ugly hack to prevent scaling the output above window size
                var keepScale = T3Ui.UiScaleFactor;
                T3Ui.UiScaleFactor = 1;
                
                // Draw output
                _imageCanvas.SetAsCurrent();

                // Move down to avoid overlapping with toolbar
                ImGui.SetCursorPos(ImGui.GetWindowContentRegionMin() + new Vector2(0, 40));

                Pinning.TryGetPinnedOrSelectedInstance(out var drawnInstance,  out var canvas);

                if (canvas != null)
                {
                    Pinning.TryGetPinnedEvaluationInstance(canvas?.Structure, out var evaluationInstance);

                    var drawnType = UpdateAndDrawOutput(drawnInstance, evaluationInstance);
                    _imageCanvas.Deactivate();
                    _camSelectionHandling.Update(drawnInstance, drawnType);
                    var editingFlags = _camSelectionHandling.PreventCameraInteraction | _camSelectionHandling.PreventImageCanvasInteraction
                                           ? T3Ui.EditingFlags.PreventMouseInteractions
                                           : T3Ui.EditingFlags.None;

                    _imageCanvas.Update(editingFlags);

                    T3Ui.UiScaleFactor = keepScale;
                    DrawToolbar(drawnType);
                }
                CustomComponents.DrawWindowFocusFrame();
            }
            ImGui.EndChild();
        }

        private void DrawToolbar(Type drawnType)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);
            ImGui.SetCursorPos(ImGui.GetWindowContentRegionMin());
            Pinning.DrawPinning();

            if (CustomComponents.StateButton("1:1",
                                             Math.Abs(_imageCanvas.Scale.X - 1f) < 0.001f
                                                 ? CustomComponents.ButtonStates.Disabled
                                                 : CustomComponents.ButtonStates.Normal))
            {
                _imageCanvas.SetScaleToMatchPixels();
                _imageCanvas.SetViewMode(ImageOutputCanvas.Modes.Pixel);
            }

            ImGui.SameLine();

            {
                if (CustomComponents.StateButton("Fit",
                                                 _imageCanvas.ViewMode == ImageOutputCanvas.Modes.Fitted
                                                     ? CustomComponents.ButtonStates.Disabled
                                                     : CustomComponents.ButtonStates.Normal)
                    || KeyboardBinding.Triggered(UserActions.FocusSelection))
                {
                    if (drawnType == typeof(Texture2D))
                    {
                        _imageCanvas.SetViewMode(ImageOutputCanvas.Modes.Fitted);
                    }
                    else if (drawnType == typeof(Command))
                    {
                        _camSelectionHandling.ResetView();
                    }
                }

                var label = drawnType == typeof(Texture2D) ? "Fit image to view" : "Reset view or camera position";
                var shortCut = KeyboardBinding.ListKeyboardShortcuts(UserActions.FocusSelection);
                CustomComponents.TooltipForLastItem(label, shortCut);
            }

            ImGui.SameLine();

            var showGizmos = _evaluationContext.ShowGizmos != GizmoVisibility.Off;
            if (CustomComponents.ToggleIconButton(Icon.Grid, "##gizmos", ref showGizmos, Vector2.One * ImGui.GetFrameHeight()))
            {
                _evaluationContext.ShowGizmos = showGizmos
                                                    ? GizmoVisibility.On
                                                    : GizmoVisibility.Off;
            }

            CustomComponents.TooltipForLastItem("Toggle gizmos and floor grid.",
                                                "Gizmos are available for selected transform operators and can be dragged to adjust their position.");
            ImGui.SameLine();

            _camSelectionHandling.DrawCameraControlSelection();
            
            ResolutionHandling.DrawSelector(ref _selectedResolution, _resolutionDialog);

            ImGui.SameLine();
            ColorEditButton.Draw(ref _backgroundColor, new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight()));
            CustomComponents.TooltipForLastItem("Adjust background color of view");
            ImGui.PopStyleColor();

            var texture = GetCurrentTexture();
            if (texture != null)
            {
                ImGui.SameLine();

                if (CustomComponents.IconButton(Icon.Snapshot, new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight())))
                {
                    const string folder = @"Screenshots/";
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    var filename = Path.Join(folder, $"{DateTime.Now:yyyy_MM_dd-HH_mm_ss_fff}.png");
                    ScreenshotWriter.StartSavingToFile(texture, filename, ScreenshotWriter.FileFormats.Png);
                }

                CustomComponents.TooltipForLastItem("Save screenshot");
            }

            ImGui.SameLine();
            ImGui.PushID("CamSpeed");
            var result = SingleValueEdit.Draw(ref UserSettings.Config.CameraSpeed, new Vector2(ImGui.GetFrameHeight() * 2, ImGui.GetFrameHeight()), 0.001f, 100,
                                              true, 0.01f, "{0:G3}");
            CustomComponents.TooltipForLastItem("Camera speed when flying with ASDW keys.", "TIP: Use mouse wheel while flying to adjust on the fly.");
            ImGui.PopID();
        }

        /// <summary>
        /// Update content with an <see cref="EvaluationContext"/> and use the DrawImplementation for the given type to draw it. 
        /// </summary>
        private Type UpdateAndDrawOutput(Instance instanceForOutput, Instance instanceForEvaluation = null)
        {
            if (instanceForEvaluation == null)
                instanceForEvaluation = instanceForOutput;

            if (instanceForEvaluation == null || instanceForEvaluation.Outputs.Count <= 0)
                return null;

            var evaluatedSymbolUi = instanceForEvaluation.GetSymbolUi();
            
            // Todo: support different outputs...
            var evalOutput = instanceForEvaluation.Outputs[0];
            if (!evaluatedSymbolUi.OutputUis.TryGetValue(evalOutput.Id, out IOutputUi evaluatedOutputUi))
                return null;

            if (_imageCanvas.ViewMode != ImageOutputCanvas.Modes.Fitted
                && evaluatedOutputUi is CommandOutputUi)
            {
                _imageCanvas.SetViewMode(ImageOutputCanvas.Modes.Fitted);
            }

            // Prepare context
            _evaluationContext.Reset();
            _evaluationContext.BypassCameras = _camSelectionHandling.BypassCamera;
            _evaluationContext.RequestedResolution = _selectedResolution.ComputeResolution();

            // Set camera
            if (_camSelectionHandling.CameraForRendering != null)
            {
                _evaluationContext.SetViewFromCamera(_camSelectionHandling.CameraForRendering);
            }

            _evaluationContext.BackgroundColor = _backgroundColor;

            const string overrideSampleVariableName = "OverrideMotionBlurSamples";
            if (BaseRenderWindow.IsToollRenderingSomething)
            {
                var samples = BaseRenderWindow.OverrideMotionBlurSamples;
                if (samples >= 0)
                {
                    _evaluationContext.IntVariables[overrideSampleVariableName] = samples;
                }
            }
            else
            {
                _evaluationContext.IntVariables.Remove(overrideSampleVariableName);
            }

            // Ugly hack to hide final target
            if (instanceForOutput != instanceForEvaluation)
            {
                ImGui.BeginChild("hidden", Vector2.One);
                {
                    evaluatedOutputUi.DrawValue(evalOutput, _evaluationContext);
                }
                ImGui.EndChild();

                if (instanceForOutput == null || instanceForOutput.Outputs.Count == 0)
                    return null;

                var viewOutput = instanceForOutput.Outputs[0];
                var viewSymbolUi = instanceForOutput.GetSymbolUi();
                if (!viewSymbolUi.OutputUis.TryGetValue(viewOutput.Id, out IOutputUi viewOutputUi))
                    return null;

                // Render!
                viewOutputUi.DrawValue(viewOutput, _evaluationContext, recompute: false);
                return viewOutputUi.Type;
            }
            else
            {
                // Render!
                evaluatedOutputUi.DrawValue(evalOutput, _evaluationContext);
                return evalOutput.ValueType;
            }
        }

        public Instance ShownInstance
        {
            get
            {
                Pinning.TryGetPinnedOrSelectedInstance(out var instance, out _);
                return instance;
            }
        }

        public static readonly List<Window> OutputWindowInstances = new();
        public ViewSelectionPinning Pinning { get; } = new();

        private System.Numerics.Vector4 _backgroundColor = new(0.1f, 0.1f, 0.1f, 1.0f);
        private readonly EvaluationContext _evaluationContext = new();
        private readonly ImageOutputCanvas _imageCanvas = new();
        private readonly CameraSelectionHandling _camSelectionHandling;
        private static int _instanceCounter;
        private ResolutionHandling.Resolution _selectedResolution = ResolutionHandling.DefaultResolution;
        private readonly EditResolutionDialog _resolutionDialog = new();
    }
}