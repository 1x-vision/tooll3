﻿using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.Utils;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Interaction
{
    public static class ColorEditButton
    {
        public static InputEditStateFlags Draw(ref Vector4 color, Vector2 size)
        {
            var edited = InputEditStateFlags.Nothing;
            
            var buttonPosition = ImGui.GetCursorScreenPos();
            ImGui.ColorButton("##thumbnail", color, ImGuiColorEditFlags.AlphaPreviewHalf, size);
            
            // Don't you ImGui.IsItemActivated() to allow quick switching between color thumbnails
            if (ImGui.IsItemHovered( ImGuiHoveredFlags.AllowWhenBlockedByPopup)
                && ImGui.IsMouseReleased(0)
                && ImGui.GetIO().MouseDragMaxDistanceAbs[0].Length() < UserSettings.Config.ClickThreshold
                && !ImGui.IsPopupOpen("##colorEdit")
                )
            {
                _modifiedSlider = false;
                ImGui.OpenPopup("##colorEdit");
            }
            
            edited |= HandleQuickSliders(ref color, buttonPosition);
            edited |= ColorEditPopup.DrawPopup(ref color, _previousColor);
            if (edited == InputEditStateFlags.ModifiedAndFinished)
                _previousColor = color;
            return edited;
        }

        private static InputEditStateFlags HandleQuickSliders(ref Vector4 color, Vector2 buttonPosition)
        {
            var edited = InputEditStateFlags.Nothing;
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                edited |= InputEditStateFlags.Started;
                _rightClickedItemId = ImGui.GetID(string.Empty);
                _previousColor = color;
            }
            
            else if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                edited |= InputEditStateFlags.Started;
                _previousColor = color;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                _rightClickedItemId = 0;
                
                if(_modifiedSlider)
                    edited |= InputEditStateFlags.Finished;
                
                _modifiedSlider = false;
            }
            
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                if(_modifiedSlider)
                    edited |= InputEditStateFlags.Finished;
                
                _modifiedSlider = false;
            }

            var pCenter = buttonPosition + Vector2.One * ImGui.GetFrameHeight() / 2;

            var showAlphaSlider = ImGui.IsMouseDragging(ImGuiMouseButton.Left) && ImGui.IsItemActive();
            if (showAlphaSlider)
            {
                var valuePos = color.W;
                VerticalColorSlider(color, pCenter, valuePos);
                if (MathF.Abs(ImGui.GetMouseDragDelta().Y) > UserSettings.Config.ClickThreshold / 2f)
                    _modifiedSlider = true;

                color.W = (_previousColor.W - ImGui.GetMouseDragDelta().Y / 100).Clamp(0, 1);
                
                if(_modifiedSlider)
                    edited |= InputEditStateFlags.Modified;
            }

            var showBrightnessSlider = ImGui.IsMouseDragging(ImGuiMouseButton.Right) && ImGui.GetID(string.Empty) == _rightClickedItemId;
            if (showBrightnessSlider)
            {
                FrameStats.Current.OpenedPopUpName = "ColorBrightnessSlider";
                var hsb = new Color(color).AsHsl;
                var previousHsb = new Color(_previousColor).AsHsl;

                var valuePos = hsb.Z;
                VerticalColorSlider(color, pCenter, valuePos);

                if (MathF.Abs(ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Y) > UserSettings.Config.ClickThreshold /2f)
                {
                    _modifiedSlider = true;
                }

                var newBrightness = (previousHsb.Z - ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Y / 100).Clamp(0, 1);
                color = Color.ColorFromHsl(previousHsb.X, previousHsb.Y, newBrightness, _previousColor.W);
                if(_modifiedSlider)
                    edited |= InputEditStateFlags.Modified;
            }

            return edited;
        }

        private static bool _modifiedSlider = false;

        public static void VerticalColorSlider(Vector4 color, Vector2 pCenter, float valuePos)
        {
            const int barHeight = 100;
            const int barWidth = 10;
            var drawList = ImGui.GetForegroundDrawList();
            var pMin = pCenter + new Vector2(15, -barHeight * valuePos);
            var pMax = pMin + new Vector2(barWidth, barHeight);
            var area = new ImRect(pMin, pMax);
            drawList.AddRectFilled(pMin - Vector2.One, pMax + Vector2.One, new Color(0.1f, 0.1f, 0.1f));
            CustomComponents.FillWithStripes(drawList, area);

            // Draw Slider
            var opaqueColor = color;
            opaqueColor.W = 1;
            var transparentColor = color;
            transparentColor.W = 0;
            drawList.AddRectFilledMultiColor(pMin, pMax,
                                             ImGui.ColorConvertFloat4ToU32(transparentColor),
                                             ImGui.ColorConvertFloat4ToU32(transparentColor),
                                             ImGui.ColorConvertFloat4ToU32(opaqueColor),
                                             ImGui.ColorConvertFloat4ToU32(opaqueColor));

            drawList.AddRectFilled(pCenter, pCenter + new Vector2(barWidth + 15, 1), Color.Black);
        }
        

        private static uint _rightClickedItemId;
        private static Vector4 _previousColor;
    }
}