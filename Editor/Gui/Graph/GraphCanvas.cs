using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX.Direct3D11;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Annotations;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.Gui.Windows.Output;
using T3.Editor.Gui.Windows.TimeLine;
using T3.Editor.SystemUi;

namespace T3.Editor.Gui.Graph
{
    /// <summary>
    /// A <see cref="ICanvas"/> that displays the graph of an Operator.
    /// </summary>
    public class GraphCanvas : ScalableCanvas, INodeCanvas
    {
        public GraphCanvas(GraphWindow window, List<Guid> idPath)
        {
            _window = window;
            _initialCompositionPath = idPath;
        }

        private bool _initializedCompositionAfterLayoutReady;

        public void SetComposition(List<Guid> childIdPath, ICanvas.Transition transition)
        {
            SelectableNodeMovement.Reset();
            // Zoom timeline out if necessary
            if (transition == ICanvas.Transition.JumpOut)
            {
                var primaryGraphWindow = GraphWindow.GetVisibleInstances().FirstOrDefault();
                primaryGraphWindow?.CurrentTimeLine?.UpdateScaleAndTranslation(primaryGraphWindow.GraphCanvas.CompositionOp,
                                                                               transition);
            }

            var previousFocusOnScreen = WindowPos + WindowSize / 2;

            var previousInstanceWasSet = _compositionPath != null && _compositionPath.Count > 0;
            if (previousInstanceWasSet)
            {
                //NodeOperations.GetInstanceFromIdPath(_compositionPath)
                var previousInstance = NodeOperations.GetInstanceFromIdPath(_compositionPath);
                UserSettings.Config.OperatorViewSettings[CompositionOp.SymbolChildId] = GetTargetScope();

                var newUiContainer = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                var matchingChildUi = newUiContainer.ChildUis.FirstOrDefault(childUi => childUi.SymbolChild.Id == previousInstance.SymbolChildId);
                if (matchingChildUi != null)
                {
                    var centerOnCanvas = matchingChildUi.PosOnCanvas + matchingChildUi.Size / 2;
                    previousFocusOnScreen = TransformPosition(centerOnCanvas);
                }
            }

            _compositionPath = childIdPath;
            var comp = NodeOperations.GetInstanceFromIdPath(childIdPath);
            if (comp == null)
            {
                Log.Error("Can't resolve instance for id-path " + childIdPath);
                return;
            }

            CompositionOp = comp;

            NodeSelection.Clear();
            TimeLineCanvas.Current?.ClearSelection();

            var newProps = GuessViewProperties();
            if (CompositionOp != null)
            {
                UserSettings.SaveLastViewedOpForWindow(_window, CompositionOp.SymbolChildId);
                if (UserSettings.Config.OperatorViewSettings.ContainsKey(CompositionOp.SymbolChildId))
                {
                    newProps = UserSettings.Config.OperatorViewSettings[CompositionOp.SymbolChildId];
                }
            }

            SetScopeWithTransition(newProps.Scale, newProps.Scroll, previousFocusOnScreen, transition);

            if (transition == ICanvas.Transition.JumpIn)
            {
                var primaryGraphWindow = GraphWindow.GetVisibleInstances().FirstOrDefault();
                primaryGraphWindow?.CurrentTimeLine?.UpdateScaleAndTranslation(primaryGraphWindow.GraphCanvas.CompositionOp,
                                                                               transition);
            }
        }

        /// <summary>
        /// Uses an ID-path to open an instance's parent composition and centers the instance.
        /// This can be useful to jump to elements (e.g. through bookmarks)
        /// </summary>
        public void OpenAndFocusInstance(List<Guid> childIdPath)
        {
            var instance = NodeOperations.GetInstanceFromIdPath(childIdPath);
            if (instance == null)
            {
                return;
            }

            if (childIdPath.Count < 2)
            {
                return;
            }

            var pathToParent = childIdPath.GetRange(0, childIdPath.Count - 1);
            SetComposition(pathToParent, ICanvas.Transition.Undefined);

            var parentComposition = instance.Parent;
            var parentUi = SymbolUiRegistry.Entries[parentComposition.Symbol.Id];
            var childInstanceUi = parentUi.ChildUis.SingleOrDefault(c => c.Id == instance.SymbolChildId);
            NodeSelection.Clear();
            NodeSelection.AddSymbolChildToSelection(childInstanceUi, instance);
            FitViewToSelectionHandling.FitViewToSelection();
        }

        public void SetCompositionToChildInstance(Instance instance)
        {
            // Validation that instance is valid
            // TODO: only do in debug mode
            var op = NodeOperations.GetInstanceFromIdPath(_compositionPath);
            var matchingChild = op.Children.SingleOrDefault(child => child == instance);
            if (matchingChild == null)
            {
                throw new ArgumentException("Can't OpenChildNode because Instance is not a child of current composition");
            }

            var newPath = _compositionPath;
            newPath.Add(instance.SymbolChildId);
            NodeSelection.Clear();
            TimeLineCanvas.Current?.ClearSelection();
            SetComposition(newPath, ICanvas.Transition.JumpIn);
        }

