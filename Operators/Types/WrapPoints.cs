using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b263a6a1_0872_4223_80e7_5e09f4aea19d
{
    public class WrapPoints : Instance<WrapPoints>
    {

        [Output(Guid = "189921cd-cc7b-4d26-83b5-726815d3617c")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "4d74f19f-0f8b-4918-9999-8ae980e33d39")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "67f6eca6-baaa-48c8-8e9a-c25718ca94f5")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "daba3196-b9eb-4cb0-b062-00626cadc28b")]
        public readonly InputSlot<System.Numerics.Vector3> Size = new InputSlot<System.Numerics.Vector3>();


        private enum Spaces
        {
            PointSpace,
            ObjectSpace,
            WorldSpace,
        }
    }
}

