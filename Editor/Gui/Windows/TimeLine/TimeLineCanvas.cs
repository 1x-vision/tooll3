using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.Snapping;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.Interaction.WithCurves;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.TimeLine.Raster;
using T3.Editor.UiModel;
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace T3.Editor.Gui.Windows.TimeLine;

/// <summary>
/// Combines multiple <see cref="ITimeObjectManipulation"/>s into a single consistent
/// timeline that allows dragging selected time elements of various types.
/// </summary>
public class TimeLineCanvas : CurveEditCanvas
{
    public TimeLineCanvas()
    {
        DopeSheetArea = new DopeSheetArea(SnapHandlerForU, this);
        _timelineCurveEditArea = new TimelineCurveEditArea(this, SnapHandlerForU, SnapHandlerForV);
        _timeSelectionRange = new TimeSelectionRange(this, SnapHandlerForU);
        LayersArea = new LayersArea(SnapHandlerForU);

        SnapHandlerForV.AddSnapAttractor(_horizontalRaster);
        SnapHandlerForU.AddSnapAttractor(_clipRange);
        SnapHandlerForU.AddSnapAttractor(_loopRange);
        SnapHandlerForU.AddSnapAttractor(_timeRasterSwitcher);
        SnapHandlerForU.AddSnapAttractor(_currentTimeMarker);
        SnapHandlerForU.AddSnapAttractor(LayersArea);
    }


    public void Draw(Instance compositionOp, Playback playback)
    {
        Current = this;
        Playback = playback;
        SelectedAnimationParameters = GetAnimationParametersForSelectedNodes(compositionOp);

        // Very ugly hack to prevent scaling the output above window size
        var keepScale = T3Ui.UiScaleFactor;
        T3Ui.UiScaleFactor = 1;

        // UpdateLocalTimeTranslation(compositionOp);
        // ImGui.TextUnformatted($"Scroll: {Scroll.X}   Scale: {Scale.X}");

        ScrollToTimeAfterStopped();

        var modeChanged = UpdateMode();
        DrawCurveCanvas(drawAdditionalCanvasContent: DrawCanvasContent, 0, T3Ui.EditingFlags.AllowHoveredChildWindows);

        Current = null;
        
        T3Ui.UiScaleFactor = keepScale;
        
        return;

        void DrawCanvasContent()
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY()-6);
            if (PlaybackUtils.TryFindingSoundtrack(out var soundtrack))
            {
                _timeLineImage.Draw(Drawlist, soundtrack);
            }
            _timeRasterSwitcher.Draw(Playback);

                
            HandleDeferredActions();

            ImGui.BeginChild(ImGuiTitle, new Vector2(0, -30), true,
                             ImGuiWindowFlags.NoMove 
                             |ImGuiWindowFlags.NoBackground
                             |ImGuiWindowFlags.NoScrollWithMouse);

            {
                if (KeyboardBinding.Triggered(UserActions.DeleteSelection))
                    DeleteSelectedElements();
                    
                switch (Mode)
                {
                    case Modes.DopeView:
                        LayersArea.Draw(compositionOp, Playback);
                        DopeSheetArea.Draw(compositionOp, SelectedAnimationParameters);
                        break;
                    case Modes.CurveEditor:
                        _horizontalRaster.Draw(this);
                        _timelineCurveEditArea.Draw(compositionOp, SelectedAnimationParameters, fitCurvesVertically: modeChanged);
                        break;
                }

                var compositionTimeClip = Structure.GetCompositionTimeClip(compositionOp);

                if (Playback.IsLooping)
                {
                    _loopRange.Draw(this, Playback, Drawlist, SnapHandlerForU);
                }
                else if (compositionTimeClip != null)
                {
                    _clipRange.Draw(this, compositionTimeClip, Drawlist, SnapHandlerForU);
                }
            }
                
            ImGui.EndChild();
                
            _currentTimeMarker.Draw(Playback);
            _timeSelectionRange.Draw(Drawlist);
            DrawDragTimeArea();

