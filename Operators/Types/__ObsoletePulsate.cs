using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ffed6f9e_2495_4cf3_9cda_740ecec75d10
{
    public class __ObsoletePulsate : Instance<__ObsoletePulsate>
    {
        [Output (Guid = "CB128B17-6855-440D-8539-BFD437E4193C")]
        public readonly Slot<float> Result = new Slot<float> ();

        [Output (Guid = "1FE51F05-F488-4DCB-98F3-B4321A991AFA")]
        public readonly Slot<float> Counter = new Slot<float> ();
        
        [Output (Guid = "853A932C-21BA-403A-A6C1-5707A6E98CE8")]
        public readonly Slot<bool> Bang = new Slot<bool> ();


        public __ObsoletePulsate()
        {
            Result.UpdateAction = Update;
            Counter.UpdateAction = Update;
            Bang.UpdateAction = Update;
        }

        private void Update (EvaluationContext context)
        {
            var beatTime = BeatTime.IsConnected 
                               ? BeatTime.GetValue(context) 
                               : (float)context.Playback.FxTimeInBars;
            var frequency = Frequency.GetValue (context);
            var intensity = Intensity.GetValue (context);

            float v = 0;
            // Off
            if (frequency == 0)
            {
                v = 0;
            }
            else if (frequency < 0.1)
            {
                v = 1 / 16f;
            }
            else if (frequency < 0.2)
            {
                v = 1 / 8f;
            }
            else if (frequency < 0.3)
            {
                v = 1 / 4f;
            }
            else if (frequency < 0.5)
            {
                v = 1;
            }
            else if (frequency < 0.7)
            {
                v = 2;
            }

            else if (frequency < 0.90)
            {
                v = 4;
            }
            else
            {
                v = 16;
            }

            v *= Speed.GetValue(context);
            Result.Value = (1 - (beatTime * v) % 1) * intensity;
            int beatCounter = (int) (beatTime * v);
            Counter.Value = beatCounter;

            var wasBang = Counter.Value > _lastBeatCounter;
            _lastBeatCounter = beatCounter;
            
            // Only ping updates on actual changes
            if (wasBang != _lastBang)
            {
                Bang.Value = wasBang;
                _lastBang = wasBang;
            } 
        }

        private int _lastBeatCounter;
        private bool _lastBang;

        [Input(Guid = "3b60db67-3a12-44c3-91ba-5517f74879d6")]
        public readonly InputSlot<float> BeatTime = new InputSlot<float>();

        [Input(Guid = "c1e5ce4c-6780-414e-9596-703fa7cb0392")]
        public readonly InputSlot<float> Frequency = new InputSlot<float>();

        [Input(Guid = "1154B0A7-E244-43FF-A80B-9D099BE85053")]
        public readonly InputSlot<float> Speed = new InputSlot<float>();

        [Input(Guid = "399c783c-7db2-4173-87c6-ffc2bb9cc859")]
        public readonly InputSlot<float> Intensity = new InputSlot<float>();
    }
}