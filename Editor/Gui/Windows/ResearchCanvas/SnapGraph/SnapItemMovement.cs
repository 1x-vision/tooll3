﻿using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using Vector2 = System.Numerics.Vector2;

// ReSharper disable UseWithExpressionToCopyStruct

namespace T3.Editor.Gui.Windows.ResearchCanvas.SnapGraph;

/// <remarks>
/// Things would be slightly more efficient if this would would use SnapGraphItems. However this would
/// prevent us from reusing fence selection. Enforcing this to be used for dragging inputs and outputs
/// makes this class unnecessarily complex.
/// </remarks>
public class SnapItemMovement
{
    public SnapItemMovement(SnapGraphCanvas snapGraphCanvas, SnapGraphLayout layout)
    {
        _canvas = snapGraphCanvas;
        _layout = layout;
    }

    /// <summary>
    /// Reset to avoid accidental dragging of previous elements 
    /// </summary>
    public static void Reset()
    {
        _modifyCommand = null;
        _draggedNodes.Clear();
    }

    /// <summary>
    /// For certain edge cases the release handling of nodes cannot be detected.
    /// This is a work around to clear the state on mouse release
    /// </summary>
    public static void CompleteFrame()
    {
        if (ImGui.IsMouseReleased(0) && _modifyCommand != null)
        {
            Reset();
        }
    }

