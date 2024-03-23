﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using T3.Editor.App;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using Color = T3.Core.DataTypes.Vector.Color;
using Point = System.Drawing.Point;

namespace T3.Editor.Gui.Interaction
{
    public static class ColorEditPopup
    {
        public static InputEditStateFlags DrawPopup(ref Vector4 color, Vector4 previousColor)
        {
            var edited = InputEditStateFlags.Nothing;
            var cColor = new Color(color);
            ImGui.SetNextWindowSize(new Vector2(257, 360));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            var dontCloseIfColorPicking = ImGui.GetIO().KeyAlt ? ImGuiWindowFlags.Modal : ImGuiWindowFlags.None;

            var id = ImGui.GetID("colorPicker");
            if (ImGui.BeginPopup(PopupId, dontCloseIfColorPicking))
            {
                if (_openedId != id)
                {
                    _openedId = id;
                    ColorUsage.CollectUsedColors();
                }

                var drawList = ImGui.GetForegroundDrawList();
                ImGui.ColorConvertRGBtoHSV(color.X, color.Y, color.Z, out var hNormalized, out var linearSaturation, out var v);

                var compareColor = _isHoveringColor ? _hoveredColor : (Color)previousColor;
                _dampedCompareColor = Vector4.Max(Vector4.Lerp(_dampedCompareColor, compareColor, 0.2f), Vector4.Zero);
                edited = DrawCircleAndSliders(ref cColor, _dampedCompareColor, ref hNormalized, ref linearSaturation, v, drawList);
                edited |= PickColor(ref cColor, previousColor);
                edited |= DrawColorInputs(ref cColor, hNormalized, linearSaturation, v);
                edited |= DrawUsedColors(ref cColor);

                if (edited == InputEditStateFlags.ModifiedAndFinished)
                    ColorUsage.CollectUsedColors();

                ImGui.EndPopup();
            }
            else
            {
                if (_openedId == id)
                {
                    _openedId = 0;
                }
            }

            color = cColor.Rgba;
                ImGui.PopStyleVar();
            return edited;
        }

        private static InputEditStateFlags PickColor(ref Color cColor, Vector4 previousColor)
        {
            InputEditStateFlags edited = InputEditStateFlags.Nothing;

            Color pickedColor;
            // Pick colors
            var altKeyPressed = ImGui.GetIO().KeyAlt;
            if (!altKeyPressed)
                return edited;
            
            pickedColor = GetColorAtMousePosition();
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                edited |= InputEditStateFlags.ModifiedAndFinished;
                cColor = pickedColor;
            }

            var dl = ImGui.GetForegroundDrawList();
            var pos = ImGui.GetMousePos();

            pos += Vector2.One * 25;
            var padding = new Vector2(5, 5);
            dl.AddRectFilled(pos, pos + new Vector2(80, 38), UiColors.BackgroundFull);
            ImGui.PushFont(Fonts.FontSmall);
            dl.AddText(pos + new Vector2(15, 2 + 0), UiColors.ForegroundFull, $"{pickedColor.R:0.000}");
            dl.AddText(pos + new Vector2(15, 2 + 10), UiColors.ForegroundFull, $"{pickedColor.G:0.000}");
            dl.AddText(pos + new Vector2(15, 2 + 20), UiColors.ForegroundFull, $"{pickedColor.B:0.000}");
            
            dl.AddText(pos + new Vector2(55, 2 + 0), UiColors.ForegroundFull, $"{(pickedColor.R * 255):0}");
            dl.AddText(pos + new Vector2(55, 2 + 10), UiColors.ForegroundFull, $"{(pickedColor.G * 255):0}");
            dl.AddText(pos + new Vector2(55, 2 + 20), UiColors.ForegroundFull, $"{(pickedColor.B * 255):0}");
            
            var swatchSize = new Vector2(3, 32);
            var swatchPos = pos + new Vector2(3, 3);
            dl.AddRectFilled(swatchPos, swatchPos+ swatchSize, pickedColor );
            ImGui.PopFont();

            return edited;
        }

        private static Vector4 _dampedCompareColor;

