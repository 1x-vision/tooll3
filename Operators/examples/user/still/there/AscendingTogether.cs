using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.there
{
	[Guid("44d744da-cf4e-43e5-853a-126cfed6c865")]
    public class AscendingTogether : Instance<AscendingTogether>
    {

        [Output(Guid = "858daa1d-d3f4-4ce3-a711-5efa0ecfb1f7")]
        public readonly TimeClipSlot<Command> Output2 = new();


        [Input(Guid = "8c6f79e9-5298-4973-a1bf-7575d013ea65")]
        public readonly InputSlot<float> UniformScale = new();

    }
}

