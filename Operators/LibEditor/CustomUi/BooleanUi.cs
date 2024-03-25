using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using Boolean = lib.math.@bool.Boolean;

namespace libEditor.CustomUi
{
    public static class BooleanUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect, Vector2 canvasScale)
        {
            if (instance is not Boolean boolean)
                return SymbolChildUi.CustomUiResult.None;

            if (!ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
                return SymbolChildUi.CustomUiResult.None;

            var dragWidth = WidgetElements.DrawDragIndicator(screenRect, drawList, canvasScale);
            var colorAsVec4 = boolean.ColorInGraph.TypedInputValue.Value;
            var color = new Color(colorAsVec4);

            var activeRect = screenRect;
            activeRect.Min.X += dragWidth;

            ImGui.PushID(instance.SymbolChildId.GetHashCode());
            screenRect.Expand(-4);
            ImGui.SetCursorScreenPos(screenRect.Min);
            var symbolChild = instance.SymbolChild;
            ImGui.PushClipRect(screenRect.Min, screenRect.Max, true);

            var refValue = boolean.BoolValue.Value;
            var label = string.IsNullOrEmpty(symbolChild.Name)
                            ? (refValue ? "True" : "False")
                            : symbolChild.ReadableName;

            drawList.AddRectFilled(activeRect.Min, activeRect.Max, color.Fade(refValue ? 0.5f : 0.1f));

            var canvasScaleY = canvasScale.Y;
            var font = WidgetElements.GetPrimaryLabelFont(canvasScaleY);
            var labelColor = WidgetElements.GetPrimaryLabelColor(canvasScaleY);

            ImGui.PushFont(font);
            var labelSize = ImGui.CalcTextSize(label);

            var labelPos = new Vector2(activeRect.Min.X + 18 * canvasScaleY,
                                       (activeRect.Min.Y + activeRect.Max.Y) / 2 - labelSize.Y / 2);
            drawList.AddText(font, font.FontSize, labelPos, labelColor, label);
            ImGui.PopFont();

            var checkCenter = new Vector2(labelPos.X - 10f * canvasScaleY,
                                          (activeRect.Min.Y + activeRect.Max.Y) / 2 + 1.5f * canvasScaleY
                                         );
            var checkSize = MathF.Min(100, 2.5f * canvasScaleY);
            var points = new[]
                             {
                                 checkCenter + new Vector2(-2, -1) * checkSize,
                                 checkCenter + new Vector2(0, 1) * checkSize,
                                 checkCenter + new Vector2(3, -2) * checkSize,
                             };
            drawList.AddPolyline(ref points[0], 3,
                                 refValue ? UiColors.WidgetTitle : UiColors.BackgroundFull.Fade(0.2f),
                                 ImDrawFlags.None,
                                 MathF.Max(1.4f, 0.5f * canvasScaleY));

            if (!boolean.BoolValue.IsConnected)
            {
                var isHoveredOrActive = boolean.SymbolChildId == activeInputId ||
                                        ImGui.IsWindowHovered() && activeRect.Contains(ImGui.GetMousePos());
                if (isHoveredOrActive)
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        activeInputId = boolean.SymbolChildId;
                    }
                    else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && ImGui.GetMouseDragDelta().LengthSquared() < UserSettings.Config.ClickThreshold)
                    {
                        activeInputId = Guid.Empty;
                        var newValue = !boolean.BoolValue.TypedInputValue.Value;
                        boolean.BoolValue.SetTypedInputValue(newValue);
                    }
                }
            }

            ImGui.PopClipRect();
            ImGui.PopID();
            return SymbolChildUi.CustomUiResult.Rendered
                   | SymbolChildUi.CustomUiResult.PreventOpenSubGraph
                   | SymbolChildUi.CustomUiResult.PreventTooltip
                   | SymbolChildUi.CustomUiResult.PreventOpenParameterPopUp
                   | SymbolChildUi.CustomUiResult.PreventInputLabels;
        }

        /// <summary>
        /// toggle button for boolean math op 
        /// </summary>
        private static bool ToggleButtonB(string label, ref bool isSelected, Vector2 size, Vector4 color, Vector2 canvasScale, bool trigger = false)
        {
            var clicked = false;
            var colorInactive = color - new Vector4(.0f, .0f, .0f, .3f);

            var xScale = canvasScale.X;
            ImGui.PushFont(xScale < 2
                               ? Fonts.FontSmall
                               : xScale < 4
                                   ? Fonts.FontNormal
                                   : Fonts.FontLarge);

            ImGui.PushStyleColor(ImGuiCol.Button, isSelected ? color : colorInactive);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color); // Adjust this as needed 
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, colorInactive); // Adjust this as needed 
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Selection.Rgba);

            if (ImGui.Button(label, size) || trigger)
            {
                isSelected = !isSelected;
                clicked = true;
            }

            ImGui.PopStyleColor(4);
            ImGui.PopFont();

            return clicked;
        }

        private static Guid activeInputId;
    }
}