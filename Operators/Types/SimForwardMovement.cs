using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_69c3b4ce_490a_48d4_b1d0_56dd6bf7a9a8
{
    public class SimForwardMovement : Instance<SimForwardMovement>
    {

        [Output(Guid = "9495dbae-0e49-449c-ab4a-58e267974385")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "2038b006-b5a3-472d-870b-d1a3623dfc0c")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "495e9766-0e5f-4ab7-abc1-c06b2edfe55d")]
        public readonly InputSlot<float> Drag = new InputSlot<float>();

        [Input(Guid = "697b7aa9-2b6c-423c-8f84-dbdbae721609")]
        public readonly InputSlot<float> Speed = new InputSlot<float>();

        [Input(Guid = "e733cd17-e854-4c23-99cb-9a03d4ae5eb5")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();
    }
}

