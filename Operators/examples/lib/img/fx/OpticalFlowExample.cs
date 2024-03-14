using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.img.fx
{
	[Guid("db89bacd-db5a-4d52-a073-ed141f413f8d")]
    public class OpticalFlowExample : Instance<OpticalFlowExample>
    {
        [Output(Guid = "350937d6-d52a-4d9e-8b35-b07a750eb179")]
        public readonly Slot<Texture2D> ColorBuffer = new();

        [Input(Guid = "c3929959-29a5-4e1f-acd0-f6c4306f881c")]
        public readonly InputSlot<string> Path = new();


    }
}

