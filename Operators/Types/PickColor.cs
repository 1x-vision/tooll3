using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3539c3a0_73f1_412c_aad2_517810126ac6
{
    public class PickColor : Instance<PickColor>
    {
        [Output(Guid = "E97325E4-7A64-4F38-B7B1-92EDD73BFB77")]
        public readonly Slot<Vector4> Selected = new();

        public PickColor()
        {
            Selected.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var connections = Input.GetCollectedTypedInputs();
            if (connections == null || connections.Count == 0)
                return;

            var index = Index.GetValue(context);
            if (index < 0)
                index = -index;

            index %= connections.Count;
            Selected.Value = connections[index].GetValue(context);
        }

        [Input(Guid = "03A48F06-5375-449F-941B-35F93B2A5825")]
        public readonly MultiInputSlot<Vector4> Input = new();

        [Input(Guid = "F3F93FEB-8B26-4F6D-BD90-4C331CF9FAC0")]
        public readonly InputSlot<int> Index = new(0);
    }
}