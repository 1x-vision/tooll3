using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Animation;
using T3.Editor.Gui.InputUi.CombinedInputs;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.Animation;
using T3.Editor.Gui.Interaction.Snapping;
using T3.Editor.Gui.Interaction.WithCurves;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace T3.Editor.Gui.Windows.TimeLine
{
    public class TimelineCurveEditArea : AnimationParameterEditing, ITimeObjectManipulation, IValueSnapAttractor
    {
        public TimelineCurveEditArea(TimeLineCanvas timeLineCanvas, ValueSnapHandler snapHandlerForU, ValueSnapHandler snapHandlerV)
        {
            _snapHandlerU = snapHandlerForU;
            _snapHandlerV = snapHandlerV;
            TimeLineCanvas = timeLineCanvas;
            // _curveEditBox = new CurveEditBox(timeLineCanvas, snapHandlerForU);
        }

        private readonly StringBuilder _stringBuilder = new StringBuilder(100);
        private readonly List<VDefinition> _visibleKeyframes = new(1000);

        public void Draw(Instance compositionOp, List<TimeLineCanvas.AnimationParameter> animationParameters, bool fitCurvesVertically = false)
        {
            _visibleKeyframes.Clear();
            _compositionOp = compositionOp;
            AnimationParameters = animationParameters;

            if (fitCurvesVertically)
            {
                var bounds = GetBoundsOnCanvas(GetSelectedOrAllPoints());
                TimeLineCanvas.Current.SetVerticalScopeToCanvasArea(bounds, flipY: true);
            }

            ImGui.BeginGroup();
            {
                var drawList = ImGui.GetWindowDrawList();
                drawList.ChannelsSplit(3);
                if (KeyboardBinding.Triggered(UserActions.FocusSelection))
                    ViewAllOrSelectedKeys(alsoChangeTimeRange: true);

                if (KeyboardBinding.Triggered(UserActions.Duplicate))
                    DuplicateSelectedKeyframes(TimeLineCanvas.Playback.TimeInBars);

                var lineStartPosition = ImGui.GetCursorPos();
                var visibleCurveCount = 0;

                ImGui.PushFont(Fonts.FontSmall);
                foreach (var param in animationParameters)
                {
                    ImGui.PushID(param.Input.GetHashCode());
                    drawList.ChannelsSetCurrent(1);
                    var hash = param.Input.GetHashCode();
                    var isParamPinned = _pinnedParameterComponents.ContainsKey(hash);

                    _stringBuilder.Clear();
                    _stringBuilder.Append(param.Instance.Symbol.Name);
                    _stringBuilder.Append('.');
                    _stringBuilder.Append(param.Input.Input.Name);
                    var paramName = _stringBuilder.ToString(); // param.Instance.Symbol.Name + "." + param.Input.Input.Name;

                    var cursorPosition = lineStartPosition;
                    ImGui.SetCursorPos(cursorPosition);

                    if (DrawPinButton(isParamPinned, paramName))
                    {
                        if (isParamPinned)
                        {
                            _pinnedParameterComponents.Remove(hash);
                            isParamPinned = false;
                        }
                        else
                        {
                            _pinnedParameterComponents[hash] = 0xffff;
                            isParamPinned = true;
                        }
                    }

                    cursorPosition += new Vector2(ImGui.GetItemRectSize().X, 0);

                    var isParamHovered = ImGui.IsItemHovered();

                    var curveNames = param.Input.ValueType == typeof(Vector4)
                                         ? DopeSheetArea.ColorCurveNames
                                         : DopeSheetArea.CurveNames;

                    var curveIndex = 0;
                    foreach (var curve in param.Curves)
                    {
                        //ImGui.SameLine();
                        ImGui.SetCursorPos(cursorPosition);

                        var componentName = curveNames[curveIndex % curveNames.Length];
                        var bit = 1 << curveIndex;

                        var isParamComponentPinned = isParamPinned && (_pinnedParameterComponents[hash] & bit) != 0;
                        if (DrawPinButton(isParamComponentPinned, componentName))
                        {
                            if (isParamComponentPinned)
                            {
                                var flags = _pinnedParameterComponents[hash] ^ bit;
                                if (flags == 0)
                                {
                                    _pinnedParameterComponents.Remove(hash);
                                    isParamPinned = false;
                                }
                                else
                                {
                                    _pinnedParameterComponents[hash] = flags;
                                }
                            }
                            else
                            {
                                if (isParamPinned)
                                {
                                    _pinnedParameterComponents[hash] |= bit;
                                }
                                else
                                {
                                    _pinnedParameterComponents[hash] = bit;
                                }
                            }

                            isParamComponentPinned = !isParamComponentPinned;
                        }

                        cursorPosition += new Vector2(ImGui.GetItemRectSize().X, 0);
                        ImGui.SetCursorPos(cursorPosition);

                        var isParamComponentHovered = ImGui.IsItemHovered();

                        drawList.ChannelsSetCurrent(0);

                        var shouldDrawCurve = _pinnedParameterComponents.Count == 0 || isParamComponentPinned;
                        if (shouldDrawCurve)
                        {
                            var color = DopeSheetArea.CurveColors[curveIndex % DopeSheetArea.CurveColors.Length];
                            DrawCurveLine(curve, TimeLineCanvas, color, isParamHovered || isParamComponentHovered);
                            drawList.ChannelsSetCurrent(1);
                            visibleCurveCount++;
                            foreach (var keyframe in curve.GetVDefinitions().ToList())
                            {
                                CurvePoint.Draw(keyframe, TimeLineCanvas, SelectedKeyframes.Contains(keyframe), this);
                                _visibleKeyframes.Add(keyframe);
                            }

                            HandleCreateNewKeyframes(curve);
                        }

                        curveIndex++;
                    }

                    ImGui.PopID();
                    lineStartPosition += new Vector2(0, 24);
                }

                drawList.ChannelsMerge();
                ImGui.PopFont();
                if (_addKeyframesCommands.Count > 0)
                {
                    var command = new MacroCommand("Insert keyframes", _addKeyframesCommands);
                    UndoRedoStack.AddAndExecute(command);
                    _addKeyframesCommands.Clear();
                }

                if (visibleCurveCount == 0 && _pinnedParameterComponents.Count > 0)
                {
                    _pinnedParameterComponents.Clear();
                }

                DrawContextMenu();
            }
            ImGui.EndGroup();

            RebuildCurveTables();
        }

        private void HandleCreateNewKeyframes(Curve curve)
        {
            var hoverNewKeyframe = !ImGui.IsAnyItemActive()
                                   && ImGui.IsWindowHovered()
                                   && ImGui.GetIO().KeyAlt
                                   && ImGui.IsWindowHovered();
            if (!hoverNewKeyframe)
                return;

            var hoverTime = TimeLineCanvas.InverseTransformX(ImGui.GetIO().MousePos.X);
            TimeLineCanvas.SnapHandlerForU.CheckForSnapping(ref hoverTime, TimeLineCanvas.Scale.X);

            if (ImGui.IsMouseReleased(0))
            {
                var dragDistance = ImGui.GetIO().MouseDragMaxDistanceAbs[0].Length();
                if (dragDistance < 2)
                {
                    _addKeyframesCommands.Add(InsertNewKeyframe(curve, hoverTime));
                }
            }
            else
            {
                var sampledValue = (float)curve.GetSampledValue(hoverTime);
                var posOnCanvas = new Vector2(hoverTime, sampledValue);
                var posOnScreen = TimeLineCanvas.TransformPosition(posOnCanvas)
                                  - new Vector2(KeyframeIconWidth / 2 + 1, KeyframeIconWidth / 2 + 1);
                Icons.Draw(Icon.CurveKeyframe, posOnScreen);
            }

            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        private readonly List<AddKeyframesCommand> _addKeyframesCommands = new();

        private static AddKeyframesCommand InsertNewKeyframe(Curve curve, float u)
        {
            var value = curve.GetSampledValue(u);
            var previousU = curve.GetPreviousU(u);

            var key = (previousU != null)
                          ? curve.GetV(previousU.Value).Clone()
                          : new VDefinition();

            key.Value = value;
            key.U = u;

            var command = new AddKeyframesCommand(curve, key);
            return command;
        }

        private const int KeyframeIconWidth = 16;

        private static bool DrawPinButton(bool isParamComponentPinned, string componentName)
        {
            var buttonColor = isParamComponentPinned ? UiColors.StatusAnimated : UiColors.Gray;
            ImGui.PushStyleColor(ImGuiCol.Text, buttonColor.Rgba);
            var result = ImGui.Button(componentName);
            ImGui.PopStyleColor();
            return result;
        }

        protected internal override void HandleCurvePointDragging(VDefinition vDef, bool isSelected)
        {
            if (vDef.U < Playback.Current.TimeInBars)
            {
                FrameStats.Current.HasKeyframesBeforeCurrentTime = true;
            }

            if (vDef.U > Playback.Current.TimeInBars)
            {
                FrameStats.Current.HasKeyframesAfterCurrentTime = true;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
            }

            if (ImGui.IsItemDeactivated())
            {
                if (_changeKeyframesCommand != null)
                    TimeLineCanvas.Current.CompleteDragCommand();
            }

            if (!ImGui.IsItemActive() || !ImGui.IsMouseDragging(0, 0f))
                return;

            if (ImGui.GetIO().KeyCtrl && _changeKeyframesCommand == null)
            {
                if (isSelected)
                    SelectedKeyframes.Remove(vDef);

                return;
            }

            if (!isSelected)
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    TimeLineCanvas.Current.ClearSelection();
                }

                SelectedKeyframes.Add(vDef);
            }

            if (_changeKeyframesCommand == null)
            {
                var mouseDragDelta = ImGui.GetMouseDragDelta();
                if (CurveInputEditing.MoveDirection == CurveInputEditing.MoveDirections.Undecided)
                {
                    if (Math.Abs(mouseDragDelta.X) > CurveInputEditing.MoveDirectionThreshold)
                    {
                        CurveInputEditing.MoveDirection = CurveInputEditing.MoveDirections.Horizontal;
                        TimeLineCanvas.Current.StartDragCommand();
                    }
                    else if (Math.Abs(mouseDragDelta.Y) > CurveInputEditing.MoveDirectionThreshold)
                    {
                        CurveInputEditing.MoveDirection = CurveInputEditing.MoveDirections.Vertical;
                        TimeLineCanvas.Current.StartDragCommand();
                    }
                }
            }

            var newDragPosition = TimeLineCanvas.Current.InverseTransformPositionFloat(ImGui.GetIO().MousePos);

            var allowHorizontal = CurveInputEditing.MoveDirection == CurveInputEditing.MoveDirections.Both
                                  || CurveInputEditing.MoveDirection == CurveInputEditing.MoveDirections.Horizontal
                                  || (ImGui.GetIO().KeyCtrl);

            var allowVertical = CurveInputEditing.MoveDirection == CurveInputEditing.MoveDirections.Both
                                || CurveInputEditing.MoveDirection == CurveInputEditing.MoveDirections.Vertical
                                || (ImGui.GetIO().KeyCtrl);

            var enableSnapping = ImGui.GetIO().KeyShift;
            double u = allowHorizontal ? newDragPosition.X : vDef.U;
            if (allowHorizontal)
            {
                if (enableSnapping)
                    _snapHandlerU.CheckForSnapping(ref u, TimeLineCanvas.Scale.X);
            }

            double v = allowVertical ? newDragPosition.Y : vDef.Value;
            if (allowVertical)
            {
                if (enableSnapping)
                    _snapHandlerV.CheckForSnapping(ref v, TimeLineCanvas.Scale.Y);
            }

            UpdateDragCommand(u - vDef.U, v - vDef.Value);
        }

        void ITimeObjectManipulation.DeleteSelectedElements()
        {
            AnimationOperations.DeleteSelectedKeyframesFromAnimationParameters(SelectedKeyframes, AnimationParameters, _compositionOp);
            RebuildCurveTables();
        }

        public void ClearSelection()
        {
            SelectedKeyframes.Clear();
        }

        public void UpdateSelectionForArea(ImRect screenArea, SelectionFence.SelectModes selectMode)
        {
            if (selectMode == SelectionFence.SelectModes.Replace)
                SelectedKeyframes.Clear();

            THelpers.DebugRect(screenArea.Min, screenArea.Max);
            var canvasArea = TimeLineCanvas.Current.InverseTransformRect(screenArea).MakePositive();
            var matchingItems = new List<VDefinition>();

            foreach (var keyframe in _visibleKeyframes)
            {
                if (canvasArea.Contains(new Vector2((float)keyframe.U, (float)keyframe.Value)))
                {
                    matchingItems.Add(keyframe);
                }
            }

            switch (selectMode)
            {
                case SelectionFence.SelectModes.Add:
                case SelectionFence.SelectModes.Replace:
                    SelectedKeyframes.UnionWith(matchingItems);
                    break;
                case SelectionFence.SelectModes.Remove:
                    SelectedKeyframes.ExceptWith(matchingItems);
                    break;
            }
        }

        public ICommand StartDragCommand()
        {
            _changeKeyframesCommand = new ChangeKeyframesCommand(_compositionOp.Symbol.Id, SelectedKeyframes, GetAllCurves());
            return _changeKeyframesCommand;
        }

        public void UpdateDragCommand(double dt, double dv)
        {
            foreach (var vDefinition in SelectedKeyframes)
            {
                vDefinition.U += dt;
                vDefinition.Value += dv;
            }

            RebuildCurveTables();
            foreach (var ap in AnimationParameters)
            {
                foreach (var c in ap.Curves)
                {
                    c.UpdateTangents();
                }
            }
        }

        public void CompleteDragCommand()
        {
            if (_changeKeyframesCommand == null)
                return;

            CurveInputEditing.MoveDirection = CurveInputEditing.MoveDirections.Undecided;
            
            // Update reference in macro command
            _changeKeyframesCommand.StoreCurrentValues();
            _changeKeyframesCommand = null;
        }

        public void UpdateDragAtStartPointCommand(double dt, double dv)
        {
        }

        public void UpdateDragAtEndPointCommand(double dt, double dv)
        {
        }

        #region implement snapping -------------------------
        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime, float canvasScale)
        {
            _snapThresholdOnCanvas = SnapDistance / canvasScale;
            var maxForce = 0.0;
            var bestSnapTime = Double.NaN;

            foreach (var vDefinition in GetAllKeyframes())
            {
                if (SelectedKeyframes.Contains(vDefinition))
                    continue;

                CheckForSnapping(targetTime, vDefinition.U, maxForce: ref maxForce, bestSnapTime: ref bestSnapTime);
            }

            return Double.IsNaN(bestSnapTime)
                       ? null
                       : new SnapResult(bestSnapTime, maxForce);
        }

        private void CheckForSnapping(double targetTime, double anchorTime, ref double maxForce, ref double bestSnapTime)
        {
            var distance = Math.Abs(anchorTime - targetTime);
            if (distance < 0.001)
                return;

            var force = Math.Max(0, _snapThresholdOnCanvas - distance);
            if (force <= maxForce)
                return;

            bestSnapTime = anchorTime;
            maxForce = force;
        }

        private double _snapThresholdOnCanvas;
        private const float SnapDistance = 4;
        #endregion

        public static void DrawCurveLine(Curve curve, ICanvas canvas, Color color, bool isParamHovered = false)
        {
            const float step = 3f;
            var width = ImGui.GetWindowWidth();

            double dU = canvas.InverseTransformDirection(new Vector2(step, 0)).X;
            double u = canvas.InverseTransformPositionFloat(canvas.WindowPos).X;
            var x = canvas.WindowPos.X;

            var steps = (int)(width / step);
            if (_curveLinePoints.Length != steps)
            {
                _curveLinePoints = new Vector2[steps];
            }

            for (var i = 0; i < steps; i++)
            {
                _curveLinePoints[i] = new Vector2(x, (int)canvas.TransformPosition(new Vector2(0, (float)curve.GetSampledValue(u))).Y + 0.5f);
                u += dU;
                x += step;
            }

            ImGui.GetWindowDrawList().AddPolyline(ref _curveLinePoints[0], steps, color, ImDrawFlags.None, isParamHovered ? 3 : 1);
        }

        private static ChangeKeyframesCommand _changeKeyframesCommand;
        private static Vector2[] _curveLinePoints = new Vector2[0];
        private Instance _compositionOp;
        private readonly ValueSnapHandler _snapHandlerU;
        private readonly ValueSnapHandler _snapHandlerV;
        private readonly Dictionary<int, int> _pinnedParameterComponents = new Dictionary<int, int>();
    }
}