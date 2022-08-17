﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Commands.Annotations;
using t3.Gui.Commands.Graph;
using T3.Gui.Graph;
using T3.Gui.Graph.Dialogs;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Interaction;
using T3.Gui.OutputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using T3.Gui.Windows.Output;
using T3.Gui.Windows.TimeLine;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A <see cref="ICanvas"/> that displays the graph of an Operator.
    /// </summary>
    public class GraphCanvas : ScalableCanvas, INodeCanvas
    {
        public GraphCanvas(GraphWindow window, List<Guid> idPath)
        {
            _window = window;
            SetComposition(idPath, Transition.JumpIn);
        }

        public void SetComposition(List<Guid> childIdPath, Transition transition)
        {
            var previousFocusOnScreen = WindowPos + WindowSize / 2;

            var previousInstanceWasSet = _compositionPath != null && _compositionPath.Count > 0;
            if (previousInstanceWasSet)
            {
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
            SetComposition(newPath, Transition.JumpIn);
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

            SetComposition(shortenedPath, Transition.JumpOut);
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
            UpdateCanvas();
            if (!_initialized)
            {
                FocusViewToSelection();
                _initialized = true;
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

                
                DrawList.PushClipRect(WindowPos, WindowPos + WindowSize);

                if (showGrid)
                    DrawGrid();

                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
                {
                    ConnectionMaker.ConnectionSplitHelper.PrepareNewFrame(this);
                }
                
                
                SymbolBrowser.Draw();

                Graph.DrawGraph(DrawList);
                RenameInstanceOverlay.Draw();
                HandleFenceSelection();

                var isOnBackground = ImGui.IsWindowFocused() && !ImGui.IsAnyItemActive();
                if (isOnBackground && ImGui.IsMouseDoubleClicked(0))
                {
                    if(CompositionOp.Parent != null)
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
                                                                    InverseTransformPosition(ImGui.GetIO().MousePos));
                    }
                    else
                    {
                        if (ConnectionMaker.TempConnections[0].GetStatus() != ConnectionMaker.TempConnection.Status.TargetIsDraftNode)
                        {
                            ConnectionMaker.Cancel();
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

        private Symbol GetSelectedSymbol()
        {
            var selectedChildUi = GetSelectedChildUis().FirstOrDefault();
            return selectedChildUi != null ? selectedChildUi.SymbolChild.Symbol : CompositionOp.Symbol;
        }

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
                        var posOnCanvas = InverseTransformPosition(ImGui.GetMousePos());
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

            var label = oneOpSelected
                            ? $"{selectedChildUis[0].SymbolChild.ReadableName}..."
                            : $"{selectedChildUis.Count} selected items...";

            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
            ImGui.TextUnformatted(label);
            ImGui.PopStyleColor();
            ImGui.PopFont();

            // Enable / Disable
            var allSelectedDisabled = selectedChildUis.TrueForAll(selectedChildUi => selectedChildUi.IsDisabled);
            var allSelectedEnabled = selectedChildUis.TrueForAll(selectedChildUi => !selectedChildUi.IsDisabled);
            if (!allSelectedDisabled && ImGui.MenuItem("Disable",
                                                       KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleDisabled, false),
                                                       selected: false,
                                                       enabled: selectedChildUis.Count > 0))
            {
                var commands = new List<ICommand>();
                foreach (var selectedChildUi in selectedChildUis)
                {
                    commands.Add(new ChangeInstanceIsDisabledCommand(selectedChildUi, true));
                }

                UndoRedoStack.AddAndExecute(new MacroCommand("Disable operators", commands));
            }

            if (!allSelectedEnabled && ImGui.MenuItem("Enable",
                                                      KeyboardBinding.ListKeyboardShortcuts(UserActions.ToggleDisabled, false),
                                                      selected: false,
                                                      enabled: someOpsSelected))
            {
                var commands = new List<ICommand>();
                foreach (var selectedChildUi in selectedChildUis)
                {
                    commands.Add(new ChangeInstanceIsDisabledCommand(selectedChildUi, false));
                }

                UndoRedoStack.AddAndExecute(new MacroCommand("Enable operators", commands));
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

            var selectedInputUis = GetSelectedInputUis().ToArray();
            var selectedOutputUis = GetSelectedOutputUis().ToArray();

            if (ImGui.MenuItem("Delete",
                               shortcut: "Del", // dynamic assigned shortcut is too long
                               selected: false,
                               enabled: someOpsSelected || selectedInputUis.Length > 0 || selectedOutputUis.Length > 0))
            {
                DeleteSelectedElements();

                if (selectedInputUis.Length > 0)
                {
                    var symbol = GetSelectedSymbol();
                    NodeOperations.RemoveInputsFromSymbol(selectedInputUis.Select(entry => entry.Id).ToArray(), symbol);
                }

                if (selectedOutputUis.Length > 0)
                {
                    var symbol = GetSelectedSymbol();
                    NodeOperations.RemoveOutputsFromSymbol(selectedOutputUis.Select(entry => entry.Id).ToArray(), symbol);
                }
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
                if (ImGui.MenuItem("Add Node...", "TAB", false,true))
                {
                    SymbolBrowser.OpenAt(InverseTransformPosition(ImGui.GetMousePos()), null, null, false, null);
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
        
        private void AddAnnotation()
        {
            var size = new Vector2(100, 140);
            var posOnCanvas = InverseTransformPosition(ImGui.GetMousePos());
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

        private void DeleteSelectedElements()
        {
            var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
            
            var commands = new List<ICommand>();
            var selectedChildUis = GetSelectedChildUis();
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

            var selectedInputUis = NodeSelection.GetSelectedNodes<IInputUi>().ToList();
            if (selectedInputUis.Count > 0)
            {
                NodeOperations.RemoveInputsFromSymbol(selectedInputUis.Select(entry => entry.Id).ToArray(), CompositionOp.Symbol);
            }

            var deleteCommand = new MacroCommand("Delete elements", commands);
            UndoRedoStack.AddAndExecute(deleteCommand);
            NodeSelection.Clear();
        }

        private void ToggleDisabledForSelectedElements()
        {
            var selectedChildren = GetSelectedChildUis();

            var isNodeHovered = T3Ui.HoveredIdsLastFrame.Count == 1 && CompositionOp != null;
            if (isNodeHovered)
            {
                var hoveredChildUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id].ChildUis
                                                     .SingleOrDefault(c => c.Id == T3Ui.HoveredIdsLastFrame.First());
                if (hoveredChildUi == null)
                    return;

                selectedChildren = new List<SymbolChildUi> { hoveredChildUi };
            }

            var allSelectedDisabled = selectedChildren.TrueForAll(selectedChildUi => selectedChildUi.IsDisabled);
            var shouldDisable = !allSelectedDisabled;

            var commands = new List<ICommand>();
            foreach (var selectedChildUi in selectedChildren)
            {
                commands.Add(new ChangeInstanceIsDisabledCommand(selectedChildUi, shouldDisable));
            }

            UndoRedoStack.AddAndExecute(new MacroCommand("Disable/Enable", commands));
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
                                                    InverseTransformPosition(ImGui.GetMousePos()));
            cmd.Do();

            using (var writer = new StringWriter())
            {
                var json = new SymbolJson { Writer = new JsonTextWriter(writer) { Formatting = Formatting.Indented } };
                json.Writer.WriteStartArray();

                json.WriteSymbol(containerOp);

                var jsonUi = new SymbolUiJson { Writer = json.Writer };
                jsonUi.WriteSymbolUi(newContainerUi);

                json.Writer.WriteEndArray();

                try
                {
                    Clipboard.SetText(writer.ToString(), TextDataFormat.UnicodeText);
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
                var text = Clipboard.GetText();
                using (var reader = new StringReader(text))
                {
                    var json = new SymbolJson { Reader = new JsonTextReader(reader) };
                    if (!(JToken.ReadFrom(json.Reader) is JArray o))
                        return;

                    var symbolJson = o[0];
                    var containerSymbol = json.ReadSymbol(null, symbolJson, true);
                    SymbolRegistry.Entries.Add(containerSymbol.Id, containerSymbol);

                    var symbolUiJson = o[1];
                    var containerSymbolUi = SymbolUiJson.ReadSymbolUi(symbolUiJson);
                    var compositionSymbolUi = SymbolUiRegistry.Entries[CompositionOp.Symbol.Id];
                    SymbolUiRegistry.Entries.Add(containerSymbolUi.Symbol.Id, containerSymbolUi);
                    var cmd = new CopySymbolChildrenCommand(containerSymbolUi, 
                                                            null,
                                                            containerSymbolUi.Annotations.Values.ToList(),
                                                            compositionSymbolUi,
                                                            InverseTransformPosition(ImGui.GetMousePos()));
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
            catch (Exception)
            {
                Log.Warning("Could not copy actual selection to clipboard.");
            }
        }

        private void DrawGrid()
        {
            var color = new Color(0, 0, 0, 0.15f);
            var gridSize = Math.Abs(64.0f * Scale.X);
            for (var x = (-Scroll.X * Scale.X) % gridSize; x < WindowSize.X; x += gridSize)
            {
                DrawList.AddLine(new Vector2(x, 0.0f) + WindowPos,
                                 new Vector2(x, WindowSize.Y) + WindowPos,
                                 color);
            }

            for (var y = (-Scroll.Y * Scale.Y) % gridSize; y < WindowSize.Y; y += gridSize)
            {
                DrawList.AddLine(
                                 new Vector2(0.0f, y) + WindowPos,
                                 new Vector2(WindowSize.X, y) + WindowPos,
                                 color);
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

        private List<Guid> _compositionPath = new List<Guid>();

        private readonly AddInputDialog _addInputDialog = new AddInputDialog();
        private readonly AddOutputDialog _addOutputDialog = new AddOutputDialog();
        private readonly CombineToSymbolDialog _combineToSymbolDialog = new CombineToSymbolDialog();
        private readonly DuplicateSymbolDialog _duplicateSymbolDialog = new DuplicateSymbolDialog();
        private readonly RenameSymbolDialog _renameSymbolDialog = new RenameSymbolDialog();
        public readonly EditNodeOutputDialog EditNodeOutputDialog = new EditNodeOutputDialog();

        //public override SelectionHandler SelectionHandler { get; } = new SelectionHandler();
        private List<SymbolChildUi> ChildUis { get; set; }
        public readonly SymbolBrowser SymbolBrowser = new SymbolBrowser();
        private string _symbolNameForDialogEdits = "";
        private string _symbolDescriptionForDialog = "";
        private string _nameSpaceForDialogEdits = "";
        private readonly GraphWindow _window;
        private bool _initialized; // fit view to to window pos / size
        
        public enum HoverModes
        {
            Disabled,
            Live,
            LastValue,
        }
    }
}