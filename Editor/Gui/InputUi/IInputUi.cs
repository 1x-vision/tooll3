﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.InputUi
{
    [Flags]
    public enum InputEditStateFlags
    {
        Nothing = 0x0,
        Started = 0x1,
        Modified = 0x2,
        Finished = 0x4,
        ShowOptions = 0x8,
        ResetToDefault = 0x10,
        ModifiedAndFinished = Modified | Finished
    }

    public enum Relevancy
    {
        Required,
        Relevant,
        Optional
    }

    public interface IInputUi : ISelectableCanvasObject
    {
        SymbolUi Parent { get; set; }
        Symbol.InputDefinition InputDefinition { get; set; }
        Type Type { get; }
        Relevancy Relevancy { get; set; }
        string GroupTitle { get; set; }
        
        /**Insert a vertical padding */
        bool AddPadding { get; set; }
        bool IsAnimatable { get; }

        string Description { get; set; }
        
        void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time);
        
        InputEditStateFlags DrawParameterEdit(IInputSlot input, SymbolUi compositionUi, SymbolChildUi symbolChildUi, bool hideNonEssentials, bool skipIfDefault);
        string GetSlotValue(IInputSlot inputSlot);
        void DrawSettings();
        void DrawDescriptionEdit();

        IInputUi Clone();
        void Write(JsonTextWriter writer);
        void Read(JToken inputToken);
    }
}