        public void SetCompositionToParentInstance(Instance instance)
        {
            if (instance == null)
            {
                Log.Warning("can't jump to parent with invalid instance");
                return;
            }

            var previousCompositionOp = CompositionOp;
            var shortenedPath = new List<Guid>();
            foreach (var pathItemId in _compositionPath)
            {
                if (pathItemId == instance.SymbolChildId)
                    break;

                shortenedPath.Add(pathItemId);
            }

            shortenedPath.Add(instance.SymbolChildId);

            if (shortenedPath.Count() == _compositionPath.Count())
                throw new ArgumentException("Can't SetCompositionToParentInstance because Instance is not a parent of current composition");

            SetComposition(shortenedPath, ICanvas.Transition.JumpOut);
            NodeSelection.Clear();
            TimeLineCanvas.Current?.ClearSelection();
            var previousCompChildUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis
                                                      .SingleOrDefault(childUi => childUi.Id == previousCompositionOp.SymbolChildId);
            if (previousCompChildUi != null)
                NodeSelection.AddSymbolChildToSelection(previousCompChildUi, previousCompositionOp);
        }

        private Scope GuessViewProperties()
        {
            ChildUis = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis;
            FocusViewToSelection();
            return GetTargetScope();
        }

        public void MakeCurrent()
        {
            Current = this;
        }

        #region drawing UI ====================================================================
        public void Draw(ImDrawListPtr dl, bool showGrid)
        {
            var flags = SymbolBrowser.IsOpen ? T3Ui.EditingFlags.PreventZoomWithMouseWheel : T3Ui.EditingFlags.None ;
            UpdateCanvas(flags);

            /*
             * This is work around to delay setting the composition until ImGui has
             * finally updated its window size and applied its layout so we can use
             * Graph window size to properly fit the content into view.
             *
             * The side effect of this hack is that CompositionOp stays undefined for
             * multiple frames with requires many checks in GraphWindow's Draw().
             */
            var imGuiLayoutReady = ImGui.GetFrameCount() > 1;
            if (!imGuiLayoutReady)
            {
                return;
            }

            if (!_initializedCompositionAfterLayoutReady)
            {
                SetComposition(_initialCompositionPath, ICanvas.Transition.JumpIn);
                FocusViewToSelection();
                _initializedCompositionAfterLayoutReady = true;
            }

            // TODO: Refresh reference on every frame. Since this uses lists instead of dictionary
            // it can be really slow
            CompositionOp = NodeOperations.GetInstanceFromIdPath(_compositionPath);
            if (CompositionOp == null)
            {
                Log.Error("unable to get composition op");
                return;
            }

            if (this.CompositionOp == null)
            {
                Log.Error("Can't show graph for undefined CompositionOp");
                return;
            }

            GraphBookmarkNavigation.HandleForCanvas(this);

            MakeCurrent();
            var symbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
            symbolUi.FlagAsModified();
            ChildUis = symbolUi.ChildUis;
            DrawList = dl;
            ImGui.BeginGroup();
            {
                CustomComponents.DrawWindowFocusFrame();
                
                DrawDropHandler();

                if (KeyboardBinding.Triggered(UserActions.FocusSelection))
                    FocusViewToSelection();

                if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.Duplicate))
                {
                    CopySelectedNodesToClipboard();
                    PasteClipboard();
                }

