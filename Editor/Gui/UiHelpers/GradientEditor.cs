﻿using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Styling;

// ReSharper disable RedundantArgumentDefaultValue

namespace T3.Editor.Gui.UiHelpers
{
    public static class GradientEditor
    {
        /// <summary>
        /// Draw a gradient control that returns true, if gradient has been modified
        /// </summary>
        public static InputEditStateFlags Draw(ref Gradient gradientRef, ImDrawListPtr drawList, ImRect areaOnScreen, bool cloneIfModified = false)
        {
            var gradientForEditing = gradientRef;

            if (cloneIfModified)
            {
                // gradientForEditing = gradientRef.TypedClone();
                gradientForEditing = gradientRef != _hoveredGradientRef
                                         ? gradientRef.TypedClone()
                                         : _hoveredGradientForEditing;
            }

            gradientForEditing.SortHandles();

            DrawGradient(gradientForEditing, drawList, areaOnScreen);

            if (!(areaOnScreen.GetHeight() >= RequiredHeightForHandles))
                return InputEditStateFlags.Nothing;

            Gradient.Step hoveredStep = null;

            // Draw handles
            var modified = InputEditStateFlags.Nothing;

            Gradient.Step removedStep = null;
            foreach (var step in gradientForEditing.Steps)
            {
                var result = DrawHandle(step);
                if(result != InputEditStateFlags.Nothing)
                {
                    modified = result;
                }
            }

            if (removedStep != null)
            {
                gradientForEditing.Steps.Remove(removedStep);
                modified = InputEditStateFlags.ModifiedAndFinished;
            }
            
            if (cloneIfModified && hoveredStep != null)
            {
                _hoveredGradientForEditing = gradientForEditing;
                _hoveredGradientRef = gradientRef;
            }

            // Insert step area...
            var insertRangeMin = new Vector2(areaOnScreen.Min.X, areaOnScreen.Max.Y - StepHandleSize.Y);
            ImGui.SetCursorScreenPos(insertRangeMin);

            var canInsertNewStep = areaOnScreen.GetHeight() > MinInsertHeight && hoveredStep == null;

            var normalizedPosition = (ImGui.GetMousePos().X - insertRangeMin.X) / areaOnScreen.GetWidth();

            if (ImGui.InvisibleButton("insertRange", areaOnScreen.Max - insertRangeMin) && canInsertNewStep)
            {
                gradientForEditing.Steps.Add(new Gradient.Step()
                                                 {
                                                     NormalizedPosition = normalizedPosition,
                                                     Id = Guid.NewGuid(),
                                                     Color = gradientForEditing.Sample(normalizedPosition)
                                                 });
                modified = InputEditStateFlags.ModifiedAndFinished;
            }

            if (canInsertNewStep && ImGui.IsItemHovered() && !ImGui.IsItemActive())
            {
                var handleArea = GetHandleAreaForPosition(normalizedPosition);
                drawList.AddRect(handleArea.Min + Vector2.One, handleArea.Max - Vector2.One, new Color(1f, 1f, 1f, 0.4f));
            }

            CustomComponents.ContextMenuForItem(() =>
                                                {
                                                    if (ImGui.MenuItem("Reverse"))
                                                    {
                                                        foreach (var s in gradientForEditing.Steps)
                                                        {
                                                            s.NormalizedPosition = 1f - s.NormalizedPosition;
                                                        }

                                                        gradientForEditing.SortHandles();
                                                        modified = InputEditStateFlags.ModifiedAndFinished;
                                                    }

                                                    if (ImGui.MenuItem("Distribute evenly", gradientForEditing.Steps.Count > 2))
                                                    {
                                                        var stepsCount = (gradientForEditing.Steps.Count - 1);
                                                        if (gradientForEditing.Interpolation == Gradient.Interpolations.Hold)
                                                            stepsCount++;
                                                        
                                                        for (var index = 0; index < gradientForEditing.Steps.Count; index++)
                                                        {
                                                            gradientForEditing.Steps[index].NormalizedPosition =
                                                                (float)index / stepsCount;
                                                        }

                                                        gradientForEditing.SortHandles();
                                                        modified = InputEditStateFlags.Finished;
                                                    }

                                                    if (ImGui.BeginMenu("Gradient presets..."))
                                                    {
                                                        var foregroundDrawList = ImGui.GetForegroundDrawList();

                                                        for (var index = 0; index < GradientPresets.Presets.Count; index++)
                                                        {
                                                            ImGui.PushID(index);
                                                            var preset = GradientPresets.Presets[index];

                                                            if (ImGui.InvisibleButton("" + index, new Vector2(100, ImGui.GetFrameHeight())))
                                                            {
                                                                var clone = preset.TypedClone();
                                                                gradientForEditing.Steps = clone.Steps;
                                                                gradientForEditing.Interpolation = clone.Interpolation;
                                                                modified = InputEditStateFlags.ModifiedAndFinished;
                                                            }

                                                            var rect = new ImRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
                                                            DrawGradient(preset, foregroundDrawList, rect);

                                                            ImGui.SameLine();
                                                            if (CustomComponents.IconButton(Icon.Trash,
                                                                                            Vector2.One * ImGui.GetFrameHeight()))
                                                            {
                                                                GradientPresets.Presets.Remove(preset);
                                                                GradientPresets.Save();
                                                                ImGui.PopID();
                                                                break;
                                                            }

                                                            ImGui.PopID();
                                                        }

                                                        if (ImGui.MenuItem("Save"))
                                                        {
                                                            GradientPresets.Presets.Add(gradientForEditing.TypedClone());
                                                            GradientPresets.Save();
                                                        }

                                                        ImGui.EndMenu();
                                                    }

                                                    if (ImGui.BeginMenu("Interpolation..."))
                                                    {
                                                        foreach (Gradient.Interpolations value in Enum.GetValues(typeof(Gradient.Interpolations)))
                                                        {
                                                            var isSelected = gradientForEditing.Interpolation == value;
                                                            if (ImGui.MenuItem(value.ToString(), "", isSelected))
                                                            {
                                                                gradientForEditing.Interpolation = value;
                                                                modified = InputEditStateFlags.ModifiedAndFinished;
                                                            }
                                                        }

                                                        ImGui.EndMenu();
                                                    }
                                                }, "Gradient");

            if (modified != InputEditStateFlags.Nothing && cloneIfModified)
            {
                gradientRef = gradientForEditing;
                _hoveredGradientRef = null;
                _hoveredGradientForEditing = null;
            }

            return modified;

            InputEditStateFlags DrawHandle(Gradient.Step step)
            {
                var stepModified = InputEditStateFlags.Nothing;
                ImGui.PushID(step.Id.GetHashCode());
                var handleArea = GetHandleAreaForPosition(step.NormalizedPosition);

                // Interaction
                ImGui.SetCursorScreenPos(handleArea.Min);
                ImGui.InvisibleButton("gradientStep", new Vector2(StepHandleSize.X, areaOnScreen.GetHeight()));

                // Stub for ColorEditButton that allows quick sliders. Sadly this doesn't work with right mouse button drag.
                //stepModified |= ColorEditButton.Draw(ref step.Color, new Vector2(StepHandleSize.X, areaOnScreen.GetHeight()));

                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup))
                {
                    hoveredStep = step;
                }

