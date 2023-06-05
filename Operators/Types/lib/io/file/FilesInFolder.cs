using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_f90fcd0a_eab9_4e2a_b393_e8d3a0380823
{
    public class FilesInFolder : Instance<FilesInFolder>
    {
        [Output(Guid = "99bd5b48-7a28-44a7-91e4-98b33cfda20f")]
        public readonly Slot<List<string>> Files = new Slot<List<string>>();

        [Output(Guid = "a40ea23c-e64a-4cca-ae3c-d447dbf7ef93")]
        public readonly Slot<int> NumberOfFiles = new Slot<int>();


        public FilesInFolder()
        {
            Files.UpdateAction = Update;
            NumberOfFiles.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var wasTriggered = MathUtils.WasTriggered(TriggerUpdate.GetValue(context), ref _trigger);
            if (wasTriggered || Folder.DirtyFlag.IsDirty)
            {
                TriggerUpdate.SetTypedInputValue(false);
                
                var folderPath = Folder.GetValue(context);
                var filter = Filter.GetValue(context);
                var filePaths = Directory.Exists(folderPath) 
                                  ? Directory.GetFiles(folderPath).ToList() 
                                  : new List<string>();
                
                Files.Value = string.IsNullOrEmpty(Filter.Value) 
                                  ? filePaths 
                                  : filePaths.FindAll(filepath => filepath.Contains(filter)).ToList();
            }
            

            
            NumberOfFiles.Value = Files.Value.Count;
        }

        private bool _trigger;

        [Input(Guid = "ca9778e7-072c-4304-9043-eeb2dc4ca5d7")]
        public readonly InputSlot<string> Folder = new InputSlot<string>(".");
        
        [Input(Guid = "8B746651-16A5-4274-85DB-0168D30C86B2")]
        public readonly InputSlot<string> Filter = new InputSlot<string>("*.png");
        
        [Input(Guid = "E14A4AAE-E253-4D14-80EF-A90271CD306A")]
        public readonly InputSlot<bool> TriggerUpdate = new InputSlot<bool>();

    }
}