using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ff5b93e3_d29f_489f_8cca_cb12cd1af65d
{
    public class _DrawPointInfo : Instance<_DrawPointInfo>
    {

        [Output(Guid = "c4f6184c-adca-4637-ba22-3ece7efbd8dc")]
        public readonly Slot<T3.Core.DataTypes.Command> Output = new Slot<T3.Core.DataTypes.Command>();

        [Input(Guid = "44e119a8-2aa5-47b1-808c-2b207d74a0b4")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();
        
        [Input(Guid = "1F8D3E29-4EC6-4339-8463-223E8336F4D7")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> OverridePositions = new InputSlot<T3.Core.DataTypes.BufferWithViews>();


        [Input(Guid = "e6d32fee-30d9-4ccf-b48b-88906f841ff8")]
        public readonly InputSlot<System.Numerics.Vector2> LabelOffset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "0353313f-64dc-4e93-8bac-3c9b78df5cc9")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "776f6602-f5f6-43c9-b00a-a60e7e7454d8")]
        public readonly InputSlot<int> IndexOffset = new InputSlot<int>();

        [Input(Guid = "17b58d1a-ee78-49cf-9a71-1b339b985088")]
        public readonly InputSlot<int> Show = new InputSlot<int>();

        [Input(Guid = "f21dfa0c-6276-47cc-8965-9060fe9d96d4")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

    }
}

