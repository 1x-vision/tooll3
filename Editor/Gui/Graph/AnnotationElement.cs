using ImGuiNET;
using T3.Core.IO;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Annotations;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.SystemUi;

namespace T3.Editor.Gui.Graph
{
    /// <summary>
    /// Draws an AnnotationElement and handles its interaction
    /// </summary>
    class AnnotationElement
    {
        private readonly Annotation _annotation;
        public AnnotationElement(GraphWindow window, Annotation annotation)
        {
            _window = window;
            _annotation = annotation;
        }
        
        public void StartRenaming()
        {
            _requestedRenameId = _annotation.Id;
        }

        private Guid _requestedRenameId = Guid.Empty;

        internal void Draw(ImDrawListPtr drawList)
        {
            var annotation = _annotation;
            var canvas = _window.GraphCanvas;
            var screenArea = canvas.TransformRect(new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + annotation.Size));
            var titleSize = annotation.Size;
            titleSize.Y = MathF.Min(titleSize.Y, 14 * T3Ui.UiScaleFactor);

            // Keep height of title area at a minimum height when zooming out
            var clickableArea = canvas.TransformRect(new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + titleSize));
            var height = MathF.Min(16 * T3Ui.UiScaleFactor, screenArea.GetHeight());
            clickableArea.Max.Y = clickableArea.Min.Y + height;

            var isVisible = ImGui.IsRectVisible(screenArea.Min, screenArea.Max);

            if (!isVisible)
                return;

            ImGui.PushID(annotation.Id.GetHashCode());

