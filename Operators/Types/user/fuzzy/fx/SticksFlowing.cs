using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_064c1f38_8b6d_44f0_aae3_32dd3916e2e9
{
    public class SticksFlowing : Instance<SticksFlowing>
    {
        [Output(Guid = "65766fa1-21a3-45c6-917d-44322b61045d")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "dd68faab-fd9c-4939-bba2-388249965678")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "9dc910c7-810f-4787-946c-d0cc811d294d")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();
    }
}

