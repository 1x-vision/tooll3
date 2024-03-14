using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.img.fx
{
	[Guid("26aa6110-def1-4140-9d1e-de3621aeae10")]
    public class SortPixelGlitchExample : Instance<SortPixelGlitchExample>
    {
        [Output(Guid = "5b1dc359-307e-44e7-bc6e-bb68b95225c7")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

