using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.img.generate
{
	[Guid("3ce7996f-dd8a-4deb-9cd5-d0aed584026f")]
    public class FragmentNoiseExample : Instance<FragmentNoiseExample>
    {
        [Output(Guid = "155b810c-19f7-4f0f-a4b4-ab0b18d2743a")]
        public readonly Slot<Texture2D> Output = new();


    }
}

