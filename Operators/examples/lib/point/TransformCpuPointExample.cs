using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
	[Guid("69020cd2-de9a-4150-bac5-b547301e7bc8")]
    public class TransformCpuPointExample : Instance<TransformCpuPointExample>
    {
        [Output(Guid = "3a1fe126-01e6-4d55-9abf-7344c195a4fd")]
        public readonly Slot<Command> Output = new();


    }
}