                var isDraggedOutside = false;
                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    if (ImGui.GetIO().KeyCtrl)
                    {
                        var previousColor = step.Color;
                        ColorEditButton.VerticalColorSlider(step.Color, handleArea.GetCenter(), step.Color.W);
                        var mouseDragDelta = ImGui.GetMouseDragDelta().Y / 100;
                        ImGui.ResetMouseDragDelta();
                        step.Color.W = (previousColor.W - mouseDragDelta).Clamp(0, 1);
                    }
                    else
                    {
                        step.NormalizedPosition = ((ImGui.GetMousePos().X - areaOnScreen.Min.X) / areaOnScreen.GetWidth()).Clamp(0, 1);
                    }

                    isDraggedOutside = ImGui.GetMousePos().Y > areaOnScreen.Max.Y + RemoveThreshold;
                    
                    // Draw Remove indicator...
                    var centerX = (int)areaOnScreen.GetCenter().X;
                    var y = areaOnScreen.Max.Y + RemoveThreshold;
                    
                    drawList.AddRectFilled(new Vector2(areaOnScreen.Min.X, y), new Vector2(areaOnScreen.Max.X, y-1), UiColors.ForegroundFull.Fade(0.1f));
                    Icons.DrawIconAtScreenPosition(Icon.Trash, new Vector2(centerX, y+10), drawList, isDraggedOutside ? UiColors.StatusAttention: UiColors.ForegroundFull.Fade(0.2f));
                    stepModified = InputEditStateFlags.Modified;
                }

                // Draw handle
                if (isDraggedOutside)
                {
                    handleArea.Min.Y += 25;
                    handleArea.Max.Y += 25;
                }

                if (ImGui.IsItemDeactivated())
                {
                    var mouseOutsideThresholdAfterDrag = ImGui.GetMousePos().Y > areaOnScreen.Max.Y + RemoveThreshold;
                    if (mouseOutsideThresholdAfterDrag && gradientForEditing.Steps.Count > 1)
                        removedStep = step;
                    
                    stepModified = InputEditStateFlags.ModifiedAndFinished;
                }

                var points = new[]
                                 {
                                     new Vector2(handleArea.Min.X, handleArea.Max.Y),
                                     handleArea.Max,
                                     new Vector2(handleArea.Max.X, handleArea.Min.Y),
                                 };
                drawList.AddConvexPolyFilled(ref points[0], 3, new Color(0.15f, 0.15f, 0.15f, 1));
                drawList.AddRectFilled(handleArea.Min, handleArea.Max, ImGui.ColorConvertFloat4ToU32(step.Color));
                drawList.AddRect(handleArea.Min, handleArea.Max, UiColors.BackgroundFull);
                drawList.AddRect(handleArea.Min + Vector2.One, handleArea.Max - Vector2.One, UiColors.ForegroundFull);

