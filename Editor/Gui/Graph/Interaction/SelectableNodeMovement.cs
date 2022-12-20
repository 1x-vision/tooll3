using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Graph.Interaction
{
    /// <summary>
    /// Handles selection and dragging (with snapping) of node canvas elements
    /// </summary>
    public static class SelectableNodeMovement
    {
        /// <summary>
        /// Reset to avoid accidental dragging of previous elements 
        /// </summary>
        public static void Reset()
        {
            _moveCommand = null;
            _draggedNodes.Clear();
        }

        /// <summary>
        /// For certain edge cases the release handling of nodes cannot be detected.
        /// This is a work around to clear the state on mouse release
        /// </summary>
        public static void CompleteFrame()
        {
            if (ImGui.IsMouseReleased(0) && _moveCommand != null)
            {
                Reset();    
            }
        }

        
        /// <summary>
        /// NOTE: This has to be called for ALL movable elements and directly after ImGui.Item
        /// </summary>
        public static void Handle(ISelectableCanvasObject node, Instance instance = null)
        {
            if (ImGui.IsItemActive())
            {
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    var compositionSymbolId = GraphCanvas.Current.CompositionOp.Symbol.Id;
                    _draggedNodeId = node.Id;
                    if (node.IsSelected)
                    {
                        _draggedNodes = NodeSelection.GetSelectedNodes<ISelectableCanvasObject>().ToList();
                    }
                    else
                    {
                        var parentUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
                        if(UserSettings.Config.SmartGroupDragging)
                            _draggedNodes = FindSnappedNeighbours(parentUi, node).ToList();
                        
                        _draggedNodes.Add(node);
                    }

                    _moveCommand = new ModifyCanvasElementsCommand(compositionSymbolId, _draggedNodes);
                    ShakeDetector.Reset();
                }
                else if (_moveCommand != null)
                {
                    if (!T3Ui.IsCurrentlySaving && ShakeDetector.TestDragForShake(ImGui.GetMousePos()))
                    {
                        _moveCommand.StoreCurrentValues();
                        UndoRedoStack.Add(_moveCommand);
                        _moveCommand = null;
                        DisconnectDraggedNodes();
                    }
                }

                HandleNodeDragging(node);
            }
            else if (ImGui.IsMouseReleased(0) && _moveCommand != null)
            {
                if (_draggedNodeId != node.Id)
                    return;

                var singleDraggedNode = (_draggedNodes.Count == 1) ? _draggedNodes[0] : null;
                _draggedNodeId = Guid.Empty;
                _draggedNodes.Clear();

                var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
                if (wasDragging)
                {
                    _moveCommand.StoreCurrentValues();
                    UndoRedoStack.Add(_moveCommand);

                    if (singleDraggedNode != null && ConnectionMaker.ConnectionSplitHelper.BestMatchLastFrame != null && singleDraggedNode is SymbolChildUi childUi)
                    {
                        var instanceForSymbolChildUi = GraphCanvas.Current.CompositionOp.Children.SingleOrDefault(child => child.SymbolChildId == childUi.Id);
                        ConnectionMaker.SplitConnectionWithDraggedNode(childUi, 
                                                                       ConnectionMaker.ConnectionSplitHelper.BestMatchLastFrame.Connection, 
                                                                       instanceForSymbolChildUi);
                    }

                    // Reorder inputs nodes if dragged
                    var selectedInputs = NodeSelection.GetSelectedNodes<IInputUi>().ToList();
                    if (selectedInputs.Count > 0)
                    {
                        var composition = GraphCanvas.Current.CompositionOp;
                        var compositionUi = SymbolUiRegistry.Entries[composition.Symbol.Id];
                        composition.Symbol.InputDefinitions.Sort((a, b) =>
                                                                 {
                                                                     var childA = compositionUi.InputUis[a.Id];
                                                                     var childB = compositionUi.InputUis[b.Id];
                                                                     return (int)(childA.PosOnCanvas.Y * 10000 + childA.PosOnCanvas.X) -
                                                                            (int)(childB.PosOnCanvas.Y * 10000 + childB.PosOnCanvas.X);
                                                                 });
                        composition.Symbol.SortInputSlotsByDefinitionOrder();
                        NodeOperations.AdjustInputOrderOfSymbol(composition.Symbol);
                    }
                }
                else
                {
                    if (!NodeSelection.IsNodeSelected(node))
                    {
                        if (!ImGui.GetIO().KeyShift)
                        {
                            NodeSelection.Clear();
                        }

                        if (node is SymbolChildUi childUi2)
                        {
                            NodeSelection.AddSymbolChildToSelection(childUi2, instance);
                        }
                        else
                        {
                            NodeSelection.AddSelection(node);
                        }
                    }
                    else
                    {
                        if (ImGui.GetIO().KeyShift)
                        {
                            NodeSelection.DeselectNode(node, instance);
                        }
                    }
                }

                _moveCommand = null;
            }
            else if (ImGui.IsMouseReleased(0) && _moveCommand == null)
            {
                // This happens after shake
                _draggedNodes.Clear();
            }


            var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
                && !wasDraggingRight
                && ImGui.IsItemHovered()
                && !NodeSelection.IsNodeSelected(node))
            {
                if (node is SymbolChildUi childUi2)
                {
                    NodeSelection.SetSelectionToChildUi(childUi2, instance);
                }
                else
                {
                    NodeSelection.SetSelection(node);
                }
            }
        }

        private static void DisconnectDraggedNodes()
        {
            var removeCommands = new List<ICommand>();
            var inputConnections = new List<(Symbol.Connection connection, Type connectionType, bool isMultiIndex, int multiInputIndex)>();
            var outputConnections = new List<(Symbol.Connection connection, Type connectionType, bool isMultiIndex, int multiInputIndex)>();
            foreach (var node in _draggedNodes)
            {
                if (node is not SymbolChildUi childUi)
                    continue;

                var instance = GraphCanvas.Current.CompositionOp.Children.SingleOrDefault(child => child.SymbolChildId == childUi.Id);
                if (instance == null)
                {
                    Log.Error("Can't disconnect missing instance");
                    continue;
                }

                // Get all input connections and
                // relative index if they have multi-index inputs
                var connectionsToInput = instance.Parent.Symbol.Connections.FindAll(c => c.TargetParentOrChildId == instance.SymbolChildId
                                                                                   && _draggedNodes.All(c2 => c2.Id != c.SourceParentOrChildId));                
                var inConnectionInputIndex = 0;
                foreach (var connectionToInput in connectionsToInput)
                {
                    bool isMultiInput = instance.Parent.Symbol.IsTargetMultiInput(connectionToInput);
                    if (isMultiInput)
                    {
                        inConnectionInputIndex = instance.Parent.Symbol.GetMultiInputIndexFor(connectionToInput);
                    }
                    Type connectionType = instance.Inputs.Find(c => c.Id == connectionToInput.TargetSlotId).ValueType;
                    inputConnections.Add((connectionToInput, connectionType, isMultiInput, isMultiInput ? inConnectionInputIndex : 0));
                }

                // Get all output connections and
                // relative index if they have multi-index inputs
                var connectionsToOutput = instance.Parent.Symbol.Connections.FindAll(c => c.SourceParentOrChildId == instance.SymbolChildId
                                                                                    && _draggedNodes.All(c2 => c2.Id != c.TargetParentOrChildId));
                var outConnectionInputIndex = 0;
                foreach (var connectionToOutput in connectionsToOutput)
                {
                    bool isMultiInput = instance.Parent.Symbol.IsTargetMultiInput(connectionToOutput);
                    if (isMultiInput)
                    {
                        outConnectionInputIndex = instance.Parent.Symbol.GetMultiInputIndexFor(connectionToOutput);
                    }
                    Type connectionType = instance.Outputs.Find(c => c.Id == connectionToOutput.SourceSlotId).ValueType;
                    outputConnections.Add((connectionToOutput, connectionType, isMultiInput, isMultiInput ? outConnectionInputIndex : 0));
                }
            }

            // Remove the input connections in index descending order to
            // prevent to get the wrong index in case of multi-input properties
            inputConnections.Sort((x, y) => y.multiInputIndex.CompareTo(x.multiInputIndex));
            foreach (var inputConnection in inputConnections)
            {
                removeCommands.Add(new DeleteConnectionCommand(GraphCanvas.Current.CompositionOp.Symbol, inputConnection.connection, inputConnection.multiInputIndex));
            }

            // Remove the output connections in index descending order to
            // prevent to get the wrong index in case of multi-input properties
            outputConnections.Sort((x, y) => y.multiInputIndex.CompareTo(x.multiInputIndex));
            foreach(var outputConnection in outputConnections)
            {
                removeCommands.Add(new DeleteConnectionCommand(GraphCanvas.Current.CompositionOp.Symbol, outputConnection.connection, outputConnection.multiInputIndex));
            }

            // Reconnect inputs of 1th nodes and outputs of last nodes if are of the same type
            // and reconnect them in ascending order
            outputConnections.Sort((x, y) => x.multiInputIndex.CompareTo(y.multiInputIndex));
            inputConnections.Sort((x, y) => x.multiInputIndex.CompareTo(y.multiInputIndex));
            var outputConnectionsRemaining = new List<(Symbol.Connection connection, Type connectionType, bool isMultiIndex, int multiInputIndex)>(outputConnections);
            foreach (var itemInputConnection in inputConnections)
            {
                foreach (var itemOutputConnectionRemaining in outputConnectionsRemaining)
                {
                    if (itemInputConnection.connectionType == itemOutputConnectionRemaining.connectionType)
                    {
                        var newConnection = new Symbol.Connection(sourceParentOrChildId: itemInputConnection.connection.SourceParentOrChildId,
                                                                  sourceSlotId: itemInputConnection.connection.SourceSlotId,
                                                                  targetParentOrChildId: itemOutputConnectionRemaining.connection.TargetParentOrChildId,
                                                                  targetSlotId: itemOutputConnectionRemaining.connection.TargetSlotId);

                        removeCommands.Add(new AddConnectionCommand(GraphCanvas.Current.CompositionOp.Symbol, newConnection, itemOutputConnectionRemaining.multiInputIndex));
                        outputConnectionsRemaining.Remove(itemOutputConnectionRemaining);

                        break;
                    }
                }
                if (outputConnectionsRemaining.Count < 1)
                {
                    break;
                }
            }

            if (removeCommands.Count > 0)
            {
                var macro = new MacroCommand("Shake off connections", removeCommands);
                UndoRedoStack.AddAndExecute(macro);
            }
        }

        
        private static void HandleNodeDragging(ISelectableCanvasObject draggedNode)
        {
            
            if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                _isDragging = false;
                return;
            }

            if (!_isDragging)
            {
                _dragStartPosInOpOnCanvas =  GraphCanvas.Current.InverseTransformPositionFloat(ImGui.GetMousePos()) - draggedNode.PosOnCanvas;
                _isDragging = true;
            }


            var mousePosOnCanvas = GraphCanvas.Current.InverseTransformPositionFloat(ImGui.GetMousePos());
            var newDragPosInCanvas = mousePosOnCanvas - _dragStartPosInOpOnCanvas;

            var bestDistanceInCanvas = float.PositiveInfinity;
            var targetSnapPositionInCanvas = Vector2.Zero;

            foreach (var offset in _snapOffsetsInCanvas)
            {
                var heightAffectFactor = 0;
                if (Math.Abs(offset.X) < 0.01f)
                {
                    if (offset.Y > 0)
                    {
                        heightAffectFactor = -1;
                    }
                    else
                    {
                        heightAffectFactor = 1;
                    }
                }

                foreach (var neighbor in GraphCanvas.Current.SelectableChildren)
                {
                    if (neighbor == draggedNode || _draggedNodes.Contains(neighbor))
                        continue;

                    var offset2 = new Vector2(offset.X, -neighbor.Size.Y * heightAffectFactor + offset.Y);
                    var snapToNeighborPos = neighbor.PosOnCanvas + offset2;

                    var d = Vector2.Distance(snapToNeighborPos, newDragPosInCanvas);
                    if (!(d < bestDistanceInCanvas))
                        continue;

                    targetSnapPositionInCanvas = snapToNeighborPos;
                    bestDistanceInCanvas = d;
                }
            }

            var snapDistanceInCanvas = GraphCanvas.Current.InverseTransformDirection(new Vector2(20, 0)).X;
            var isSnapping = bestDistanceInCanvas < snapDistanceInCanvas;

            var moveDeltaOnCanvas = isSnapping
                                        ? targetSnapPositionInCanvas - draggedNode.PosOnCanvas
                                        : newDragPosInCanvas - draggedNode.PosOnCanvas;

            // Drag selection
            foreach (var e in _draggedNodes)
            {
                e.PosOnCanvas += moveDeltaOnCanvas;
            }
        }

        public static void HighlightSnappedNeighbours(SymbolChildUi childUi)
        {
            if (childUi.IsSelected)
                return;

            var drawList = ImGui.GetWindowDrawList();
            var parentUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
            var snappedNeighbours = FindSnappedNeighbours(parentUi, childUi);

            var color = new Color(1f, 1f, 1f, 0.08f);
            snappedNeighbours.Add(childUi);
            var expandOnScreen = GraphCanvas.Current.TransformDirection(SelectableNodeMovement.SnapPadding).X / 2;

            foreach (var snappedNeighbour in snappedNeighbours)
            {
                var areaOnCanvas = new ImRect(snappedNeighbour.PosOnCanvas, snappedNeighbour.PosOnCanvas + snappedNeighbour.Size);
                var areaOnScreen = GraphCanvas.Current.TransformRect(areaOnCanvas);
                areaOnScreen.Expand(expandOnScreen);
                drawList.AddRect(areaOnScreen.Min, areaOnScreen.Max, color, rounding: 10, ImDrawFlags.RoundCornersAll, thickness: 4);
            }
        }

        public static List<ISelectableCanvasObject> FindSnappedNeighbours(SymbolUi parentUi, ISelectableCanvasObject childUi)
        {
            //var pool = new HashSet<ISelectableNode>(parentUi.ChildUis);
            var pool = new HashSet<ISelectableCanvasObject>(GraphCanvas.Current.SelectableChildren);
            pool.Remove(childUi);
            return RecursivelyFindSnappedInPool(childUi, pool).Distinct().ToList();
        }

        public static List<ISelectableCanvasObject> RecursivelyFindSnappedInPool(ISelectableCanvasObject childUi, HashSet<ISelectableCanvasObject> snapCandidates)
        {
            var snappedChildren = new List<ISelectableCanvasObject>();

            foreach (var candidate in snapCandidates.ToList())
            {
                if (AreChildUisSnapped(childUi, candidate) == Alignment.NoSnapped)
                    continue;

                snappedChildren.Add(candidate);
                snapCandidates.Remove(candidate);
                snappedChildren.AddRange(RecursivelyFindSnappedInPool(candidate, snapCandidates));
            }

            return snappedChildren;
        }

        public const float Tolerance = 0.1f;

        private static Alignment AreChildUisSnapped(ISelectableCanvasObject childA, ISelectableCanvasObject childB)
        {
            var alignedHorizontally = Math.Abs(childA.PosOnCanvas.Y - childB.PosOnCanvas.Y) < Tolerance;
            var alignedVertically = Math.Abs(childA.PosOnCanvas.X - childB.PosOnCanvas.X) < Tolerance;

            if (alignedVertically)
            {
                if (Math.Abs(childA.PosOnCanvas.Y + childA.Size.Y + SelectableNodeMovement.SnapPadding.Y
                             - childB.PosOnCanvas.Y) < Tolerance)
                {
                    return Alignment.SnappedBelow;
                }

                if (Math.Abs(childB.PosOnCanvas.Y + childB.Size.Y + SelectableNodeMovement.SnapPadding.Y
                             - childA.PosOnCanvas.Y) < Tolerance)
                {
                    return Alignment.SnappedAbove;
                }
            }

            if (alignedHorizontally)
            {
                if (Math.Abs(childA.PosOnCanvas.X + childA.Size.X + SelectableNodeMovement.SnapPadding.X
                             - childB.PosOnCanvas.X) < Tolerance)
                {
                    return Alignment.SnappedToRight;
                }

                if (Math.Abs(childB.PosOnCanvas.X + childB.Size.X + SelectableNodeMovement.SnapPadding.X
                             - childA.PosOnCanvas.X) < Tolerance)
                {
                    return Alignment.SnappedToLeft;
                }
            }

            return Alignment.NoSnapped;
        }

        public static void ArrangeOps()
        {
            var commands = new List<ICommand>();
            
            var freshlySnapped = new List<ISelectableCanvasObject>();
            foreach (var n in NodeSelection.GetSelectedChildUis())
            {
                RecursivelyAlignChildren(n, commands, freshlySnapped);
            }
            var command = new MacroCommand("arrange", commands);
            UndoRedoStack.Add(command);
        }

        private static float RecursivelyAlignChildren(SymbolChildUi childUi, List<ICommand> commands, List<ISelectableCanvasObject> freshlySnappedOpWidgets)
        {
            if (freshlySnappedOpWidgets == null)
                freshlySnappedOpWidgets = new List<ISelectableCanvasObject>();
            
            var parentUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
            var compositionSymbol = GraphCanvas.Current.CompositionOp.Symbol;
            var connectedChildUis = (from con in compositionSymbol.Connections
                                     where !con.IsConnectedToSymbolInput && !con.IsConnectedToSymbolOutput
                                     from sourceChildUi in parentUi.ChildUis
                                     where con.SourceParentOrChildId == sourceChildUi.Id
                                           && con.TargetParentOrChildId == childUi.Id
                                     select sourceChildUi).Distinct().ToArray();
            
            // Order connections by input definition order
            var connections = (from con in compositionSymbol.Connections
                                     where !con.IsConnectedToSymbolInput && !con.IsConnectedToSymbolOutput
                                     from sourceChildUi in parentUi.ChildUis
                                     where con.SourceParentOrChildId == sourceChildUi.Id
                                           && con.TargetParentOrChildId == childUi.Id
                                     select con).Distinct().ToArray();

            // Sort the incoming operators into the correct input order and
            // ignore operators that can't be auto-layouted because their outputs
            // have connection to multiple operators
            var sortedInputOps = new List<SymbolChildUi>();
            foreach (var inputDef in childUi.SymbolChild.Symbol.InputDefinitions)
            {
                var matchingConnections = connections.Where(c => c.TargetSlotId == inputDef.Id).ToArray();
                var connectedOpsForInput=  matchingConnections.SelectMany(c => connectedChildUis.Where(ccc => ccc.Id == c.SourceParentOrChildId));
                
                foreach (var op in connectedOpsForInput)
                {
                    var connectionsFromOutput = compositionSymbol.Connections.Where(c5 => c5.SourceParentOrChildId == op.Id
                                                                                          && c5.TargetParentOrChildId != childUi.Id);
                    var opHasUnpredictableConnections = connectionsFromOutput.Any();
                    if (!opHasUnpredictableConnections)
                    {
                        sortedInputOps.Add(op);
                    }
                }
            }

            float verticalOffset = 0;
            var snappedCount = 0;
            foreach (var connectedChildUi in sortedInputOps)
            {
                if (freshlySnappedOpWidgets.Contains(connectedChildUi))
                    continue;

                
                var thumbnailPadding = HasThumbnail(connectedChildUi) ? connectedChildUi.Size.X : 0;
                if (snappedCount > 0)
                    verticalOffset += thumbnailPadding;
                
                var moveCommand = new ModifyCanvasElementsCommand(compositionSymbol.Id, new List<ISelectableCanvasObject>() {connectedChildUi}); 
                connectedChildUi.PosOnCanvas = childUi.PosOnCanvas + new Vector2(- (childUi.Size.X + SnapPadding.X),verticalOffset);
                moveCommand.StoreCurrentValues();
                commands.Add(moveCommand);

                freshlySnappedOpWidgets.Add(connectedChildUi);
                verticalOffset += RecursivelyAlignChildren(connectedChildUi, commands, freshlySnappedOpWidgets);

                freshlySnappedOpWidgets.Add(connectedChildUi);
                NodeSelection.AddSelection(connectedChildUi);
                snappedCount++;
            }
            
            var requiredHeight =Math.Max(verticalOffset, childUi.Size.Y);
            return requiredHeight + SnapPadding.Y;
        }

        private static bool HasThumbnail(SymbolChildUi childUi)
        {
            return childUi.SymbolChild.Symbol.OutputDefinitions.Count > 0 && childUi.SymbolChild.Symbol.OutputDefinitions[0].ValueType == typeof(Texture2D);
        }
        

        private enum Alignment
        {
            NoSnapped,
            SnappedBelow,
            SnappedAbove,
            SnappedToRight,
            SnappedToLeft,
        }

        public static readonly Vector2 SnapPadding = new Vector2(40, 20);

        private static readonly Vector2[] _snapOffsetsInCanvas =
            {
                new(SymbolChildUi.DefaultOpSize.X + SnapPadding.X, 0),
                new(-SymbolChildUi.DefaultOpSize.X - +SnapPadding.X, 0),
                new(0, SnapPadding.Y),
                new(0, -SnapPadding.Y)
            };

        private static bool _isDragging;
        private static Vector2 _dragStartPosInOpOnCanvas;

        private static ModifyCanvasElementsCommand _moveCommand;

        private static Guid _draggedNodeId = Guid.Empty;
        private static List<ISelectableCanvasObject> _draggedNodes = new();

        private static class ShakeDetector
        {
            public static bool TestDragForShake(Vector2 mousePosition)
            {
                var delta = mousePosition - _lastPosition;
                _lastPosition = mousePosition;

                int dx = 0;
                if (Math.Abs(delta.X) > 2)
                {
                    dx = delta.X > 0 ? 1 : -1;
                }

                _directions.Add(dx);

                if (_directions.Count < 2)
                    return false;

                // Queue length is optimized for 60 fps
                // adjust length for different frame rates
                if (_directions.Count > QueueLength * (1 / 60f) / T3.Core.Animation.Playback.LastFrameDuration)
                    _directions.RemoveAt(0);

                // count direction changes
                var changeDirectionCount = 0;

                var lastD = 0;
                var lastRealD = 0;
                foreach (var d in _directions)
                {
                    if (lastD != 0 && d != 0)
                    {
                        if (d != lastRealD)
                        {
                            changeDirectionCount++;
                        }

                        lastRealD = d;
                    }

                    lastD = d;
                }
                
                var wasShaking = changeDirectionCount >= ChangeDirectionThreshold;
                if (wasShaking)
                    Reset();

                return wasShaking;
            }

            public static void Reset()
            {
                _directions.Clear();
            }

            private static Vector2 _lastPosition = Vector2.Zero;
            private const int QueueLength = 35;
            private const int ChangeDirectionThreshold = 5;
            private static readonly List<int> _directions = new(QueueLength);
        }

    }
}