    /// <summary>
    /// NOTE: This has to be called for ALL movable elements (ops, inputs, outputs) and directly after ImGui.Item
    /// </summary>
    public void Handle(SnapGraphItem item, SnapGraphCanvas canvas)
    {
        var justClicked = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var composition = item.Instance.Parent;
        var isActiveNode = item.Id == _draggedNodeId;
        if (justClicked)
        {
            var compositionSymbolId = composition.Symbol.Id;
            _draggedNodeId = item.Id;
            if (item.IsSelected)
            {
                _draggedNodes.Clear();
                foreach (var s in NodeSelection.Selection)
                {
                    if (_layout.Items.TryGetValue(s.Id, out var i))
                    {
                        _draggedNodes.Add(i);
                    }
                }
                //_draggedNodes = NodeSelection.GetSelectedNodes<SnapGraphItem>().ToHashSet();
            }
            else
            {
                _draggedNodes = new HashSet<SnapGraphItem> { item };
            }

            StartDragging(_draggedNodes);

            var snapGraphItems = _draggedNodes.Select(i => i as ISelectableCanvasObject).ToList();
            _modifyCommand = new ModifyCanvasElementsCommand(composition.Symbol.Id, snapGraphItems);
            //ShakeDetector.ResetShaking();
        }
        else if (isActiveNode && ImGui.IsMouseDown(ImGuiMouseButton.Left) && _modifyCommand != null)
        {
            // TODO: Implement shake disconnect later
            HandleNodeDragging(item, canvas);
        }
        else if (isActiveNode && ImGui.IsMouseReleased(0) && _modifyCommand != null)
        {
            if (_draggedNodeId != item.Id)
                return;

            var singleDraggedNode = (_draggedNodes.Count == 1) ? _draggedNodes.First() : null;
            _draggedNodeId = Guid.Empty;
            _draggedNodes.Clear();

            var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
            if (wasDragging)
            {
                _modifyCommand.StoreCurrentValues();

                if (singleDraggedNode != null
                    && ConnectionSplitHelper.BestMatchLastFrame != null
                    && singleDraggedNode is SnapGraphItem graphItem)
                {
                    //var instanceForSymbolChildUi = composition.Children.SingleOrDefault(child => child.SymbolChildId == graphItem.SymbolChildUi.Id);
                    ConnectionMaker.SplitConnectionWithDraggedNode(graphItem.SymbolChildUi,
                                                                   ConnectionSplitHelper.BestMatchLastFrame.Connection,
                                                                   graphItem.Instance,
                                                                   _modifyCommand);
                    _modifyCommand = null;
                }
                else
                {
                    UndoRedoStack.Add(_modifyCommand);
                }

                // Reorder inputs nodes if dragged
                var selectedInputs = NodeSelection.GetSelectedNodes<IInputUi>().ToList();
                if (selectedInputs.Count > 0)
                {
                    var compositionUi = SymbolUiRegistry.Entries[composition.Symbol.Id];
                    composition.Symbol.InputDefinitions.Sort((a, b) =>
                                                             {
                                                                 var childA = compositionUi.InputUis[a.Id];
                                                                 var childB = compositionUi.InputUis[b.Id];
                                                                 return (int)(childA.PosOnCanvas.Y * 10000 + childA.PosOnCanvas.X) -
                                                                        (int)(childB.PosOnCanvas.Y * 10000 + childB.PosOnCanvas.X);
                                                             });
                    composition.Symbol.SortInputSlotsByDefinitionOrder();
                    InputsAndOutputs.AdjustInputOrderOfSymbol(composition.Symbol);
                }
            }
            else
            {
                if (!NodeSelection.IsNodeSelected(item))
                {
                    var replaceSelection = !ImGui.GetIO().KeyShift;
                    if (replaceSelection)
                    {
                        NodeSelection.SetSelectionToChildUi(item.SymbolChildUi, item.Instance);
                    }
                    else
                    {
                        NodeSelection.AddSymbolChildToSelection(item.SymbolChildUi, item.Instance);
                    }
                }
                else
                {
                    if (ImGui.GetIO().KeyShift)
                    {
                        NodeSelection.DeselectNode(item, item.Instance);
                    }
                }
            }

            _modifyCommand = null;
        }
        else if (ImGui.IsMouseReleased(0) && _modifyCommand == null)
        {
            // This happens after shake
            _draggedNodes.Clear();
        }

        var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
            && !wasDraggingRight
            && ImGui.IsItemHovered()
            && !NodeSelection.IsNodeSelected(item))
        {
            NodeSelection.SetSelectionToChildUi(item.SymbolChildUi, item.Instance);
        }
    }

    private void StartDragging(HashSet<SnapGraphItem> draggedNodes)
    {
        _currentAppliedSnapOffset = Vector2.Zero;
        _lastAppliedOffset = Vector2.Zero;
        UpdateDragConnectionOnStart(draggedNodes);
    }

    public struct SnapResult
    {
        public float BestDistance;
        public SnapGraphItem BestA;
        public SnapGraphItem BestB;
        public SnapGraphItem.Directions Direction;
        public int InputLineIndex;
        public int MultiInputIndex;
        public int OutLineIndex;
        public Vector2 OutAnchorPos;
        public Vector2 InputAnchorPos;
        public bool Reverse;

        public bool TestAndKeepPositionsForSnapping(Vector2 outPos, Vector2 inputPos)
        {
            var d = Vector2.Distance(outPos, inputPos);
            if (d >= BestDistance)
                return false;

            BestDistance = d;
            OutAnchorPos = outPos;
            InputAnchorPos = inputPos;
            return true;
        }

        public void TestForSnap(SnapGraphItem a, SnapGraphItem b, bool revert)
        {
            MultiInputIndex = 0;
            for (var bInputLineIndex = 0; bInputLineIndex < b.InputLines.Length; bInputLineIndex++)
            {
                ref var bInputLine = ref b.InputLines[bInputLineIndex];
                var inConnection = bInputLine.Connection;

                for (var aOutLineIndex = 0; aOutLineIndex < a.OutputLines.Length; aOutLineIndex++)
                {
                    ref var ol = ref a.OutputLines[aOutLineIndex];
                    
                    // A -> B vertical
                    if (aOutLineIndex == 0 && bInputLineIndex == 0)
                    {
                        // If input is connected the only valid output is the one with the connection line

                        var outPos = new Vector2(a.Area.Min.X + SnapGraphItem.WidthHalf, a.Area.Max.Y);
                        var inPos = new Vector2(b.Area.Min.X + SnapGraphItem.WidthHalf, b.Area.Min.Y);

                        if (inConnection != null)
                        {
                            foreach (var c in ol.Connections)
                            {
                                if (c != inConnection)
                                    continue;

                                if (TestAndKeepPositionsForSnapping(outPos, inPos))
                                {
                                    Direction = SnapGraphItem.Directions.Vertical;
                                    OutLineIndex = aOutLineIndex;
                                    InputLineIndex = bInputLineIndex;
                                    MultiInputIndex = 0;
                                    BestA = a;
                                    BestB = b;
                                    Reverse = revert;
                                }
                            }
                        }
                        else
                        {
                            if (TestAndKeepPositionsForSnapping(outPos, inPos))
                            {
                                Direction = SnapGraphItem.Directions.Vertical;
                                OutLineIndex = aOutLineIndex;
                                InputLineIndex = bInputLineIndex;
                                MultiInputIndex = 0;
                                BestA = a;
                                BestB = b;
                                Reverse = revert;
                            }
                        }
                    }

                    // // A -> B horizontally
                    {
                        var outPos = new Vector2(a.Area.Max.X, a.Area.Min.Y + (0.5f + ol.VisibleIndex) * SnapGraphItem.LineHeight);
                        var inPos = new Vector2(b.Area.Min.X, b.Area.Min.Y + (0.5f + bInputLine.VisibleIndex) * SnapGraphItem.LineHeight);
                        
                        if (TestAndKeepPositionsForSnapping(outPos, inPos))
                        {
                            Direction = SnapGraphItem.Directions.Horizontal;
                            OutLineIndex = aOutLineIndex;
                            InputLineIndex = bInputLineIndex;
                            MultiInputIndex = bInputLine.MultiInputIndex;
                            BestA = a;
                            BestB = b;
                            Reverse = revert;
                        }
                    }
                }
            }

            Direction = SnapGraphItem.Directions.Horizontal;
        }
    }

    private void HandleNodeDragging(ISelectableCanvasObject draggedNode, ScalableCanvas canvas)
    {
        if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            _isDragging = false;
            return;
        }

        if (!_isDragging)
        {
            _dragStartPosInOpOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
            _isDragging = true;
        }

        var dl = ImGui.GetWindowDrawList();
        var showDebug = true; //ImGui.GetIO().KeyCtrl;
        var mousePosOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
        var requestedDeltaOnCanvas = mousePosOnCanvas - _dragStartPosInOpOnCanvas;

        var dragExtend = SnapGraphItem.GetGroupBoundingBox(_draggedNodes);
        dragExtend.Expand(SnapThreshold * canvas.Scale.X);

        if (showDebug)
        {
            dl.AddCircle(_canvas.TransformPosition(_dragStartPosInOpOnCanvas), 10, Color.Blue);

            dl.AddLine(_canvas.TransformPosition(_dragStartPosInOpOnCanvas),
                       _canvas.TransformPosition(_dragStartPosInOpOnCanvas + requestedDeltaOnCanvas), Color.Blue);

            dl.AddRect(_canvas.TransformPosition(dragExtend.Min),
                       _canvas.TransformPosition(dragExtend.Max),
                       Color.Green.Fade(0.1f));
        }

        var overlappingItems = new List<SnapGraphItem>();
        foreach (var otherItem in _layout.Items.Values)
        {
            if (_draggedNodes.Contains(otherItem) || !dragExtend.Overlaps(otherItem.Area))
                continue;

            overlappingItems.Add(otherItem);
        }
        
        // New possible ConnectionsOptions
        List<Symbol.Connection> newPossibleConnections = new();

        // Move back to non-snapped position
        foreach (var n in _draggedNodes)
        {
            n.PosOnCanvas -= _lastAppliedOffset; // Move to position
            n.PosOnCanvas += requestedDeltaOnCanvas; // Move to request position
        }

        _lastAppliedOffset = requestedDeltaOnCanvas;

        var snapResult = new SnapResult() { BestDistance = float.PositiveInfinity };

        foreach (var otherItem in overlappingItems)
        {
            foreach (var draggedItem in _draggedNodes)
            {
                snapResult.TestForSnap(otherItem, draggedItem, false);
                snapResult.TestForSnap(draggedItem, otherItem , true);
            }
        }

        // Snapped
        if (snapResult.BestDistance < SnapGraphItem.LineHeight * 0.5f)
        {
            var bestSnapDelta = !snapResult.Reverse
                                    ? snapResult.OutAnchorPos - snapResult.InputAnchorPos
                                    : snapResult.InputAnchorPos - snapResult.OutAnchorPos;

            dl.AddLine(_canvas.TransformPosition(mousePosOnCanvas),
                       canvas.TransformPosition(mousePosOnCanvas) + _canvas.TransformDirection(bestSnapDelta),
                       Color.White);

            if (Vector2.Distance(snapResult.InputAnchorPos, LastSnapPositionOnCanvas) > 2)
            {
                LastSnapTime = ImGui.GetTime();
                LastSnapPositionOnCanvas = snapResult.InputAnchorPos;
            }

            foreach (var n in _draggedNodes)
            {
                n.PosOnCanvas += bestSnapDelta;
            }

            _lastAppliedOffset += bestSnapDelta;
        }
        else
        {
            LastSnapPositionOnCanvas = Vector2.Zero;
            LastSnapTime = double.NegativeInfinity;
        }
    }

    private void UpdateDragConnectionOnStart(HashSet<SnapGraphItem> draggedItems)
    {
        _bridgeConnectionsOnStart.Clear();
        foreach (var c in _layout.SnapConnections)
        {
            var targetDragged = draggedItems.Contains(c.TargetItem);
            var sourceDragged = draggedItems.Contains(c.TargetItem);
            if (targetDragged != sourceDragged)
            {
                _bridgeConnectionsOnStart.Add(c);
            }
        }
    }

    public double LastSnapTime = double.NegativeInfinity;
    public Vector2 LastSnapPositionOnCanvas;

    private Vector2 _currentAppliedSnapOffset;
    private Vector2 _lastAppliedOffset;

    private const float SnapThreshold = 10;
    private readonly List<SnapGraphConnection> _bridgeConnectionsOnStart = new();

    private static bool _isDragging;
    private static Vector2 _dragStartPosInOpOnCanvas;

    private static ModifyCanvasElementsCommand _modifyCommand;

    private static Guid _draggedNodeId = Guid.Empty;
    private static HashSet<SnapGraphItem> _draggedNodes = new();
    private readonly SnapGraphCanvas _canvas;
    private readonly SnapGraphLayout _layout;
}