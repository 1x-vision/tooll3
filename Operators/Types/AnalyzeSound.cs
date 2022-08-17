using SharpDX.Direct3D11;
using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_112ea923_a349_412f_8dd3_4d9d9fc42dc6
{
    public class AnalyzeSound : Instance<AnalyzeSound>
    {

        [Input(Guid = "7357ea4b-201d-484b-8398-4a8e13fa3880")]
        public readonly InputSlot<T3.Core.Command> SubGraph = new InputSlot<T3.Core.Command>();

        [Input(Guid = "cc3b0ab3-7379-4197-9a84-dc1b368790c5")]
        public readonly InputSlot<float> BeatThreshold = new InputSlot<float>();

        [Input(Guid = "9aff8ca6-d2f5-4bb5-9c38-af798061596c")]
        public readonly InputSlot<float> HihatThreshold = new InputSlot<float>();

        [Input(Guid = "1d92b4fa-a8e9-4c1e-891a-65bf7632bdcf")]
        public readonly InputSlot<float> HiRange = new InputSlot<float>();

        [Input(Guid = "57bb8790-4c02-422a-833b-0b1cf0f3e781", MappedType = typeof(InputSources))]
        public readonly InputSlot<int> InputSource = new InputSlot<int>();

        [Input(Guid = "29ef504f-c6a4-4740-a5fe-95e45117fc32")]
        public readonly InputSlot<bool> Reset = new InputSlot<bool>();

        [Input(Guid = "f2e2898f-bd68-4750-9058-2be39e63bd28")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> Visibility = new InputSlot<T3.Core.Operator.GizmoVisibility>();

        [Input(Guid = "f79c66ee-b06a-4684-b979-ef3b8ddd1e59")]
        public readonly InputSlot<int> DeviceIndex2 = new InputSlot<int>();

        [Input(Guid = "9e0249c1-8a8c-48b3-8bbb-cb0b0514f299")]
        public readonly InputSlot<float> Gain = new InputSlot<float>();

        [Output(Guid = "6ba78564-8315-49ef-9d47-eb96e0a52fd5")]
        public readonly Slot<T3.Core.Command> Detection = new Slot<T3.Core.Command>();
        
        [Output(Guid = "70ab1e5d-7947-400a-851b-b1d3d61fdccf")]
        public readonly Slot<float> BeatCount = new Slot<float>();

        [Output(Guid = "01a62ccf-9383-43c8-b4a4-f06e94f33b81")]
        public readonly Slot<float> BeatLevel = new Slot<float>();

        [Output(Guid = "48ee0045-82cb-40b1-8b14-a4a1f495cf49")]
        public readonly Slot<float> HihatCount = new Slot<float>();

        [Output(Guid = "3a98fc87-fb8f-482a-a16d-06de9c0aa691")]
        public readonly Slot<float> HihatLevel = new Slot<float>();

        [Output(Guid = "7d5b7192-f81c-4186-a0e9-864b033caf15")]
        public readonly Slot<float> MovingSum = new Slot<float>();


        [Output(Guid = "1c9c6951-c300-4ada-ae34-783b30f31180")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        private enum InputSources
        {
            ExternalWasabi,
            InternalSoundtrack,
        }
    }
}

