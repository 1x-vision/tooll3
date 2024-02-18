using System;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3f6a85be_a837_4681_9c2d_5c87e838f25a 
{
    public class ToggleBoolean : Instance<ToggleBoolean>
    {
        [Output(Guid = "D6FDBE01-B25C-4E8A-A134-DADB192B1864")]
        public readonly Slot<bool> Result = new();

        public ToggleBoolean()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var triggerToggle = TriggerToggle.GetValue(context);
            if (triggerToggle)
            {
                TriggerToggle.SetTypedInputValue(false);
                _isActive = !_isActive;
            }
            
            var triggerReset = TriggerReset.GetValue(context);
            if (triggerReset)
            {
                TriggerReset.SetTypedInputValue(false);
                _isActive = false;
            }

            Result.Value = _isActive;

        }

        private bool _isActive;
        
        [Input(Guid = "2216EA9A-A6FB-446E-B4BE-0B9A111BE745")]
        public readonly InputSlot<bool> TriggerToggle = new();
        
        [Input(Guid = "F8416220-11C9-4D60-AC45-406822690754")]
        public readonly InputSlot<bool> TriggerReset = new();

    }
}
