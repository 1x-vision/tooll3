using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7eefe668_d290_4673_b766_39c98b9ba12e
{
    public class SnapToAnglesForce : Instance<SnapToAnglesForce>
    {

        [Output(Guid = "501b3c20-3998-4f7d-ae0c-76d7f879954f")]
        public readonly Slot<T3.Core.DataTypes.ParticleSystem> Particles = new();

        [Input(Guid = "e596a8ec-3681-4f45-b72c-d50a97240270")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "1ad4c103-a0ab-4477-94ec-520d5bf16903")]
        public readonly InputSlot<float> AngleCount = new InputSlot<float>();

        [Input(Guid = "a54ed62a-a6a3-4b7b-a68c-82691467aa6f")]
        public readonly InputSlot<float> VariationThreshold = new InputSlot<float>();

        [Input(Guid = "afd8fd12-8b3b-462b-a117-90981ddfccc6")]
        public readonly InputSlot<float> Variation = new InputSlot<float>();

        [Input(Guid = "098f4485-7aa7-4ae8-a2b5-e59fa2c2309c")]
        public readonly InputSlot<float> KeepPlanar = new InputSlot<float>();

        [Input(Guid = "8dabcbb3-2aa6-4213-82c9-e92c774c13f7")]
        public readonly InputSlot<float> Twist = new InputSlot<float>();
    }
}

