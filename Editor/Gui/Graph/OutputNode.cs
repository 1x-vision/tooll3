﻿using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Graph
{
    /// <summary>
    /// Draws published output parameters of a <see cref="Symbol"/> and uses <see cref="ConnectionMaker"/> 
    /// to create new connections with it.
    /// </summary>
    static class OutputNode
    {
        public static void Draw(Symbol.OutputDefinition outputDef, IOutputUi outputUi)
        {
            ImGui.PushID(outputDef.Id.GetHashCode());
            {
                LastScreenRect = GraphCanvas.Current.TransformRect(new ImRect(outputUi.PosOnCanvas, outputUi.PosOnCanvas + outputUi.Size));
                LastScreenRect.Floor();

                // Interaction
                ImGui.SetCursorScreenPos(LastScreenRect.Min);
                ImGui.InvisibleButton("node", LastScreenRect.GetSize());

                THelpers.DebugItemRect();
                var hovered = ImGui.IsItemHovered();
                if (hovered)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                SelectableNodeMovement.Handle(outputUi);

                // Rendering
                var typeColor = TypeUiRegistry.Entries[outputDef.ValueType].Color;

                var drawList = GraphCanvas.Current.DrawList;
                drawList.AddRectFilled(LastScreenRect.Min, LastScreenRect.Max,
                                 hovered
                                     ? ColorVariations.OperatorHover.Apply(typeColor)
                                     : ColorVariations.OutputNodes.Apply(typeColor));

                drawList.AddRectFilled(new Vector2(LastScreenRect.Min.X, LastScreenRect.Max.Y),
                                 new Vector2(LastScreenRect.Max.X,
                                             LastScreenRect.Max.Y + GraphNode.InputSlotThickness + GraphNode.InputSlotMargin),
                                 ColorVariations.OperatorInputZone.Apply(typeColor));

                // Label
                {
                    var isScaledDown = GraphCanvas.Current.Scale.X < 1;
                    drawList.PushClipRect(LastScreenRect.Min, LastScreenRect.Max, true);
                    ImGui.PushFont(isScaledDown ? Fonts.FontSmall : Fonts.FontBold);

                    var label = string.Format($"{outputDef.Name}");
                    drawList.AddText(LastScreenRect.Min, ColorVariations.OperatorLabel.Apply(typeColor), label);
                    
                    ImGui.PopFont();
                    drawList.PopClipRect();
                }

                if (outputUi.IsSelected)
                {
                    drawList.AddRect(LastScreenRect.Min - Vector2.One, LastScreenRect.Max + Vector2.One, Color.White, 1);
                }

                // Draw slot 
                {
                    var usableSlotArea = new ImRect(
                                                    new Vector2(LastScreenRect.Min.X - GraphNode.UsableSlotThickness,
                                                                LastScreenRect.Min.Y),
                                                    new Vector2(LastScreenRect.Min.X,
                                                                LastScreenRect.Max.Y)); 
                    
                    ConnectionSnapEndHelper.RegisterAsPotentialTarget(outputUi, usableSlotArea);
                    
                    
                    ImGui.SetCursorScreenPos(usableSlotArea.Min);
                    ImGui.InvisibleButton("output", usableSlotArea.GetSize());
                    THelpers.DebugItemRect();
                    var color = TypeUiRegistry.Entries[outputDef.ValueType].Color;

                    //Note: isItemHovered will not work
                    var slotHovered = ConnectionMaker.TempConnections.Count > 0
                                          ? usableSlotArea.Contains(ImGui.GetMousePos())
                                          : ImGui.IsItemHovered();

                    if (ConnectionMaker.IsOutputNodeCurrentConnectionTarget(outputDef))
                    {
                        drawList.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, color);
                    }
                    else if (ConnectionSnapEndHelper.IsNextBestTarget(outputUi) || slotHovered)
                    {
                        if (ConnectionMaker.IsMatchingInputType(outputDef.ValueType))
                        {
                            drawList.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, color);

                            if (ImGui.IsMouseReleased(0))
                            {
                                ConnectionMaker.CompleteAtSymbolOutputNode(GraphCanvas.Current.CompositionOp.Symbol, outputDef);
                            }
                        }
                        else
                        {
                            drawList.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, Color.White);
                            if (ImGui.IsItemClicked(0))
                            {
                                ConnectionMaker.StartFromOutputNode(GraphCanvas.Current.CompositionOp.Symbol, outputDef);
                            }
                        }
                    }
                    else
                    {
                        var colorWithMatching = ConnectionMaker.IsMatchingInputType(outputDef.ValueType) ? Color.White : color;
                        drawList.AddRectFilled(new Vector2(usableSlotArea.Max.X - GraphNode.InputSlotMargin- GraphNode.InputSlotThickness,
                                                           usableSlotArea.Min.Y), 
                                               new Vector2(
                                                           usableSlotArea.Max.X - GraphNode.InputSlotMargin , 
                                                           usableSlotArea.Max.Y), 
                                               colorWithMatching);
                    }
                }
            }
            ImGui.PopID();
        }

        internal static ImRect LastScreenRect;
    }
}