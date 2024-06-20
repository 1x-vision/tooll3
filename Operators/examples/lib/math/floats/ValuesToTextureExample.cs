using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.math.floats
{
    [Guid("805d5196-f253-4fb6-9c5e-d69915b56328")]
    public class ValuesToTextureExample : Instance<ValuesToTextureExample>
    {
        [Output(Guid = "db89d748-5fa7-4f47-926b-51cc949aac41")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();


    }
}

