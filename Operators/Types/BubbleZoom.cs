using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ca3f3c1b_6f22_4bf3_b06b_d2b0d85a8881
{
    public class BubbleZoom : Instance<BubbleZoom>
    {
        [Output(Guid = "f49bcb2b-106d-45f5-8161-8d4975b30f8c")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "019b7a1a-0f86-48e2-b5e9-9a04480a3b07")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "277c5e50-7d0f-416f-ba14-69ba407802dc")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "5bb5934d-28a8-4bd3-a146-f0ecb804170a")]
        public readonly InputSlot<float> ScaleFactor = new InputSlot<float>();

        [Input(Guid = "1e93d2b1-d21c-44bd-a96c-9844b9a89c8f")]
        public readonly InputSlot<float> Width = new InputSlot<float>();

        [Input(Guid = "064b98dc-60f8-4019-a40c-21cb3bed9f4a")]
        public readonly InputSlot<float> Radius = new InputSlot<float>();

        [Input(Guid = "e5a5d0cf-447b-471f-871a-f3237a760df2")]
        public readonly InputSlot<float> Bias = new InputSlot<float>();

        [Input(Guid = "412055dc-88f7-4def-ac8e-76f7f424f905")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "0ce5b753-bcc0-4102-b011-64e603a50567")]
        public readonly InputSlot<T3.Core.DataTypes.Gradient> Gradient = new InputSlot<T3.Core.DataTypes.Gradient>();
    }
}

