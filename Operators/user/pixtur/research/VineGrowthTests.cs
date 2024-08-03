using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.research
{
	[Guid("49f58668-8a36-48f3-b162-c33c98dc0675")]
    public class VineGrowthTests : Instance<VineGrowthTests>
    {
        [Output(Guid = "fa9545a5-4f06-4841-b83a-8bdcf1407dfc")]
        public readonly Slot<Texture2D> Output = new();


    }
}
