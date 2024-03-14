using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
	[Guid("ab5fbecb-abef-4a17-a0bf-2ce8f81ff813")]
    public class ApplyRandomWalkExamples : Instance<ApplyRandomWalkExamples>
    {
        [Output(Guid = "9b69c66f-da27-4533-9345-6c392d00b26c")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

