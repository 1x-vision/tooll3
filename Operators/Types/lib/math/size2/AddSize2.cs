using System.Numerics;
using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_40f0b1c9_9f87_489c_a6b9_ff9a5bd263ec
{
    public class AddSize2 : Instance<AddSize2>
    {
        [Output(Guid = "951EE7B3-ABC2-47EE-93A6-717C624D49E2")]
        public readonly Slot<Size2> Result = new();


        public AddSize2()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var s1 = Input1.GetValue(context);
            var s2 = Input2.GetValue(context);
            Result.Value = new Size2(s1.Width + s2.Width, s1.Height + s2.Height);
        }
        
        [Input(Guid = "C14E4756-C8CC-42FC-AF70-3473D9C6C861")]
        public readonly InputSlot<Size2> Input1 = new();

        [Input(Guid = "9B56CA60-D55C-44FF-BBBB-726D4BEB60A8")]
        public readonly InputSlot<Size2> Input2 = new();

    }
}
