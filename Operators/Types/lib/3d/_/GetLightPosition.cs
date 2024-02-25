using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Operators.Types.Id_5914f00f_6600_42a2_bbd5_4201053058ec
{
    public class GetLightPosition : Instance<GetLightPosition>
    {
        [Output(Guid = "598EA068-AAA0-43A7-BA58-A8021370CA90", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector3> Position = new();
        
        [Output(Guid = "09C82272-F0EB-4FD6-BBEC-6D6C0725230D", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Radius = new();
        
        [Output(Guid = "2F3E1B9C-FDA5-4555-993D-75322A5927CE", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector4> Color = new();

        [Output(Guid = "CB03AAEF-DAB9-4974-8FAB-258F6CAB521B", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Intensity = new();
        
        [Output(Guid = "23D3F621-0420-4845-B739-3B6D40AE2B5C", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> LightCount = new();
        
        public GetLightPosition()
        {
            Position.UpdateAction = Update;
            LightCount.UpdateAction = Update;
            Radius.UpdateAction = Update;
            Intensity.UpdateAction = Update;
            Color.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var pointLightsCount = context.PointLights.Count;
            
            LightCount.Value = pointLightsCount;
            
            if (pointLightsCount == 0)
                return;
            
            var requestedLightIndex = LightIndex.GetValue(context).Clamp(0, pointLightsCount-1);
            
            var pointLight = context.PointLights.GetPointLight(requestedLightIndex);
            Position.Value = pointLight.Position;
            Radius.Value = pointLight.Range;
            Color.Value = pointLight.Color;
            Intensity.Value = pointLight.Intensity;
        }

        
        [Input(Guid = "a5c6ce06-f260-43ae-8ac7-5ea1cfb3f94e")]
        public readonly InputSlot<int> LightIndex = new();

    }
}