        private static InputEditStateFlags DrawCircleAndSliders(ref Color cColor, Vector4 compareColor, ref float hNormalized, ref float linearSaturation,
                                                                float v,
                                                                ImDrawListPtr drawList)
        {
            const float saturationWarp = 1.5f;

            ImGui.ColorConvertRGBtoHSV(compareColor.X, compareColor.Y, compareColor.Z, out var compareHueNormalized, out var compareSaturation,
                                       out var compareValue);

            var edited = InputEditStateFlags.Nothing;
            ImGui.Dummy(new Vector2(10, 10));

            var wheelRadius = 209f;
            var windowPos = ImGui.GetCursorScreenPos() + new Vector2(10, 10);
            var size = new Vector2(wheelRadius, wheelRadius);
            var clampedV = v.Clamp(0, 1);

            const float colorEdgeWidth = 3;
            drawList.AddImage((IntPtr)SharedResources.ColorPickerImageSrv, windowPos - Vector2.One * colorEdgeWidth,
                              windowPos + size + Vector2.One * colorEdgeWidth);
            drawList.AddImage((IntPtr)SharedResources.ColorPickerImageSrv, windowPos, windowPos + size, Vector2.Zero, Vector2.One,
                              new Color(clampedV, clampedV, clampedV));

            var hueAngle = (hNormalized + 0.25f) * 2 * MathF.PI;
            var warpedSaturation = MathF.Pow(linearSaturation, 1 / saturationWarp);
            var pickedColorPos = new Vector2(MathF.Sin(hueAngle), MathF.Cos(hueAngle)) * size / 2 * warpedSaturation + size / 2;

            // Show compare color
            {
                var pHueAngle = (compareHueNormalized + 0.25f) * 2 * MathF.PI;
                var pWarpedSaturation = MathF.Pow(compareSaturation, 1 / saturationWarp);
                var pPickedColorPos = new Vector2(MathF.Sin(pHueAngle), MathF.Cos(pHueAngle)) * size / 2 * pWarpedSaturation + size / 2;
                drawList.AddCircle(windowPos + pPickedColorPos, 3, UiColors.BackgroundFull.Fade(0.7f));
                drawList.AddCircle(windowPos + pPickedColorPos, 2, UiColors.ForegroundFull.Fade(0.7f));
            }

            {
                drawList.AddCircle(windowPos + pickedColorPos, 5, UiColors.BackgroundFull);
                drawList.AddCircle(windowPos + pickedColorPos, 4, UiColors.ForegroundFull);
            }

            ImGui.SetCursorPosX(10);
            ImGui.InvisibleButton("colorWheel", size);
            if (ImGui.IsItemActive())
            {
                var localPosition = ImGui.GetMousePos() - windowPos - size / 2;
                edited |= InputEditStateFlags.Modified;

                hNormalized = (MathF.Atan2(localPosition.X, localPosition.Y) / (2 * MathF.PI) - 0.25f);
                if (hNormalized < 0)
                {
                    hNormalized = 1 + hNormalized;
                }

                var saturation = MathUtils.Clamp(localPosition.Length() / size.X * 2f, 0, 1);
                linearSaturation = MathF.Pow(saturation, saturationWarp);

                cColor = Color.FromHSV(hNormalized, linearSaturation, v, cColor.A);
            }

            if (ImGui.IsItemActivated())
            {
                edited |= InputEditStateFlags.Started;
            }

            if (ImGui.IsItemDeactivated())
            {
                edited |= InputEditStateFlags.Finished;
            }

            var transparentColor = cColor.Rgba;
            transparentColor.W = 0;

            var opaqueColor = cColor;
            opaqueColor.A = 1;

            // Draw value slider 
            {
                var barHeight = wheelRadius;
                const float barWidth = 10;
                var pMin = windowPos + new Vector2(size.X + 10, 0);
                var visibleBarSize = new Vector2(barWidth, barHeight);
                var pMax = pMin + visibleBarSize;

                var hrdEditEnabled = ImGui.GetIO().KeyCtrl;
                if (hrdEditEnabled)
                {
                    var hdrOffset = new Vector2(0, -300);
                    drawList.AddRectFilled(pMin - Vector2.One + hdrOffset, pMax + Vector2.One, Color.Black);
                }
                else
                {
                    drawList.AddRectFilled(pMin - Vector2.One, pMax + Vector2.One, Color.Black);
                    if (cColor.V > 1)
                    {
                        var offset = new Vector2(4,-5);
                        drawList.AddTriangleFilled(
                                                   pMin + new Vector2(-6,0) + offset,
                                                   pMin + new Vector2(0,-10) + offset,
                                                   pMin + new Vector2(6,0) + offset,
                                                   UiColors.ForegroundFull
                                                  );
                    }
                }

                if (cColor.V > 1)
                {
                    var label = $"× {cColor.V:0.00}";
                    var labelWidth = ImGui.CalcTextSize(label);
                    drawList.AddText(pMin - new Vector2(labelWidth.X+10, +20), UiColors.Text, label);
                }

                var brightColor = cColor;
                brightColor.V = 1;
                brightColor.A = 1;
                
                drawList.AddRectFilledMultiColor(pMin, pMax,
                                                 ImGui.ColorConvertFloat4ToU32(brightColor),
                                                 ImGui.ColorConvertFloat4ToU32(brightColor),
                                                 ImGui.ColorConvertFloat4ToU32(transparentColor),
                                                 ImGui.ColorConvertFloat4ToU32(transparentColor));

                // Draw compare value
                if(compareValue <= 1 || hrdEditEnabled) 
                {
                    var mappedHdrValue = GetYFromHdr(compareValue);
                    var handlePos = new Vector2(0, barHeight * (1 - mappedHdrValue)) + pMin;
                    drawList.AddRectFilled(handlePos, handlePos + new Vector2(barWidth + 2, 2), UiColors.ForegroundFull.Fade(0.5f));
                }

                // Draw indicator
                if(cColor.V <= 1 || hrdEditEnabled) 
                {
                    var mappedHdrValue = GetYFromHdr(cColor.V);

                    var handlePos = new Vector2(0, barHeight * (1 - mappedHdrValue)) + pMin;
                    drawList.AddRectFilled(handlePos - Vector2.One, handlePos + new Vector2(barWidth + 2, 3), UiColors.BackgroundFull);
                    drawList.AddRectFilled(handlePos, handlePos + new Vector2(barWidth + 2, 2), UiColors.ForegroundFull);
                }
                ImGui.SetCursorScreenPos(pMin - new Vector2(10, 0));
                ImGui.InvisibleButton("intensitySlider", new Vector2(visibleBarSize.X * 4, visibleBarSize.Y));
                if (ImGui.IsItemActive())
                {
                    var clampUpperValue = hrdEditEnabled ? 100 : 1;
                    var normalizedValue = (1 - (ImGui.GetMousePos() - pMin).Y / barHeight).Clamp(0, clampUpperValue);
                    normalizedValue = GetHdrYValue(normalizedValue);
                    // if (normalizedValue > 1)
                    // {
                    //     normalizedValue = MathF.Pow(normalizedValue, 3);
                    // }

                    cColor.V = normalizedValue;

                    edited |= InputEditStateFlags.Modified;
                }

                if (ImGui.IsItemActivated())
                {
                    edited |= InputEditStateFlags.Started;
                }

                if (ImGui.IsItemDeactivated())
                {
                    edited |= InputEditStateFlags.Finished;
                }
            }

            // Draw alpha slider 
            {
                var barHeight = 10;
                var barWidth = wheelRadius;

                var pMin = windowPos + new Vector2(0, size.X + 10);
                var barSize = new Vector2(barWidth, barHeight);
                var pMax = pMin + barSize;

                var area = new ImRect(pMin, pMax);
                drawList.AddRectFilled(pMin - Vector2.One, pMax + Vector2.One, new Color(0.1f, 0.1f, 0.1f));
                CustomComponents.FillWithStripes(drawList, area);

                drawList.AddRectFilledMultiColor(pMin, pMax,
                                                 ImGui.ColorConvertFloat4ToU32(transparentColor),
                                                 ImGui.ColorConvertFloat4ToU32(opaqueColor),
                                                 ImGui.ColorConvertFloat4ToU32(opaqueColor),
                                                 ImGui.ColorConvertFloat4ToU32(transparentColor));

                // Draw compare value
                {
                    var handlePos = new Vector2(barWidth * compareColor.W, 0) + pMin;
                    drawList.AddRectFilled(handlePos,
                                           handlePos + new Vector2(2, barHeight + 2), UiColors.ForegroundFull.Fade(0.5f));
                }
                
                // Draw handle
                {
                    var handlePos = new Vector2(barWidth * cColor.A, 0) + pMin;
                    drawList.AddRectFilled(handlePos - Vector2.One,
                                           handlePos + new Vector2(3, barHeight + 2), UiColors.BackgroundFull);
                    drawList.AddRectFilled(handlePos,
                                           handlePos + new Vector2(2, barHeight + 2), UiColors.ForegroundFull);
                }

                ImGui.SetCursorScreenPos(pMin - new Vector2(0, 10));

                ImGui.InvisibleButton("alphaSlider", new Vector2(barSize.X, barSize.Y * 3));
                if (ImGui.IsItemActive())
                {
                    cColor.A = ((ImGui.GetMousePos() - pMin).X / barWidth).Clamp(0, 1);
                    edited |= InputEditStateFlags.Modified;
                }

                if (ImGui.IsItemActivated())
                {
                    edited |= InputEditStateFlags.Started;
                }

                if (ImGui.IsItemDeactivated())
                {
                    edited |= InputEditStateFlags.Finished;
                }
            }
            return edited;
        }

