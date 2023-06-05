using System;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d002bd90_5921_48b0_a940_a8d0c779f674
{
    public class Rings : Instance<Rings>
    {
        [Output(Guid = "ee4053c2-10a4-4cf5-83ea-be4a8e12b80f")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "a22945ec-c576-491a-83d0-ffea9dd0cdc4")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "2323ca7b-af43-4630-af27-78c9ad4c9dcc")]
        public readonly InputSlot<System.Numerics.Vector4> Fill = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "6a169837-8d59-4874-82e5-78de530c58ce")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "57adbae0-c35e-42b2-857e-28cbb6e9dbc7")]
        public readonly InputSlot<System.Numerics.Vector4> Highlight = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "eeabbad9-a4a5-4ee6-9e53-c0012e339b77")]
        public readonly InputSlot<System.Numerics.Vector2> Radius = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "b8ea3c63-d55d-47d9-97f9-bdbf7cec5e05")]
        public readonly InputSlot<System.Numerics.Vector2> Position = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "f74b3fd6-eee4-4218-9771-3f04c38e4ff1")]
        public readonly InputSlot<float> Count = new InputSlot<float>();

        [Input(Guid = "fc112417-fb8d-44af-a4b8-fced5d8ef71d")]
        public readonly InputSlot<float> Feather = new InputSlot<float>();

        [Input(Guid = "65c41729-794a-4d36-8a4e-e111866ca2f2")]
        public readonly InputSlot<float> Rotate = new InputSlot<float>();

        [Input(Guid = "dc5386d2-cd0c-449e-9dfa-4546a25c8e0e")]
        public readonly InputSlot<float> Offset = new InputSlot<float>();

        [Input(Guid = "7c751a95-2bc5-4bef-b729-fc133af3132f")]
        public readonly InputSlot<System.Numerics.Vector2> _Segments = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "0556c0a1-bdbf-4a20-a9a7-8ab7effb087f")]
        public readonly InputSlot<System.Numerics.Vector2> _Twist = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "1dc466f8-3baa-46d9-a05c-831de0b4590a")]
        public readonly InputSlot<System.Numerics.Vector2> _Thickness = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "d5d2bf78-af34-4202-8d2f-7f96e05a3a61")]
        public readonly InputSlot<System.Numerics.Vector2> _Ratio = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "a7eb1998-baba-445a-abcf-c1befd205c3c")]
        public readonly InputSlot<float> _FillRatio = new InputSlot<float>();

        [Input(Guid = "49851fde-715d-4409-aa3d-cd31f71457ef")]
        public readonly InputSlot<float> _HighlightRatio = new InputSlot<float>();

        [Input(Guid = "f4b75f6f-4fb9-4c05-894a-24a274d4541f")]
        public readonly InputSlot<int> HighlightSeed = new InputSlot<int>();

        [Input(Guid = "b56a84aa-7e66-442a-b063-641bc91cc37a")]
        public readonly InputSlot<float> Distort = new InputSlot<float>();

        [Input(Guid = "34474902-0c2b-44b2-8dd7-09152f8c8b97")]
        public readonly InputSlot<float> Constrast = new InputSlot<float>();

        [Input(Guid = "67cdd8b3-8ee6-47a8-8b43-8b6ff1dc8b4b")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "a46daf9d-ebb3-478f-8462-c183ea2ef15e")]
        public readonly InputSlot<int> Seed = new InputSlot<int>();

        [Input(Guid = "4084b10d-d0be-4a88-a713-29107193f694",MappedType = typeof(SharedEnums.RgbBlendModes))]
        public readonly InputSlot<int> BlendMode = new InputSlot<int>();
    }
}

