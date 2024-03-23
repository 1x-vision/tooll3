using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2ab1bbef_8322_4638_8b1d_7e31aaa6a457
{
    public class KeyColor : Instance<KeyColor>
    {
        [Output(Guid = "1d5f3a4c-227d-4875-a2ad-0ef445a675fe")]
        public readonly Slot<Texture2D> Output = new();

        [Input(Guid = "645e7d14-d4f8-46db-a5bc-b47136a00af5")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture2d = new();

        [Input(Guid = "8d07d9ad-fe83-4b40-bebd-21c563d6d6ac")]
        public readonly InputSlot<System.Numerics.Vector4> Key = new();

        [Input(Guid = "abadeee0-5ebb-4e5a-ae92-fcafb20f52a1")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new();

        [Input(Guid = "6a4efded-cf95-439c-bb4c-4b3c41165f4a")]
        public readonly InputSlot<float> Exposure = new();

        [Input(Guid = "52942004-3a62-4c2d-95c0-0c18e531983e")]
        public readonly InputSlot<float> WeightHue = new();

        [Input(Guid = "d044302c-d87f-4ee7-b24d-11b3508a91c1")]
        public readonly InputSlot<float> WeightSaturation = new();

        [Input(Guid = "85826060-b94d-4f65-bf35-cad8fd8eb508")]
        public readonly InputSlot<float> WeightBrightness = new();

        [Input(Guid = "91e604d7-aed0-4fb9-9690-ab60a74c4df5")]
        public readonly InputSlot<float> Amplify = new();

        [Input(Guid = "e32268a7-8527-4d0d-b9ba-11c23cb07dd2", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new();

        [Input(Guid = "749654ec-1203-4f28-be9b-e3d3e2bff9d2")]
        public readonly InputSlot<float> Choke = new InputSlot<float>();

        private enum Modes
        {
            RemoveKeyed,
            FillKeyed,
            KeyedWhiteOnBackground,
            ReturnKeyed,
        }
    }
}