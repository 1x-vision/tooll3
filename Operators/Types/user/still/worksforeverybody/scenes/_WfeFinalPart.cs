using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1026538b_f021_43f0_b6bd_dda89c57de94
{
    public class _WfeFinalPart : Instance<_WfeFinalPart>
    {

        [Output(Guid = "4d077ee1-277d-474d-889e-30db32471ce8")]
        public readonly Slot<T3.Core.DataTypes.Command> CommandOutput = new Slot<T3.Core.DataTypes.Command>();

        [Input(Guid = "fb0adf5b-f16a-4ccc-b270-9fb87f0c9529")]
        public readonly InputSlot<System.Collections.Generic.List<string>> Scores = new InputSlot<System.Collections.Generic.List<string>>();

        [Input(Guid = "2301f6d0-f715-4f92-be99-f22dca8aac12")]
        public readonly InputSlot<int> LastScore = new InputSlot<int>();


    }
}

