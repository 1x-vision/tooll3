using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.worksforeverybody.scenes
{
	[Guid("224aa808-3aaf-4385-be16-0b723c33ab39")]
    public class _WFE_IntroPart : Instance<_WFE_IntroPart>
    {
        [Output(Guid = "43b8050a-1f18-4d15-9c03-78a275d1ff2b")]
        public readonly Slot<Command> Output = new();


    }
}

