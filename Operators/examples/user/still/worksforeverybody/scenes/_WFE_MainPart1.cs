using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.still.worksforeverybody.scenes
{
	[Guid("5f5b787e-dc9b-4d0e-a826-13ce6fd38afd")]
    public class _WFE_MainPart1 : Instance<_WFE_MainPart1>
    {
        [Output(Guid = "736f93fe-1d33-4504-8000-8e2cab2f7c43")]
        public readonly Slot<Command> Output = new();


    }
}

