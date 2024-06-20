using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.point.modify
{
	[Guid("191e5057-4da4-447e-b7cf-e9e0ed8c5dd8")]
    public class SpreadPointAttributes : Instance<SpreadPointAttributes>
    {

        [Output(Guid = "39c62e1e-7c63-4a88-9923-3f7f5fffbfbf")]
        public readonly Slot<BufferWithViews> Output = new();

        [Input(Guid = "d504c3f9-290f-4a73-bf9d-f9266ea955f6")]
        public readonly InputSlot<BufferWithViews> Points = new InputSlot<BufferWithViews>();

        [Input(Guid = "7634c477-6891-4fac-8f8a-3e580cb02277", MappedType = typeof(SpreadModes))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        [Input(Guid = "d5a5862b-d3a2-4e6e-ad54-32cff7ced0fd")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "840aa616-6983-4840-a58b-d5396a91c2f9", MappedType = typeof(MappingModes))]
        public readonly InputSlot<int> Mapping = new InputSlot<int>();

        [Input(Guid = "ba00fa7a-fcda-48da-a4c7-f2fe97997e50", MappedType = typeof(Modes))]
        public readonly InputSlot<int> ApplyMode = new InputSlot<int>();

        [Input(Guid = "38046ede-e786-4cb1-ac17-de6cb7b91c32")]
        public readonly InputSlot<float> Range = new InputSlot<float>();

        [Input(Guid = "82a1a932-f5c3-41d5-9539-9b21663aee1b")]
        public readonly InputSlot<float> Phase = new InputSlot<float>();

        [Input(Guid = "cd91ff45-7f21-40fd-86c8-8dd95204c3b3")]
        public readonly InputSlot<Texture2D> ValueTexture = new InputSlot<Texture2D>();

        [Input(Guid = "27bf737b-966e-4203-b8fd-2d9c7b19dcad")]
        public readonly InputSlot<Curve> WCurve = new InputSlot<Curve>();

        [Input(Guid = "7c944690-d5b2-4894-a178-97593ecd797a")]
        public readonly InputSlot<Gradient> Gradient = new InputSlot<Gradient>();

        private enum SpreadModes
        {
            UseBufferOrder,
            UseW,
        }
        

        private enum MappingModes
        {
            Normal,
            ForStart,
            PingPong,
            Repeat,
            UseOriginalW,
        }
        
        private enum Modes
        {
            Replace,
            Multiply,
            Add,
        }
    }
}

