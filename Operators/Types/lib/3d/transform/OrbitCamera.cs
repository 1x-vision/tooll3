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
using T3.Operators.Utils;
using Unsplasharp.Models;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

// ReSharper disable SuggestVarOrType_SimpleTypes

namespace T3.Operators.Types.Id_6415ed0e_3692_45e2_8e70_fe0cf4d29ebc
{
    public class OrbitCamera : Instance<OrbitCamera>
,ICameraPropertiesProvider,ICamera
    {
        [Output(Guid = "14a63b62-5fbb-4f82-8cf3-d0faf279eff8", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Output(Guid = "451245E2-AC0B-435A-841E-7C9EDC804606", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Object> Reference = new Slot<Object>();        
        
        public OrbitCamera()
        {
            Output.UpdateAction = Update;
            Reference.UpdateAction = Update;
            Reference.Value = this;
        }

        private void Update(EvaluationContext context)
        {
            LastObjectToWorld = context.ObjectToWorld;
            
            
            float fov = MathUtil.DegreesToRadians(Fov.GetValue(context));
            float aspectRatio = AspectRatio.GetValue(context);
            if (aspectRatio < 0.0001f)
            {
                aspectRatio = (float)context.RequestedResolution.Width / context.RequestedResolution.Height;
            }
            System.Numerics.Vector2 clip = NearFarClip.GetValue(context);
            
            CameraToClipSpace = Math3DUtils.PerspectiveFovRH(fov, aspectRatio, clip.X, clip.Y);

            Vector3 p = new Vector3(0,0, Radius.GetValue(context));
            var seed = Seed.GetValue(context);
            var wobbleSpeed = WobbleSpeed.GetValue(context);
            var wobbleComplexity = (int)MathUtils.Clamp(WobbleComplexity.GetValue(context),1,8);

            var rotOffset =  RotationOffset.GetValue(context);

            // Orbit rotation
            System.Numerics.Vector3 t = Center.GetValue(context);
            Vector3 target = new Vector3(t.X, t.Y, t.Z);
            
            var rot = Matrix4x4.CreateFromYawPitchRoll(
                                                  ComputeAngle(SpinAngleAndWobble,1) 
                                                  + MathUtil.DegreesToRadians((float)((SpinRate.GetValue(context) * context.LocalFxTime) * 360+ SpinOffset.GetValue(context)  
                                                                                      + MathUtils.PerlinNoise(0, 1, 6, seed) * 360 ) ), 
                                                  -ComputeAngle(OrbitAngleAndWobble, 2), 
                                                  0);
            var p2 = Vector3.Transform(p, rot);
            var eye = new Vector3(p2.X, p2.Y, p2.Z);

            // View rotation
            var viewDirection = target - eye;
            
            var rotateAim = Matrix4x4.CreateFromYawPitchRoll(
                                                  ComputeAngle(AimYawAngleAndWobble,3) + rotOffset.X * MathUtils.ToRad,
                                                  ComputeAngle(AimPitchAngleAndWobble,4) + rotOffset.Y * MathUtils.ToRad,
                                                  0);

            
            var adjustedViewDirection = Vector3.TransformNormal(viewDirection, rotateAim);
            adjustedViewDirection.Normalize();
            target = eye + adjustedViewDirection;

            // Computing matrix
            var u = Up.GetValue(context);
            Vector3 up = new Vector3(u.X, u.Y, u.Z);

            var roll = ComputeAngle(AimRollAngleAndWobble, 5);
            var rotateAroundViewDirection = Matrix4x4.CreateFromAxisAngle(adjustedViewDirection, (roll + rotOffset.Z) * MathUtils.ToRad);
            up = Vector3.TransformNormal(up, rotateAroundViewDirection);
            up.Normalize();

            WorldToCamera = Math3DUtils.LookAtRH(eye, target, up);
                        
            if (context.BypassCameras)
            {
                Command.GetValue(context);
                return;
            }
            
            // Set properties and evaluate sub tree
            var prevCameraToClipSpace = context.CameraToClipSpace;
            var prevWorldToCamera = context.WorldToCamera;
            
            context.CameraToClipSpace = CameraToClipSpace;
            context.WorldToCamera = WorldToCamera;

            CameraPosition = eye;
            CameraTarget = (eye + adjustedViewDirection);
            
            Command.GetValue(context);
            
            context.CameraToClipSpace = prevCameraToClipSpace;
            context.WorldToCamera = prevWorldToCamera;

            
            float ComputeAngle(Slot<Vector2> angleAndWobbleInput, int seedIndex)
            {
                var angleAndWobble = angleAndWobbleInput.GetValue(context);
                var wobble=  Math.Abs(angleAndWobble.Y) < 0.001f 
                                 ? 0 
                                 : (MathUtils.PerlinNoise((float)context.LocalFxTime * wobbleSpeed, 
                                                         1, 
                                                         wobbleComplexity, 
                                                         seed+ 123* seedIndex) 
                                    -0.5f) *2 * angleAndWobble.Y ;
                return MathUtil.DegreesToRadians(angleAndWobble.X + wobble);
            }
        }

        public Matrix4x4 CameraToClipSpace { get; set; }
        public System.Numerics.Vector3 CameraPosition { get; set; }
        public System.Numerics.Vector3 CameraTarget { get; set; }
        public float CameraRoll { get; set; }
        public Matrix4x4 WorldToCamera { get; set; }
        public Matrix4x4 LastObjectToWorld { get; set; }
        
        
        [Input(Guid = "33752356-8348-4938-8f73-6257e6bb1c1f")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();
        
        [Input(Guid = "ACF14901-3373-4B0C-8567-03EA0051A21F")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "DD92FB0A-4B3E-4492-BF59-437D914A1AD3")]
        public readonly InputSlot<float> Radius = new InputSlot<float>();
        
        [Input(Guid = "DF65E717-E2FD-4E4F-9E41-D6BCD3FE67F1")]
        public readonly InputSlot<float> SpinRate = new InputSlot<float>();
        
        [Input(Guid = "8C4DAB88-68CB-40CC-B576-CA3F3EA8461F")]
        public readonly InputSlot<float> SpinOffset = new InputSlot<float>();

        [Input(Guid = "8B75047F-03B7-4619-8869-2906E66731D1")]
        public readonly InputSlot<System.Numerics.Vector2> OrbitAngleAndWobble = new InputSlot<System.Numerics.Vector2>();
        
        [Input(Guid = "7412E22C-1F15-4471-883B-4FCD792146F7")]
        public readonly InputSlot<System.Numerics.Vector2> SpinAngleAndWobble = new InputSlot<System.Numerics.Vector2>();

        
        [Input(Guid = "4D2D2D2D-00BD-4DF9-B209-62F0C7926C38")]
        public readonly InputSlot<System.Numerics.Vector2> AimPitchAngleAndWobble = new InputSlot<System.Numerics.Vector2>();
        
        [Input(Guid = "066CD0E7-DE72-4E04-BF13-686CCC301C5A")]
        public readonly InputSlot<System.Numerics.Vector2> AimYawAngleAndWobble = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "1AF76B2E-CFFE-4E3F-8793-C7A59D00430B")]
        public readonly InputSlot<System.Numerics.Vector2> AimRollAngleAndWobble = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "C81B91C6-2D06-4E3E-97BD-01D60F5F0F7D")]
        public readonly InputSlot<System.Numerics.Vector3> RotationOffset = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "B6BF6FE1-6733-46C0-A274-FAB2A950F606")]
        public readonly InputSlot<float> WobbleSpeed = new InputSlot<float>();

        [Input(Guid = "0AD10E15-06EF-4DDE-8EB3-ED7FE989C88E")]
        public readonly InputSlot<int> WobbleComplexity = new InputSlot<int>();
        
        [Input(Guid = "DD81B2AA-3252-4130-8DEF-A5B399D3E283")]
        public readonly InputSlot<int> Seed = new InputSlot<int>();
        
        [Input(Guid = "f51b38d7-2380-457f-897d-2429b2ad6ac3")]
        public readonly InputSlot<System.Numerics.Vector3> Up = new InputSlot<System.Numerics.Vector3>();
        
        [Input(Guid = "bd1bc8a5-72ce-42b0-8914-4f6e124a18ae")]
        public readonly InputSlot<float> AspectRatio = new InputSlot<float>();
        
        [Input(Guid = "353e2f08-a55f-48ce-ae65-53d5d081b6f0")]
        public readonly InputSlot<System.Numerics.Vector2> NearFarClip = new InputSlot<System.Numerics.Vector2>();
        
        [Input(Guid = "21f595ad-0808-48f1-bdd8-118d1527944c")]
        public readonly InputSlot<float> Fov = new InputSlot<float>();

    }
}