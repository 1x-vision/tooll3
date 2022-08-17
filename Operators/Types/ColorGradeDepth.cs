using System.Numerics;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a1e3b8d3_3888_4e18_80e4_f6c65a136e75
{
    public class ColorGradeDepth : Instance<ColorGradeDepth>
    {
        [Output(Guid = "9189d238-5196-41fb-8dc0-b6c4befe71a8")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();

        [Input(Guid = "81749c4d-a01d-4279-86dd-fec14e265301")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture2d = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "8f850308-21f4-41f4-865e-1a5f2bddb01e")]
        public readonly InputSlot<float> PreSaturate = new InputSlot<float>();

        [Input(Guid = "2f41d547-f23b-448c-b19d-c95cf8d3a3ea")]
        public readonly InputSlot<System.Numerics.Vector4> Gain = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "6a4eb09f-070a-4fd2-82f5-677b7cafb1dc")]
        public readonly InputSlot<System.Numerics.Vector4> Gamma = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "bfb058cb-3611-441a-840f-0a1c4efd4de1")]
        public readonly InputSlot<System.Numerics.Vector4> Lift = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "2e91b9e5-67eb-424f-be6f-3ed6ab77f3b8")]
        public readonly InputSlot<System.Numerics.Vector4> VignetteColor = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "686a23e0-bd6a-4662-85e8-e910315a4e84")]
        public readonly InputSlot<float> VignetteRadius = new InputSlot<float>();

        [Input(Guid = "11e8c433-deab-4979-9054-90dc373cb5f9")]
        public readonly InputSlot<float> VignetteFeather = new InputSlot<float>();

        [Input(Guid = "c3693fbf-83d1-4780-98a0-302e7ecf7ca2")]
        public readonly InputSlot<System.Numerics.Vector2> VignetteCenter = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "c0f721fb-f39a-4147-a288-db8546c240fd")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> DepthBuffer = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "92912c42-9928-495e-a8c7-c752e79eeba6")]
        public readonly InputSlot<T3.Core.DataTypes.Gradient> Gradient = new InputSlot<T3.Core.DataTypes.Gradient>();

        [Input(Guid = "c36d2140-8f34-4172-ac77-8e48c40c01df")]
        public readonly InputSlot<System.Numerics.Vector2> GradientDepthRange = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "e104b69f-c9ca-4125-b99f-f85f48409aa0")]
        public readonly InputSlot<System.Numerics.Vector2> CamNearFarClip = new InputSlot<System.Numerics.Vector2>();

    }
}