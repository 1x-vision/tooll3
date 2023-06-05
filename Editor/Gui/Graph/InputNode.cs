﻿using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Graph
{
    /// <summary>
    /// Draws published input parameters of a <see cref="Symbol"/> and uses <see cref="ConnectionMaker"/> 
    /// create new connections with it.
    /// </summary>
    static class InputNode
    {
        internal static bool Draw(Symbol.InputDefinition inputDef, IInputUi inputUi, int index)
        { 
            var isSelectedOrHovered = false;
            ImGui.PushID(inputDef.Id.GetHashCode());
            {
                
                _lastScreenRect = GraphCanvas.Current.TransformRect(new ImRect(inputUi.PosOnCanvas, inputUi.PosOnCanvas + inputUi.Size));
                _lastScreenRect.Floor();

                // Interaction
                ImGui.SetCursorScreenPos(_lastScreenRect.Min);
                ImGui.InvisibleButton("node", _lastScreenRect.GetSize());

                THelpers.DebugItemRect();
                var hovered = ImGui.IsItemHovered();
                if (hovered)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    isSelectedOrHovered = true;
                }

                SelectableNodeMovement.Handle(inputUi);

                // Rendering
                var typeColor = TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;

                var drawList = GraphCanvas.Current.DrawList;
                drawList.AddRectFilled(_lastScreenRect.Min, _lastScreenRect.Max,
                                       hovered
                                           ? ColorVariations.OperatorHover.Apply(typeColor)
                                           : ColorVariations.ConnectionLines.Apply(typeColor));

                var inputUiIsSelected = inputUi.IsSelected;
                isSelectedOrHovered |= inputUiIsSelected;
                
                if (inputUiIsSelected)
                {
                    const float thickness = 1;
                    drawList.AddRect(_lastScreenRect.Min - Vector2.One * thickness,
                                     _lastScreenRect.Max + Vector2.One * thickness ,
                                     Color.White, 0f, 0, thickness);
                }
                
                // Label
                {
                    
                    var isScaledDown = GraphCanvas.Current.Scale.X < 1;
                    ImGui.PushFont(isScaledDown ? Fonts.FontSmall : Fonts.FontBold);
                    
                    // Index
                    //var nameLabel = string.Format($"{inputDef.Name}");
                    //var nameLabel = inputDef.Name;
                    var size = ImGui.CalcTextSize(inputDef.Name);
                    var yPos = _lastScreenRect.GetCenter().Y - size.Y / 2;
                    drawList.AddText(new Vector2(_lastScreenRect.Min.X - 20, yPos),
                                     ColorVariations.ConnectionLines.Apply(typeColor),
                                     (index+1) + ".");
                    
                    drawList.PushClipRect(_lastScreenRect.Min, _lastScreenRect.Max, true);
                    var labelPos = new Vector2(
                                          _lastScreenRect.Max.X - size.X -4,
                                          yPos);

                    drawList.AddText(labelPos,
                                     ColorVariations.OperatorLabel.Apply(typeColor),
                                     inputDef.Name);
                    ImGui.PopFont();
                    drawList.PopClipRect();
                }
                

                // Draw slot 
                {
                    var usableSlotArea = new ImRect(
                                                    new Vector2(_lastScreenRect.Max.X,
                                                                _lastScreenRect.Min.Y),
                                                    new Vector2(_lastScreenRect.Max.X + GraphNode.UsableSlotThickness,
                                                                _lastScreenRect.Max.Y));

                    ImGui.SetCursorScreenPos(usableSlotArea.Min);
                    ImGui.InvisibleButton("output", usableSlotArea.GetSize());
                    THelpers.DebugItemRect();
                    var color = TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;
                    color = ColorVariations.ConnectionLines.Apply(color);

                    if (ConnectionMaker.IsInputNodeCurrentConnectionSource(inputDef))
                    {
                        drawList.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, color);
                    }
                    else if (ImGui.IsItemHovered())
                    {
                        if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                        {
                            drawList.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, color);

                            if (ImGui.IsMouseReleased(0))
                            {
                                ConnectionMaker.CompleteAtSymbolInputNode(GraphCanvas.Current.CompositionOp.Symbol, inputDef);
                            }
                        }
                        else
                        {
                            drawList.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, Color.White);
                            if (ImGui.IsItemClicked(0))
                            {
                                ConnectionMaker.StartFromInputNode(inputDef);
                            }
                        }
                    }
                    else
                    {
                        drawList.AddRectFilled(new Vector2(usableSlotArea.Min.X + 1,
                                                           usableSlotArea.Min.Y),
                                               new Vector2(usableSlotArea.Min.X + GraphNode.UsableSlotThickness,
                                                           usableSlotArea.Max.Y),
                                               color);
                    }
                }
            }
            ImGui.PopID();
            return isSelectedOrHovered;
        }

        internal static ImRect _lastScreenRect;
    }
}