using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_fd9bffd3_5c57_462f_8761_85f94c5a629b;

namespace T3.Operators.Types.Id_4499dcb1_c936_49ed_861b_2ad8ae58cb28
{
    public class DrawMeshUnlit : Instance<DrawMeshUnlit>
    {
        [Output(Guid = "0e5c4ba6-278c-4c3c-96d8-00b706c5605b")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "be057b0a-1302-4076-bde1-6ae453815642")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "5100a9db-ee56-4023-9fb0-36cbfb439734")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "922cf855-2676-4a96-9d90-622791a6a423")]
        public readonly InputSlot<int> BlendMode = new InputSlot<int>();

        [Input(Guid = "8d223463-edff-45fb-9ead-6650a911cebd")]
        public readonly InputSlot<SharpDX.Direct3D11.CullMode> Culling = new InputSlot<SharpDX.Direct3D11.CullMode>();

        [Input(Guid = "c004d3c2-de74-48ee-9504-d7de7fe1e554")]
        public readonly InputSlot<bool> EnableZTest = new InputSlot<bool>();

        [Input(Guid = "2c88e7c4-04f8-4e45-94d8-214775c5609c")]
        public readonly InputSlot<bool> EnableZWrite = new InputSlot<bool>();

        [Input(Guid = "a02180a6-7778-4fa2-9a69-228a26936755")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "a0b6601e-4fbb-4489-9e15-59e80db0d26c")]
        public readonly InputSlot<bool> UseCubeMap = new InputSlot<bool>();

        [Input(Guid = "72060e5d-090f-4c84-890a-ca9ee238fe82")]
        public readonly InputSlot<float> AlphaCutOff = new InputSlot<float>();

        [Input(Guid = "44b31261-df87-4289-bc64-db349476e418")]
        public readonly InputSlot<float> BlurLevel = new InputSlot<float>();

    }
}

