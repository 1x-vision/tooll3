using System.Collections.Generic;
using System.ComponentModel.Design;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_7ad3a38a_9f04_43ba_a16f_6982b87dd2d4
{
    public class AnalyzeBuffers : Instance<AnalyzeBuffers>
    {
        [Output(Guid = "4906B2CE-7AAF-4025-B48E-49E6D660C13B")]
        public readonly Slot<int> BufferCount = new();

        [Output(Guid = "5638E071-A0A7-4EE2-AA04-9B651821BEBB")]
        public readonly Slot<BufferWithViews> SelectedBuffer = new();

        [Output(Guid = "D7BBD6D5-57EB-4C3A-8C84-E497E490AF83")]
        public readonly Slot<int> StartPositionForSelected = new();

        [Output(Guid = "0702a722-0b93-4840-9abd-f8ee348c3647")]
        public readonly Slot<int> TotalSize = new();

        public AnalyzeBuffers()
        {
            BufferCount.UpdateAction = Update;
            StartPositionForSelected.UpdateAction = Update;
            TotalSize.UpdateAction = Update;
            SelectedBuffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var connections = Input.GetCollectedTypedInputs();
            var selectedIndex = Index.GetValue(context).Clamp(0, connections.Count-1);

            if (connections.Count == 0)
            {
                TotalSize.Value = 0;
                StartPositionForSelected.Value = 0;
                BufferCount.Value = 0;
                return;
            }

            var totalSize = 0;
            var startPosition = 0;
            BufferWithViews selectedBuffer = null;
            var hadErrors = false;
            for (var connectionIndex = 0; connectionIndex < connections.Count; connectionIndex++)
            {
                var input = connections[connectionIndex];
                
                var bufferWithViews = input.GetValue(context);
                if (bufferWithViews !=null && bufferWithViews.Srv != null)
                {
                    var length = bufferWithViews.Srv.Description.Buffer.ElementCount;

                    if (connectionIndex == selectedIndex)
                    {
                        startPosition = totalSize;
                        selectedBuffer = input.Value;
                    }
                    totalSize += length;
                }
                else
                {
                    hadErrors = true;
                    if (_complainedOnces)
                        continue;
                    
                    Log.Warning($"Undefined BufferWithViews at index {connectionIndex}", this);
                    _complainedOnces = true;
                }
            }

            if (!hadErrors)
            {
                _complainedOnces = false;
            }

            SelectedBuffer.Value = selectedBuffer;
            StartPositionForSelected.Value = startPosition;
            BufferCount.Value = connections.Count; 
            TotalSize.Value = totalSize;
        }

        private bool _complainedOnces;
        
        [Input(Guid = "c8a5769e-2536-4caa-8380-22fbeed1ef12")]
        public readonly MultiInputSlot<BufferWithViews> Input = new();

        [Input(Guid = "bf9c64ac-39b5-41c0-a896-84809b12fff6")]
        public readonly InputSlot<int> Index = new();

    }
}