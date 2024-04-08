using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_69bd6cd3_85b2_4a6c_9184_ab25045a51aa
{
    public class BoundingBoxPoints : Instance<BoundingBoxPoints>
    {

        [Output(Guid = "921d43ce-6167-409b-9748-d6f59daa1cde")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new();

        [Input(Guid = "c6082d55-5e47-404f-96f9-612959cd75ce")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();


        private enum SampleModes
        {
            StartEnd,
            StartLength,
        }

        private enum RotationModes
        {
            Interpolate,
            Recompute,
        }
    }
}