                if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.DeleteSelection))
                {
                    DeleteSelectedElements();
                }

                if (KeyboardBinding.Triggered(UserActions.ToggleDisabled))
                {
                    ToggleDisabledForSelectedElements();
                }
                
                if (KeyboardBinding.Triggered(UserActions.ToggleBypassed))
                {
                    ToggleBypassedForSelectedElements();
                }
                
                if (KeyboardBinding.Triggered(UserActions.PinToOutputWindow))
                {
                    PinSelectedToOutputWindow();
                }

                if (KeyboardBinding.Triggered(UserActions.CopyToClipboard))
                {
                    CopySelectedNodesToClipboard();
                }

                if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.PasteFromClipboard))
                {
                    PasteClipboard();
                }

                if (KeyboardBinding.Triggered(UserActions.LayoutSelection))
                {
                    SelectableNodeMovement.ArrangeOps();
                }

                if (!T3Ui.IsCurrentlySaving && KeyboardBinding.Triggered(UserActions.AddAnnotation))
                {
                    AddAnnotation();
                }

                if (KeyboardBinding.Triggered(UserActions.DisplayImageAsBackground))
                {
                    var selectedImage = NodeSelection.GetFirstSelectedInstance();
                    if (selectedImage != null)
                    {
                        GraphWindow.SetBackgroundOutput(selectedImage);
                    }
                }

                if (KeyboardBinding.Triggered(UserActions.ClearBackgroundImage))
                {
                    GraphWindow.ClearBackground();
                }

                if (ImGui.IsWindowFocused())
                {
                    var io = ImGui.GetIO();
                    var editingSomething = ImGui.IsAnyItemActive();
                    if (!io.KeyCtrl && !io.KeyShift && !io.KeyAlt && !editingSomething)
                    {
                        if (ImGui.IsKeyDown((ImGuiKey)Key.W))
                        {
                            _dampedScrollVelocity.Y -= InverseTransformDirection(Vector2.One * UserSettings.Config.KeyboardScrollAcceleration).Y;
                        }

                        if (ImGui.IsKeyDown((ImGuiKey)Key.S))
                        {
                            _dampedScrollVelocity.Y += InverseTransformDirection(Vector2.One * UserSettings.Config.KeyboardScrollAcceleration).Y;
                        }

                        if (ImGui.IsKeyDown((ImGuiKey)Key.A))
                        {
                            _dampedScrollVelocity.X -= InverseTransformDirection(Vector2.One * UserSettings.Config.KeyboardScrollAcceleration).X;
                        }

                        if (ImGui.IsKeyDown((ImGuiKey)Key.D))
                        {
                            _dampedScrollVelocity.X += InverseTransformDirection(Vector2.One * UserSettings.Config.KeyboardScrollAcceleration).X;
                        }
                        
                        if (ImGui.IsKeyDown((ImGuiKey)Key.Q))
                        {
                            var center = WindowPos + WindowSize / 2;
                            ApplyZoomDelta( center, 1.05f);
                        }
                        if (ImGui.IsKeyDown((ImGuiKey)Key.E))
                        {
                            var center = WindowPos + WindowSize / 2;
                            ApplyZoomDelta( center, 1/1.05f);
                        }
                    }
                }

                ScrollTarget += _dampedScrollVelocity;
                _dampedScrollVelocity *= 0.90f;

                DrawList.PushClipRect(WindowPos, WindowPos + WindowSize);

                if (showGrid)
                    DrawGrid();

                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
                {
                    ConnectionSplitHelper.PrepareNewFrame(this);
                }

                SymbolBrowser.Draw();

                T3.Editor.Gui.Graph.Graph.DrawGraph(DrawList);
                RenameInstanceOverlay.Draw();
                HandleFenceSelection();

                var isOnBackground = ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive();
                if (isOnBackground && ImGui.IsMouseDoubleClicked(0))
                {
                    if (CompositionOp.Parent != null)
                        SetCompositionToParentInstance(CompositionOp.Parent);
                }

                if (ConnectionMaker.TempConnections.Count > 0 && ImGui.IsMouseReleased(0))
                {
                    var isAnyItemHovered = ImGui.IsAnyItemHovered();
                    var droppedOnBackground =
                        ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.AllowWhenBlockedByPopup) && !isAnyItemHovered;
                    if (droppedOnBackground)
                    {
                        ConnectionMaker.InitSymbolBrowserAtPosition(SymbolBrowser,
                                                                    InverseTransformPositionFloat(ImGui.GetIO().MousePos));
                    }
                    else
                    {
                        var connectionDroppedOnBackground = ConnectionMaker.TempConnections[0].GetStatus() != ConnectionMaker.TempConnection.Status.TargetIsDraftNode;
                        if (connectionDroppedOnBackground)
                        {
                            ConnectionMaker.CompleteOperation();
                        }
                    }
                }

                DrawList.PopClipRect();
                DrawContextMenu();

                _duplicateSymbolDialog.Draw(CompositionOp, GetSelectedChildUis(), ref _nameSpaceForDialogEdits, ref _symbolNameForDialogEdits,
                                            ref _symbolDescriptionForDialog);
                _combineToSymbolDialog.Draw(CompositionOp, GetSelectedChildUis(),
                                            NodeSelection.GetSelectedNodes<Annotation>().ToList(),
                                            ref _nameSpaceForDialogEdits,
                                            ref _symbolNameForDialogEdits,
                                            ref _symbolDescriptionForDialog);
                _renameSymbolDialog.Draw(GetSelectedChildUis(), ref _symbolNameForDialogEdits);
                _addInputDialog.Draw(CompositionOp.Symbol);
                _addOutputDialog.Draw(CompositionOp.Symbol);
                LibWarningDialog.Draw();
                EditNodeOutputDialog.Draw();
            }
            ImGui.EndGroup();
            Current = null;
        }

        private void HandleFenceSelection()
        {
            _fenceState = SelectionFence.UpdateAndDraw(_fenceState);
            switch (_fenceState)
            {
                case SelectionFence.States.PressedButNotMoved:
                    if (SelectionFence.SelectMode == SelectionFence.SelectModes.Replace)
                        NodeSelection.Clear();
                    break;

                case SelectionFence.States.Updated:
                    HandleSelectionFenceUpdate(SelectionFence.BoundsInScreen);
                    break;

                case SelectionFence.States.CompletedAsClick:
                    NodeSelection.Clear();
                    NodeSelection.SetSelectionToParent(CompositionOp);
                    break;
            }
        }

        private SelectionFence.States _fenceState = SelectionFence.States.Inactive;

        private void HandleSelectionFenceUpdate(ImRect boundsInScreen)
        {
            var boundsInCanvas = InverseTransformRect(boundsInScreen);
            var nodesToSelect = (from child in SelectableChildren
                                 let rect = new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size)
                                 where rect.Overlaps(boundsInCanvas)
                                 select child).ToList();

            if (SelectionFence.SelectMode == SelectionFence.SelectModes.Replace)
            {
                NodeSelection.Clear();
            }

            foreach (var node in nodesToSelect)
            {
                if (node is SymbolChildUi symbolChildUi)
                {
                    var instance = CompositionOp.Children.FirstOrDefault(child => child.SymbolChildId == symbolChildUi.Id);
                    if (instance == null)
                    {
                        Log.Warning("Can't find instance");
                    }

                    if (SelectionFence.SelectMode == SelectionFence.SelectModes.Remove)
                    {
                        NodeSelection.DeselectNode(symbolChildUi, instance);
                    }
                    else
                    {
                        NodeSelection.AddSymbolChildToSelection(symbolChildUi, instance);
                    }
                }
                else if (node is Annotation annotation)
                {
                    var annotationRect = new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + annotation.Size);
                    if (boundsInCanvas.Contains(annotationRect))
                    {
                        if (SelectionFence.SelectMode == SelectionFence.SelectModes.Remove)
                        {
                            NodeSelection.DeselectNode(annotation);
                        }
                        else
                        {
                            NodeSelection.AddSelection(annotation);
                        }
                    }
                }
                else
                {
                    if (SelectionFence.SelectMode == SelectionFence.SelectModes.Remove)
                    {
                        NodeSelection.DeselectNode(node);
                    }
                    else
                    {
                        NodeSelection.AddSelection(node);
                    }
                }
            }
        }

        /// <remarks>
        /// This method is completed, because it has to handle several edge cases and has potential to remove previous user data:
        /// - We have to preserve the previous state.
        /// - We have to make space -> Shift all connected operators towards the right.
        /// - We have to convert all existing connections from the output into temporary connections.
        /// - We have to insert a new temp connection line between output and symbol browser
        ///
        /// - If the user completes the symbol browser, it must complete the previous connections from the temp connections.
        /// - If the user cancels the operation, the previous state has to be restored. This might be tricky
        /// </remarks>
        public void OpenSymbolBrowserForOutput(SymbolChildUi childUi, Symbol.OutputDefinition outputDef)
        {
            ConnectionMaker.InitSymbolBrowserAtPosition(SymbolBrowser,
                                                        childUi.PosOnCanvas + new Vector2(childUi.Size.X + SelectableNodeMovement.SnapPadding.X, 0));
        }

        // private Symbol GetSelectedSymbol()
        // {
        //     var selectedChildUi = GetSelectedChildUis().FirstOrDefault();
        //     return selectedChildUi != null ? selectedChildUi.SymbolChild.Symbol : CompositionOp.Symbol;
        // }

        private void DrawDropHandler()
        {
            if (!T3Ui.DraggingIsInProgress)
                return;

            ImGui.SetCursorPos(Vector2.Zero);
            ImGui.InvisibleButton("## drop", ImGui.GetWindowSize());

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload("Symbol");
                if (ImGui.IsMouseReleased(0))
                {
                    var myString = Marshal.PtrToStringAuto(payload.Data);
                    if (myString != null)
                    {
                        var guidString = myString.Split('|')[0];
                        var guid = Guid.Parse(guidString);
                        Log.Debug("dropped symbol here" + payload + " " + myString + "  " + guid);

                        var symbol = SymbolRegistry.Entries[guid];
                        var parent = CompositionOp.Symbol;
                        var posOnCanvas = InverseTransformPositionFloat(ImGui.GetMousePos());
                        var childUi = NodeOperations.CreateInstance(symbol, parent, posOnCanvas);

                        var instance = CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);
                        NodeSelection.SetSelectionToChildUi(childUi, instance);

                        T3Ui.DraggingIsInProgress = false;
                    }
                }

                ImGui.EndDragDropTarget();
            }
        }

        public IEnumerable<Symbol> GetParentSymbols()
        {
            return NodeOperations.GetParentInstances(CompositionOp, includeChildInstance: true).Select(p => p.Symbol);
        }

        private void FocusViewToSelection()
        {
            FitAreaOnCanvas(GetSelectionBounds());
        }

        private ImRect GetSelectionBounds(float padding = 50)
        {
            var selectedOrAll = NodeSelection.IsAnythingSelected()
                                    ? NodeSelection.GetSelectedNodes<ISelectableCanvasObject>().ToArray()
                                    : SelectableChildren.ToArray();

            if (selectedOrAll.Length == 0)
                return new ImRect();

            var firstElement = selectedOrAll[0];
            var bounds = new ImRect(firstElement.PosOnCanvas, firstElement.PosOnCanvas + Vector2.One);
            foreach (var element in selectedOrAll)
            {
                bounds.Add(element.PosOnCanvas);
                bounds.Add(element.PosOnCanvas + element.Size);
            }

            bounds.Expand(padding);
            return bounds;
        }

        private void DrawContextMenu()
        {
            if (T3Ui.OpenedPopUpName == string.Empty)
            {
                CustomComponents.DrawContextMenuForScrollCanvas(DrawContextMenuContent, ref _contextMenuIsOpen);
            }
        }

        private void DrawContextMenuContent()
        {
            var selectedChildUis = GetSelectedChildUis();
            var nextUndoTitle = UndoRedoStack.CanUndo ? $" ({UndoRedoStack.GetNextUndoTitle()})" : string.Empty;
            if (ImGui.MenuItem("Undo" + nextUndoTitle,
                               shortcut: KeyboardBinding.ListKeyboardShortcuts(UserActions.Undo, false),
                               selected: false,
                               enabled: UndoRedoStack.CanUndo))
            {
                UndoRedoStack.Undo();
            }

            ImGui.Separator();

            // ------ for selection -----------------------
            var oneOpSelected = selectedChildUis.Count == 1;
            var someOpsSelected = selectedChildUis.Count > 0;
            var snapShotsEnabledFromSomeOps = !selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.SnapshotGroupIndex == 0);;

            var label = oneOpSelected
                            ? $"{selectedChildUis[0].SymbolChild.ReadableName}..."
                            : $"{selectedChildUis.Count} selected items...";

            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
            ImGui.TextUnformatted(label);
            ImGui.PopStyleColor();
            ImGui.PopFont();

            var allSelectedDisabled = selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.IsDisabled);
            if (ImGui.MenuItem("Disable",
                                                       KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleDisabled, false),
                                                       selected: allSelectedDisabled,
                                                       enabled: selectedChildUis.Count > 0))
            {
                ToggleDisabledForSelectedElements();

            }

            var allSelectedBypassed = selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.SymbolChild.IsBypassed);
            if (ImGui.MenuItem("Bypassed",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleBypassed, false),
                               selected: allSelectedBypassed,
                               enabled: selectedChildUis.Count > 0))
            {
                ToggleBypassedForSelectedElements();
            }

            if (ImGui.MenuItem("Rename", oneOpSelected))
            {
                RenameInstanceOverlay.OpenForSymbolChildUi(selectedChildUis[0]);
            }

            if (ImGui.MenuItem("Arrange sub graph",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.LayoutSelection, false),
                               selected: false,
                               enabled: someOpsSelected))
            {
                SelectableNodeMovement.ArrangeOps();
            }
            
            if (ImGui.MenuItem("Enable for snapshots",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleSnapshotControl, false),
                               selected: snapShotsEnabledFromSomeOps,
                               enabled: someOpsSelected))
            {
                // Disable if already enabled for all
                var enabledForAll = selectedChildUis.TrueForAll(c2 => c2.SnapshotGroupIndex > 0);
                foreach (var c in selectedChildUis)
                {
                    c.SnapshotGroupIndex = enabledForAll ? 0 : 1;
                }
                FlagCurrentCompositionAsModified();
            }

            if (ImGui.BeginMenu("Display as..."))
            {
                if (ImGui.MenuItem("Small", "",
                                   selected: selectedChildUis.Any(child => child.Style == SymbolChildUi.Styles.Default),
                                   enabled: someOpsSelected))
                {
                    foreach (var childUi in selectedChildUis)
                    {
                        childUi.Style = SymbolChildUi.Styles.Default;
                    }
                }

                if (ImGui.MenuItem("Resizable", "",
                                   selected: selectedChildUis.Any(child => child.Style == SymbolChildUi.Styles.Resizable),
                                   enabled: someOpsSelected))
                {
                    foreach (var childUi in selectedChildUis)
                    {
                        childUi.Style = SymbolChildUi.Styles.Resizable;
                    }
                }

                if (ImGui.MenuItem("Expanded", "",
                                   selected: selectedChildUis.Any(child => child.Style == SymbolChildUi.Styles.Resizable),
                                   enabled: someOpsSelected))
                {
                    foreach (var childUi in selectedChildUis)
                    {
                        childUi.Style = SymbolChildUi.Styles.Expanded;
                    }
                }

                ImGui.Separator();

                var isImage = oneOpSelected
                              && selectedChildUis[0].SymbolChild.Symbol.OutputDefinitions.Count > 0
                              && selectedChildUis[0].SymbolChild.Symbol.OutputDefinitions[0].ValueType == typeof(Texture2D);
                if (ImGui.MenuItem("Set image as graph background",
                                   KeyboardBinding.ListKeyboardShortcuts(UserActions.DisplayImageAsBackground, false),
                                   selected: false,
                                   enabled: isImage))
                {
                    var instance = CompositionOp.Children.Single(child => child.SymbolChildId == selectedChildUis[0].Id);
                    GraphWindow.SetBackgroundOutput(instance);
                }

                if (ImGui.MenuItem("Pin to output", oneOpSelected))
                {
                    PinSelectedToOutputWindow();
                }

                ImGui.EndMenu();
            }

            ImGui.Separator();

            if (ImGui.MenuItem("Copy",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.CopyToClipboard, false),
                               selected: false,
                               enabled: someOpsSelected))
            {
                CopySelectedNodesToClipboard();
            }

            if (ImGui.MenuItem("Paste", KeyboardBinding.ListKeyboardShortcuts(UserActions.PasteFromClipboard, false)))
            {
                PasteClipboard();
            }

            var selectedInputUis = GetSelectedInputUis().ToList();
            var selectedOutputUis = GetSelectedOutputUis().ToList();

            if (ImGui.MenuItem("Delete",
                               shortcut: "Del", // dynamic assigned shortcut is too long
                               selected: false,
                               enabled: someOpsSelected || selectedInputUis.Count > 0 || selectedOutputUis.Count > 0))
            {
                DeleteSelectedElements(selectedChildUis, selectedInputUis, selectedOutputUis);
            }

            if (ImGui.MenuItem("Duplicate",
                               KeyboardBinding.ListKeyboardShortcuts(UserActions.Duplicate, false),
                               selected: false,
                               enabled: selectedChildUis.Count > 0))
            {
                CopySelectedNodesToClipboard();
                PasteClipboard();
            }

            ImGui.Separator();
            if (ImGui.BeginMenu("Symbol definition..."))
            {
                if (ImGui.MenuItem("Rename Symbol", oneOpSelected))
                {
                    _renameSymbolDialog.ShowNextFrame();
                    _symbolNameForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Name;
                    //NodeOperations.RenameSymbol(selectedChildUis[0].SymbolChild.Symbol, "NewName");
                }

                if (ImGui.MenuItem("Duplicate as new type...", oneOpSelected))
                {
                    _symbolNameForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Name ?? string.Empty;
                    _nameSpaceForDialogEdits = selectedChildUis[0].SymbolChild.Symbol.Namespace ?? string.Empty;
                    _symbolDescriptionForDialog = "";
                    _duplicateSymbolDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("Combine into new type...", someOpsSelected))
                {
                    _nameSpaceForDialogEdits = CompositionOp.Symbol.Namespace ?? string.Empty;
                    _symbolDescriptionForDialog = "";
                    _combineToSymbolDialog.ShowNextFrame();
                }

                ImGui.EndMenu();
            }
            //}

            if (ImGui.BeginMenu("Add..."))
            {
                if (ImGui.MenuItem("Add Node...", "TAB", false, true))
                {
                    SymbolBrowser.OpenAt(InverseTransformPositionFloat(ImGui.GetMousePos()), null, null, false);
                }

                if (ImGui.MenuItem("Add input parameter..."))
                {
                    _addInputDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("Add output..."))
                {
                    _addOutputDialog.ShowNextFrame();
                }

                if (ImGui.MenuItem("Add Annotation",
                                   shortcut: KeyboardBinding.ListKeyboardShortcuts(UserActions.AddAnnotation, false),
                                   selected: false,
                                   enabled: true))
                {
                    AddAnnotation();
                }

                ImGui.EndMenu();
            }

            ImGui.Separator();

            if (ImGui.MenuItem("Export as Executable", oneOpSelected))
            {
                PlayerExporter.ExportInstance(this, selectedChildUis.Single());
            }
        }

        private void FlagCurrentCompositionAsModified()
        {
            SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].FlagAsModified();
        }

        private void AddAnnotation()
        {
            var size = new Vector2(100, 140);
            var posOnCanvas = InverseTransformPositionFloat(ImGui.GetMousePos());
            var area = new ImRect(posOnCanvas, posOnCanvas + size);

            if (NodeSelection.IsAnythingSelected())
            {
                for (var index = 0; index < NodeSelection.Selection.Count; index++)
                {
                    var node = NodeSelection.Selection[index];
                    var nodeArea = new ImRect(node.PosOnCanvas,
                                              node.PosOnCanvas + node.Size);

                    if (index == 0)
                    {
                        area = nodeArea;
                    }
                    else
                    {
                        area.Add(nodeArea);
                    }
                }

                area.Expand(60);
            }

            var symbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
            var annotation = new Annotation()
                                 {
                                     Id = Guid.NewGuid(),
                                     Title = "Untitled Annotation",
                                     Color = Color.Gray,
                                     PosOnCanvas = area.Min,
                                     Size = area.GetSize()
                                 };
            var command = new AddAnnotationCommand(symbolUi, annotation);
            UndoRedoStack.AddAndExecute(command);
            AnnotationElement.StartRenaming(annotation);
        }

        private void PinSelectedToOutputWindow()
        {
            var outputWindow = OutputWindow.OutputWindowInstances.FirstOrDefault(ow => ow.Config.Visible) as OutputWindow;
            if (outputWindow == null)
            {
                Log.Warning("Can't pin selection without visible output window");
                return;
            }

            var selection = GetSelectedChildUis();
            if (selection.Count != 1)
            {
                Log.Warning("Please select only one operator to pin to output window");
                return;
            }

            var instance = CompositionOp.Children.SingleOrDefault(child => child.SymbolChildId == selection[0].Id);
            if (instance != null)
            {
                outputWindow.Pinning.PinInstance(instance);
            }
        }

        private bool _contextMenuIsOpen;

        private void DeleteSelectedElements(List<SymbolChildUi> selectedChildUis = null, List<IInputUi> selectedInputUis = null,
                                            List<IOutputUi> selectedOutputUis = null)
        {
            var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];

            var commands = new List<ICommand>();
            selectedChildUis = selectedChildUis == null ? GetSelectedChildUis() : selectedChildUis;
            if (selectedChildUis.Any())
            {
                var cmd = new DeleteSymbolChildrenCommand(compositionSymbolUi, selectedChildUis);
                commands.Add(cmd);
            }

            foreach (var selectedAnnotation in NodeSelection.GetSelectedNodes<Annotation>())
            {
                var cmd = new DeleteAnnotationCommand(compositionSymbolUi, selectedAnnotation);
                commands.Add(cmd);
            }

            selectedInputUis ??= NodeSelection.GetSelectedNodes<IInputUi>().ToList();
            if (selectedInputUis.Count > 0)
            {
                NodeOperations.RemoveInputsFromSymbol(selectedInputUis.Select(entry => entry.Id).ToArray(), CompositionOp.Symbol);
            }

            selectedOutputUis ??= NodeSelection.GetSelectedNodes<IOutputUi>().ToList();
            if (selectedOutputUis.Count > 0)
            {
                NodeOperations.RemoveOutputsFromSymbol(selectedOutputUis.Select(entry => entry.Id).ToArray(), CompositionOp.Symbol);
            }

            var deleteCommand = new MacroCommand("Delete elements", commands);
            UndoRedoStack.AddAndExecute(deleteCommand);
            NodeSelection.Clear();
        }

        private void ToggleDisabledForSelectedElements()
        {
            var selectedChildren = GetSelectedChildUis();
            
            var allSelectedDisabled = selectedChildren.TrueForAll(selectedChildUi => selectedChildUi.IsDisabled);
            var shouldDisable = !allSelectedDisabled;

            var commands = new List<ICommand>();
            foreach (var selectedChildUi in selectedChildren)
            {
                commands.Add(new ChangeInstanceIsDisabledCommand(selectedChildUi, shouldDisable));
            }

            UndoRedoStack.AddAndExecute(new MacroCommand("Disable/Enable", commands));
        }
        
        private void ToggleBypassedForSelectedElements()
        {
            var selectedChildUis = GetSelectedChildUis();
            
            var allSelectedAreBypassed = selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.SymbolChild.IsBypassed);
            var shouldBypass = !allSelectedAreBypassed;

            var commands = new List<ICommand>();
            foreach (var selectedChildUi in selectedChildUis)
            {
                commands.Add(new ChangeInstanceBypassedCommand(selectedChildUi.SymbolChild, shouldBypass));
            }

            UndoRedoStack.AddAndExecute(new MacroCommand("Changed Bypassed", commands));
        }
        

        private static List<SymbolChildUi> GetSelectedChildUis()
        {
            return NodeSelection.GetSelectedNodes<SymbolChildUi>().ToList();
        }

        private IEnumerable<IInputUi> GetSelectedInputUis()
        {
            return NodeSelection.GetSelectedNodes<IInputUi>();
        }

        private IEnumerable<IOutputUi> GetSelectedOutputUis()
        {
            return NodeSelection.GetSelectedNodes<IOutputUi>();
        }

        private void CopySelectedNodesToClipboard()
        {
            var selectedChildren = NodeSelection.GetSelectedNodes<SymbolChildUi>();
            var selectedAnnotations = NodeSelection.GetSelectedNodes<Annotation>().ToList();

            var containerOp = new Symbol(typeof(object), Guid.NewGuid());
            var newContainerUi = new SymbolUi(containerOp);
            SymbolUiRegistry.Entries.Add(newContainerUi.Symbol.Id, newContainerUi);

            var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
            var cmd = new CopySymbolChildrenCommand(compositionSymbolUi,
                                                    selectedChildren,
                                                    selectedAnnotations,
                                                    newContainerUi,
                                                    InverseTransformPositionFloat(ImGui.GetMousePos()));
            cmd.Do();

            using (var writer = new StringWriter())
            {
                var jsonWriter = new JsonTextWriter(writer);
                jsonWriter.WriteStartArray();
                SymbolJson.WriteSymbol(containerOp, jsonWriter);
                SymbolUiJson.WriteSymbolUi(newContainerUi, jsonWriter);
                jsonWriter.WriteEndArray();

                try
                {
                    EditorUi.Instance.SetClipboardText(writer.ToString());
                    //Log.Info(Clipboard.GetText(TextDataFormat.UnicodeText));
                }
                catch (Exception)
                {
                    Log.Error("Could not copy elements to clipboard. Perhaps a tool like TeamViewer locks it.");
                }
            }

            SymbolUiRegistry.Entries.Remove(newContainerUi.Symbol.Id);
        }

        private void PasteClipboard()
        {
            try
            {
                var text = EditorUi.Instance.GetClipboardText();
                using (var reader = new StringReader(text))
                {
                    var jsonReader = new JsonTextReader(reader);
                    if (JToken.ReadFrom(jsonReader, SymbolJson.LoadSettings) is not JArray jArray)
                        return;

                    var symbolJson = jArray[0];
                    
                    var gotSymbolJson = SymbolJson.GetPastedSymbol(symbolJson, out var containerSymbol);
                    if (!gotSymbolJson)
                    {
                        Log.Error($"Failed to paste symbol due to invalid symbol json");
                        return;
                    }
                    
                    SymbolRegistry.Entries.Add(containerSymbol.Id, containerSymbol);

                    var symbolUiJson = jArray[1];
                    var hasContainerSymbolUi = SymbolUiJson.TryReadSymbolUi(symbolUiJson, out var containerSymbolUi);
                    if (!hasContainerSymbolUi)
                    {
                        Log.Error($"Failed to paste symbol due to invalid symbol ui json");
                        return;
                    }
                    
                    var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                    SymbolUiRegistry.Entries.Add(containerSymbolUi.Symbol.Id, containerSymbolUi);
                    var cmd = new CopySymbolChildrenCommand(containerSymbolUi,
                                                            null,
                                                            containerSymbolUi.Annotations.Values.ToList(),
                                                            compositionSymbolUi,
                                                            InverseTransformPositionFloat(ImGui.GetMousePos()));
                    cmd.Do(); // FIXME: Shouldn't this be UndoRedoQueue.AddAndExecute() ? 
                    SymbolUiRegistry.Entries.Remove(containerSymbolUi.Symbol.Id);
                    SymbolRegistry.Entries.Remove(containerSymbol.Id);

                    // Select new operators
                    NodeSelection.Clear();

                    foreach (var id in cmd.NewSymbolChildIds)
                    {
                        var newChildUi = compositionSymbolUi.ChildUis.Single(c => c.Id == id);
                        var instance = CompositionOp.Children.Single(c2 => c2.SymbolChildId == id);
                        NodeSelection.AddSymbolChildToSelection(newChildUi, instance);
                    }

                    foreach (var id in cmd.NewSymbolAnnotationIds)
                    {
                        var annotation = compositionSymbolUi.Annotations[id];
                        NodeSelection.AddSelection(annotation);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("Could not paste selection from clipboard.");
                Log.Debug("Paste exception: " + e);
            }
        }

        private void DrawGrid()
        {
            var gridSize = Math.Abs(64.0f * Scale.X);
            for (var x = (-Scroll.X * Scale.X) % gridSize; x < WindowSize.X; x += gridSize)
            {
                DrawList.AddLine(new Vector2(x, 0.0f) + WindowPos,
                                 new Vector2(x, WindowSize.Y) + WindowPos,
                                 GridColor);
            }

            for (var y = (-Scroll.Y * Scale.Y) % gridSize; y < WindowSize.Y; y += gridSize)
            {
                DrawList.AddLine(
                                 new Vector2(0.0f, y) + WindowPos,
                                 new Vector2(WindowSize.X, y) + WindowPos,
                                 GridColor);
            }
        }

        public IEnumerable<ISelectableCanvasObject> SelectableChildren
        {
            get
            {
                _selectableItems.Clear();
                _selectableItems.AddRange(ChildUis);
                var symbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                _selectableItems.AddRange(symbolUi.InputUis.Values);
                _selectableItems.AddRange(symbolUi.OutputUis.Values);
                _selectableItems.AddRange(symbolUi.Annotations.Values);

                return _selectableItems;
            }
        }

        private readonly List<ISelectableCanvasObject> _selectableItems = new List<ISelectableCanvasObject>();
        #endregion

        #region public API
        /// <summary>
        /// The canvas that is currently being drawn from the UI.
        /// Note that <see cref="GraphCanvas"/> is NOT a singleton so you can't rely on this to be valid outside of the Drawing() context.
        /// </summary>
        public static GraphCanvas Current { get; private set; }

        public ImDrawListPtr DrawList { get; private set; }
        public Instance CompositionOp { get; private set; }
        #endregion

        private List<Guid> _compositionPath = new();

        private readonly AddInputDialog _addInputDialog = new();
        private readonly AddOutputDialog _addOutputDialog = new();
        private readonly CombineToSymbolDialog _combineToSymbolDialog = new();
        private readonly DuplicateSymbolDialog _duplicateSymbolDialog = new();
        private readonly RenameSymbolDialog _renameSymbolDialog = new();
        public readonly EditNodeOutputDialog EditNodeOutputDialog = new();
        public static readonly LibWarningDialog LibWarningDialog = new();

        //public override SelectionHandler SelectionHandler { get; } = new SelectionHandler();
        private static readonly Color GridColor = new(0, 0, 0, 0.15f);
        private List<SymbolChildUi> ChildUis { get; set; }
        public readonly SymbolBrowser SymbolBrowser = new SymbolBrowser();
        private string _symbolNameForDialogEdits = "";
        private string _symbolDescriptionForDialog = "";
        private string _nameSpaceForDialogEdits = "";
        private readonly GraphWindow _window;
        private static Vector2 _dampedScrollVelocity = Vector2.Zero;
        private readonly List<Guid> _initialCompositionPath;

        public enum HoverModes
        {
            Disabled,
            Live,
            LastValue,
        }
    }
}