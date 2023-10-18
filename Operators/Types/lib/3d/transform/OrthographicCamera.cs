using System;
using SharpDX;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using T3.Operators.Utils;

namespace T3.Operators.Types.Id_954af16f_b37b_4e64_a965_4bec02b9179e
{
    public class OrthographicCamera : Instance<OrthographicCamera>, ICamera, ICameraPropertiesProvider
    {
        [Output(Guid = "93241f33-8a3e-4bba-8852-ca5d4d4523aa", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();
        
        [Output(Guid = "761245E2-AC0B-435A-841E-7C9EDC804606")]
        public readonly Slot<Object> Reference = new();

        public OrthographicCamera()
        {
            Output.UpdateAction = Update;
            Reference.Value = this;
        }

        private void Update(EvaluationContext context)
        {
            System.Numerics.Vector2 size = Size.GetValue(context);
            System.Numerics.Vector2 clip = NearFarClip.GetValue(context);
            CameraToClipSpace = Matrix.OrthoRH(size.X, size.Y, clip.X, clip.Y);
            LastObjectToWorld = context.ObjectToWorld;
            
            var pos = Position.GetValue(context);
            Vector3 eye = new Vector3(pos.X, pos.Y, pos.Z);
            var t = Target.GetValue(context);
            Vector3 target = new Vector3(t.X, t.Y, t.Z);
            var u = Up.GetValue(context);
            Vector3 up = new Vector3(u.X, u.Y, u.Z);
            WorldToCamera = Matrix.LookAtRH(eye, target, up);

            var prevCameraToClipSpace = context.CameraToClipSpace;
            context.CameraToClipSpace = CameraToClipSpace;

            var prevWorldToCamera = context.WorldToCamera;
            context.WorldToCamera = WorldToCamera;
            Command.GetValue(context);

            context.CameraToClipSpace = prevCameraToClipSpace;
            context.WorldToCamera = prevWorldToCamera;
        }

        [Input(Guid = "4f5832eb-23a0-4cdf-8144-3537578e3e26")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();

        [Input(Guid = "a0a28003-d6b5-4af5-9444-acf7af18ab4e")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "1399ce7f-9352-4976-b02e-7e7102b14db5")]
        public readonly InputSlot<System.Numerics.Vector3> Target = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "7b181495-ebd6-48c1-a866-b7b8337ef10d")]
        public readonly InputSlot<System.Numerics.Vector3> Up = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "e4761300-f383-4d4c-9aa0-7d7ab7997973")]
        public readonly InputSlot<float> Roll = new InputSlot<float>();

        [Input(Guid = "9326957b-bc25-4f89-a833-9b8bb415d8ef")]
        public readonly InputSlot<System.Numerics.Vector2> NearFarClip = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "8042eb60-ca86-42b3-a338-d733c3cbb1fb")]
        public readonly InputSlot<System.Numerics.Vector2> Size = new InputSlot<System.Numerics.Vector2>();

        // Implement ICamera 
        public System.Numerics.Vector3 CameraPosition
        {
            get { return Position.Value;} 
            set { Animator.UpdateVector3InputValue(Position, value); }
        }

        public System.Numerics.Vector3 CameraTarget
        {
            get { return Target.Value;} 
            set { Animator.UpdateVector3InputValue(Target, value); }
        }

        public float CameraRoll
        {
            get { return Roll.Value;} 
            set { Animator.UpdateFloatInputValue(Roll, value); }

        }

        public CameraDefinition CameraDefinition => new();  // Not implemented
        
        public Matrix WorldToCamera { get; set; }
        public Matrix LastObjectToWorld { get; set; }
        public Matrix CameraToClipSpace { get; set; }
    }
}