﻿using System;
using T3.Core.Operator;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Commands.Graph
{
    public class ChangeSymbolChildNameCommand : ICommand
    {
        public string Name => "Change Symbol Name";
        public bool IsUndoable => true;

        public ChangeSymbolChildNameCommand(SymbolUi.Child symbolChildUi, Symbol parentSymbol)
        {
            _symbolChildId = symbolChildUi.SymbolChild.Id;
            _parentSymbolId = parentSymbol.Id;
            NewName = _originalName = symbolChildUi.SymbolChild.Name;
        }

        public void Do()
        {
            AssignValue(NewName);
        }

        public void Undo()
        {
            AssignValue(_originalName);
        }

        private void AssignValue(string newName)
        {
            if (!SymbolUiRegistry.TryGetSymbolUi(_parentSymbolId, out var symbolUi))
                throw new Exception("Symbol not found: " + _parentSymbolId);
            var symbolParent = symbolUi.Symbol;
            var symbol = symbolParent.Children[_symbolChildId];
            symbol.Name = newName;
        }

        public string NewName { get; set; }
        private readonly string _originalName;
        private readonly Guid _symbolChildId;
        private readonly Guid _parentSymbolId;
    }
}