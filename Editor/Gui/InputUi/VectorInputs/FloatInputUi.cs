﻿using System.Linq;
using System.Numerics;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Interaction;

namespace T3.Editor.Gui.InputUi.VectorInputs
{
    public class FloatInputUi : FloatVectorInputValueUi<float>
    {
        public FloatInputUi() : base(1) { }
        
        public override IInputUi Clone()
        {
            return CloneWithType<FloatInputUi>();
        }
        
        protected override InputEditStateFlags DrawEditControl(string name, SymbolChild.Input input, ref float value, bool readOnly)
        {
            FloatComponents[0] = value;
            var inputEditState = VectorValueEdit.Draw(FloatComponents, Min, Max, Scale, Clamp, 0, Format);
            if (readOnly)
                return InputEditStateFlags.Nothing;
            
            value = FloatComponents[0];
            return inputEditState;
        }
        
        public InputEditStateFlags DrawEditControl(ref float value)
        {
            return SingleValueEdit.Draw(ref value, -Vector2.UnitX, Min, Max, Clamp, Scale);
        }
        
        public override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time)
        {
            if (inputValue is not InputValue<float> typedInputValue)
                return;
            
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            FloatComponents[0] = typedInputValue.Value;
            Curve.UpdateCurveValues(curves, time, FloatComponents);
        }        
    }
}