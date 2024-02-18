using System;
using System.Numerics;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using T3.Core.Utils.Geometry;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace T3.Operators.Types.Id_e07550cf_033a_443d_b6f3_73eb71c72d9d
{
    public class SpreadLayout : Instance<SpreadLayout>
,ITransformable
    {
        [Output(Guid = "60c25429-be91-4552-b1fe-b08479793abe")]
        public readonly Slot<Command> Output = new();
        
        IInputSlot ITransformable.TranslationInput => Translation;
        IInputSlot ITransformable.RotationInput => Rotation;
        IInputSlot ITransformable.ScaleInput => Scale;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        public SpreadLayout()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            TransformCallback?.Invoke(this, context); // this this is stupid stupid

            var pivot = Pivot.GetValue(context);
            
            var spread = Spread.GetValue(context);
            
            var commands = Commands.CollectedInputs;
            var isEnabled = IsEnabled.GetValue(context);
            
            var s = Scale.GetValue(context) * UniformScale.GetValue(context);
            var r = Rotation.GetValue(context);
            var yaw = r.Y.ToRadians();
            var pitch = r.X.ToRadians();
            var roll = r.Z.ToRadians();
            var t = Translation.GetValue(context);
            
            if (isEnabled && commands != null && commands.Count > 0)
            {
                if(commands.Count == 1)
                    spread = System.Numerics.Vector3.Zero;
                

                var count = commands.Count;
                //var spreadDelta = spread / (count); 
                
                var previousColor = context.ForegroundColor;
                var previousWorldTobject = context.ObjectToWorld;

                var originalObjectToWorld = context.ObjectToWorld;

                for (var spreadIndex = 0; spreadIndex < commands.Count; spreadIndex++)
                {
                    var t1 = commands[spreadIndex];
                    
                    var f =  0.5f - ((float)spreadIndex / (count-1) - 0.5f) - pivot;
                    var tSpreaded = t - spread * f;  

                    // Build and set transform matrix
                    var objectToParentObject
                        = GraphicsMath.CreateTransformationMatrix(scalingCenter: Vector3.Zero,
                                                scalingRotation: Quaternion.Identity,
                                                scaling: s,
                                                rotationCenter: Vector3.Zero,
                                                rotation: Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll),
                                                translation: tSpreaded);

                    var forceColorUpdate = ForceColorUpdate.GetValue(context);
                    var color = Color.GetValue(context);

                    //color.W *= previousColor.W;     // TODO: this should be probably be controlled by an input parameter
                    context.ForegroundColor *= color;

                    context.ObjectToWorld = Matrix4x4.Multiply(objectToParentObject, originalObjectToWorld);

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

                context.ForegroundColor = previousColor;
                context.ObjectToWorld = previousWorldTobject;
            }
            
            Commands.DirtyFlag.Clear();
            
        }

        [Input(Guid = "2f112e29-beb3-4b78-a9e6-5d4ed55b49c7")]
        public readonly MultiInputSlot<Command> Commands = new();
        
        [Input(Guid = "A46DBA34-0A69-4A9E-8E76-5DC01054FBB4")]
        public readonly InputSlot<System.Numerics.Vector3> Spread = new();

        [Input(Guid = "2d59d002-838e-4150-ae8f-ff97bb19bd78")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new();
        
        [Input(Guid = "2dae39aa-141c-4fd2-8367-4895df85cceb")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new();
        
        [Input(Guid = "d730dc2f-0cd4-46eb-b917-ae886a319eeb")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new();

        [Input(Guid = "b89c93b7-2051-4458-9a86-fe51ba2c15d9")]
        public readonly InputSlot<float> UniformScale = new();

        [Input(Guid = "5F2C1B38-4B0C-45E4-810F-8F126084B285")]
        public readonly InputSlot<float> Pivot = new();
        
        [Input(Guid = "d0e0058c-5d46-41f4-a280-42befd5a5570")]
        public readonly InputSlot<bool> IsEnabled = new();

        [Input(Guid = "84c700dc-17da-4e4b-b636-dbb040bceefa")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "938e7f13-c78f-4474-abe1-38e413fb74e6")]
        public readonly InputSlot<bool> ForceColorUpdate = new();

    }
}