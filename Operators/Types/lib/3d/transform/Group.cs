using System;
using System.Numerics;
using SharpDX;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Operators.Types.Id_a3f64d34_1fab_4230_86b3_1c3deba3f90b
{
    public class Group : Instance<Group>
,ITransformable
    {
        [Output(Guid = "977ca2f4-cddb-4b9a-82b2-ff66453bbf9b")]
        public readonly Slot<Command> Output = new Slot<Command>();
        
        IInputSlot ITransformable.TranslationInput => Translation;
        IInputSlot ITransformable.RotationInput => Rotation;
        IInputSlot ITransformable.ScaleInput => Scale;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        public Group()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            TransformCallback?.Invoke(this, context); // this this is stupid stupid

            // Build and set transform matrix
            var s = Scale.GetValue(context) * UniformScale.GetValue(context);
            var r = Rotation.GetValue(context);
            var yaw = MathUtil.DegreesToRadians(r.Y);
            var pitch = MathUtil.DegreesToRadians(r.X);
            var roll = MathUtil.DegreesToRadians(r.Z);
            var t = Translation.GetValue(context);
            var objectToParentObject = Math3DUtils.CreateTransformationMatrix(scalingCenter: Vector3.Zero, scalingRotation: Quaternion.Identity, scaling: new Vector3(s.X, s.Y, s.Z), rotationCenter: Vector3.Zero,
                                                             rotation: Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll), translation: new Vector3(t.X, t.Y, t.Z));

            var forceColorUpdate = ForceColorUpdate.GetValue(context);
            var previousColor = context.ForegroundColor;
            var color = Color.GetValue(context);
            
            //color.W *= previousColor.W;     // TODO: this should be probably be controlled by an input parameter
            context.ForegroundColor *= color;
            
            var previousWorldTobject = context.ObjectToWorld;
            context.ObjectToWorld = Matrix4x4.Multiply(objectToParentObject, context.ObjectToWorld);
            
            var commands = Commands.CollectedInputs;
            if (IsEnabled.GetValue(context))
            {
                foreach (var t1 in commands)
                {
                    // Do preparation if needed
                    t1.Value?.PrepareAction?.Invoke(context);

                    if (forceColorUpdate)
                    {
                        DirtyFlag.InvalidationRefFrame++;
                        t1.Invalidate();
                    }
                    
                    // Execute commands
                    t1.GetValue(context);

                    // Cleanup after usage
                    t1.Value?.RestoreAction?.Invoke(context);
                }
            }
            
            Commands.DirtyFlag.Clear();
            
            context.ForegroundColor = previousColor;
            context.ObjectToWorld = previousWorldTobject;
        }

        [Input(Guid = "9E961F73-1EE7-4369-9AC7-5C653E570B6F")]
        public readonly MultiInputSlot<Command> Commands = new();
        
        [Input(Guid = "9e24c5d1-191a-4c1a-b88e-03df826fffc0")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new();
        
        [Input(Guid = "4e15eb7a-3872-4f26-93e3-1dd38bce01a5")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new();
        
        [Input(Guid = "7db56b09-350e-4831-9620-fe68e0617b86")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new();

        [Input(Guid = "25eb87f8-43b6-46fa-8066-3fdc1efb5e01")]
        public readonly InputSlot<float> UniformScale = new();
        
        [Input(Guid = "83D80B87-DF2E-4A7E-8BB3-6D5F041A60E4")]
        public readonly InputSlot<bool> IsEnabled = new();

        [Input(Guid = "996BD2D7-3741-4ADE-B1B6-18EB3D884081")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "35A18838-B095-431F-A3AF-2DBA81DCC16F")]
        public readonly InputSlot<bool> ForceColorUpdate = new();

    }
}