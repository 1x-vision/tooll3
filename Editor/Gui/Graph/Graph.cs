﻿using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

// ReSharper disable LoopCanBeConvertedToQuery

namespace T3.Editor.Gui.Graph
{
    /// <summary>Rendering a node graph</summary>
    /// <remarks>
    /// Rendering the graph is complicated because:
    /// - Connection has no real model to store computations
    /// - Connections are defined by Guid references to Symbol-Definitions
    /// - Computing connection end point position involves...
    ///    - ...many states of the graph nodes
    ///    - ...connections under construction
    ///    - ...potentially hidden connections
    ///    - ...layout of connections into multi input slots
    ///    
    /// This implementation first collects the information required to drawing the input sockets
    /// and connection links over several passes in which the information about visible connection-lines
    /// is collected into a list of ConnectionLineUi instances. These passes are...
    /// 
    /// 1. Initializes lists of ConnectionLineUis
    /// 2. Fill the lists of which nodes are connected to which lines
    /// 3. Draw nodes and their sockets and set positions for connection lines
    /// 4. Draw inputs
    /// 5. Draw outputs 
    /// 6. Draw connection lines
    ///</remarks>
    internal partial class Graph
    {
        private readonly GraphWindow _window;
        public Graph(GraphWindow window, GraphCanvas canvas)
        {
            _window = window;
            _connectionSorter = new ConnectionSorter(this, window, canvas);
        }
        
        public void DrawGraph(ImDrawListPtr drawList, bool preventInteraction, Instance composition, float graphOpacity)
        {
            var canvas = _window.GraphCanvas;
            var needsReinit = false;
            var graphSymbol = composition.Symbol;

            if (ConnectionMaker.TempConnections.Count > 0 || AllConnections.Count != ConnectionMaker.TempConnections.Count + graphSymbol.Connections.Count)
            {
                _lastCheckSum = 0;
                needsReinit = true;
            }

            // Checksum
            if (!needsReinit)
            {
                var checkSum = 0;

                for (var index = 0; index < graphSymbol.Connections.Count; index++)
                {
                    var c = graphSymbol.Connections[index];
                    checkSum += c.GetHashCode() * (index + 1);
                }

                foreach (var c in ConnectionMaker.TempConnections)
                {
                    checkSum += c.GetHashCode();
                }

                if (checkSum != _lastCheckSum)
                {
                    needsReinit = true;
                    _lastCheckSum = checkSum;
                }
            }

            //needsReinit = true;
            var compositionUi = composition.GetSymbolUi();
            if (needsReinit)
            {
                AllConnections.Clear();
                AllConnections.AddRange(graphSymbol.Connections);
                AllConnections.AddRange(ConnectionMaker.TempConnections);

                // 1. Initializes lists of ConnectionLineUis
                _connectionSorter.Init();

                // 2. Collect which nodes are connected to which lines
                foreach (var c in AllConnections)
                {
                    _connectionSorter.CreateAndSortLineUi(c, compositionUi);
                }
            }
            else
            {
                foreach (var c in _connectionSorter.Lines)
                {
                    c.IsSelected = false;
                }
            }

            drawList.ChannelsSplit(2);
            drawList.ChannelsSetCurrent((int)Channels.Operators);

            compositionUi = composition.GetSymbolUi();
            // 3. Draw Nodes and their sockets and set positions for connection lines
            foreach (var instance in _window.CompositionOp.Children.Values)
            {
                if (instance == null)
                    continue;

                var childUi = compositionUi.ChildUis[instance.SymbolChildId];

                var isSelected = canvas.NodeSelection.IsNodeSelected(childUi);

                // todo - remove nodes that are not in the graph anymore?
                if (!_graphNodes.TryGetValue(childUi, out var node))
                {
                    node = new GraphNode(_window, _connectionSorter);
                    _graphNodes[childUi] = node;
                }

                node.Draw(drawList, graphOpacity, isSelected, childUi, instance, preventInteraction);
            }

            // 4. Draw Inputs Nodes
            foreach (var (nodeId, node) in compositionUi.InputUis)
            {
                var index = graphSymbol.InputDefinitions.FindIndex(def => def.Id == nodeId);
                if (index < 0)
                {
                    Log.Warning($"Input {nodeId} not found in {graphSymbol.Name}");
                    continue;
                }
                var inputDef = graphSymbol.InputDefinitions[index];
                var isSelectedOrHovered = InputNode.Draw(_window, drawList, inputDef, node, index);

                var sourcePos = new Vector2(
                                            InputNode._lastScreenRect.Max.X + GraphNode.UsableSlotThickness,
                                            InputNode._lastScreenRect.GetCenter().Y
                                           );
                foreach (var line in _connectionSorter.GetLinesFromInputNodes(node, nodeId))
                {
                    line.SourcePosition = sourcePos;
                    line.IsSelected |= isSelectedOrHovered;
                }
            }

            // 5. Draw Output Nodes
            foreach (var (outputId, outputNode) in compositionUi.OutputUis)
            {
                var outputDef = graphSymbol.OutputDefinitions.Find(od => od.Id == outputId);
                OutputNode.Draw(_window, drawList, outputDef, outputNode);

                var targetPos = new Vector2(OutputNode.LastScreenRect.Min.X + GraphNode.InputSlotThickness,
                                            OutputNode.LastScreenRect.GetCenter().Y);

                foreach (var line in _connectionSorter.GetLinesToOutputNodes(outputNode, outputId))
                {
                    line.TargetPosition = targetPos;
                }
            }

            // 6. Draw ConnectionLines
            foreach (var line in _connectionSorter.Lines)
            {
                line.Draw(canvas.Scale, drawList);
            }

            // 7. Draw Annotations
            drawList.ChannelsSetCurrent((int)Channels.Annotations);
            foreach (var annotation in compositionUi.Annotations.Values)
            {
                //var posOnScreen = GraphCanvas.Current.TransformPosition(annotation.Position);
                //drawList.AddRectFilled(  posOnScreen, posOnScreen + new Vector2(300,300), Color.Green);
                // todo - remove annotations that are not in the graph anymore?
                if(!_annotationElements.TryGetValue(annotation, out var annotationElement))
                {
                    annotationElement = new AnnotationElement(_window, annotation);
                    _annotationElements[annotation] = annotationElement;
                }
                
                annotationElement.Draw(drawList);
            }

            drawList.ChannelsMerge();
        }

