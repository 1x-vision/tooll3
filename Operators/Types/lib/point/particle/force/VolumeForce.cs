using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fbe1a703_f372_4236_9f20_5d0b69183843
{
    public class VolumeForce : Instance<VolumeForce>
,ITransformable
    {
        [Output(Guid = "13B7DBBC-D418-4BD5-A8A1-182DF2071A25")]
        public readonly Slot<T3.Core.DataTypes.ParticleSystem> ParticleSystem = new();

        // public SurfaceForce()
        // {
        //     ParticleSystem.TransformableOp = this;
        // }

        IInputSlot ITransformable.TranslationInput => VolumeCenter;
        IInputSlot ITransformable.RotationInput => VolumeRotate;
        IInputSlot ITransformable.ScaleInput => VolumeScale;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "e3485fea-3a75-47f9-9a7d-ea69f4feb5f6")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeCenter = new();

        [Input(Guid = "a75686be-909f-42fd-88f1-005e2fcd9f70")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeStretch = new();

        [Input(Guid = "60eb8c10-93fa-487b-9eda-1767b485bb21")]
        public readonly InputSlot<float> VolumeScale = new();

        [Input(Guid = "721c500b-ae7c-4249-a374-1bcf6ae13abd")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeRotate = new();

        [Input(Guid = "86c81e40-79e8-4699-a7fe-581f0b09d266", MappedType = typeof(VolumeShapes))]
        public readonly InputSlot<int> VolumeShape = new();

        [Input(Guid = "ceb7008d-c536-4d87-b3f8-d5ba9fe29eed")]
        public readonly InputSlot<float> Bounciness = new();

        [Input(Guid = "1097b588-db96-49c1-b6e6-78b5cb0f5f73")]
        public readonly InputSlot<float> RandomizeBounce = new();

        [Input(Guid = "685cd07f-d2ae-4e07-a495-caeefb4c6064")]
        public readonly InputSlot<float> RandomizeReflection = new();

        [Input(Guid = "3b975bd9-b8aa-4a41-97fc-ccd3b5e89e63")]
        public readonly InputSlot<float> Attraction = new();

        [Input(Guid = "7c6f58e7-27fc-4271-adef-248cffe5a8b7")]
        public readonly InputSlot<float> Repulsion = new();

        [Input(Guid = "0db5c531-721d-403e-9154-e31f6be20ec6")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> GizmoVisibility = new();

        [Input(Guid = "2ab85ac3-819d-4e3e-891a-7f27af627fdf")]
        public readonly InputSlot<bool> InvertVolume = new();

        [Input(Guid = "b7a82887-9b8a-4999-8970-515fe199724a")]
        public readonly InputSlot<float> AttractionDecay = new();


        
        private enum VolumeShapes
        {
            Sphere,
            Box,
            Plane,
            Cylinder
        }
    }
}

