using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.wake.revision2021
{
	[Guid("b23f04e1-a648-4734-9b8e-265c794a0811")]
    public class RevisionSoundRev : Instance<RevisionSoundRev>
    {

        [Output(Guid = "52a17b4a-5bc9-468d-b543-772c4e60ca7a")]
        public readonly TimeClipSlot<Command> output2 = new();


    }
}

