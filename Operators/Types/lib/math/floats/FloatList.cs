using System.Collections.Generic;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_7ba0451c_9b8d_46a7_bfac_9dcb15e00023
{
    public class FloatList : Instance<FloatList>
    {
        [Output(Guid = "918a871a-1ed6-4d40-a73e-05dbb3399b38")]
        public readonly Slot<List<float>> Result = new(new List<float>(20));

        public FloatList()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var defaultValue = DefaultValue.GetValue(context);
            
            var reset = ResetTrigger.GetValue(context);
            
            var length = ListLenght.GetValue(context).Clamp(0, 100000);
            if (length != _list.Count || reset)
            {
                _list.Clear();
                _list.Capacity = length;
                for (var i = 0; i < length; i++)
                {
                    _list.Add(defaultValue);
                }
            }
            
            Result.Value = _list;

        }
        
        private readonly List<float> _list = new(20);
        
        [Input(Guid = "2B51B0BE-25D1-4B7F-832D-8BC5498E5620")]
        public readonly InputSlot<bool> ResetTrigger = new();
        
        [Input(Guid = "5f2b5280-a157-4499-b3fa-60939a9e3ddd")]
        public readonly InputSlot<float> DefaultValue = new();
        
        [Input(Guid = "A81D55FE-60FB-453F-A6C9-F08932066213")]
        public readonly InputSlot<int> ListLenght = new();

    }
}