            // Resize indicator
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNWSE);
                ImGui.SetCursorScreenPos(screenArea.Max - new Vector2(10, 10) * T3Ui.UiScaleFactor);
                ImGui.Button("##resize", new Vector2(10, 10) * T3Ui.UiScaleFactor);
                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    var delta = canvas.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                    annotation.Size += delta;
                }

                ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
            }

            // Background
            const float backgroundAlpha = 0.2f;
            const float headerHoverAlpha = 0.3f;

            drawList.AddRectFilled(screenArea.Min, screenArea.Max, UiColors.BackgroundFull.Fade(backgroundAlpha));

            // Interaction
            ImGui.SetCursorScreenPos(clickableArea.Min);
            ImGui.InvisibleButton("##annotationHeader", clickableArea.GetSize());

            THelpers.DebugItemRect();
            var isHeaderHovered = ImGui.IsItemHovered();
            if (isHeaderHovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            // Header
            drawList.AddRectFilled(clickableArea.Min, clickableArea.Max,
                                   annotation.Color.Fade(isHeaderHovered ? headerHoverAlpha : 0));

            HandleDragging();
            var shouldRename = (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) || _requestedRenameId == annotation.Id;
            Renaming.Draw(annotation, screenArea, shouldRename);
            if (shouldRename)
            {
                _requestedRenameId = Guid.Empty;
            }

            var borderColor = canvas.NodeSelection.IsNodeSelected(annotation)
                                  ? UiColors.Selection
                                  : UiColors.BackgroundFull.Fade(isHeaderHovered ? headerHoverAlpha : backgroundAlpha);

            const float thickness = 1;
            drawList.AddRect(screenArea.Min - Vector2.One * thickness,
                             screenArea.Max + Vector2.One * thickness,
                             borderColor,
                             0f,
                             0,
                             thickness);

            // Label
            if(!string.IsNullOrEmpty(annotation.Title)) {
                var canvasScale = canvas.Scale.X;
                var font = annotation.Title.StartsWith("# ") ? Fonts.FontLarge: Fonts.FontNormal;
                var fade = MathUtils.SmootherStep(0.25f, 0.6f, canvasScale);
                drawList.PushClipRect(screenArea.Min, screenArea.Max, true);
                var labelPos = screenArea.Min + new Vector2(8, 6) * T3Ui.DisplayScaleFactor;

                var fontSize = canvasScale > 1 
                                   ? font.FontSize
                                   : canvasScale >  Fonts.FontSmall.Scale / Fonts.FontNormal.Scale
                                       ? font.FontSize
                                       : font.FontSize * canvasScale;
                drawList.AddText(font,
                                 fontSize,
                                 labelPos,
                                 ColorVariations.OperatorLabel.Apply(annotation.Color.Fade(fade)),
                                 annotation.Title);
                drawList.PopClipRect();
            }

            ImGui.PopID();
        }

        private void HandleDragging()
        {
            var nodeSelection = _window.GraphCanvas.NodeSelection;
            if (ImGui.IsItemActive())
            {
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    var parentUi = _window.CompositionOp.GetSymbolUi();
                    _draggedNodeId = _annotation.Id;
                    if (nodeSelection.IsNodeSelected(_annotation))
                    {
                        _draggedNodes = nodeSelection.GetSelectedNodes<ISelectableCanvasObject>().ToList();
                    }
                    else
                    {

                        if (!ImGui.GetIO().KeyCtrl)
                            _draggedNodes = FindAnnotatedOps(parentUi, _annotation).ToList();
                        _draggedNodes.Add(_annotation);
                    }

                    _moveCommand = new ModifyCanvasElementsCommand(parentUi, _draggedNodes, nodeSelection);
                }

                HandleNodeDragging(_annotation);
            }
            else if (ImGui.IsMouseReleased(0) && _moveCommand != null)
            {
                if (_draggedNodeId != _annotation.Id)
                    return;

                // var singleDraggedNode = (_draggedNodes.Count == 1) ? _draggedNodes[0] : null;
                _draggedNodeId = Guid.Empty;
                _draggedNodes.Clear();

                var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
                if (wasDragging)
                {
                    _moveCommand.StoreCurrentValues();
                    UndoRedoStack.Add(_moveCommand);
                }
                else
                {
                    if (!nodeSelection.IsNodeSelected(_annotation))
                    {
                        if (!ImGui.GetIO().KeyShift)
                        {
                            nodeSelection.Clear();
                        }

                        nodeSelection.AddSelection(_annotation);
                    }
                    else
                    {
                        if (ImGui.GetIO().KeyShift)
                        {
                            nodeSelection.DeselectNode(_annotation, null);
                        }
                    }
                }

                _moveCommand = null;
            }

            var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
                && !wasDraggingRight
                && ImGui.IsItemHovered()
                && !nodeSelection.IsNodeSelected(_annotation))
            {
                nodeSelection.SetSelection(_annotation);
            }
        }

        private static List<ISelectableCanvasObject> FindAnnotatedOps(SymbolUi parentUi, Annotation annotation)
        {
            var matches = new List<ISelectableCanvasObject>();
            var aRect = new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + annotation.Size);

            foreach (var n in parentUi.ChildUis)
            {
                var nRect = new ImRect(n.PosOnCanvas, n.PosOnCanvas + n.Size);
                if (aRect.Contains(nRect))
                    matches.Add(n);
            }

            foreach (var a in parentUi.Annotations.Values)
            {
                if (a == annotation)
                    continue;

                var nRect = new ImRect(a.PosOnCanvas, a.PosOnCanvas + a.Size);
                if (aRect.Contains(nRect))
                    matches.Add(a);
            }

            return matches;
        }

        private void HandleNodeDragging(ISelectableCanvasObject draggedNode)
        {
            if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                _isDragging = false;
                return;
            }
            
            var canvas = _window.GraphCanvas;

            if (!_isDragging)
            {
                _dragStartDelta = ImGui.GetMousePos() - canvas.TransformPosition(draggedNode.PosOnCanvas);
                _isDragging = true;
            }

            var newDragPos = ImGui.GetMousePos() - _dragStartDelta;
            var newDragPosInCanvas = canvas.InverseTransformPositionFloat(newDragPos);
            var moveDeltaOnCanvas = newDragPosInCanvas - draggedNode.PosOnCanvas;

            // Drag selection
            foreach (var e in _draggedNodes)
            {
                e.PosOnCanvas += moveDeltaOnCanvas;
            }
        }

        private static class Renaming
        {
            public static void Draw(Annotation annotation, ImRect screenArea, bool shouldBeOpened)
            {
                var justOpened = false;
                if (_focusedAnnotationId == Guid.Empty)
                {
                    if (shouldBeOpened)
                    {
                        justOpened = true;
                        ImGui.SetKeyboardFocusHere();
                        _focusedAnnotationId = annotation.Id;
                        _changeAnnotationTextCommand = new ChangeAnnotationTextCommand(annotation, annotation.Title);
                    }
                }

                if (_focusedAnnotationId == Guid.Empty)
                    return;

                if (_focusedAnnotationId != annotation.Id)
                    return;

                var positionInScreen = screenArea.Min;
                ImGui.SetCursorScreenPos(positionInScreen);

                var text = annotation.Title;

                ImGui.SetNextItemWidth(150);
                ImGui.InputTextMultiline("##renameAnnotation", ref text, 256, screenArea.GetSize(), ImGuiInputTextFlags.AutoSelectAll);
                if (!ImGui.IsItemDeactivated())
                    annotation.Title = text;

                if (!justOpened && (ImGui.IsItemDeactivated() || ImGui.IsKeyPressed((ImGuiKey)Key.Esc)))
                {
                    _focusedAnnotationId = Guid.Empty;
                    _changeAnnotationTextCommand.NewText = annotation.Title;
                    UndoRedoStack.AddAndExecute(_changeAnnotationTextCommand);
                }
            }

            private static Guid _focusedAnnotationId;
            private static ChangeAnnotationTextCommand _changeAnnotationTextCommand;
        }

        bool _isDragging;
        Vector2 _dragStartDelta;
        ModifyCanvasElementsCommand _moveCommand;

        Guid _draggedNodeId = Guid.Empty;
        List<ISelectableCanvasObject> _draggedNodes = new();

        readonly GraphWindow _window;
    }
}