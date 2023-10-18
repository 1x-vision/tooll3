using System;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using Buffer = SharpDX.Direct3D11.Buffer;
using Utilities = T3.Core.Utils.Utilities;

namespace T3.Operators.Types.Id_724da755_2d0c_42ab_8335_8c88ec5fb078
{
    public class FloatsToBuffer : Instance<FloatsToBuffer>
    {
        [Output(Guid = "f5531ffb-dbde-45d3-af2a-bd90bcbf3710")]
        public readonly Slot<Buffer> Buffer = new Slot<Buffer>();

        public FloatsToBuffer()
        {
            Buffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            try
            {
                var matrixParams = Vec4Params.GetCollectedTypedInputs();
                var floatParams = Params.GetCollectedTypedInputs();

                var floatParamCount = floatParams.Count;
                var vec4ArrayLength = matrixParams.Count;

                var totalFloatCount = floatParamCount + vec4ArrayLength * 4 * 4;

                var arraySize = (totalFloatCount / 4 + (totalFloatCount % 4 == 0 ? 0 : 1)) * 4; // always 16byte slices for alignment
                var array = new float[arraySize];

                if (array.Length == 0)
                    return;

                var totalFloatIndex = 0;
                
                foreach (var aInput in matrixParams)
                {
                    var aaa = aInput.GetValue(context);
                    foreach (var vec4 in aaa)
                    {
                        array[totalFloatIndex++] = vec4[0];
                        array[totalFloatIndex++] = vec4[1];
                        array[totalFloatIndex++] = vec4[2];
                        array[totalFloatIndex++] = vec4[3];
                    }
                }

                // Add Floats
                for (var floatIndex = 0; floatIndex < floatParamCount; floatIndex++)
                {
                    array[totalFloatIndex++] = floatParams[floatIndex].GetValue(context);
                }

                Params.DirtyFlag.Clear();
                Vec4Params.DirtyFlag.Clear();

                var device = ResourceManager.Device;

                var size = sizeof(float) * array.Length;
                using (var data = new DataStream(size, true, true))
                {
                    data.WriteRange(array);
                    data.Position = 0;

                    if (Buffer.Value == null || Buffer.Value.Description.SizeInBytes != size)
                    {
                        Utilities.Dispose(ref Buffer.Value);
                        var bufferDesc = new BufferDescription
                                             {
                                                 Usage = ResourceUsage.Default,
                                                 SizeInBytes = size,
                                                 BindFlags = BindFlags.ConstantBuffer
                                             };
                        Buffer.Value = new Buffer(device, data, bufferDesc);
                    }
                    else
                    {
                        device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), Buffer.Value, 0);
                    }
                }
                Buffer.Value.DebugName = nameof(FloatsToBuffer);
            }
            catch (Exception e)
            {
                Log.Warning("Failed to setup shader parameters:" + e.Message);
            }
        }
        
        [Input(Guid = "914EA6E8-ABC6-4294-B895-8BFBE5AFEA0E")]
        public readonly MultiInputSlot<SharpDX.Vector4[]> Vec4Params = new();

        [Input(Guid = "49556D12-4CD1-4341-B9D8-C356668D296C")]
        public readonly MultiInputSlot<float> Params = new();

    }
}