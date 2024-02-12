﻿using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Interaction;
using T3.Core.Resource;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.InputUi.VectorInputs
{
    public class Vector2InputUi : FloatVectorInputValueUi<Vector2>
    {
        public Vector2InputUi() : base(2)
        {
        }

        public override IInputUi Clone()
        {
            return CloneWithType<Vector2InputUi>();
        }


        protected override InputEditStateFlags DrawEditControl(string name, SymbolChild.Input input, ref Vector2 float2Value, bool readOnly)
        {
            float2Value.CopyTo(FloatComponents);

            var controlRatio = MathUtils.RemapAndClamp(ImGui.GetWindowWidth(), 300, 350, 1, 16f / 9);
            var showControl = (controlRatio > 1f && UseVec2Control != Vec2Controls.None);
            var rightPadding = showControl ? ImGui.GetFrameHeight() * controlRatio : 0;

            var inputEditState = VectorValueEdit.Draw(FloatComponents, Min, Max, Scale, Clamp, rightPadding + 4, Format);
            var shouldClamp = false;
            
            if (showControl)
            {
                ImGui.SameLine();
                ImGui.InvisibleButton("## dragging", new Vector2(rightPadding, ImGui.GetFrameHeight()));
                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                var drawList = ImGui.GetWindowDrawList();

                Color backgroundColor = ImGui.IsItemActive()
                                            ? UiColors.BackgroundActive.Fade(0.4f)
                                            : (UiColors.BackgroundButton.Fade(ImGui.IsItemHovered() ? 1 : 0.5f));
                drawList.AddRectFilled(min, max, backgroundColor);
                var normalizedVec = ((float2Value - new Vector2(Min)) / (Max - Min) - new Vector2(0.5f)) * new Vector2(controlRatio,1) + new Vector2(0.5f);
                var color = input == null ? UiColors.StatusAutomated
                            : input.IsDefault ? UiColors.TextMuted
                            : UiColors.ForegroundFull;

                var exceedsDragCanvas = normalizedVec.X < 0 || normalizedVec.X > 1 || normalizedVec.Y < 0 || normalizedVec.Y > 1;
                if (exceedsDragCanvas)
                {
                    color = color.Fade(0.3f);
                    normalizedVec = new Vector2(normalizedVec.X.Clamp(0, 1), normalizedVec.Y.Clamp(0, 1));
                }

                var controlPointScreenPos = new Vector2(
                                                        MathUtils.Lerp(min.X, max.X, normalizedVec.X),
                                                        MathUtils.Lerp(max.Y, min.Y, normalizedVec.Y)
                                                       );

                if (UseVec2Control == Vec2Controls.Position)
                {
                    drawList.AddCircleFilled(controlPointScreenPos, 2, color);

                    // Center lines...
                    var center = (min + max) / 2;
                    drawList.AddLine(new Vector2(center.X, min.Y), new Vector2(center.X, max.Y), UiColors.ForegroundFull.Fade(0.06f), 1);
                    drawList.AddLine(new Vector2(min.X, center.Y), new Vector2(max.X, center.Y), UiColors.ForegroundFull.Fade(0.06f), 1);
                }
                else if (UseVec2Control == Vec2Controls.BiasAndGain)
                {
                    shouldClamp = true;
                    drawList.AddCircleFilled(controlPointScreenPos, 2, color);

                    const int steps = 15;
                    var points = new Vector2[steps];
                    
                    // Distribution...
                    for(var i=0; i < steps ; i ++)
                    {
                        var t = (float)i / (steps-1); 
                        var f = t.ApplyBiasAndGain(float2Value.X, float2Value.Y);
                        var x = MathUtils.Lerp(min.X, max.X, f);
                        var y = MathUtils.Lerp(max.Y, min.Y, f);
                        // drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y),
                        //                  UiColors.ForegroundFull.Fade(0.1f), 1);

                        
                        drawList.AddLine(new Vector2(min.X, y), new 
                                             Vector2(max.X, y),
                                         UiColors.ForegroundFull.Fade(0.1f), 1);
                        
                        
                        points[i] = new Vector2(MathUtils.Lerp(min.X, max.X, t), 
                                                MathUtils.Lerp(min.Y, max.Y, 1-f));
                    }
                    
                    drawList.AddPolyline(ref points[0], steps, UiColors.TextMuted, ImDrawFlags.None, 1);
                }

                if (ImGui.IsItemActivated())
                {
                    _valueInDragStart = float2Value;
                }

                if (ImGui.IsItemActive())
                {
                    inputEditState = InputEditStateFlags.Modified;
                    var mouseDragDelta = ImGui.GetMouseDragDelta();

                    var speedFactor = 1f;

                    if (ImGui.GetIO().KeyShift)
                    {
                        speedFactor *= 0.1f;
                    }

                    if (ImGui.GetIO().KeyAlt)
                    {
                        speedFactor *= 10f;
                    }

                    FloatComponents[0] = _valueInDragStart.X + mouseDragDelta.X * 0.005f * speedFactor;
                    FloatComponents[1] = _valueInDragStart.Y - mouseDragDelta.Y * 0.005f * speedFactor;

                    if (shouldClamp)
                    {
                        FloatComponents[0] = FloatComponents[0].Clamp(Min, Max);
                        FloatComponents[1] = FloatComponents[1].Clamp(Min, Max);
                    }
                }
            }

            if (readOnly)
                return InputEditStateFlags.Nothing;

            float2Value = new Vector2(FloatComponents[0], FloatComponents[1]);

            return inputEditState;
        }

        public override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time)
        {
            if (inputValue is not InputValue<Vector2> typedInputValue)
                return;

            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            typedInputValue.Value.CopyTo(FloatComponents);
            Curve.UpdateCurveValues(curves, time, FloatComponents);
        }

        public override void DrawSettings()
        {
            base.DrawSettings();
            
            {
                FormInputs.AddVerticalSpace();
                var tmpForRef = UseVec2Control;
                if (FormInputs.AddEnumDropdown(ref tmpForRef, "Vec2 Control"))
                    UseVec2Control = tmpForRef;
            }
        }

        public override void Write(JsonTextWriter writer)
        {
            base.Write(writer);

            if (UseVec2Control != Vec2Controls.None)
                writer.WriteObject(nameof(UseVec2Control), UseVec2Control.ToString());
        }

        public override void Read(JToken inputToken)
        {
            base.Read(inputToken);
            UseVec2Control = (inputToken[nameof(UseVec2Control)] == null)
                                 ? Vec2Controls.None
                                 : (Vec2Controls)Enum.Parse(typeof(Vec2Controls), inputToken[nameof(UseVec2Control)].ToString());
        }

        public Vec2Controls UseVec2Control;
        private static Vector2 _valueInDragStart;

        public enum Vec2Controls
        {
            None,
            Position,
            BiasAndGain,
            Range,
        }
    }
}