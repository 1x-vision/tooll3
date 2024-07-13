using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using NewTek.NDI;
using NewTek;
using System.Linq;

namespace T3.Operators.Types.Id_9412d0f4_dab8_4145_9719_10395e154fa7
{
    public class NdiOutput : Instance<NdiOutput>
    {
        [Output(Guid = "3c0ae0e5-a2af-4437-b7fa-8ad300cb8b8b", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Texture2D> TextureOutput = new();

        public NdiOutput()
        {
            TextureOutput.UpdateAction = Update;
        }
        ~NdiOutput()
        {
            Dispose();
        }

        private static readonly SharpDX.DXGI.Format[] allowedFormats = new Format[]
        {
            Format.B8G8R8A8_UNorm,
            Format.R8G8B8A8_UNorm,
            Format.B8G8R8A8_Typeless,
            Format.R8G8B8A8_Typeless
        };

        private void Update(EvaluationContext context)
        {
            var texture = Texture.GetValue(context);
            var senderName = SenderName.GetValue(context);
            var fps = FrameRate.GetValue(context);
            var alpha = EnableAlpha.GetValue(context);

            TextureOutput.Value = texture;

            //Single mip, non array and non multisampled //FIXME : we can actually handle mips using copy subresource
            if (texture == null ||
                (!allowedFormats.Contains(texture.Description.Format))
                || texture.Description.SampleDescription.Count > 1
                || texture.Description.ArraySize != 1)
            {
                //FIXME : error pls 
                return;
            }

            SendTexture(senderName, fps, alpha, ref texture);
            SenderName.Update(context);
        }

        private void SetupNdiSender(string senderName, uint width, uint height)
        {
            IntPtr groupsNamePtr = IntPtr.Zero;

            IntPtr ndiString = NewTek.NDI.UTF.StringToUtf8(senderName);

            // Create an NDI source description using sourceNamePtr and it's clocked to the video.
            NDIlib.send_create_t createDesc = new NDIlib.send_create_t()
            {
                p_ndi_name = ndiString,
                p_groups = groupsNamePtr,
                clock_video = true,
                clock_audio = false
            };

            // We create the NDI finder instance
            ndiSender = NDIlib.send_create(ref createDesc);

            Marshal.FreeHGlobal(ndiString); //created with allochglobal in utils
            currentSenderName = senderName;
        }

        private bool SendTexture(string senderName, int frameRate, bool enableAlpha, ref Texture2D frame)
        {
            var device = ResourceManager.Device;

            if (frame == null)
                return false;

            if (stagingTexture != null)
            {
                //reset sender if resolution is different
                if (stagingTexture.Description.Width != frame.Description.Width 
                   || stagingTexture.Description.Height != frame.Description.Height
                   || senderName != currentSenderName)
                {
                    ReleaseCpuData();
                }
            }

            int width = frame.Description.Width;
            int height = frame.Description.Height;
            int stride = width * 4;

            //whichever null or intptr zero does the job
            if (stagingTexture == null)
            {
                SetupNdiSender(senderName, (uint)frame.Description.Width, (uint)frame.Description.Height);

                Texture2DDescription stagingDesc = frame.Description;
                stagingDesc.BindFlags = BindFlags.None;
                stagingDesc.CpuAccessFlags = CpuAccessFlags.Read;
                stagingDesc.Usage = ResourceUsage.Staging;
                stagingDesc.MipLevels = 1; // later use copysubresource to use only mip 1 if necessary

                stagingTexture = new Texture2D(device, stagingDesc);

                textureData = Marshal.AllocHGlobal(width * height * 4);
            }

            //Readback from gpu
            {
                //device.ImmediateContext.CopyResource(frame, stagingTexture);

                //This allow to only copy mip level 0;
                device.ImmediateContext.CopySubresourceRegion(frame, 0, null, stagingTexture, 0);

                var dataBox = device.ImmediateContext.MapSubresource(stagingTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out _);
                Utilities.CopyImageMemory(dataBox.DataPointer, textureData, height, dataBox.RowPitch, stride);
                device.ImmediateContext.UnmapSubresource(stagingTexture, 0);
            }


            //Send data
            {
                bool isBgr = frame.Description.Format == Format.B8G8R8A8_UNorm || frame.Description.Format == Format.B8G8R8A8_Typeless;

                NDIlib.FourCC_type_e fcc;
                if (isBgr)
                {
                    fcc = enableAlpha ? NDIlib.FourCC_type_e.FourCC_type_BGRA : NDIlib.FourCC_type_e.FourCC_type_BGRX;
                }
                else
                {
                    fcc = enableAlpha ? NDIlib.FourCC_type_e.FourCC_type_RGBA : NDIlib.FourCC_type_e.FourCC_type_RGBX;
                }

                NDIlib.video_frame_v2_t videoFrame = new NDIlib.video_frame_v2_t()
                {
                    // Resolution
                    xres = width,
                    yres = height,
                    FourCC = fcc,
                    frame_rate_N = frameRate,
                    frame_rate_D = 1,
                    picture_aspect_ratio = (float)width / (float)height,
                    frame_format_type = NDIlib.frame_format_type_e.frame_format_type_progressive,
                    timecode = NDIlib.send_timecode_synthesize,
                    p_data = textureData,
                    line_stride_in_bytes = stride,
                    p_metadata = IntPtr.Zero
                };

                int connectionCount = NDIlib.send_get_no_connections(ndiSender, 10);

                if (connectionCount > 0)
                {
                    NDIlib.send_send_video_v2(ndiSender, ref videoFrame);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #region IDisposable Support

        private void ReleaseCpuData()
        {
            Utilities.Dispose(ref stagingTexture);
            if (ndiSender != IntPtr.Zero)
            {
                // Destroy the NDI sender
                NDIlib.send_destroy(ndiSender);
                ndiSender = IntPtr.Zero;
            }

            if (textureData != IntPtr.Zero)
            {
                // Destroy the NDI sender
                Marshal.FreeHGlobal(textureData);
                textureData = IntPtr.Zero;
            }
        }

        public new void Dispose()
        {
            ReleaseCpuData();
        }

        #endregion
        private IntPtr ndiSender = IntPtr.Zero;
        private string currentSenderName = string.Empty;

        private Texture2D stagingTexture;   // texture to send
        private IntPtr textureData;         //need a copy here since we need to handle stride (also necessary if we want to queue frame later to remove stall)
    
        [Input(Guid = "15ddab7a-aad4-49eb-9e56-70714e10679f")]
        public readonly InputSlot<Texture2D> Texture = new();

        [Input(Guid = "740db4c4-4182-4743-a713-5a45855499d9")]
        public InputSlot<string> SenderName = new();

        [Input(Guid = "CE0E267E-E96D-441F-86FB-BACEF1F0186A")]
        public InputSlot<int> FrameRate = new();

        [Input(Guid = "E6359F2E-E4F7-419D-A403-CE5EB0528898")]
        public InputSlot<bool> EnableAlpha = new();
    }
}