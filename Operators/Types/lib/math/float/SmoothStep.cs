using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_549ec76b_cc65_47b7_ad41_0abe93e86fe3 
{
    public class SmoothStep : Instance<SmoothStep>
    {
        [Output(Guid = "7caa79cf-1a5a-42cc-b245-5937cd83e8dc")]
        public readonly Slot<float> Result = new Slot<float>();

        public SmoothStep()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = MathUtils.SmootherStep(Min.GetValue(context), Max.GetValue(context), Value.GetValue(context));
        }
        
        [Input(Guid = "2FC8855F-3AEF-42F1-9269-9390079FD348")]
        public readonly InputSlot<float> Min = new InputSlot<float>();

        [Input(Guid = "338E3B0B-4012-4C7D-899B-12B7384F5A80")]
        public readonly InputSlot<float> Max = new InputSlot<float>();
        
        [Input(Guid = "fe01e41c-bd3b-441c-82f6-6c972097e155")]
        public readonly InputSlot<float> Value = new InputSlot<float>();
        

    }
}