        internal void RenameAnnotation(Annotation annotation)
        {
            if (!_annotationElements.TryGetValue(annotation, out var annotationElement))
            {
                annotationElement = new AnnotationElement(_window, annotation);
                _annotationElements[annotation] = annotationElement;
            }
            
            annotationElement.StartRenaming();
        }

        internal class ConnectionLineUi
        {
            public readonly Symbol.Connection Connection;
            public Vector2 TargetPosition;
            public Vector2 SourcePosition;
            public Color ColorForType;

            public bool IsSelected;
            public int UpdateCount;
            public int FramesSinceLastUsage;
            public ImRect SourceNodeArea;
            public ImRect TargetNodeArea;
            public bool IsAboutToBeReplaced;
            private Graph _graph;

            internal ConnectionLineUi(Symbol.Connection connection, Graph graph)
            {
                Connection = connection;
                _graph = graph;
            }

            internal void Draw(Vector2 canvasScale, ImDrawListPtr drawList)
            {
                var color = IsSelected
                                ? ColorVariations.Highlight.Apply(ColorForType)
                                : ColorVariations.ConnectionLines.Apply(ColorForType);

                if (IsAboutToBeReplaced)
                    color = Color.Mix(color, UiColors.StatusAttention, (float)Math.Sin(ImGui.GetTime() * 15) / 2 + 0.5f);

                if (!IsSelected)
                    color = color.Fade(0.6f);

                var usageFactor = Math.Max(0, 1 - FramesSinceLastUsage / 50f);
                var thickness = ((1 - 1 / (UpdateCount + 1f)) * 3 + 1) * 0.5f * (usageFactor * 2 + 1);

                if (UserSettings.Config.UseArcConnections)
                {
                    var hoverPositionOnLine = Vector2.Zero;
                    var isHovering = ArcConnection.Draw(canvasScale, new ImRect(SourcePosition, SourcePosition + new Vector2(10, 10)),
                                                        SourcePosition,
                                                        TargetNodeArea,
                                                        TargetPosition,
                                                        color,
                                                        thickness,
                                                        ref hoverPositionOnLine);

                    const float minDistanceToTargetSocket = 10;
                    if (isHovering && Vector2.Distance(hoverPositionOnLine, TargetPosition) > minDistanceToTargetSocket
                                   && Vector2.Distance(hoverPositionOnLine, SourcePosition) > minDistanceToTargetSocket)
                    {
                        ConnectionSplitHelper.RegisterAsPotentialSplit(Connection, ColorForType, hoverPositionOnLine);
                    }
                }
                else
                {
                    var tangentLength = MathUtils.RemapAndClamp(Vector2.Distance(SourcePosition, TargetPosition),
                                                                30, 300,
                                                                5, 200);

                    drawList.AddBezierCubic(
                                            SourcePosition,
                                            SourcePosition + new Vector2(tangentLength, 0),
                                            TargetPosition + new Vector2(-tangentLength, 0),
                                            TargetPosition,
                                            color,
                                            thickness,
                                            num_segments: 20);
                }
            }
        }

        private enum Channels
        {
            Annotations = 0,
            Operators = 1,
        }

        private int _lastCheckSum;
        private readonly ConnectionSorter _connectionSorter;

        // Try to avoid allocations
        private readonly List<Symbol.Connection> AllConnections = new(100);
        private readonly Dictionary<SymbolUi.Child, GraphNode> _graphNodes = new();
        private readonly Dictionary<Annotation, AnnotationElement> _annotationElements = new();
    }
}