﻿using System;
using System.Numerics;
using SharpDX;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Quaternion = System.Numerics.Quaternion;

namespace T3.Operators.Types.Id_284d2183_197d_47fd_b130_873cced78b1c
{
    public class Transform : Instance<Transform>, ITransformable
    {
        [Output(Guid = "2D329133-29B9-4F56-B5A6-5FF7D83638FA")]
        public readonly Slot<Command> Output = new Slot<Command>();
        
        IInputSlot ITransformable.TranslationInput => Translation;
        IInputSlot ITransformable.RotationInput => Rotation;
        IInputSlot ITransformable.ScaleInput => Scale;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        public Transform()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
           
            TransformCallback?.Invoke(this, context); // this this is stupid stupid

            var pivot = Pivot.GetValue(context);
            var s = Scale.GetValue(context) * UniformScale.GetValue(context);
            var r = Rotation.GetValue(context);
            float yaw = MathUtil.DegreesToRadians(r.Y);
            float pitch = MathUtil.DegreesToRadians(r.X);
            float roll = MathUtil.DegreesToRadians(r.Z);
            var t = Translation.GetValue(context);
            var objectToParentObject = Math3DUtils.CreateTransformationMatrix(
                                                             scalingCenter: pivot, 
                                                             scalingRotation: Quaternion.Identity, 
                                                             scaling: s, 
                                                             rotationCenter: pivot,
                                                             rotation: Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll), 
                                                             translation: t);
            
            var previousWorldTobject = context.ObjectToWorld;
            context.ObjectToWorld = Matrix4x4.Multiply(objectToParentObject, context.ObjectToWorld);
            Command.GetValue(context);
            context.ObjectToWorld = previousWorldTobject;
        }

        [Input(Guid = "DCD066CE-AC44-4E76-85B3-78821245D9DC")]
        public readonly InputSlot<Command> Command = new();
        
        [Input(Guid = "B4A8C16D-5A0F-4867-AE03-92A675ABE709")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new();
        
        [Input(Guid = "712ADB09-D249-4C91-86DB-3FEDF6B05971")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new();
        
        [Input(Guid = "DA4CD6C8-2307-45DA-9258-49C578025AA8")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new();

        [Input(Guid = "A7B1E667-BCE3-4E76-A5B1-0955C118D0FC")]
        public readonly InputSlot<float> UniformScale = new();

        [Input(Guid = "95C8BEF2-504C-42A1-93BA-DC7E38C0DD49")]
        public readonly InputSlot<System.Numerics.Vector3> Pivot = new();
    }
}