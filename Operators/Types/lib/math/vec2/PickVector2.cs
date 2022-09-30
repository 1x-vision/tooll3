using System.Collections.Generic;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1cd2f5b1_bc54_4405_8ee6_d1acf59ebe14
{
    public class PickVector2 : Instance<PickVector2>
    {
        [Output(Guid = "8c28230f-0ff8-4158-b6db-33ffe7b64ad5")]
        public readonly Slot<System.Numerics.Vector2> Selected = new Slot<System.Numerics.Vector2>();

        public PickVector2()
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

        [Input(Guid = "f210558a-4f9e-4e94-bbd4-ef73da498614")]
        public readonly MultiInputSlot<System.Numerics.Vector2> Input = new MultiInputSlot<System.Numerics.Vector2>();

        [Input(Guid = "dad7d212-e895-44d9-83cc-69fb11898aec")]
        public readonly InputSlot<int> Index = new InputSlot<int>(0);
    }
}