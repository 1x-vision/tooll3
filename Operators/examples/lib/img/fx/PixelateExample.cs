using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.img.fx
{
    [Guid("ea7536a2-f69b-4964-a04c-4474bfacfa56")]
    public class PixelateExample : Instance<PixelateExample>
    {
        [Output(Guid = "32ae7d3f-5fcd-474a-8f5e-99ffe28f7b60")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}
