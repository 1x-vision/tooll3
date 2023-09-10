using System;
using System.Collections.Generic;
using NAudio.Midi;
using Operators.Utils;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_f9f4281b_92ee_430d_a930_6b588a5cb9a9 
{
    public class MidiOutput : Instance<MidiOutput>
,ICustomDropdownHolder,IStatusProvider
    {
        [Output(Guid = "8ccaa82b-5acf-4556-b2e4-2bd1c13ce929")]
        public readonly Slot<float> Result = new Slot<float>();

        public MidiOutput()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var deviceName = Device.GetValue(context);
            var foundDevice = false;
            var noteOrControllerIndex = NoteOrController.GetValue(context).Clamp(0, 127);
            
            var velocity = (int)(Velocity.GetValue(context).Clamp(0, 1)*127);
            var durationInMs = ((int)(DurationInSecs.GetValue(context)*1000)).Clamp(1, 100000);
            var sendMode = SendMode.GetEnumValue<SendModes>(context);
            var triggerActive = TriggerSend.GetValue(context);

            var triggerJustActivated = false;
            var triggerJustDeactivated = false;
            if (triggerActive != _triggered)
            {
                if (triggerActive)
                {
                    triggerJustActivated = true;
                }
                else
                {
                    triggerJustDeactivated = true;
                }

                _triggered = triggerActive;
            }
            
            
            foreach (var (m, device) in MidiInConnectionManager._midiOutsWithDevices)
            {
                if (device.ProductName != deviceName)
                    continue;
                
                var channel = ChannelNumber.GetValue(context).Clamp(1,16);
                try
                {
                    MidiEvent midiEvent =null;
                    switch (sendMode)
                    {
                        case SendModes.Note_WhileTriggered:
                            if (triggerJustActivated)
                            {
                                midiEvent = new NoteOnEvent(0, channel, noteOrControllerIndex, velocity, durationInMs);
                            }
                            else if (triggerJustDeactivated)
                            {
                                midiEvent = new NoteOnEvent(0, channel, noteOrControllerIndex, 0, durationInMs);
                            }
                            break;
                        
                        case SendModes.Notes_FixedDuration:
                            if(triggerActive)
                                midiEvent = new NoteOnEvent(0, channel, noteOrControllerIndex, velocity, durationInMs);
                            break;
                        
                        case SendModes.ControllerChange:
                            midiEvent = new ControlChangeEvent(0, channel, (MidiController)noteOrControllerIndex, velocity);
                            break;
                    }
                    if(midiEvent != null)
                        m.Send(midiEvent.GetAsShortMessage());
                    
                    //Log.Debug("Sending MidiTo " + device.Manufacturer + " " + device.ProductName, this);
                    foundDevice = true;
                    break;
                }
                catch (Exception e)
                {
                    _lastErrorMessage = $"Failed to send midi to {deviceName}: " + e.Message;
                    Log.Warning(_lastErrorMessage, this);
                }
                
            }
            _lastErrorMessage = !foundDevice ? $"Can't find MidiDevice {deviceName}" : null;
        }

        private enum SendModes
        {
            Notes_FixedDuration,
            Note_WhileTriggered,
            ControllerChange,
        }
        private bool _triggered;

        #region device dropdown
        
        string ICustomDropdownHolder.GetValueForInput(Guid inputId)
        {
            return Device.Value;
        }

        IEnumerable<string> ICustomDropdownHolder.GetOptionsForInput(Guid inputId)
        {
            if (inputId != Device.Id)
            {
                yield return "undefined";
                yield break;
            }
            
            foreach (var device in MidiInConnectionManager._midiOutsWithDevices.Values)
            {
                yield return device.ProductName;
            }
        }

        void ICustomDropdownHolder.HandleResultForInput(Guid inputId, string result)
        {
            Log.Debug($"Got {result}", this);
            Device.SetTypedInputValue(result);
        }
        #endregion
        
        #region Implement statuslevel
        IStatusProvider.StatusLevel IStatusProvider.GetStatusLevel()
        {
            return string.IsNullOrEmpty(_lastErrorMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Error;
        }

        string IStatusProvider.GetStatusMessage()
        {
            return _lastErrorMessage;
        }

        private string _lastErrorMessage;
        #endregion
        
        [Input(Guid = "9EFA1BE1-F102-4457-A69B-2AEF59F6B845")]
        public readonly InputSlot<bool> TriggerSend = new ();
        
        
        [Input(Guid = "EC4D83B6-78EB-4CAC-826B-CAFB0BE3F604", MappedType = typeof(SendModes))]
        public readonly InputSlot<int> SendMode = new ();

        [Input(Guid = "ADEA6968-35EF-436A-BC2D-D9433B623DF6")]
        public readonly InputSlot<string> Device = new ();
        
        [Input(Guid = "A7E1EAC2-5602-4C40-8519-19CA53763C76")]
        public readonly InputSlot<int> ChannelNumber = new ();

        [Input(Guid = "0FFF2CE2-DEFA-442C-A089-4B12E7D71620")]
        public readonly InputSlot<int> NoteOrController = new ();

        [Input(Guid = "61CCD308-0006-42DE-B190-C006E99B5871")]
        public readonly InputSlot<float> Velocity = new ();
        
        [Input(Guid = "ABE9393E-282E-4DE0-8F86-541FA955658F")]
        public readonly InputSlot<float> DurationInSecs = new ();

    }
}
