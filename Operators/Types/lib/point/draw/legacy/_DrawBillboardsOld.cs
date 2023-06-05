using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;


namespace T3.Operators.Types.Id_071c9aad_ecbf_47bf_b2f6_c43e8212d5b1
{
    public class _DrawBillboardsOld : Instance<_DrawBillboardsOld>
    {
        [Output(Guid = "b90aed8c-8c65-4dca-9ed3-fc2f08fc2cd1")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "2548a5d8-b2ab-4783-9298-c2e261d390f0")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "1a9e522a-38fb-478f-9f45-d3846cf401c2")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "0e4e5d78-8fe1-4b84-a0ab-d21f9704ccda")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "43d1169f-e6db-4229-882c-ca912e1cb6fd")]
        public readonly InputSlot<float> ScaleRandomly = new InputSlot<float>();

        [Input(Guid = "CC9CD2A7-70EC-4DBE-A151-C23EA08BCE56", MappedType = typeof(UsePointOrientationModes))]
        public readonly InputSlot<int> ApplyPointOrientation = new InputSlot<int>();


        [Input(Guid = "4e02b0c1-8a71-480b-8e9b-1a2b38641e1e")]
        public readonly InputSlot<float> Rotate = new InputSlot<float>();

        [Input(Guid = "407b79be-13da-477d-8123-90e6408de354")]
        public readonly InputSlot<float> RotateRandomly = new InputSlot<float>();

        [Input(Guid = "0555c899-3049-4d47-a2da-0febf6871754", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMod = new InputSlot<int>();

        [Input(Guid = "6176e442-a1b5-4762-be92-3f65df4ccdae")]
        public readonly InputSlot<bool> EnableDepthWrite = new InputSlot<bool>();
        
        [Input(Guid = "059554d5-bee7-4724-bf62-3d492aaae62c")]
        public readonly InputSlot<float> WSpeed = new InputSlot<float>();

        [Input(Guid = "7d0f3c5d-4860-4dde-ad33-22376520b59d")]
        public readonly InputSlot<T3.Core.DataTypes.Gradient> GradientForW = new InputSlot<T3.Core.DataTypes.Gradient>();

        [Input(Guid = "81e652c3-d12c-4b45-afd2-f6de9de20df1")]
        public readonly InputSlot<Curve> ScaleForW = new InputSlot<Curve>();

        [Input(Guid = "c82b7f84-508e-4a04-905e-39b55f75f4e6")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "957c9ece-5e66-4aee-b0c6-03421cbaf075")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture_ = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "2c2337ce-9890-4cb3-b85b-5d1edce66671")]
        public readonly InputSlot<float> AlphaCut = new InputSlot<float>();

        private enum UsePointOrientationModes
        {
            Ignore,
            AsRotation,
            AsColor,
        }
    }
}

