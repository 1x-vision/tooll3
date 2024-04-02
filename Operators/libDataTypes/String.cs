using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.types
{
	[Guid("5880cbc3-a541-4484-a06a-0e6f77cdbe8e")]
    public class String : Instance<String>, IExtractedInput<string>
    {
        [Output(Guid = "dd9d8718-addc-49b1-bd33-aac22b366f94")]
        public readonly Slot<string> Result = new();

        public String()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = InputString.GetValue(context);
        }
        
        [Input(Guid = "ceeae47b-d792-471d-a825-49e22749b7b9")]
        public readonly InputSlot<string> InputString = new();
        

        public Slot<string> OutputSlot => Result;

        public void SetTypedInputValuesTo(string value)
        {
            InputString.Input.IsDefault = false;
            InputString.TypedInputValue.Value = value;
        }
    }
}