            if (FenceState == SelectionFence.States.CompletedAsClick)
            {
                var newTime = InverseTransformPositionFloat(ImGui.GetMousePos()).X;
                if (Playback.IsLooping)
                {
                    Playback.TimeInBars = newTime;
                    // var newStartTime = newTime - newTime % 4;
                    // var duration = Playback.LoopRange.Duration;
                    // Playback.LoopRange.Start = newStartTime;
                    // Playback.LoopRange.Duration = duration;
                }
                else
                {
                    Playback.TimeInBars = newTime;
                }
            }
        }
    }



    #region handle nested timelines ----------------------------------
    public override void UpdateScaleAndTranslation(Instance compositionOp, ICanvas.Transition transition)
    {
        if (transition == ICanvas.Transition.Undefined) return;

        // remember the old scroll state
        var oldScale = Scale;
        var oldScroll = Scroll;

        var clip = Structure.GetCompositionTimeClip(compositionOp);
        if (clip == null) return;

        // determine scaling factor
        TimeRange sourceRange, targetRange;
        if (transition == ICanvas.Transition.JumpIn)
        {
            sourceRange = clip.TimeRange;
            targetRange = clip.SourceRange;
        }
        else
        {
            sourceRange = clip.SourceRange;
            targetRange = clip.TimeRange;
        }

        float scale = targetRange.Duration / sourceRange.Duration;

        // remove scrolling, then determine where the time clip is centered
        Scroll = new Vector2(0, Scroll.Y);
        var centerOfSourceRange = (sourceRange.Start + sourceRange.End) * 0.5f;
        var originalScreenPos = TransformX(centerOfSourceRange);

        // now apply scaling and determine where the source clip is centered
        Scale /= scale;
        var centerOfTargetRange = (targetRange.Start + targetRange.End) * 0.5f;
        var newScreenPos = TransformX(centerOfTargetRange);

        // set final scale and "undo" the movement of the position
        ScaleTarget.X = Scale.X;
        var positionDelta = new Vector2(newScreenPos - originalScreenPos, 0f);
        ScrollTarget.X = oldScroll.X * scale + InverseTransformDirection(positionDelta).X;

        // restore the old scale and scroll state
        Scale = oldScale;
        Scroll = oldScroll;
    }
    #endregion

    private void HandleDeferredActions()
    {
        if (UserActionRegistry.WasActionQueued(UserActions.PlaybackJumpToNextKeyframe))
        {

            var bestNextTime = double.PositiveInfinity;
            var foundNext = false;
            var time = Playback.TimeInBars + 0.001f;
            foreach (var next in SelectedAnimationParameters)
            {
                foreach (var curve in next.Curves)
                {
                    if (!curve.TryGetNextKey(time, out var key)
                        || key.U > bestNextTime )
                        continue;

                    foundNext = true;
                    bestNextTime = key.U;
                }
            }

            if (foundNext)
                Playback.TimeInBars = bestNextTime;
        }

        if (UserActionRegistry.WasActionQueued(UserActions.PlaybackJumpToPreviousKeyframe))
        {
            var bestPreviousTime = double.NegativeInfinity;
            var foundNext = false;
                
            var time = Playback.TimeInBars - 0.001f;
            foreach (var next in SelectedAnimationParameters)
            {
                foreach (var curve in next.Curves)
                {
                    if (!curve.TryGetPreviousKey(time, out var key)
                        || key.U < bestPreviousTime )
                        continue;

                    foundNext = true;
                    bestPreviousTime = key.U;
                }
            }

            if (foundNext)
                Playback.TimeInBars = bestPreviousTime;                
        }
    }

    private void DrawDragTimeArea()
    {
        if (Playback == null)
            return;

        var max = ImGui.GetContentRegionMax();
        var clampedSize = max;
        clampedSize.Y = Math.Min(TimeLineDragHeight, max.Y - 1);

        ImGui.SetCursorPos(new Vector2(0, max.Y - clampedSize.Y));
        var screenPos = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddRectFilled(screenPos, screenPos + new Vector2(clampedSize.X, clampedSize.Y), UiColors.BackgroundFull.Fade(0.1f));

        ImGui.InvisibleButton("##TimeDrag", clampedSize);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
        }

        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left) || ImGui.IsItemClicked())
        {
            var draggedTime = InverseTransformX(Io.MousePos.X);
            if (ImGui.GetIO().KeyShift)
            {
                SnapHandlerForU.CheckForSnapping(ref draggedTime, Scale.X, new List<IValueSnapAttractor> { _currentTimeMarker });
            }

            Playback.TimeInBars = draggedTime;
        }

        ImGui.SetCursorPos(Vector2.Zero);
    }

    private void ScrollToTimeAfterStopped()
    {
        var isPlaying = Math.Abs(Playback.PlaybackSpeed) > 0.01f;
        var wasPlaying = Math.Abs(_lastPlaybackSpeed) > 0.01f;

        if (!isPlaying && wasPlaying)
        {
            if (!IsCurrentTimeVisible())
            {
                // assume we are not scrolling, what screen position would the playhead be at?
                var oldScroll = Scroll;
                Scroll = new Vector2(0, Scroll.Y);
                var posScreen = TransformX((float) Playback.TimeInBars);
                // position that playhead in the center of the window
                ScrollTarget.X = InverseTransformX(posScreen - WindowSize.X*0.5f);
                // restore old state of scrolling
                Scroll = oldScroll;
            }
        }

        _lastPlaybackSpeed = Playback.PlaybackSpeed;
    }

    private bool IsCurrentTimeVisible()
    {
        var timePosInScreen = TransformPosition(new Vector2((float)this.Playback.TimeInBars, 0));
        var timelineArea = ImRect.RectWithSize(WindowPos, WindowSize);
        timePosInScreen.Y = timelineArea.GetCenter().Y; // Adjust potential vertical scrolling of timeline area
        return timelineArea.Contains(timePosInScreen);
    }

    #region view modes
    private bool UpdateMode()
    {
        if (Mode == _lastMode)
            return false;

        switch (_lastMode)
        {
            case Modes.DopeView:
                TimeObjectManipulators.Remove(DopeSheetArea);
                TimeObjectManipulators.Remove(LayersArea);
                SnapHandlerForU.RemoveSnapAttractor(DopeSheetArea);
                break;

            case Modes.CurveEditor:
                TimeObjectManipulators.Remove(_timelineCurveEditArea);
                SnapHandlerForU.RemoveSnapAttractor(_timelineCurveEditArea);
                break;
        }

        switch (Mode)
        {
            case Modes.DopeView:
                TimeObjectManipulators.Add(DopeSheetArea);
                TimeObjectManipulators.Add(LayersArea);
                SnapHandlerForU.AddSnapAttractor(DopeSheetArea);
                break;

            case Modes.CurveEditor:
                TimeObjectManipulators.Add(_timelineCurveEditArea);
                SnapHandlerForU.AddSnapAttractor(_timelineCurveEditArea);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        _lastMode = Mode;
        return true;
    }

    public enum Modes
    {
        DopeView,
        CurveEditor,
    }

    public Modes Mode = Modes.DopeView;
    private Modes _lastMode = Modes.CurveEditor; // Make different to force initial update
    #endregion

        
        
    // TODO: this is horrible and should be refactored
    private List<AnimationParameter> GetAnimationParametersForSelectedNodes(Instance compositionOp)
    {
        var selection = NodeSelection.GetSelectedNodes<ISelectableCanvasObject>();
        var symbolUi = SymbolUiRegistry.Entries[compositionOp.Symbol.Id];
        var animator = symbolUi.Symbol.Animator;
            
        // No Linq to avoid allocations
        _pinnedParams.Clear();
        foreach (Instance child in compositionOp.Children)
        foreach (var input in child.Inputs)
        {
            if (animator.IsInputSlotAnimated(input))
                foreach (var pinnedInputSlot in DopeSheetArea.PinnedParameters)
                {
                    if (pinnedInputSlot == input.GetHashCode())
                        _pinnedParams.Add(new AnimationParameter() { Instance = child, Input = input, Curves = animator.GetCurvesForInput(input), ChildUi = symbolUi.ChildUis.Single(childUi => childUi.Id == child.SymbolChildId) });
                }
        }

        _curvesForSelection.Clear();
            
        foreach (Instance child in compositionOp.Children)
        foreach (var selectedElement in selection)
        {
            if (child.SymbolChildId == selectedElement.Id)
                foreach (var input in child.Inputs)
                {
                    if (animator.IsInputSlotAnimated(input))
                        _curvesForSelection.Add(new AnimationParameter() { Instance = child, Input = input, Curves = animator.GetCurvesForInput(input), ChildUi = symbolUi.ChildUis.Single(childUi => childUi.Id == selectedElement.Id) });
                }
        }

        _pinnedParams.AddRange(_curvesForSelection.FindAll(sp => _pinnedParams.All(pp => pp.Input != sp.Input)));
        return _pinnedParams;
    }

    public List<AnimationParameter> SelectedAnimationParameters = new();

    internal Playback Playback;

    private readonly TimeRasterSwitcher _timeRasterSwitcher = new();
    private readonly HorizontalRaster _horizontalRaster = new();
    private readonly ClipRange _clipRange = new();
    private readonly LoopRange _loopRange = new();

    public readonly DopeSheetArea DopeSheetArea;
    private readonly TimelineCurveEditArea _timelineCurveEditArea;
    private readonly TimeLineImage _timeLineImage = new();

    private readonly CurrentTimeMarker _currentTimeMarker = new();
    private readonly TimeSelectionRange _timeSelectionRange;
    public readonly LayersArea LayersArea;

    public static TimeLineCanvas Current;

    private double _lastPlaybackSpeed;
    private readonly List<AnimationParameter> _pinnedParams = new(20);
    private readonly List<AnimationParameter> _curvesForSelection = new(64);

    // Styling
    public const float TimeLineDragHeight = 30;

    public struct AnimationParameter
    {
        public IEnumerable<Curve> Curves;
        public IInputSlot Input;
        public Instance Instance;
        public SymbolChildUi ChildUi;
    }
        
    public enum FrameStepAmount
    {
        FrameAt60Fps,
        FrameAt30Fps,
        FrameAt15Fps,
        Bar,
        Beat,
        Tick,
    }
}