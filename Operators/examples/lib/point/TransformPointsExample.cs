using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
	[Guid("0d94d3d9-3023-4763-b80c-2c63415500d4")]
    public class TransformPointsExample : Instance<TransformPointsExample>
    {
        [Output(Guid = "894cb20e-ff5a-48b5-a8d7-1fc64f933154")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

