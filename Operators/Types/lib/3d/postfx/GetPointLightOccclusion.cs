using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_574d636f_64e6_4f07_ac17_49754d2c3599
{
    public class GetPointLightOccclusion : Instance<GetPointLightOccclusion>
    {
        [Output(Guid = "21ef57a3-de27-4e31-8693-e38e693f8948")]
        public readonly Slot<float> Occlusion = new Slot<float>();
        
        [Output(Guid = "6bad1d19-77e7-4ea7-9763-9bb66231a9ab")]
        public readonly Slot<Texture2D> Output = new();


        [Input(Guid = "9a427b76-4976-4f7e-9033-1ba08b743c23")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> DepthMap = new();

        [Input(Guid = "370e5f20-a349-4a15-b7e2-342f9ece0b07")]
        public readonly InputSlot<System.Numerics.Vector2> NearFarRange = new();

        [Input(Guid = "a2a707cf-67ed-4107-8d06-1e28cbf539f8")]
        public readonly InputSlot<int> LightIndex = new InputSlot<int>();

        [Input(Guid = "ef73ef3c-ad47-48ab-abaf-aec450a8459f")]
        public readonly InputSlot<float> Damping = new InputSlot<float>();

    }
}