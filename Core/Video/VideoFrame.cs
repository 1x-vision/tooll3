using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T3.Core.Video
{
    public struct VideoFrame : IDisposable
    {
        public Size Size;
        public int StrideInBytes;
        public IntPtr Data;
        public Format Format;
        public bool IsOwned; //tells if data is owned

        public int TotalSize
        {
            get
            {
                return this.Size.Height * this.StrideInBytes;
            }
        }

        public static VideoFrame Reference(Size size, int stride, IntPtr data, Format format)
        {
            VideoFrame result = new VideoFrame();
            result.Size = size;
            result.StrideInBytes = stride;
            result.Data = data;
            result.Format = format;
            result.IsOwned = false;
            return result;
        }

        public static VideoFrame Owned(Size size, int stride, Format format)
        {
            VideoFrame result = new VideoFrame();
            result.Size = size;
            result.StrideInBytes = stride;
            result.Data = SharpDX.Utilities.AllocateMemory(stride * size.Height);
            result.Format = format;
            result.IsOwned = true;
            return result;
        }

        public void Dispose()
        {
            if (this.Data != IntPtr.Zero && this.IsOwned)
            {
                SharpDX.Utilities.FreeMemory(this.Data);
                this.Data = IntPtr.Zero;
            }
        }
    }
}
