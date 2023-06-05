﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Animation;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.Animation;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.InputUi
{
    /// <summary>
    /// This abstract implementation for drawing and serializing parameters. 
    /// </summary>
    public abstract class InputValueUi<T> : IInputUi
    {
        private const float ConnectionAreaWidth = 30.0f;
        private static float ParameterNameWidth => ImGui.GetTextLineHeight() * 120.0f / 16;

        public SymbolUi Parent { get; set; }
        public Symbol.InputDefinition InputDefinition { get; set; }
        public Guid Id => InputDefinition.Id;
        public Relevancy Relevancy { get; set; } = Relevancy.Optional;
        public string GroupTitle { get; set; }
        public bool AddPadding { get; set; }
        public virtual bool IsAnimatable => false;
        protected Type MappedType { get; private set; }

        public abstract IInputUi Clone();

        public virtual void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time)
        {
            Log.Warning(IsAnimatable
                            ? "Input type has no implementation to insert values into animation curves"
                            : "Should only be called for animated input types");
        }

        /// <summary>
        /// Wraps the implementation of an parameter control to handle <see cref="InputEditStateFlags"/>
        /// </summary>
        protected abstract InputEditStateFlags DrawEditControl(string name, SymbolChild.Input input, ref T value, bool readOnly);

        protected abstract void DrawReadOnlyControl(string name, ref T value);

        protected virtual string GetSlotValueAsString(ref T value)
        {
            return string.Empty;
        }

        protected virtual InputEditStateFlags DrawAnimatedValue(string name, InputSlot<T> inputSlot, Animator animator)
        {
            Log.Warning("Animated type didn't not implement DrawAnimatedValue");
            return InputEditStateFlags.Nothing;
        }

        public virtual string GetSlotValue(IInputSlot inputSlot)
        {
            if (inputSlot is InputSlot<T> typedInputSlot)
            {
                return GetSlotValueAsString(ref typedInputSlot.Value);
            }

            return string.Empty;
        }

        private readonly Icon[] _keyframeButtonIcons = new[]
                                                           {
                                                               Icon.KeyframeToggleOffNone,
                                                               Icon.KeyframeToggleOffLeft,
                                                               Icon.KeyframeToggleOffRight,
                                                               Icon.KeyframeToggleOffBoth,
                                                               Icon.KeyframeToggleOnNone,
                                                               Icon.KeyframeToggleOnLeft,
                                                               Icon.KeyframeToggleOnRight,
                                                               Icon.KeyframeToggleOnBoth,
                                                           };

        public InputEditStateFlags DrawParameterEdit(IInputSlot inputSlot, SymbolUi compositionUi, SymbolChildUi symbolChildUi, bool hideNonEssentials,
                                                     bool skipIfDefault)
        {
            var editState = InputEditStateFlags.Nothing;
            if ((inputSlot.IsConnected || inputSlot.IsMultiInput) && hideNonEssentials)
                return editState;
            
            var name = inputSlot.Input.Name;
            var typeColor = TypeUiRegistry.Entries[Type].Color;
            var compositionSymbol = compositionUi.Symbol;
            var animator = compositionSymbol.Animator;

            Curve animationCurve = null;
            var isAnimated = IsAnimatable && animator.TryGetFirstInputAnimationCurve(inputSlot, out animationCurve);
            MappedType = inputSlot.MappedType;

            if (inputSlot is not InputSlot<T> typedInputSlot)
            {
                Debug.Assert(false);
                return editState;
            }

            var input = inputSlot.Input;
            if (input.IsDefault && skipIfDefault)
                return InputEditStateFlags.Nothing;

            if (inputSlot.IsConnected)
            {
                editState = DrawConnectedParameter();
            }
            else if (isAnimated)
            {
                editState = DrawAnimatedParameter();
            }
            else
            {
                editState = DrawNormalParameter();
            }

            return editState;

            #region draw parameter types --------------------------------------------------------
            InputEditStateFlags DrawConnectedParameter()
            {
                if (typedInputSlot.IsMultiInput)
                {
                    // Just show actual value
                    ImGui.Button(name + "##paramName", new Vector2(-1, 0));
                    if (ImGui.BeginPopupContextItem("##parameterOptions", 0))
                    {
                        if (ImGui.MenuItem("Parameters settings"))
                            editState = InputEditStateFlags.ShowOptions;

                        ImGui.EndPopup();
                    }

                    var multiInput = (MultiInputSlot<T>)typedInputSlot;
                    var allInputs = multiInput.GetCollectedTypedInputs();

                    for (int multiInputIndex = 0; multiInputIndex < allInputs.Count; multiInputIndex++)
                    {
                        ImGui.PushID(multiInputIndex);

                        ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Highlight.Apply(typeColor).Rgba);
                        if (ImGui.Button("->", new Vector2(ConnectionAreaWidth, 0)))
                        {
                            // TODO: implement with proper SelectionManager
                            //var compositionSymbol = compositionSymbol;
                            //var allConnections = compositionSymbol.Connections.FindAll(c => c.IsTargetOf(symbolChildUi.Id, inputSlot.Id));
                            //var connection = allConnections[multiInputIndex];
                            // var sourceUi = compositionUi.GetSelectables()
                            //                             .First(ui => ui.Id == connection.SourceParentOrChildId || ui.Id == connection.SourceSlotId);
                        }

                        ImGui.PopStyleColor();

                        ImGui.SameLine();

                        ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1.0f, 0.5f));
                        ImGui.Button("#" + multiInputIndex, new Vector2(ParameterNameWidth, 0.0f));
                        ImGui.PopStyleVar();
                        ImGui.SameLine();

                        ImGui.SetNextItemWidth(-1);
                        ImGui.PushStyleColor(ImGuiCol.Text, T3Style.Colors.ConnectedParameter.Rgba);
                        var slot = allInputs[multiInputIndex];
                        DrawReadOnlyControl("##multiInputParam", ref slot.Value);
                        ImGui.PopStyleColor();
                        ImGui.PopID();
                    }

                    ImGui.Spacing();
                }
                else
                {
                    // Connected single inputs
                    ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Highlight.Apply(typeColor).Rgba);
                    if (ImGui.Button("->", new Vector2(ConnectionAreaWidth, 0.0f)))
                    {
                        // TODO: implement with proper selectionManager
                        //var compositionSymbol = compositionSymbol;
                        var connection = compositionSymbol.Connections.First(c => c.IsTargetOf(symbolChildUi.Id, inputSlot.Id));
                        var sourceUi = compositionUi.GetSelectables()
                                                    .First(ui => ui.Id == connection.SourceParentOrChildId || ui.Id == connection.SourceSlotId);
                        // Try to find instance
                        if (sourceUi is SymbolChildUi sourceSymbolChildUi)
                        {
                            var selectedInstance = NodeSelection.GetFirstSelectedInstance();
                            var parent = selectedInstance.Parent;
                            var selectionTargetInstance = parent.Children.Single(instance => instance.SymbolChildId == sourceUi.Id);
                            NodeSelection.SetSelectionToChildUi(sourceSymbolChildUi, selectionTargetInstance);
                            FitViewToSelectionHandling.FitViewToSelection();
                        }
                    }

                    ImGui.PopStyleColor();
                    ImGui.SameLine();

                    // Draw Name
                    ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1.0f, 0.5f));
                    ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0.0f));
                    if (ImGui.BeginPopupContextItem("##parameterOptions", 0))
                    {
                        if (ImGui.MenuItem("Parameters settings"))
                            editState = InputEditStateFlags.ShowOptions;

                        ImGui.EndPopup();
                    }

                    ImGui.PopStyleVar();

                    ImGui.SameLine();

                    //// Draw name
                    ImGui.PushItemWidth(200.0f);
                    ImGui.PushStyleColor(ImGuiCol.Text, input.IsDefault ? Color.Gray.Rgba : Color.White.Rgba);
                    ImGui.SetNextItemWidth(-1);

                    DrawReadOnlyControl(name, ref typedInputSlot.Value);
                    ImGui.PopStyleColor();
                    ImGui.PopItemWidth();
                }

                return editState;
            }

            InputEditStateFlags DrawAnimatedParameter()
            {
                var hasKeyframeAtCurrentTime = animationCurve.HasVAt(Playback.Current.TimeInBars);
                var hasKeyframeBefore = animationCurve.ExistVBefore(Playback.Current.TimeInBars);
                var hasKeyframeAfter = animationCurve.ExistVAfter(Playback.Current.TimeInBars);

                var iconIndex = 0;
                const int leftBit = 1 << 0;
                const int rightBit = 1 << 1;
                const int onBit = 1 << 2;

                if (hasKeyframeBefore) iconIndex |= leftBit;
                if (hasKeyframeAfter) iconIndex |= rightBit;
                if (hasKeyframeAtCurrentTime) iconIndex |= onBit;
                var icon = _keyframeButtonIcons[iconIndex];

                if (ImGui.Button("##icon", new Vector2(ConnectionAreaWidth, 0.0f)))
                {
                    // TODO: this should use Undo/Redo commands
                    if (hasKeyframeAtCurrentTime)
                    {
                        AnimationOperations.RemoveKeyframeFromCurves(animator.GetCurvesForInput(inputSlot),
                                                                     Playback.Current.TimeInBars);
                    }
                    else
                    {
                        AnimationOperations.InsertKeyframeToCurves(animator.GetCurvesForInput(inputSlot),
                                                                   Playback.Current.TimeInBars);
                    }
                }

                Icons.DrawIconOnLastItem(icon, Color.White);

                ImGui.SameLine();

                // Draw Name
                ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1.0f, 0.5f));
                ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0.0f));
                CustomComponents.ContextMenuForItem(() =>
                                                    {
                                                        if (ImGui.MenuItem("Jump To Previous Keyframe", hasKeyframeBefore))
                                                        {
                                                            UserActionRegistry.DeferredActions.Add(UserActions.PlaybackJumpToPreviousKeyframe);
                                                        }

                                                        if (ImGui.MenuItem("Jump To Next Keyframe", hasKeyframeBefore))
                                                        {
                                                            UserActionRegistry.DeferredActions.Add(UserActions.PlaybackJumpToNextKeyframe);
                                                        }

                                                        if (hasKeyframeAtCurrentTime)
                                                        {
                                                            if (ImGui.MenuItem("Remove keyframe"))
                                                            {
                                                                AnimationOperations.RemoveKeyframeFromCurves(animator.GetCurvesForInput(inputSlot),
                                                                    Playback.Current.TimeInBars);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (ImGui.MenuItem("Insert keyframe"))
                                                            {
                                                                AnimationOperations.InsertKeyframeToCurves(animator.GetCurvesForInput(inputSlot),
                                                                    Playback.Current.TimeInBars);
                                                            }
                                                        }

                                                        ImGui.Separator();

                                                        if (ImGui.MenuItem("Remove Animation"))
                                                        {
                                                            UndoRedoStack.AddAndExecute(new RemoveAnimationsCommand(animator, new[] { inputSlot }));
                                                        }

                                                        if (ImGui.MenuItem("Parameters settings"))
                                                            editState = InputEditStateFlags.ShowOptions;
                                                    });
                ImGui.PopStyleVar();
                ImGui.SameLine();

                ImGui.PushItemWidth(200.0f);
                ImGui.PushStyleColor(ImGuiCol.Text, Color.Orange.Rgba);
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Color.Black.Rgba);

                ImGui.SetNextItemWidth(-1);

                editState |= DrawAnimatedValue(name, typedInputSlot, animator);

                ImGui.PopStyleColor(2);
                ImGui.PopItemWidth();
                return editState;
            }

            InputEditStateFlags DrawNormalParameter()
            {
                ImGui.PushStyleColor(ImGuiCol.Button, ColorVariations.Operator.Apply(typeColor).Rgba);

                
                ImGui.PushStyleColor(ImGuiCol.Text, T3Style.Colors.DarkGray.Rgba);
                
                if (ImGui.Button(string.Empty, new Vector2(ConnectionAreaWidth, 0.0f)))
                {
                    if (IsAnimatable && (UserSettings.Config.ParameterMode == UserSettings.ParameterModes.AnimatesInput || ImGui.GetIO().KeyCtrl))
                    {
                        var cmd = new MacroCommand("add animation",
                                                   new List<ICommand>()
                                                       {
                                                           new ChangeInputValueCommand(compositionUi.Symbol, symbolChildUi.SymbolChild.Id, input,
                                                                                       inputSlot.Input.Value),
                                                           new AddAnimationCommand(animator, inputSlot),
                                                       });

                        UndoRedoStack.AddAndExecute(cmd);
                    }
                    else if (UserSettings.Config.ParameterMode == UserSettings.ParameterModes.CreatesConnectedOp || ImGui.GetIO().KeyCtrl)
                    {
                        if (ConnectionMaker.TempConnections.Count == 0)
                        {
                            ConnectionMaker.StartFromInputSlot(compositionSymbol, symbolChildUi, InputDefinition);
                            var freePosition = NodeGraphLayouting.FindPositionForNodeConnectedToInput(compositionSymbol, symbolChildUi, InputDefinition);
                            ConnectionMaker.InitSymbolBrowserOnPrimaryGraphWindow(freePosition);
                        }
                    }
                }

                
                if (IsAnimatable && ImGui.GetIO().KeyCtrl)
                {
                    Icons.DrawIconOnLastItem(Icon.AddKeyframe, T3Style.Colors.TextMuted.Fade(0.3f));
                }
                else
                {
                    Icons.DrawIconOnLastItem(Icon.AddOpToInput, T3Style.Colors.TextMuted.Fade(0.3f));
                }


                if (ImGui.IsItemActive() && ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).Length() > UserSettings.Config.ClickThreshold)
                {
                    if (ConnectionMaker.TempConnections.Count == 0)
                    {
                        ConnectionMaker.StartFromInputSlot(compositionSymbol, symbolChildUi, InputDefinition);
                    }
                }
                ImGui.PopStyleColor(2);

                //CustomComponents.TooltipForLastItem("");
                if (ImGui.IsItemHovered() && IsAnimatable)
                    ImGui.SetTooltip($"Click CTRL to Animate\n{input.DefaultValue.ValueType}");

                ImGui.SameLine();

                // Draw Name
                ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1.0f, 0.5f));

                if (input.IsDefault)
                {
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, T3Style.Colors.Button.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.Text, T3Style.Colors.TextMuted.Rgba);
                    ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0.0f));
                    ImGui.PopStyleColor(2);
                    ImGui.SameLine();
                }
                else
                {
                    var isClicked = ImGui.Button(input.Name + "##ParamName", new Vector2(ParameterNameWidth, 0.0f));
                    ImGui.SameLine();
                    if (!input.IsDefault)
                    {
                        Icons.DrawIconAtScreenPosition(Icon.Revert, ImGui.GetItemRectMin() + new Vector2(6, 2));
                        if (isClicked)
                        {
                            UndoRedoStack.AddAndExecute(new ResetInputToDefault(compositionSymbol, symbolChildUi.Id, input));
                        }
                    }
                }

                CustomComponents.ContextMenuForItem(() =>
                                                    {
                                                        if (ImGui.MenuItem("Set as default", !input.IsDefault))
                                                        {
                                                            // Todo: Implement Undo/Redo Command
                                                            input.SetCurrentValueAsDefault();
                                                            var symbolUi = SymbolUiRegistry.Entries[symbolChildUi.SymbolChild.Symbol.Id];
                                                            symbolUi.Symbol.InvalidateInputDefaultInInstances(inputSlot);
                                                            symbolUi.FlagAsModified();
                                                        }

                                                        if (ImGui.MenuItem("Reset to default", !input.IsDefault))
                                                        {
                                                            UndoRedoStack.AddAndExecute(new ResetInputToDefault(compositionSymbol, symbolChildUi.Id,
                                                                                            input));
                                                        }

                                                        if (ImGui.MenuItem("Extract as connection operator"))
                                                        {
                                                            ParameterExtraction.ExtractAsConnectedOperator(inputSlot, symbolChildUi, input);
                                                        }
                                                        
                                                        if (ImGui.MenuItem("Publish as Input"))
                                                        {
                                                            PublishAsInput(inputSlot, symbolChildUi, input);
                                                        }

                                                        if (ImGui.MenuItem("Parameters settings"))
                                                            editState = InputEditStateFlags.ShowOptions;
                                                    });

                ImGui.PopStyleVar();

                // Draw control
                ImGui.PushItemWidth(200.0f);
                ImGui.PushStyleColor(ImGuiCol.Text, input.IsDefault ? Color.Gray.Rgba : Color.White.Rgba);
                if (input.IsDefault)
                {
                    input.Value.Assign(input.DefaultValue);
                }

                ImGui.SetNextItemWidth(-1);

                editState |= DrawEditControl(name, input, ref typedInputSlot.TypedInputValue.Value, false);
                if ((editState & InputEditStateFlags.Modified) == InputEditStateFlags.Modified ||
                    (editState & InputEditStateFlags.Finished) == InputEditStateFlags.Finished)
                {
                    compositionSymbol.InvalidateInputInAllChildInstances(inputSlot);
                }

                if ((editState & InputEditStateFlags.ResetToDefault) == InputEditStateFlags.ResetToDefault)
                {
                    input.ResetToDefault();
                    compositionSymbol.InvalidateInputInAllChildInstances(inputSlot);
                }

                input.IsDefault &= (editState & InputEditStateFlags.Modified) != InputEditStateFlags.Modified;

                ImGui.PopStyleColor();
                ImGui.PopItemWidth();
                return editState;
            }
            #endregion
        }

        private static void PublishAsInput(IInputSlot inputSlot, SymbolChildUi symbolChildUi, SymbolChild.Input input)
        {
            var composition = NodeSelection.GetSelectedComposition() ?? inputSlot.Parent.Parent;

            if (composition == null)
            {
                Log.Warning("Can't publish input to undefined composition");
                return;
            }

            NodeOperations.AddInputToSymbol(input.Name, input.InputDefinition.IsMultiInput, input.DefaultValue.ValueType, composition.Symbol);
            NodeOperations.UpdateChangedOperators();

            var updatedComposition = NodeOperations.GetInstanceFromIdPath(OperatorUtils.BuildIdPathForInstance(composition));

            var newInput = updatedComposition.Symbol.InputDefinitions.SingleOrDefault(i => i.Name == input.Name);
            if (newInput != null)
            {
                var cmd = new AddConnectionCommand(updatedComposition.Symbol,
                                                   new Symbol.Connection(sourceParentOrChildId: ConnectionMaker.UseSymbolContainerId,
                                                                         sourceSlotId: newInput.Id,
                                                                         targetParentOrChildId: symbolChildUi.Id,
                                                                         targetSlotId: input.InputDefinition.Id),
                                                   0);
                cmd.Do();
                newInput.DefaultValue = input.Value.Clone();
                inputSlot.DirtyFlag.Invalidate();
            }
        }

        public virtual void DrawSettings()
        {
            var addPadding = AddPadding;
            if (FormInputs.AddCheckBox("Insert Padding above", ref addPadding))
            {
                AddPadding = addPadding;
            }

            var opensGroups = GroupTitle != null;
            if (FormInputs.AddCheckBox("Starts Parameter group", ref opensGroups))
            {
                GroupTitle = opensGroups ? "Group Title" : null;
            }

            if (opensGroups)
            {
                var groupTitle = GroupTitle;
                if (FormInputs.AddStringInput("Group Title", ref groupTitle, "GroupTitle", null,
                                              "Group title shown above parameter\n\nGroup will be collapsed by default if name ends with '...' (three dots)."))
                {
                    GroupTitle = groupTitle;
                }
            }

            FormInputs.AddVerticalSpace();

            var tmpForRef = Relevancy;
            if (FormInputs.AddEnumDropdown(ref tmpForRef, "Relevancy"))
                Relevancy = tmpForRef;
        }

        public virtual void Write(JsonTextWriter writer)
        {
            if (Relevancy != DefaultRelevancy)
                writer.WriteObject(nameof(Relevancy), Relevancy.ToString());

            var vec2Writer = TypeValueToJsonConverters.Entries[typeof(Vector2)];
            writer.WritePropertyName("Position");
            vec2Writer(writer, PosOnCanvas);
            if (!string.IsNullOrEmpty(GroupTitle))
                writer.WriteObject(nameof(GroupTitle), GroupTitle);

            if (AddPadding)
                writer.WriteObject(nameof(AddPadding), AddPadding);
        }

        public virtual void Read(JToken inputToken)
        {
            Relevancy = (inputToken[nameof(Relevancy)] == null)
                            ? DefaultRelevancy
                            : (Relevancy)Enum.Parse(typeof(Relevancy), inputToken["Relevancy"].ToString());

            JToken positionToken = inputToken["Position"];
            if(positionToken != null)
                PosOnCanvas = new Vector2((positionToken["X"] ?? 0).Value<float>(), 
                                          (positionToken["Y"] ?? 0).Value<float>());
            
            GroupTitle = inputToken[nameof(GroupTitle)]?.Value<string>();
            AddPadding = inputToken[nameof(AddPadding)]?.Value<bool>() ?? false;
        }

        public Type Type { get; } = typeof(T);

        /// <summary>
        /// Defines position of inputNode within graph 
        /// </summary>
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;

        public Vector2 Size { get; set; } = SymbolChildUi.DefaultOpSize;
        public bool IsSelected => NodeSelection.IsNodeSelected(this);

        // ReSharper disable once StaticMemberInGenericType
        private const Relevancy DefaultRelevancy = Relevancy.Optional;
    }
}