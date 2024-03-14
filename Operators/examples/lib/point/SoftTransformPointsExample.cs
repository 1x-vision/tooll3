using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
	[Guid("0b019a98-0470-4d98-9d34-e06abd8c72d1")]
    public class SoftTransformPointsExample : Instance<SoftTransformPointsExample>
    {
        [Output(Guid = "d4ccbf12-6e9e-461e-867b-bc72b89afc80")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

