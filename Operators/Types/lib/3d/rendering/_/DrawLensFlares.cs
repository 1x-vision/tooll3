using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_de173d83_66e6_4363_a478_4336100c2dbd
{
    public class DrawLensFlares : Instance<DrawLensFlares>
    {
        [Output(Guid = "0e87c2a6-3ab8-41f5-ad78-ccc8e6df67e8")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "3c028f98-5130-4cb0-b09b-eca193a8382b")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "b84b107c-5179-48b2-ab2e-eb0b847fbb1e")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "ce9153c5-d9f6-45b7-8132-180a36dce18c", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMod = new InputSlot<int>();

        [Input(Guid = "01ba26e4-46a2-453e-a29f-3ae0ddf463a6")]
        public readonly InputSlot<bool> EnableDepthWrite = new InputSlot<bool>();

        [Input(Guid = "2366c1cb-3d0d-4b76-8d21-648c05fcc996")]
        public readonly InputSlot<T3.Core.DataTypes.Vector.Int2> TextureCells = new InputSlot<T3.Core.DataTypes.Vector.Int2>();

        [Input(Guid = "6800d879-88fb-4e66-97dc-f0e63e0bdd17")]
        public readonly MultiInputSlot<T3.Core.DataTypes.StructuredList> LenseFlareDefinitions = new MultiInputSlot<T3.Core.DataTypes.StructuredList>();

        [Input(Guid = "8e1ceafe-4697-45a2-9464-9f0797cb6de5")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture_ = new InputSlot<SharpDX.Direct3D11.Texture2D>();
    }
}

