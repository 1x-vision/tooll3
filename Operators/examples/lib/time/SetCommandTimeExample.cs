using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.time
{
	[Guid("1cccb283-18f0-4ef2-817f-dd28d40ebb7a")]
    public class SetCommandTimeExample : Instance<SetCommandTimeExample>
    {
        [Output(Guid = "7b7a6d6a-dd67-4a62-83bb-e5912ba611aa")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

