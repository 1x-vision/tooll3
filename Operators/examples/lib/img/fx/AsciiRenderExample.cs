using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.img.fx
{
	[Guid("2ff86df2-6492-4996-bd5e-fc12ec2e0947")]
    public class AsciiRenderExample : Instance<AsciiRenderExample>
    {
        [Output(Guid = "20cdf670-2e51-44ab-9af2-3ff2c58485fd")]
        public readonly Slot<Texture2D> TextureOut = new();


    }
}

