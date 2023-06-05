using System;
using System.Collections.Generic;

namespace T3.Core.Operator.Slots
{
    public class InputSlot<T> : Slot<T>, IInputSlot
    {
        public Type MappedType { get; set; }
        public List<int> LimitMultiInputInvalidationToIndices { get; set; }

        public InputSlot(InputValue<T> typedInputValue)
        {
            UpdateAction = InputUpdate;
            _keepOriginalUpdateAction = UpdateAction;
            TypedInputValue = typedInputValue;
            Value = typedInputValue.Value;
        }

        public InputSlot() : this(default(T))
        {
            UpdateAction = InputUpdate;
            _keepOriginalUpdateAction = UpdateAction;
        }

        public InputSlot(T value) : this(new InputValue<T>(value))
        {
        }

        public void InputUpdate(EvaluationContext context)
        {
            Value = Input.IsDefault ? TypedDefaultValue.Value : TypedInputValue.Value;
        }

        private SymbolChild.Input _input;

        public SymbolChild.Input Input
        {
            get => _input;
            set
            {
                _input = value;
                TypedInputValue = (InputValue<T>)value.Value;
                TypedDefaultValue = (InputValue<T>)value.DefaultValue;

                if (_input.IsDefault && TypedDefaultValue.IsEditableInputReferenceType)
                {
                    TypedInputValue.AssignClone(TypedDefaultValue);
                }
            }
        }

        public void SetTypedInputValue(T newValue)
        {
            Input.IsDefault = false;
            TypedInputValue.Value = newValue;
            DirtyFlag.Invalidate();
        }

        public InputValue<T> TypedInputValue;
        public InputValue<T> TypedDefaultValue;
    }
}