                if (ImGui.IsItemHovered()
                    && ImGui.IsMouseReleased(0)
                    && ImGui.GetIO().MouseDragMaxDistanceAbs[0].Length() < UserSettings.Config.ClickThreshold
                    && !ImGui.IsPopupOpen("##colorEdit"))
                {
                    FrameStats.Current.OpenedPopUpName = "##colorEdit";
                    ImGui.OpenPopup("##colorEdit");
                    ImGui.SetNextWindowPos(new Vector2(handleArea.Min.X, handleArea.Max.Y));
                }

                var popUpResult = ColorEditPopup.DrawPopup(ref step.Color, step.Color);
                if (popUpResult != InputEditStateFlags.Nothing)
                {
                    stepModified = popUpResult;
                }
                ImGui.PopID();
                return stepModified;
            }

            ImRect GetHandleAreaForPosition(float normalizedStepPosition)
            {
                var x = areaOnScreen.Min.X - StepHandleSize.X / 2f + areaOnScreen.GetWidth() * normalizedStepPosition;
                return new ImRect(new Vector2(x, areaOnScreen.Max.Y - StepHandleSize.Y), new Vector2(x + StepHandleSize.X, areaOnScreen.Max.Y + 2));
            }
        }

        /// <summary>
        /// Dealing with default reference values is complex:
        /// 1. We want to render the default gradient.
        /// 2. We directly want to start manipulating from the default without modifying the default.
        /// 3. We NEVER ever want to modify the references default gradient.
        /// 4. To manipulate a gradient step we have to have a stable step GUID
        ///
        /// To do this, we...
        /// - clone gradients before rendering (which results in new random ids for steps)
        /// - reused a previously cloned gradient one of it's steps is being hovered.
        /// </summary>
        private static Gradient _hoveredGradientRef;
        private static Gradient _hoveredGradientForEditing;

        private static void DrawGradient(Gradient gradient, ImDrawListPtr drawList, ImRect areaOnScreen)
        {
            drawList.AddRect(areaOnScreen.Min, areaOnScreen.Max, Color.Black);
            drawList.AddRectFilled(areaOnScreen.Min, areaOnScreen.Max, new Color(0.15f, 0.15f, 0.15f, 1));

            if (gradient.Steps == null || gradient.Steps.Count == 0)
            {
                Log.Warning("Can't draw invalid gradient");
                return;
            }

            // Draw Gradient background
            {
                CustomComponents.FillWithStripes(drawList, areaOnScreen);
            }

            // Draw Gradient
            var minPos = areaOnScreen.Min;
            var maxPos = areaOnScreen.Max;

            uint leftColor = ImGui.ColorConvertFloat4ToU32(gradient.Steps[0].Color);

            // Draw complex gradient
            if (gradient.Interpolation == Gradient.Interpolations.Smooth || gradient.Interpolation == Gradient.Interpolations.OkLab)
            {
                var f = 0f;

                for (var stepIndex = 0; stepIndex < gradient.Steps.Count; stepIndex++)
                {
                    var step = gradient.Steps[stepIndex];

                    var steps = 5;

                    var rightF = step.NormalizedPosition;
                    var rangeF = (rightF - f);
                    var stepSizeF = rangeF / steps;

                    var pixelStepSize = (areaOnScreen.GetWidth() * rangeF) / steps;
                    maxPos.X = minPos.X + pixelStepSize;

                    for (int i = 0; i < steps; i++)
                    {
                        var nextF = f + stepSizeF;
                        var nextColor = ImGui.ColorConvertFloat4ToU32(gradient.Sample(nextF));

                        drawList.AddRectFilledMultiColor(minPos,
                                                         maxPos,
                                                         leftColor,
                                                         nextColor,
                                                         nextColor,
                                                         leftColor);
                        maxPos.X += pixelStepSize;
                        minPos.X += pixelStepSize;

                        f = nextF;
                        leftColor = nextColor;
                    }
                }
            }
            // Linear gradient
            else
            {
                foreach (var step in gradient.Steps)
                {
                    uint rightColor = ImGui.ColorConvertFloat4ToU32(step.Color);
                    maxPos.X = areaOnScreen.Min.X + areaOnScreen.GetWidth() * step.NormalizedPosition;
                    if (gradient.Interpolation == Gradient.Interpolations.Hold)
                    {
                        drawList.AddRectFilledMultiColor(minPos,
                                                         maxPos,
                                                         leftColor,
                                                         leftColor,
                                                         leftColor,
                                                         leftColor);
                    }
                    else
                    {
                        drawList.AddRectFilledMultiColor(minPos,
                                                         maxPos,
                                                         leftColor,
                                                         rightColor,
                                                         rightColor,
                                                         leftColor);
                    }

                    minPos.X = maxPos.X;
                    leftColor = rightColor;
                }
            }

            if (minPos.X < areaOnScreen.Max.X)
            {
                drawList.AddRectFilled(minPos, areaOnScreen.Max, leftColor);
            }
        }

        private const float RemoveThreshold = 35;
        private const float RequiredHeightForHandles = 20;
        private const int MinInsertHeight = 20;
        public static readonly Vector2 StepHandleSize = new(14, 24);
    }
}