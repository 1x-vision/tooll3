using System;
using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using Vector2 = System.Numerics.Vector2;

namespace T3.Operators.Types.Id_eff2ffff_dc39_4b90_9b1c_3c0a9a0108c6
{
    public class MouseInput : Instance<MouseInput>
    {
        [Output(Guid = "CDC87CE1-FAB8-4B96-9137-9965E064BFA3", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector2> Position = new Slot<Vector2>();

        [Output(Guid = "78CAABCF-9C3B-4E50-9D80-BDCBABAEB003", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> IsLeftButtonDown = new Slot<bool>();

        public MouseInput()
        {
            Position.UpdateAction = Update;
            IsLeftButtonDown.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var mode = OutputMode.GetEnumValue<OutputModes>(context);
            var scale = Scale.GetValue(context);
            var aspectRatio = (float)context.RequestedResolution.Width / context.RequestedResolution.Height;

            var lastPosition = Core.IO.MouseInput.LastPosition;
            
            switch (mode)
            {
                case OutputModes.Normalized:
                    Position.Value = lastPosition;
                    break;
                case OutputModes.SignedPosition:
                    Position.Value = (lastPosition - new Vector2(0.5f, 0.5f)) * new Vector2(aspectRatio, -1) * scale;
                    break;
                case OutputModes.OnWorldXYPlane:
                    var clipSpaceToWorld = ComposeClipSpaceToWorld(context);
                    var cameraToWorld = context.WorldToCamera;
                    cameraToWorld.Invert();
                    
                    var posInClip =  (lastPosition - new Vector2(0.5f, 0.5f)) * new Vector2(2, -2);
                    var posInWorld = Vector3.Transform(new Vector3(posInClip.X, posInClip.Y, 0f), clipSpaceToWorld);
                    var targetInWorld = Vector3.Transform(new Vector3(posInClip.X, posInClip.Y, 1f), clipSpaceToWorld);
                    var ray = new Ray(posInWorld, targetInWorld - posInWorld);
                    var xyPlane = PlaneExtensions.PlaneFromPointAndNormal(Vector3.Zero, Vector3.UnitZ);
                    if (xyPlane.Intersects(in ray, out Vector3 p))
                    {
                        Position.Value = new Vector2(p.X, p.Y) ;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }

            IsLeftButtonDown.Value = Core.IO.MouseInput.IsLeftButtonDown;
        }

        private static Matrix4x4 ComposeClipSpaceToWorld(EvaluationContext context)
        {
            var clipSpaceToCamera = context.CameraToClipSpace;
            clipSpaceToCamera.Invert();
            var cameraToWorld = context.WorldToCamera;
            cameraToWorld.Invert();
            var worldToObject = context.ObjectToWorld;
            worldToObject.Invert();
            var clipSpaceToWorld = Matrix4x4.Multiply(clipSpaceToCamera, cameraToWorld);
            return clipSpaceToWorld;
        }

        [Input(Guid = "49775CC2-35B7-4C9F-A502-59FE8FBBE2A7")]
        public readonly InputSlot<bool> DoUpdate = new InputSlot<bool>();

        [Input(Guid = "1327525C-716C-43E4-A5D1-58CF35440462", MappedType = typeof(OutputModes))]
        public readonly InputSlot<int> OutputMode = new();

        [Input(Guid = "6B1E81BD-2430-4439-AD0C-859B2433C38B")]
        public readonly InputSlot<float> Scale = new();

        private enum OutputModes
        {
            Normalized,
            SignedPosition,
            OnWorldXYPlane,
        }
    }
}