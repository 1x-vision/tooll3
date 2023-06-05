﻿using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator
{
    

    /// <summary>
    /// Represents an instance of a <see cref="Symbol"/> within a combined symbol group.
    /// </summary>
    public class SymbolChild
    {
        /// <summary>A reference to the <see cref="Symbol"/> this is an instance from.</summary>
        public Symbol Symbol { get; }

        public Guid Id { get; }
        
        public Symbol Parent { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ReadableName => string.IsNullOrEmpty(Name) ? Symbol.Name : Name;

        public bool IsBypassed {
            get => _isBypassed;
            set => SetBypassed(value);
        }
        
        public Dictionary<Guid, Input> Inputs { get; } = new();
        public Dictionary<Guid, Output> Outputs { get; } = new();

        public SymbolChild(Symbol symbol, Guid childId, Symbol parent)
        {
            Symbol = symbol;
            Id = childId;
            Parent = parent;

            foreach (var inputDefinition in symbol.InputDefinitions)
            {
                if(!Inputs.TryAdd(inputDefinition.Id, new Input(inputDefinition)))
                {  
                    throw new ApplicationException($"The ID for symbol input {symbol.Name}.{inputDefinition.Name} must be unique.");
                }
            }

            foreach (var outputDefinition in symbol.OutputDefinitions)
            {
                var outputData = (outputDefinition.OutputDataType != null) ? (Activator.CreateInstance(outputDefinition.OutputDataType) as IOutputData) : null;
                var output = new Output(outputDefinition, outputData) { DirtyFlagTrigger = outputDefinition.DirtyFlagTrigger };
                if (!Outputs.TryAdd(outputDefinition.Id, output))
                {
                    throw new ApplicationException($"The ID for symbol output {symbol.Name}.{outputDefinition.Name} must be unique.");
                }
            }
        }

        #region sub classes =============================================================

        public class Output
        {
            public Symbol.OutputDefinition OutputDefinition { get; }
            public IOutputData OutputData { get; }

            public bool IsDisabled { get; set; }
            
            public DirtyFlagTrigger DirtyFlagTrigger
            {
                get => _dirtyFlagTrigger ?? OutputDefinition.DirtyFlagTrigger;
                set => _dirtyFlagTrigger = (value != OutputDefinition.DirtyFlagTrigger) ? (DirtyFlagTrigger?)value : null;
            }

            private DirtyFlagTrigger? _dirtyFlagTrigger = null;

            public Output(Symbol.OutputDefinition outputDefinition, IOutputData outputData)
            {
                OutputDefinition = outputDefinition;
                OutputData = outputData;
            }
        }

        public class Input
        {
            public Symbol.InputDefinition InputDefinition { get; }
            public InputValue DefaultValue => InputDefinition.DefaultValue;

            public string Name => InputDefinition.Name;

            /// <summary>The input value used for this symbol child</summary>
            public InputValue Value { get; }

            public bool IsDefault { get; set; }

            public Input(Symbol.InputDefinition inputDefinition)
            {
                InputDefinition = inputDefinition;
                Value = DefaultValue.Clone();
                IsDefault = true;
            }

            public void SetCurrentValueAsDefault()
            {
                if (DefaultValue.IsEditableInputReferenceType)
                {
                    DefaultValue.AssignClone(Value);
                }
                else
                {
                    DefaultValue.Assign(Value);
                }

                IsDefault = true;
            }

            public void ResetToDefault()
            {
                if (DefaultValue.IsEditableInputReferenceType)
                {
                    Value.AssignClone(DefaultValue);
                }
                else
                {
                    Value.Assign(DefaultValue);
                }
                IsDefault = true;
            }
        }

        #endregion
        

        private bool _isBypassed;
        
        private bool IsBypassable()
        {
            if(Symbol.OutputDefinitions.Count == 0)
                return false;
            
            if(Symbol.InputDefinitions.Count == 0)
                return false;

            var mainInput = Symbol.InputDefinitions[0];
            var mainOutput = Symbol.OutputDefinitions[0];

            if (mainInput.DefaultValue.ValueType != mainOutput.ValueType)
                return false;
            
            if(mainInput.DefaultValue.ValueType == typeof(Command))
                return true;
            
            if(mainInput.DefaultValue.ValueType == typeof(Texture2D))
                return true;

            if(mainInput.DefaultValue.ValueType == typeof(BufferWithViews))
                return true;

            if(mainInput.DefaultValue.ValueType == typeof(MeshBuffers))
                return true;

            if(mainInput.DefaultValue.ValueType == typeof(float))
                return true;

            return false;
        }

        private void SetBypassed(bool shouldBypass)
        {
            if(!IsBypassable())
                return;

            if (Parent == null)
            {
                _isBypassed = true;  // during loading parents are not yet assigned. This flag will later be used when creating instances
                return;
            }
            
            
            var parentInstancesOfSymbol = Parent.InstancesOfSymbol;
            foreach (var parentInstance in parentInstancesOfSymbol)
            {
                var instance = parentInstance.Children.First(child => child.SymbolChildId == Id);

                var mainInputSlot = instance.Inputs[0];
                var mainOutputSlot = instance.Outputs[0];

                var wasByPassed = false;
                
                switch (mainOutputSlot)
                {
                    case Slot<Command> commandOutput when mainInputSlot is Slot<Command> commandInput:
                        if (shouldBypass)
                        {
                            wasByPassed= commandOutput.TrySetBypassToInput(commandInput);
                        }
                        else
                        {
                            commandOutput.RestoreUpdateAction();
                        }
                        break;
                    
                    case Slot<BufferWithViews> bufferOutput when mainInputSlot is Slot<BufferWithViews> bufferInput:
                        if (shouldBypass)
                        {
                            wasByPassed= bufferOutput.TrySetBypassToInput(bufferInput);
                        }
                        else
                        {
                            bufferOutput.RestoreUpdateAction();
                        }
                        break;
                    case Slot<MeshBuffers> bufferOutput when mainInputSlot is Slot<MeshBuffers> bufferInput:
                        if (shouldBypass)
                        {
                            wasByPassed= bufferOutput.TrySetBypassToInput(bufferInput);
                        }
                        else
                        {
                            bufferOutput.RestoreUpdateAction();
                        }
                        break;
                    case Slot<Texture2D> texture2dOutput when mainInputSlot is Slot<Texture2D> texture2dInput:
                        if (shouldBypass)
                        {
                            wasByPassed= texture2dOutput.TrySetBypassToInput(texture2dInput);
                        }
                        else
                        {
                            texture2dOutput.RestoreUpdateAction();
                        }
                        break;
                    case Slot<float> floatOutput when mainInputSlot is Slot<float> floatInput:
                        if (shouldBypass)
                        {
                            wasByPassed= floatOutput.TrySetBypassToInput(floatInput);
                        }
                        else
                        {
                            floatOutput.RestoreUpdateAction();
                        }
                        break;
                }

                _isBypassed = wasByPassed;
            }
        }

        public override string ToString()
        {
            return Parent.Name + ">" + ReadableName;
        }
    }
}
