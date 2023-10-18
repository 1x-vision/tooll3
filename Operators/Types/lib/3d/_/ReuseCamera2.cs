using System;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1de05a51_4a22_44cd_a584_6f1ae1c0e8d1
{
    public class ReuseCamera2 : Instance<ReuseCamera2>
    {
        [Output(Guid = "04c676d4-012b-44ef-b3b2-6b7d7f09d490")]
        public readonly Slot<Command> Output = new();
        
        public ReuseCamera2()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var obj = CameraReference.GetValue(context);
            if (obj == null)
            {
                Log.Warning("Camera reference is undefined", this);
                return;
            }

            if (obj is not ICamera camera)
            {
                Log.Warning("Can't GetCamProperties from invalid reference type", this);
                return;
            }                   
            
            // Set properties and evaluate sub tree
            var prevWorldToCamera = context.WorldToCamera;
            var prevCameraToClipSpace = context.CameraToClipSpace;
            
            context.WorldToCamera = camera.WorldToCamera;
            context.CameraToClipSpace = camera.CameraToClipSpace;
            
            Command.GetValue(context);
            
            context.CameraToClipSpace = prevCameraToClipSpace;
            context.WorldToCamera = prevWorldToCamera;
        }

        [Input(Guid = "dfc3c909-ae13-4364-b9db-c594dad1bee4")]
        public readonly InputSlot<Command> Command = new();
        
        [Input(Guid = "8cac9f22-c6a1-4ced-9733-fe366eafb5c4")]
        public readonly InputSlot<Object> CameraReference = new();
    }
}