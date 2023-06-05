using System.Diagnostics;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_32325c5b_53f7_4414_b4dd_a436e45528b0
{
    public class SetCommandTime : Instance<SetCommandTime>
    {
       
        [Output(Guid = "FE01C3B6-72E2-494E-8511-6D50C527463F")]
        public readonly Slot<Command> Result = new Slot<Command>();

        
        public SetCommandTime()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var newTime = NewTime.GetValue(context);
            var mode = OffsetMode.GetEnumValue<Modes>(context);

            if (SubTree.IsConnected)
            {
                var previousKeyframeTime = context.LocalTime;
                var previousEffectTime = context.LocalFxTime;

                if (mode == Modes.Absolute)
                {
                    context.LocalTime = newTime;
                    context.LocalFxTime = newTime;
                }
                else
                {
                    context.LocalTime += newTime;
                    context.LocalFxTime += newTime;
                }
                
                // Execute subtree
                Result.Value = SubTree.GetValue(context);
                context.LocalTime = previousKeyframeTime;
                context.LocalFxTime = previousEffectTime;
            }
            else if(mode == Modes.GlobalAbsolute)
            {
                context.LocalTime = newTime;
                context.LocalFxTime = newTime;
            }
        }
        
        [Input(Guid = "01F2EEF1-E3C1-49A5-B532-9C12DA8CAAC5")]
        public readonly InputSlot<Command> SubTree = new();
        
        [Input(Guid = "d2c934bb-de5c-449c-adf8-7a2f48082e9c")]
        public readonly InputSlot<float> NewTime = new();
        
        [Input(Guid = "EFA8C97D-769C-4A26-B442-E702AC74A1F4", MappedType = typeof(Modes))]
        public readonly InputSlot<int> OffsetMode = new();

        private enum Modes
        {
            Absolute,
            Relative,
            GlobalAbsolute,
        }
    }
}