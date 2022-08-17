﻿using System.Linq;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Gui.InputUi.SingleControl
{
    public class BoolInputUi : SingleControlInputUi<bool>
    {
        public override bool IsAnimatable => true;        
        
        public override IInputUi Clone()
        {
            return new BoolInputUi()
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy
                   };
        }

        protected override bool DrawSingleEditControl(string name, ref bool value)
        {
            return ImGui.Checkbox("##boolParam", ref value);
        }

        protected override void DrawReadOnlyControl(string name, ref bool value)
        {
            ImGui.TextUnformatted(value.ToString());
        }
        

        protected override InputEditStateFlags DrawAnimatedValue(string name, InputSlot<bool> inputSlot, Animator animator)
        {
            var time = Playback.Current.TimeInBars;
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            if (curves.Length != 1)
            {
                Log.Assert($"Animated bool requires a singe animation curve (got {curves.Length})");
                return InputEditStateFlags.Nothing;
            }

            var curve = curves[0];
            var value = curve.GetSampledValue(time) > 0.5f;
            
            var modified = DrawSingleEditControl(name, ref value);
            if (!modified)
                return InputEditStateFlags.Nothing;
            
            inputSlot.SetTypedInputValue(value);

            return InputEditStateFlags.ModifiedAndFinished;

        }        
        
        public override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time)
        {
            if (inputValue is not InputValue<bool> boolInputValue)
                return;
            
            var value = boolInputValue.Value;
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            if (curves.Length != 1)
            {
                Log.Error("Expected 1 curve for bool animation");
                return;
            } 
            Curve.UpdateCurveBoolValue(curves[0], time, value );
        }        
    }
}