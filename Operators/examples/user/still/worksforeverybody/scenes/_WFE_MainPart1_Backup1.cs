using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.worksforeverybody.scenes
{
	[Guid("6aa19380-e446-4b9d-a3d9-b275de97f374")]
    public class _WFE_MainPart1_Backup1 : Instance<_WFE_MainPart1_Backup1>
    {
        [Output(Guid = "258d3135-c058-4dc7-b6b8-1af2ba65d438")]
        public readonly Slot<Command> Output = new();


    }
}

