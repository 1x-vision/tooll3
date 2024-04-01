using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.t3
{
	[Guid("03408590-c8a4-4eb8-a237-bc8d9e1686c2")]
    public class HandsOn001Abstract : Instance<HandsOn001Abstract>
    {
        [Output(Guid = "db81be67-ea70-4ac1-b9b8-cfcdabb5cace")]
        public readonly Slot<Command> Output = new Slot<Command>();


    }
}
