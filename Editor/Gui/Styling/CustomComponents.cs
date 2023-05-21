using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.IO;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Styling
{
    internal static class CustomComponents
    {
        public static bool JogDial(string label, ref double delta, Vector2 size)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1, 0.5f));
            var isActive = ImGui.Button(label + "###dummy", size);
            ImGui.PopStyleVar();
            var io = ImGui.GetIO();
            if (ImGui.IsItemActive())
            {
                var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                ImGui.GetForegroundDrawList().AddCircle(center, 100, Color.Gray, 50);
                isActive = true;

                var pLast = io.MousePos - io.MouseDelta - center;
                var pNow = io.MousePos - center;
                var aLast = Math.Atan2(pLast.X, pLast.Y);
                var aNow = Math.Atan2(pNow.X, pNow.Y);
                delta = aLast - aNow;
                if (delta > 1.5)
                {
                    delta -= 2 * Math.PI;
                }
                else if (delta < -1.5)
                {
                    delta += 2 * Math.PI;
                }
            }

            return isActive;
        }

        /// <summary>Draw a splitter</summary>
        /// <remarks>
        /// Take from https://github.com/ocornut/imgui/issues/319#issuecomment-147364392
        /// </remarks>
        public static bool SplitFromBottom(ref float offsetFromBottom)
        {
            const float thickness = 3;
            var hasBeenDragged = false;

            var backupPos = ImGui.GetCursorPos();

            var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var contentMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();

            var pos = new Vector2(contentMin.X, contentMin.Y + size.Y - offsetFromBottom - thickness-1);
            ImGui.SetCursorScreenPos(pos);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.5f, 0.5f, 1));

            ImGui.Button("##Splitter", new Vector2(-1, thickness));

            ImGui.PopStyleColor(3);

            // Disabled for now, since Setting MouseCursor wasn't working reliably
            // if (ImGui.IsItemHovered() )
            // {
            //     //ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            // }

            if (ImGui.IsItemActive())
            {
                if (Math.Abs(ImGui.GetIO().MouseDelta.Y) > 0)
                {
                    hasBeenDragged = true;
                    offsetFromBottom =
                        (offsetFromBottom - ImGui.GetIO().MouseDelta.Y)
                       .Clamp(0, size.Y - thickness);
                }
            }

            ImGui.SetCursorPos(backupPos);
            return hasBeenDragged;
        }

        public static bool ToggleButton(string label, ref bool isSelected, Vector2 size, bool trigger = false)
        {
            var wasSelected = isSelected;
            var clicked = false;
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, T3Style.Colors.Text.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, T3Style.Colors.Text.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, T3Style.Colors.Text.Rgba);
            }

            if (ImGui.Button(label, size) || trigger)
            {
                isSelected = !isSelected;
                clicked = true;
            }

            if (wasSelected)
            {
                ImGui.PopStyleColor(3);
            }

            return clicked;
        }

        public static bool ToggleIconButton(Icon icon, string label, ref bool isSelected, Vector2 size, bool trigger = false)
        {
            var clicked = false;

            var stateColor = isSelected
                                 ? Color.White.Rgba
                                 : T3Style.Colors.DarkGray.Rgba;
            ImGui.PushStyleColor(ImGuiCol.Text, stateColor);

            var padding = string.IsNullOrEmpty(label) ? new Vector2(0.1f, 0.5f) : new Vector2(0.5f, 0.5f);
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, padding);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);

            ImGui.PushFont(Icons.IconFont);

            if (ImGui.Button($"{(char)icon}##label", size))
            {
                isSelected = !isSelected;
                clicked = true;
            }

            ImGui.PopFont();

            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(1);

            return clicked;
        }

        public enum ButtonStates
        {
            Normal,
            Dimmed,
            Disabled,
            Activated,
        }

        public static bool IconButton(Icon icon, Vector2 size, ButtonStates state = ButtonStates.Normal, bool triggered =false)
        {
            ImGui.PushFont(Icons.IconFont);
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);

            
            if (state != ButtonStates.Normal)
            {
                Color c;
                if (state == ButtonStates.Dimmed)
                    c = T3Style.Colors.TextMuted;
                else if (state == ButtonStates.Disabled)
                    c = T3Style.Colors.TextDisabled;
                else if (state == ButtonStates.Activated)
                    c = T3Style.Colors.Background;
                else
                    c = T3Style.Colors.Text;

                ImGui.PushStyleColor(ImGuiCol.Text, c.Rgba);
                if (state == ButtonStates.Activated)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, T3Style.Colors.TextMuted.Rgba);
                    //ImGui.PushStyleColor(ImGuiCol.ButtonActive, T3Style.Colors.Text.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.White.Rgba);
                }
            }

            var clicked = ImGui.Button("" + (char)icon, size) || triggered;

            if (state != ButtonStates.Normal)
                ImGui.PopStyleColor();
            
            if (state == ButtonStates.Activated)
                ImGui.PopStyleColor(2);

            ImGui.PopStyleVar(2);
            ImGui.PopFont();
            return clicked;
        }

        public static void ContextMenuForItem(Action drawMenuItems, string title = null, string id = "context_menu",
                                              ImGuiPopupFlags flags = ImGuiPopupFlags.MouseButtonRight)
        {
            var wasAlreadyOpen = ImGui.IsPopupOpen(id);
            
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));

            if (ImGui.BeginPopupContextItem(id, flags))
            {
                if(wasAlreadyOpen)
                    ImGui.Separator();
                
                FrameStats.Current.IsItemContextMenuOpen = true;
                if (title != null)
                {
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                    ImGui.TextUnformatted(title);
                    ImGui.PopStyleColor();
                    ImGui.PopFont();
                }

                drawMenuItems?.Invoke();
                ImGui.EndPopup();
            }

            ImGui.PopStyleVar(2);
        }

        public static void DrawContextMenuForScrollCanvas(Action drawMenuContent, ref bool contextMenuIsOpen)
        {
            if (!contextMenuIsOpen)
            {
                if (FrameStats.Current.IsItemContextMenuOpen)
                    return;
                
                var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
                if (wasDraggingRight)
                    return;

                if (!ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup))
                    return;
            }

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));


            if (ImGui.BeginPopupContextWindow("windows_context_menu"))
            {
                ImGui.GetMousePosOnOpeningCurrentPopup();
                contextMenuIsOpen = true;

                drawMenuContent.Invoke();
                ImGui.EndPopup();
            }
            else
            {
                contextMenuIsOpen = false;
            }

            ImGui.PopStyleVar(2);
        }

        public static bool DisablableButton(string label, bool isEnabled, bool enableTriggerWithReturn = false)
        {
            if (isEnabled)
            {
                ImGui.PushFont(Fonts.FontBold);
                if (ImGui.Button(label)
                    || (enableTriggerWithReturn && ImGui.IsKeyPressed((ImGuiKey)Key.Return)))
                {
                    ImGui.PopFont();
                    return true;
                }

                ImGui.PopFont();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.1f));
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 0.15f));
                ImGui.Button(label);
                ImGui.PopStyleColor(2);
            }

            return false;
        }

        public static void HelpText(string text)
        {
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
            ImGui.TextUnformatted(text);
            ImGui.PopStyleColor();
            ImGui.PopFont();
        }

        /// <summary>
        /// A small label that can be used to structure context menus
        /// </summary>
        public static void HintLabel(string label)
        {
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
            ImGui.TextUnformatted(label);
            ImGui.PopStyleColor();
            ImGui.PopFont();
        }

        public static void FillWithStripes(ImDrawListPtr drawList, ImRect areaOnScreen, float patternWidth = 16)
        {
            drawList.PushClipRect(areaOnScreen.Min, areaOnScreen.Max, true);
            var lineColor = new Color(0f, 0f, 0f, 0.2f);
            var stripeOffset = GraphCanvas.Current == null ? patternWidth : (patternWidth / 2 * GraphCanvas.Current.Scale.X);
            var lineWidth = stripeOffset / 2.7f;

            var h = areaOnScreen.GetHeight();
            var stripeCount = (int)((areaOnScreen.GetWidth() + h + 3 * lineWidth) / stripeOffset);
            var p = areaOnScreen.Min - new Vector2(h + lineWidth, +lineWidth);
            var offset = new Vector2(h + 2 * lineWidth,
                                     h + 2 * lineWidth);

            for (var i = 0; i < stripeCount; i++)
            {
                drawList.AddLine(p, p + offset, lineColor, lineWidth);
                p.X += stripeOffset;
            }

            drawList.PopClipRect();
        }

        public static bool EmptyWindowMessage(string message, string buttonLabel = null)
        {
            var center = (ImGui.GetWindowContentRegionMax() + ImGui.GetWindowContentRegionMin()) / 2 + ImGui.GetWindowPos();
            var lines = message.Split('\n').ToArray();

            var lineCount = lines.Length;
            if (!string.IsNullOrEmpty(buttonLabel))
                lineCount++;

            var textLineHeight = ImGui.GetTextLineHeight();
            var y = center.Y - lineCount * textLineHeight / 2;
            var drawList = ImGui.GetWindowDrawList();

            var emptyMessageColor = new Color(0.3f);

            foreach (var line in lines)
            {
                var textSize = ImGui.CalcTextSize(line);
                var position = new Vector2(center.X - textSize.X / 2, y);
                drawList.AddText(position, emptyMessageColor, line);
                y += textLineHeight;
            }

            if (!string.IsNullOrEmpty(buttonLabel))
            {
                y += 10;
                var style = ImGui.GetStyle();
                var textSize = ImGui.CalcTextSize(buttonLabel) + style.FramePadding;
                var position = new Vector2(center.X - textSize.X / 2, y);
                ImGui.SetCursorScreenPos(position);
                return ImGui.Button(buttonLabel);
            }

            return false;
        }

        public static void TooltipForLastItem(string message, string additionalNotes = null, bool useHoverDelay = true)
        {
            if (!ImGui.IsAnyItemHovered())
            {
                _hoverStartTime = -1;
                return;
            }

            if (!ImGui.IsItemHovered())
                return;

            if (_hoverStartTime <= 0)
                _hoverStartTime = ImGui.GetTime();

            var hoverDuration = ImGui.GetTime() - _hoverStartTime;
            if (useHoverDelay && !(hoverDuration > UserSettings.Config.TooltipDelay))
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(300);
            ImGui.TextUnformatted(message);
            if (!string.IsNullOrEmpty(additionalNotes))
            {
                ImGui.TextColored(Color.Gray, additionalNotes);
            }

            ImGui.PopTextWrapPos();

            ImGui.EndTooltip();
            ImGui.PopStyleVar();
        }

        private static double _hoverStartTime;

        public static bool DrawSegmentedToggle(ref int currentIndex, List<string> options)
        {
            var changed = false;
            for (var index = 0; index < options.Count; index++)
            {
                var isActive = currentIndex == index;
                var option = options[index];

                ImGui.SameLine(0);
                ImGui.PushFont(isActive ? Fonts.FontBold : Fonts.FontNormal);
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.White.Fade(0.1f).Rgba);
                ImGui.PushStyleColor(ImGuiCol.Text, isActive ? Color.White : Color.White.Fade(0.5f).Rgba);

                if (ImGui.Button(option))
                {
                    if (!isActive)
                    {
                        currentIndex = index;
                        changed = true;
                    }
                }

                ImGui.PopFont();
                ImGui.PopStyleColor(3);
            }

            return changed;
        }

        public static bool DrawSearchField(string placeHolderLabel, ref string value, float width = 0)
        {
            var wasNull = value == null;
            if (wasNull)
                value = string.Empty;

            ImGui.SetNextItemWidth(width - FormInputs.ParameterSpacing);
            var modified = ImGui.InputText("##" + placeHolderLabel, ref value, 1000);
            if (!modified && wasNull)
                value = null;

            if (!string.IsNullOrEmpty(value))
            {
                ImGui.SameLine();
                if (ImGui.Button("×"))
                {
                    value = null;
                    modified = true;
                }
            }
            else
            {
                var drawList = ImGui.GetWindowDrawList();
                var minPos = ImGui.GetItemRectMin();
                var maxPos = ImGui.GetItemRectMax();
                drawList.PushClipRect(minPos, maxPos);
                drawList.AddText(minPos + new Vector2(8, 5), Color.White.Fade(0.25f), placeHolderLabel);
                drawList.PopClipRect();
            }

            return modified;
        }

        /// <summary>
        /// Draws a frame that indicates if the current window is focused.
        /// This is useful for windows that have window specific keyboard short cuts.
        /// </summary>
        public static void DrawWindowFocusFrame()
        {
            if (!ImGui.IsWindowFocused())
                return;
            
            var min = ImGui.GetWindowPos() + new Vector2(1,0);
            ImGui.GetWindowDrawList().AddRect(min, min+ImGui.GetWindowSize() + new Vector2(-2,-1) , Color.White.Fade(0.1f));
        }
    }
}