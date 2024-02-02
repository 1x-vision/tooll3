using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5a0b0485_7a55_4bf4_ae23_04f51d890334
{
    public class FractalNoise : Instance<FractalNoise>
    {
        [Output(Guid = "c85e033e-794c-4943-bf5d-545555df9360")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

        [Input(Guid = "091aaf77-46f4-4aeb-aaa8-f11fe34e8a7f")]
        public readonly InputSlot<System.Numerics.Vector4> ColorA = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "1c5670bf-c794-4bad-bf52-94a1c715f04c")]
        public readonly InputSlot<System.Numerics.Vector4> ColorB = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "ca5da68e-9c64-4331-b434-79bb139c6d3e")]
        public readonly InputSlot<System.Numerics.Vector2> BiasAndGain = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "751f9a41-d97f-4e04-8338-cebe9be88c5a")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "31e06af8-15be-4923-b5c6-c0e4bedc3347")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "34c1dc46-8001-47d4-b9d1-b4d0816a2294")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "2238e8c8-6bf8-4d3f-be5e-3291b6dc1441")]
        public readonly InputSlot<float> Phase = new InputSlot<float>();

        [Input(Guid = "c5f42436-432c-4d18-8bc2-f7f0772442f8")]
        public readonly InputSlot<int> Iterations = new InputSlot<int>();

        [Input(Guid = "6252840d-113a-416d-af7d-7c39e435f068")]
        public readonly InputSlot<System.Numerics.Vector2> WarpXY = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "c8eda097-f139-464c-8573-b08220b3b2c8")]
        public readonly InputSlot<float> WarpZ = new InputSlot<float>();

        [Input(Guid = "1d7d99e6-4306-4ebc-97b4-40fcb2abb4d0")]
        public readonly InputSlot<T3.Core.DataTypes.Vector.Int2> Resolution = new InputSlot<T3.Core.DataTypes.Vector.Int2>();

        [Input(Guid = "41fc212b-d221-4467-a955-4f8ea63a776f")]
        public readonly InputSlot<bool> GenerateMips = new InputSlot<bool>();


        private enum Methods
        {
            Legacy,
            OpenSimplex2S,
            OpenSimplex2S_NormalMap,
        }
    }
}

