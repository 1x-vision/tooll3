using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_72b6327d_4304_48bb_b9b5_15705352c225
{
    public class LoadSoundtrack : Instance<LoadSoundtrack>
    {
        [Output(Guid = "a9d725ec-57dc-4e25-9e25-a6dc2ff2b056")]
        public readonly Slot<Command> Output = new();

        public LoadSoundtrack()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var commands = Command.CollectedInputs;
            if (IsEnabled.GetValue(context))
            {
                // do preparation if needed
                for (int i = 0; i < commands.Count; i++)
                {
                    commands[i].Value?.PrepareAction?.Invoke(context);
                }

                // execute commands
                for (int i = 0; i < commands.Count; i++)
                {
                    commands[i].GetValue(context);
                }

                // cleanup after usage
                for (int i = 0; i < commands.Count; i++)
                {
                    commands[i].Value?.RestoreAction?.Invoke(context);
                }
            }

            Command.DirtyFlag.Clear();
        }
        
        [Input(Guid = "c321b833-d42f-46f7-b576-046f114f3d54")]
        public readonly MultiInputSlot<Command> Command = new();

        [Input(Guid = "21de464d-2abf-405c-9b48-8c7705b98b9d")]
        public readonly InputSlot<bool> IsEnabled = new();

    }
}