using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.howto
{
	[Guid("1ec749af-fe7d-4728-9855-d1fa3e879751")]
    public class HowToUseTooll : Instance<HowToUseTooll>
    {
        [Output(Guid = "c301380c-8fe6-4e3d-af10-9cebd230b0e9")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}
