using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user._1x.@mars_epress
{
	[Guid("ee4065db-3b30-4b1f-95e1-127c41a9d185")]
    public class MarsExpressMain : Instance<MarsExpressMain>
    {
        [Output(Guid = "e7ebc68e-a1e4-4699-be3b-e9c87478fd53")]
        public readonly Slot<Texture2D> Output2 = new();


    }
}

