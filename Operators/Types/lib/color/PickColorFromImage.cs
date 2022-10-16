using System;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Utilities = T3.Core.Utilities;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace T3.Operators.Types.Id_42703423_1414_489e_aac2_21a3d7204262
{
    public class PickColorFromImage : Instance<PickColorFromImage>
    {
        [Output(Guid = "4f0c5c55-74b3-46d9-bbbc-4aad5dc14ea3")]
        public readonly Slot<Vector4> Output = new();

        public PickColorFromImage()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var alwaysUpdate = AlwaysUpdate.GetValue(context);
            var inputImage = InputImage.GetValue(context);

            var position = Position.GetValue(context);

            if (inputImage == null)
            {
                return;
            }

            var d3DDevice = ResourceManager.Device;
            var immediateContext = d3DDevice.ImmediateContext;

            var column = ((int)(position.X * inputImage.Description.Width)).Clamp(0, inputImage.Description.Width - 1);
            var row = ((int)(position.Y * inputImage.Description.Height)).Clamp(0, inputImage.Description.Height - 1);
            //column = column.Clamp(0, inputImage.Description.Width - 1);

            if (alwaysUpdate
                || _imageWithCpuAccess == null
                || _imageWithCpuAccess.Description.Format != inputImage.Description.Format
                || _imageWithCpuAccess.Description.Width != inputImage.Description.Width
                || _imageWithCpuAccess.Description.Height != inputImage.Description.Height
                || _imageWithCpuAccess.Description.MipLevels != inputImage.Description.MipLevels
                )
            {
                // keep a copy of the texture which can be accessed by CPU
                var desc = new Texture2DDescription()
                               {
                                   BindFlags = BindFlags.None,
                                   Format = inputImage.Description.Format,
                                   Width = inputImage.Description.Width,
                                   Height = inputImage.Description.Height,
                                   MipLevels = inputImage.Description.MipLevels,
                                   SampleDescription = new SampleDescription(1, 0),
                                   Usage = ResourceUsage.Staging,
                                   OptionFlags = ResourceOptionFlags.None,
                                   CpuAccessFlags = CpuAccessFlags.Read, // <- that we want
                                   ArraySize = 1
                               };
                Utilities.Dispose(ref _imageWithCpuAccess);
                _imageWithCpuAccess = new Texture2D(d3DDevice, desc);
                immediateContext.CopyResource(inputImage, _imageWithCpuAccess);
            }

            var width = inputImage.Description.Width;
            var height = inputImage.Description.Height;

            column %= width;
            row %= height;

            // Gets a pointer to the image data, and denies the GPU access to that subresource.            
            var sourceDataBox =
                immediateContext.MapSubresource(_imageWithCpuAccess, 0, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out var sourceStream);

            using (sourceStream)
            {

                Vector4 color;

                switch (inputImage.Description.Format)
                {
                    case Format.R8G8B8A8_UNorm:
                    {
                        // Position to the wanted pixel. 4 of bytes per pixel
                        sourceStream.Seek(row * sourceDataBox.RowPitch + 4 * column, System.IO.SeekOrigin.Begin);

                        var dxColor = new SharpDX.Color4(sourceStream.Read<Int32>()); 
                        color = new Vector4(dxColor.Red, dxColor.Green, dxColor.Blue, dxColor.Alpha);
                    }
                        break;

                    case Format.R16G16B16A16_Float:
                    {
                        sourceStream.Seek(row * sourceDataBox.RowPitch + 8 * column, System.IO.SeekOrigin.Begin);

                        var r = Read2BytesToHalf(sourceStream);
                        var g = Read2BytesToHalf(sourceStream);
                        var b = Read2BytesToHalf(sourceStream);
                        var a = Read2BytesToHalf(sourceStream);
                        color = new Vector4(r, g, b, a);
                    }
                        break;

                    case Format.R16G16B16A16_UNorm:
                    {
                        sourceStream.Seek(row * sourceDataBox.RowPitch + 8 * column, System.IO.SeekOrigin.Begin);
                        sourceStream.ReadByte();
                        var r = (byte)sourceStream.ReadByte();
                        sourceStream.ReadByte();
                        var g = (byte)sourceStream.ReadByte();
                        sourceStream.ReadByte();
                        var b = (byte)sourceStream.ReadByte();
                        sourceStream.ReadByte();
                        var a = (byte)sourceStream.ReadByte();
                        color = new Vector4(r, g, b, a);
                    }
                        break;

                    default:
                        throw new InvalidOperationException($"Can't export unknown texture format {inputImage.Description.Format}");
                }

                Output.Value = color;
            }

            immediateContext.UnmapSubresource(_imageWithCpuAccess, 0);
        }

        private static float Read2BytesToHalf(SharpDX.DataStream imageStream)
        {
            var low = (byte)imageStream.ReadByte();
            var high = (byte)imageStream.ReadByte();
            return ToTwoByteFloat(low, high);
        }

        private static float ToTwoByteFloat(byte ho, byte lo)
        {
            var intVal = BitConverter.ToInt32(new byte[] { ho, lo, 0, 0 }, 0);

            var mant = intVal & 0x03ff;
            var exp = intVal & 0x7c00;
            if (exp == 0x7c00) exp = 0x3fc00;
            else if (exp != 0)
            {
                exp += 0x1c000;
                if (mant == 0 && exp > 0x1c400)
                    return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | exp << 13 | 0x3ff), 0);
            }
            else if (mant != 0)
            {
                exp = 0x1c400;
                do
                {
                    mant <<= 1;
                    exp -= 0x400;
                }
                while ((mant & 0x400) == 0);

                mant &= 0x3ff;
            }

            return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | (exp | mant) << 13), 0);
        }

        Texture2D _imageWithCpuAccess;

        [Input(Guid = "3b8c51c9-c544-47eb-9d70-4bd6b161be2d")]
        public readonly InputSlot<Texture2D> InputImage = new();

        [Input(Guid = "27C1B604-4883-4B20-83E1-C435BF9D5499")]
        public readonly InputSlot<Vector2> Position = new();

        [Input(Guid = "84FF0CE4-443D-438D-8FC7-6D6EDE75D67B")]
        public readonly InputSlot<bool> AlwaysUpdate = new();
    }
}