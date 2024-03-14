using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.img.fx
{
	[Guid("8c370b92-c977-449f-8d35-15abfb3f0e29")]
    public class ChromaticDistortionExample : Instance<ChromaticDistortionExample>
    {
        [Output(Guid = "952874af-9138-4944-ba5c-8e53a77c424a")]
        public readonly Slot<Texture2D> Output = new();

        [Output(Guid = "f6f1aedc-f140-42c6-81c3-2e54a1432fdf")]
        public readonly Slot<Texture2D> Output2 = new();


    }
}

