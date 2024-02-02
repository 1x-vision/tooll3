using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_436e93a8_03c0_4366_8d9a_2245e5bcaa6c
{
    public class PerlinNoise : Instance<PerlinNoise>
    {
        [Output(Guid = "4a62f8ae-cb15-4e63-ad8d-749bdf24982c")]
        public readonly Slot<float> Result = new();

        public PerlinNoise()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var value = OverrideTime.IsConnected
                            ? OverrideTime.GetValue(context)
                            : (float)context.LocalFxTime;            
            
            var seed = Seed.GetValue(context);
            var period = Frequency.GetValue(context);
            var octaves = Octaves.GetValue(context);
            //var zoom = Zoom.GetValue(context);
            var rangeMin = RangeMin.GetValue(context);
            var rangeMax = RangeMax.GetValue(context);
            var scale = Amplitude.GetValue(context);
            var biasAndGain = BiasAndGain.GetValue(context);

            //var noiseSum = ComputePerlinNoise(value, period, octaves, seed);
            var noiseSum  = MathUtils.PerlinNoise(value, period, octaves, seed);
            var dist = rangeMax - rangeMin;
            var scaleToUniformFactor = 1.37f;
            
            Result.Value = ((scale * noiseSum * scaleToUniformFactor + 1f) * 0.5f).ApplyBiasAndGain(biasAndGain.X, biasAndGain.Y) * dist + rangeMin;
        }

        
        [Input(Guid = "eabbaf77-5f74-4303-9453-6fa44facc5db")]
        public readonly InputSlot<float> OverrideTime = new();
        
        
        [Input(Guid = "B7434932-AEEA-407E-BB00-22337A21F293")]
        public readonly InputSlot<float> Frequency = new();

        [Input(Guid = "C6286F1C-00A3-40AF-94DD-66375ED0343F")]
        public readonly InputSlot<int> Octaves = new();
        
        [Input(Guid = "E704CF6C-963E-4701-BA3D-127B880676FA")]
        public readonly InputSlot<float> Amplitude = new();
        
        [Input(Guid = "B112705E-3EC3-4904-B978-BC784D9B2F94")]
        public readonly InputSlot<float> RangeMin = new();

        [Input(Guid = "557AE817-EC36-4866-8FED-64490E9255BE")]
        public readonly InputSlot<float> RangeMax = new();

        

        [Input(Guid = "296D0F7F-D484-4781-B01F-48839DEF0324")]
        public readonly InputSlot<Vector2> BiasAndGain = new();

        [Input(Guid = "bd43ee20-1ff1-4c49-ac87-87ca4a1fe66f")]
        public readonly InputSlot<int> Seed = new();
    }
}