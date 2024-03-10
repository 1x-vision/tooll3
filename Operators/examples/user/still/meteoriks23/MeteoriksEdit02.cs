using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.meteoriks23
{
	[Guid("d90cce62-09e8-44da-a843-62a697b8e99a")]
    public class MeteoriksEdit02 : Instance<MeteoriksEdit02>
    {
        [Output(Guid = "7e6deb3b-ffdc-4832-ab73-1110328b28e8")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

