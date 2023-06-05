using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_bfe540ef_f8ad_45a2_b557_cd419d9c8e44
{
    public class DataList : Instance<DataList>
    {
        [Output(Guid = "d117b613-c41b-42ce-889d-502a8c779fff")]
        public readonly Slot<StructuredList> Result = new Slot<StructuredList>();

        
        public DataList()
        {
            Result.UpdateAction = Update;
        }


        private void Update(EvaluationContext context)
        {
            Result.Value = InputList.GetValue(context);
        }


        [Input(Guid = "669AE1E4-DD47-4369-83C6-26D2705ABF7B")]
        public readonly InputSlot<StructuredList> InputList = new InputSlot<StructuredList>();
    }
}