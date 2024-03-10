using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.newemka980.Examples
{
	[Guid("9913adf3-8fe5-4d85-95ab-f04439c6edcb")]
    public class BooleanExample : Instance<BooleanExample>
    {
        [Output(Guid = "5b42af7d-3128-498e-8b96-cc707101e9e3")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

