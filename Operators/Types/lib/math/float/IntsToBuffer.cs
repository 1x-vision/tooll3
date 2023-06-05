using SharpDX;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
//using Buffer = SharpDX.Direct3D11.Buffer;
//using Utilities = T3.Core.Utils.Utilities;

namespace T3.Operators.Types.Id_2eb20a76_f8f7_49e9_93a5_1e5981122b50 
{
    public class IntsToBuffer : Instance<IntsToBuffer>
    {
        [Output(Guid = "f5531ffb-dbde-45d3-af2a-bd90bcbf3710")]
        public readonly Slot<SharpDX.Direct3D11.Buffer> Result = new();

        public IntsToBuffer()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var intParams = Params.GetCollectedTypedInputs();
            var intParamCount = intParams.Count;

            var arraySize = (intParamCount / 4 + (intParamCount % 4 == 0 ? 0 : 1)) * 4; // always 16byte slices for alignment
            var array = new int[arraySize];

            if (array.Length == 0)
                return;
            
            for (var intIndex = 0; intIndex < intParamCount; intIndex++)
            {
                array[intIndex] = intParams[intIndex].GetValue(context);
            }

            Params.DirtyFlag.Clear();

            var device = ResourceManager.Device;
            var size = sizeof(float) * array.Length;
            using (var data = new DataStream(size, true, true))
            {
                data.WriteRange(array);
                data.Position = 0;

                if (Result.Value == null || Result.Value.Description.SizeInBytes != size)
                {
                    Utilities.Dispose(ref Result.Value);
                    var bufferDesc = new BufferDescription
                                         {
                                             Usage = ResourceUsage.Default,
                                             SizeInBytes = size,
                                             BindFlags = BindFlags.ConstantBuffer
                                         };
                    Result.Value = new SharpDX.Direct3D11.Buffer(device, data, bufferDesc);
                }
                else
                {
                    device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), Result.Value, 0);
                }
            }

            Result.Value.DebugName = nameof(IntsToBuffer);
        }


        [Input(Guid = "49556D12-4CD1-4341-B9D8-C356668D296C")]
        public readonly MultiInputSlot<int> Params = new();

    }
}