        private static float GetYFromHdr(float value)
        {
            return  value < 1 ? value : MathF.Pow((value).Clamp(0,1000), 1/3f);
        }
        
        private static float GetHdrYValue(float value)
        {
            return  value < 1 ? value : MathF.Pow((value).Clamp(0,1000), 3f);
        }

        private enum ColorInputModes
        {
            Hsl,
            Rgba,
            iRgba,
            Hex,
        }

        private static ColorInputModes _inputMode;

        private static InputEditStateFlags DrawColorInputs(ref Color cColor, float hNormalized, float linearSaturation, float v)
        {
            var edited = InputEditStateFlags.Nothing;

            const float inputWidth = 45;
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2, 1));
            var inputSize = new Vector2(inputWidth, ImGui.GetFrameHeight());

            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
            ImGui.AlignTextToFramePadding();

            {
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                if (ImGui.Button((_inputMode + "...").ToUpperInvariant()))
                {
                    _inputMode = (ColorInputModes)((int)(_inputMode + 1) % Enum.GetNames(typeof(ColorInputModes)).Length);
                }
                CustomComponents.TooltipForLastItem("Click to toggle between HSL, RGBA, integers and Hex input");

                ImGui.PopStyleColor();
            }

            ImGui.SameLine();

            switch (_inputMode)
            {
                case ColorInputModes.Hsl:
                {
                    var hueDegrees = hNormalized * 360f;

                    ImGui.PushID("h");
                    if (SingleValueEdit.Draw(ref hueDegrees, inputSize, 0, 360, false, 1, "{0:0.0}") is InputEditStateFlags.Modified)
                    {
                        if (hueDegrees < 360)
                        {
                            hueDegrees += 360;
                        }
                        else if (hueDegrees > 360)
                        {
                            hueDegrees -= 360;
                        }

                        cColor.Hue = hueDegrees / 360;
                        edited |= InputEditStateFlags.Modified;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.PushID("s");
                    if (SingleValueEdit.Draw(ref linearSaturation, inputSize, 0, 1, true,
                                             scale: 0.005f,
                                             format: "{0:0.00}") is InputEditStateFlags.Modified)
                    {
                        cColor.Saturation = linearSaturation.Clamp(0, 1);
                        edited |= InputEditStateFlags.Modified;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.PushID("v");
                    if (SingleValueEdit.Draw(ref v, inputSize, 0, 20, true, 0.005f, "{0:0.00}") is InputEditStateFlags.Modified)
                    {
                        cColor.V = v.Clamp(0, 1000);
                        edited |= InputEditStateFlags.Modified;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    ImGui.Dummy(Vector2.One * 5);

                    ImGui.SameLine();
                    ImGui.PushID("a");
                    var a = cColor.A;
                    if (SingleValueEdit.Draw(ref a, inputSize, 0, 1, true, 0.005f, "{0:0.00}") is InputEditStateFlags.Modified)
                    {
                        cColor.A = a.Clamp(0, 1);
                        edited |= InputEditStateFlags.Modified;
                    }

                    ImGui.PopID();
                    break;
                }
                case ColorInputModes.Rgba:
                {
                    var r = cColor.R;
                    ImGui.PushID("r");
                    if (SingleValueEdit.Draw(ref r, inputSize, 0, 1, false, 1) is InputEditStateFlags.Modified)
                    {
                        cColor.R = r;
                        edited |= InputEditStateFlags.Modified;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    var g = cColor.G;
                    ImGui.PushID("g");
                    if (SingleValueEdit.Draw(ref g, inputSize, 0, 1, false, 1) is InputEditStateFlags.Modified)
                    {
                        cColor.G = g;
                        edited |= InputEditStateFlags.Modified;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    var b = cColor.B;
                    ImGui.PushID("b");
                    if (SingleValueEdit.Draw(ref b, inputSize, 0, 1, false, 1) is InputEditStateFlags.Modified)
                    {
                        cColor.B = b;
                        edited |= InputEditStateFlags.Modified;
                    }

                    ImGui.PopID();
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(5, 5));

                    ImGui.SameLine();
                    var a = cColor.A;
                    ImGui.PushID("a");
                    if (SingleValueEdit.Draw(ref a, inputSize, 0, 1, false, 1) is InputEditStateFlags.Modified)
                    {
                        cColor.A = a;
                        edited |= InputEditStateFlags.Modified;
                    }

                    ImGui.PopID();

                    break;
                }
                case ColorInputModes.iRgba:
                {
                    var r = MathF.Round(cColor.R * 255);
                    ImGui.PushID("r");
                    if (SingleValueEdit.Draw(ref r, inputSize, 0, 255, false, 1, "{0:0}") is InputEditStateFlags.Modified)
                    {
                        cColor.R = r / 255;
                        edited |= InputEditStateFlags.Modified;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    var g = MathF.Round(cColor.G * 255);
                    ImGui.PushID("g");
                    if (SingleValueEdit.Draw(ref g, inputSize, 0, 255, false, 1, "{0:0}") is InputEditStateFlags.Modified)
                    {
                        cColor.G = g / 255;
                        edited |= InputEditStateFlags.Modified;
                    }

                    ImGui.PopID();

                    ImGui.SameLine();
                    var b = MathF.Round(cColor.B * 255);
                    ImGui.PushID("b");
                    if (SingleValueEdit.Draw(ref b, inputSize, 0, 255, false, 1, "{0:0}") is InputEditStateFlags.Modified)
                    {
                        cColor.B = b / 255;
                        edited |= InputEditStateFlags.Modified;
                    }

                    ImGui.PopID();
                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(5, 5));

                    ImGui.SameLine();
                    var a = MathF.Round(cColor.A * 255);
                    ;
                    ImGui.PushID("a");
                    if (SingleValueEdit.Draw(ref a, inputSize, 0, 255, false, 1, "{0:0}") is InputEditStateFlags.Modified)
                    {
                        cColor.A = a / 255;
                        edited |= InputEditStateFlags.Modified;
                    }

                    ImGui.PopID();

                    break;
                }
                case ColorInputModes.Hex:
                    var html = cColor.ToHTML();
                    if (ImGui.InputText("##hex", ref html, 10))
                    {
                        try
                        {
                            var prefix = html.StartsWith("#") ? "" : "#";
                            cColor = Color.FromString(prefix + html);
                        }
                        catch
                        {
                            // ignored
                        }
                        finally
                        {
                            edited |= InputEditStateFlags.Modified;
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            return edited;
        }

        private static InputEditStateFlags DrawUsedColors(ref Color cColor)
        {
            var edited = InputEditStateFlags.Nothing;
            _isHoveringColor = false;

            ImGui.BeginChild("##swatches");
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            FormInputs.AddVerticalSpace();
            ImGui.Indent(7);
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
            ImGui.TextUnformatted("Used colors...");
            ImGui.PopStyleColor();
            FormInputs.AddVerticalSpace(2);

            var wdl = ImGui.GetWindowDrawList();
            var index = 0;

            // Active
            var activeRoundedColor = new Vector4(MathF.Round(cColor.R, 3),
                                                 MathF.Round(cColor.G, 3),
                                                 MathF.Round(cColor.B, 3),
                                                 MathF.Round(cColor.A, 3)
                                                );

            foreach (var usedColor in ColorUsage.ColorsOrderedByFrequency)
            {
                if (!ColorUsage.ColorUses.TryGetValue(usedColor, out var uses))
                    continue;

                if (index % 14 > 0)
                {
                    ImGui.SameLine();
                }

                var c = new Color(usedColor);

                // Draw color swatch
                {
                    ImGui.InvisibleButton($"Color {usedColor:0.00000}", Vector2.One * 16);
                    if (ImGui.IsItemHovered())
                    {
                        CustomComponents.TooltipForLastItem($"{c} used {uses.Count}×", "CTRL+Click to select operators");
                        _isHoveringColor = true;
                        _hoveredColor = c;

                        foreach (var use in uses)
                        {
                            if (use.Instance == null)
                                continue;
                            
                            FrameStats.AddHoveredId(use.Instance.SymbolChildId);
                        }

                        if (ImGui.IsItemDeactivated())
                        {
                            if (ImGui.GetIO().KeyCtrl)
                            {
                                SelectRelatedInstances(uses);
                            }
                            else
                            {
                                cColor = new Color(usedColor);
                                edited |= InputEditStateFlags.ModifiedAndFinished;
                                // if (ImGui.GetIO().KeyShift)
                                // {
                                //     ColorUsage.CollectUsedColors();
                                // }
                            }
                        }
                    }

                    var min = ImGui.GetItemRectMin();
                    var max = ImGui.GetItemRectMax() - Vector2.One;
                    wdl.AddText(Icons.IconFont, 13, min, new Color(0.1f), "" + (char)T3.Editor.Gui.Styling.Icon.Stripe4PxPattern);

                    var opaqueColor = new Color(
                                                usedColor.X,
                                                usedColor.Y,
                                                usedColor.Z
                                               );
                    wdl.AddTriangleFilled(min,
                                          new Vector2(max.X, min.Y),
                                          new Vector2(min.X, max.Y),
                                          opaqueColor);

                    wdl.AddTriangleFilled(new Vector2(max.X, min.Y),
                                          new Vector2(max.X, max.Y),
                                          new Vector2(min.X, max.Y),
                                          new Color(usedColor));

                    // Fix aliasing glitch
                    wdl.AddLine(new Vector2(max.X - 1, min.Y),
                                new Vector2(min.X, max.Y - 1),
                                opaqueColor);

                    // Mark single uses
                    if (uses.Count > 1)
                    {
                        wdl.AddTriangleFilled(new Vector2(min.X, max.Y),
                                              new Vector2(min.X, max.Y - 4),
                                              new Vector2(min.X + 4, max.Y),
                                              UiColors.Gray);
                    }

                    if (usedColor == activeRoundedColor)
                    {
                        wdl.AddRect(min - Vector2.One, max + Vector2.One, UiColors.ForegroundFull, 1);
                        wdl.AddRect(min, max, UiColors.BackgroundFull, 1);
                    }
                }
                index++;
            }

            ImGui.PopStyleVar();
            ImGui.EndChild();
            return edited;
        }

        private static void SelectRelatedInstances(List<ColorUsage.ColorUse> uses)
        {
            var selectedIds = new HashSet<Guid>();

            NodeSelection.Clear();
            foreach (var use in uses)
            {
                if (use.Instance == null || selectedIds.Contains(use.Instance.SymbolChildId))
                    continue;

                if (!SymbolUiRegistry.Entries.TryGetValue(use.Instance.Parent.Symbol.Id, out var parentSymbolUi))
                    continue;

                var childUi = parentSymbolUi.ChildUis.SingleOrDefault(cc => cc.Id == use.Instance.SymbolChildId);
                if (childUi == null)
                    continue;

                NodeSelection.AddSymbolChildToSelection(childUi, use.Instance);
                selectedIds.Add(childUi.Id);
            }

            if (selectedIds.Count > 0)
                FitViewToSelectionHandling.FitViewToSelection();
        }

        private static uint _openedId;

        private static Color GetColorAtMousePosition()
        {
            var pos = UiContentUpdate.CursorPosOnScreen;
            var x = (int)pos.X;
            var y = (int)pos.Y;

            var bounds = new Rectangle(x, y, 1, 1);
            try
            {
                using (var g = Graphics.FromImage(_bmp))
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

                var c = _bmp.GetPixel(0, 0);

                return new Color(c.R, c.G, c.B, c.A);
            }
            catch(Exception e)
            {
                Log.Warning("Failed to pick color: " + e.Message);
            }
            return Color.Transparent;
        }

        private static Color _hoveredColor;
        private static bool _isHoveringColor;
        private static readonly Bitmap _bmp = new(1, 1);
        public  const string PopupId =  "##colorEdit";
    }

    public static class ColorUsage
    {
        public static readonly Dictionary<Vector4, List<ColorUse>> ColorUses = new();
        public static List<Vector4> ColorsOrderedByFrequency = new();

        public static void CollectUsedColors()
        {
            ColorUses.Clear();

            // Add defaults
            foreach (var dc in _defaultColors)
            {
                SaveUse(dc, new LibraryColorUse());
            }

            var op = GraphWindow.GetMainComposition();
            if (op == null)
                return;

            var animator = op.Symbol.Animator;

            foreach (var child in op.Children)
            {
                if (_ignoredSymbols.Contains(child.Symbol.Name))
                    continue;

                foreach (var input in child.Inputs)
                {
                    if (input.Input.IsDefault || input.IsConnected)
                        continue;

                    if (animator.IsAnimated(child.SymbolChildId, input.Id))
                    {
                        continue;
                    }

                    switch (input)
                    {
                        case InputSlot<Vector4> vec4Input:
                            SaveUse(vec4Input.TypedInputValue.Value,
                                    new ParameterColorUse
                                        {
                                            Instance = child,
                                            ColorParameter = vec4Input
                                        });
                            break;
                        case InputSlot<Gradient> gradientInput:
                        {
                            var gradient = gradientInput.TypedInputValue.Value;

                            foreach (var step in gradient.Steps)
                            {
                                SaveUse(step.Color,
                                        new GradientColorUse
                                            {
                                                Instance = child,
                                                GradientParameter = gradientInput,
                                            });
                            }

                            break;
                        }
                    }
                }
            }

            ColorsOrderedByFrequency = ColorUses.OrderBy(u2 => _defaultColors.IndexOf(u2.Key))
                                                .ThenBy(u => u.Value.Count)
                                                .Select(u2 => u2.Key)
                                                .Reverse()
                                                .ToList();
        }

        public static void SaveUse(Vector4 color, ColorUse use)
        {
            var roundedColor = new Vector4(MathF.Round(color.X, 3),
                                           MathF.Round(color.Y, 3),
                                           MathF.Round(color.Z, 3),
                                           MathF.Round(color.W, 3)
                                          );
            if (ColorUses.TryGetValue(roundedColor, out var uses))
            {
                uses.Add(use);
                return;
            }

            ColorUses[roundedColor] = new List<ColorUse> { use };
        }

        public abstract class ColorUse
        {
            public Instance Instance;
        }

        public class GradientColorUse : ColorUse
        {
            public InputSlot<Gradient> GradientParameter;
            public Gradient.Step Gradient;
        }

        public class LibraryColorUse : ColorUse
        {
        }

        public class ParameterColorUse : ColorUse
        {
            public InputSlot<Vector4> ColorParameter;
        }

        /// <summary>
        /// This values will be listed at the beginning of the swatch list in reversed order
        /// </summary>
        private static readonly List<Vector4> _defaultColors = new() 
                                                                   {
                                                                       new Vector4(0f, 0f, 0f, 1f),
                                                                       new Vector4(0.5f, 0.5f, 0.5f, 1f),
                                                                       new Vector4(1f, 1f, 1f, 1f),
                                                                   };

        /// <summary>
        /// Using color swatches doesn't make sense for these operators.
        /// </summary>
        private static readonly List<string> _ignoredSymbols = new()
                                                                   {
                                                                       "ColorGrade",
                                                                       "ChannelMixer",
                                                                   };
    }
}