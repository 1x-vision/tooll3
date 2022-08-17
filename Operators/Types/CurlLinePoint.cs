using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_97ded8ca_bdcf_4cb8_a791_a05ba4393888
{
    public class CurlLinePoint : Instance<CurlLinePoint>
,ITransformable
    {

        
        
        [Output(Guid = "3df6761a-c023-4a89-8d31-b56e87849bcd")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> Output = new TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews>();

        public CurlLinePoint()
        {
            Output.TransformableOp = this;
        }        
        IInputSlot ITransformable.TranslationInput => Translation;
        IInputSlot ITransformable.RotationInput => Rotation;
        IInputSlot ITransformable.ScaleInput => Scale;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "c13a0f89-4f34-40fd-9d24-18f6b5e04890")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "d968f497-0428-42eb-8e97-f4ebb57c8dcf")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "7afcb0a8-65e6-4744-acd9-09e320243af8")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "6032da21-729a-48a4-be56-ac257dcebbd9")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "17aa5fac-eb41-4f64-848b-09cb3a32c0e1")]
        public readonly InputSlot<float> PhaseA = new InputSlot<float>();

        [Input(Guid = "171d34d9-09b1-4ee1-8ed7-d18e13127286")]
        public readonly InputSlot<float> B = new InputSlot<float>();

        [Input(Guid = "274a70d4-c6a7-493a-905e-4745c9fb566f")]
        public readonly InputSlot<float> C = new InputSlot<float>();

        [Input(Guid = "9d0f4e8a-9dc0-47ba-8961-0d70833bed19")]
        public readonly InputSlot<float> MagnitudeA = new InputSlot<float>();

        [Input(Guid = "4bfe5723-8b7d-4e26-b965-65c370dd49b6")]
        public readonly InputSlot<float> FreqA = new InputSlot<float>();

        [Input(Guid = "40e3f6f5-73e0-48b8-ac70-dbf7d125a2b9")]
        public readonly InputSlot<int> LineLength = new InputSlot<int>();

        [Input(Guid = "8460bcc9-0d50-4773-afe3-2d69fd2384ea")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> FxCurveTexture = new InputSlot<SharpDX.Direct3D11.Texture2D>();
        
        
        
        private enum Spaces
        {
            PointSpace,
            ObjectSpace,
        }
        
        
        
        
        
    }
}

