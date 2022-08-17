using System.Numerics;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ccc06dd6_4eec_4d17_af0b_4f1700e7887a
{
    public class PerlinNoise2 : Instance<PerlinNoise2>
    {
        [Output(Guid = "2B60892B-BE0E-46C0-B30B-562E34BD92A5")]
        public readonly Slot<Vector2> Result = new();

        public PerlinNoise2()
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
            var rangeMin = RangeMin.GetValue(context);
            var rangeMax = RangeMax.GetValue(context);
            var scale = Scale.GetValue(context);
            var scaleXY = ScaleXY.GetValue(context);

            Result.Value  = new Vector2(
                                          (MathUtils.PerlinNoise(value, period, octaves, seed) + 1f) * 0.5f * (rangeMax.X - rangeMin.X) + rangeMin.X,
                                          (MathUtils.PerlinNoise(value, period, octaves, seed+123) + 1f) * 0.5f * (rangeMax.Y - rangeMin.Y) + rangeMin.Y) * scaleXY  * scale;
        }


        [Input(Guid = "c1ffdc20-7c90-49f9-8deb-f0a415e130c8")]
        public readonly InputSlot<int> Seed = new();

        [Input(Guid = "463d2c27-721f-41ad-ba76-5db138d92bf4")]
        public readonly InputSlot<float> Frequency = new();

        [Input(Guid = "cbcbce93-8c8d-41ed-b91b-9e3583c5a3b5")]
        public readonly InputSlot<int> Octaves = new();
        
        [Input(Guid = "D72D0DCF-62D6-498A-838D-88D33D798D4F")]
        public readonly InputSlot<Vector2> RangeMin = new();

        [Input(Guid = "DAE5B55C-0C30-4EE9-A535-7654B8357669")]
        public readonly InputSlot<Vector2> RangeMax = new();

        [Input(Guid = "A5731884-2EFC-4CFD-A098-4A0B4B6BDD6B")]
        public readonly InputSlot<Vector2> ScaleXY = new();
        
        [Input(Guid = "0abcff87-ace5-4a06-9217-b2caf831ecae")]
        public readonly InputSlot<float> Scale = new();
        
        [Input(Guid = "f294c517-7427-4c14-a397-4605bffc52a4")]
        public readonly InputSlot<float> OverrideTime = new();

    }
}