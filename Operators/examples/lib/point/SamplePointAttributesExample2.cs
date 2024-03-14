using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
	[Guid("01ebb7a3-9caa-4259-aaa1-c79248b39325")]
    public class SamplePointAttributesExample2 : Instance<SamplePointAttributesExample2>
    {
        [Output(Guid = "acf384c2-66d8-4802-ab79-de2ac9eef9a4")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

