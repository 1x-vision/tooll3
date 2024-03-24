﻿using T3.Editor.Gui.Interaction.Variations.Midi.CommandProcessing;

namespace T3.Editor.Gui.Interaction.Variations.Midi
{
    public class NanoControl8 : AbstractMidiDevice
    {
        public NanoControl8()
        {
            CommandTriggerCombinations = new List<CommandTriggerCombination>()
                                             {
                                                 new(VariationHandling.ActivateOrCreateSnapshotAtIndex, InputModes.Default, new[] { NanoButtonR1To8 }, CommandTriggerCombination.ExecutesAt.SingleRangeButtonPressed ),
                                                 new(VariationHandling.SaveSnapshotAtIndex, InputModes.Save, new[] { ManagerSet, NanoButtonR1To8 }, CommandTriggerCombination.ExecutesAt.SingleRangeButtonPressed ),
                                                 //new CommandTriggerCombination(VariationHandling.ActivateGroupAtIndex, InputModes.Default, new[] { NanoButtonR1To8 }, CommandTriggerCombination.ExecutesAt.SingleRangeButtonPressed ),
                                             };
        }

        private static readonly ButtonRange ButtonRewind = new(43);
        private static readonly ButtonRange ButtonFastForward = new(44);
        private static readonly ButtonRange ButtonStop = new(42);
        private static readonly ButtonRange ButtonPlay = new(41);
        private static readonly ButtonRange ButtonRecord = new(45);
        private static readonly ButtonRange ButtonLoop = new(46);
        private static readonly ButtonRange TextPrevious = new(58);
        private static readonly ButtonRange TrackNext = new(59);
        private static readonly ButtonRange ManagerSet = new(60);
        private static readonly ButtonRange ManagerNext = new(61);
        private static readonly ButtonRange ManagerPrevious = new(62);

        private static readonly ButtonRange NanoButtonSlider1To8 = new(0, 0 + 7);
        private static readonly ButtonRange NanoButtonKnob1To8 = new(16, 16 + 7);
        private static readonly ButtonRange NanoButtonSolo1To8 = new(32, 32 + 7);
        private static readonly ButtonRange NanoButtonMute1To8 = new(48, 48 + 7);
        private static readonly ButtonRange NanoButtonR1To8 = new(64, 64 + 7);

        public override int GetProductNameHash()
        {
            return _productNameHash;
        }

        private readonly int _productNameHash = "Korg NanoControl".GetHashCode(); //Todo: this needs the correct product name
    }
}