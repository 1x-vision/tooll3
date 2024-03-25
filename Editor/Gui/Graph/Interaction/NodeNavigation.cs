﻿using System.Diagnostics;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction;

internal class NodeNavigation
{
    public NodeNavigation(Func<Instance> getComposition, GraphCanvas canvas, Structure structure, NavigationHistory navigationHistory)
    {
        _canvas = canvas;
        _structure = structure;
        _navigationHistory = navigationHistory;
        _getComposition = getComposition;
    }
    
    public void SelectAbove()
    {
        TryMoveSelectionTowards(Directions.Up);
    }
    
    public void SelectBelow()
    {
        TryMoveSelectionTowards(Directions.Down);
    }
    
    public void SelectLeft()
    {
        TryMoveSelectionTowards(Directions.Left);
    }

    public void SelectRight()
    {
        TryMoveSelectionTowards(Directions.Right);
    }
    
    
    private void TryMoveSelectionTowards(Directions direction)
    {
        var composition = _getComposition();
        var currentInstance = _navigationHistory.GetLastSelectedInstance();

        var symbolUi = composition.Symbol.GetSymbolUi();
        var currentSymbolChildUi = composition.GetSymbolChildUiWithId(currentInstance.SymbolChildId);
        
        // Search all children
        SymbolChildUi bestMatch = null;
        var bestRelevancy = float.PositiveInfinity;
        foreach (var otherChildUi in symbolUi.ChildUis.Values)
        {
            var alignedDelta = GetAlignedDelta(direction, otherChildUi.PosOnCanvas - currentSymbolChildUi.PosOnCanvas);

            if (otherChildUi == currentSymbolChildUi)
            {
                continue;
            }
            
            if (alignedDelta.X <= 0)
                continue;

            var r = alignedDelta.X + alignedDelta.Y * 5;
            if (r > bestRelevancy)
                continue;
            
            bestMatch = otherChildUi;
            bestRelevancy = r;
        }

        if (bestMatch == null)
        {
            return;
        }

        var bestInstance = currentInstance.Parent.Children[bestMatch.Id];

        //Log.Debug($"Found with relevancy {bestRelevancy}: " + Structure.GetReadableInstancePath( OperatorUtils.BuildIdPathForInstance( bestInstance)), bestInstance);
        
        var path = OperatorUtils.BuildIdPathForInstance(bestInstance);
        if (_structure.GetInstanceFromIdPath(path) == null)
            return;
            
        _canvas.OpenAndFocusInstance(path);

        if (!ParameterWindow.IsAnyInstanceVisible())
        {
            ParameterPopUp.Open(bestInstance);
        }
    }

    private static Vector2 GetAlignedDelta(Directions direction, Vector2 deltaOnCanvas)
    {
        return direction switch
                   {
                       Directions.Up    => new Vector2(-deltaOnCanvas.Y, MathF.Abs(deltaOnCanvas.X)),
                       Directions.Right => new Vector2(deltaOnCanvas.X, MathF.Abs(deltaOnCanvas.Y)),
                       Directions.Down  => new Vector2(deltaOnCanvas.Y, MathF.Abs(deltaOnCanvas.X)),
                       Directions.Left  => new Vector2(-deltaOnCanvas.X, MathF.Abs(deltaOnCanvas.Y)),
                       _                => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
                   };
    }
    
    
    private enum Directions
    {
        Up,
        Right,
        Down,
        Left,
    }

    private readonly Func<Instance> _getComposition;
    private readonly GraphCanvas _canvas;
    private readonly Structure _structure;
    private readonly NavigationHistory _navigationHistory;
}