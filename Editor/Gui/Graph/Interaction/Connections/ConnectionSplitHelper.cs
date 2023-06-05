﻿using System.Linq;
using ImGuiNET;
using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using T3.Editor.Gui.Interaction.TransformGizmos;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using Color = T3.Editor.Gui.Styling.Color;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Graph.Interaction.Connections
{
    public class ConnectionSplitHelper
    {
        public static void PrepareNewFrame(GraphCanvas graphCanvas)
        {
            _mousePosition = ImGui.GetMousePos();
            BestMatchLastFrame = _bestMatchYetForCurrentFrame;
            if (BestMatchLastFrame != null && ConnectionMaker.TempConnections.Count == 0)
            {
                var time = ImGui.GetTime();
                if (_hoverStartTime < 0)
                    _hoverStartTime = time;

                var hoverDuration = time - _hoverStartTime;
                var radius = EaseFunctions.EaseOutElastic((float)hoverDuration) * 4;
                var drawList = ImGui.GetForegroundDrawList();

                drawList.AddCircleFilled(_bestMatchYetForCurrentFrame.PositionOnScreen, radius, _bestMatchYetForCurrentFrame.Color, 30);

                var buttonMin = _mousePosition - Vector2.One * radius / 2;
                ImGui.SetCursorScreenPos(buttonMin);

                if (ImGui.InvisibleButton("splitMe", Vector2.One * radius))
                {
                    var posOnScreen = graphCanvas.InverseTransformPositionFloat(_bestMatchYetForCurrentFrame.PositionOnScreen)
                                      - new Vector2(SymbolChildUi.DefaultOpSize.X * 0.25f,
                                                    SymbolChildUi.DefaultOpSize.Y * 0.5f);

                    ConnectionMaker.SplitConnectionWithSymbolBrowser(graphCanvas.CompositionOp.Symbol,
                                                                     graphCanvas.SymbolBrowser,
                                                                     _bestMatchYetForCurrentFrame.Connection,
                                                                     posOnScreen);
                }

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                ImGui.BeginTooltip();
                {
                    var connection = _bestMatchYetForCurrentFrame.Connection;

                    ISlot outputSlot = null;
                    SymbolChild.Output output = null;
                    Symbol.OutputDefinition outputDefinition = null;

                    var sourceOpInstance =
                        graphCanvas.CompositionOp.Children.SingleOrDefault(child => child.SymbolChildId == connection.SourceParentOrChildId);
                    var sourceOp = graphCanvas.CompositionOp.Symbol.Children.SingleOrDefault(child => child.Id == connection.SourceParentOrChildId);
                    if (sourceOpInstance != null)
                    {
                        outputDefinition = sourceOpInstance.Symbol.OutputDefinitions.SingleOrDefault(outDef => outDef.Id == connection.SourceSlotId);
                        if (outputDefinition != null && sourceOp != null)
                        {
                            output = sourceOp.Outputs[connection.SourceSlotId];
                            outputSlot = sourceOpInstance.Outputs.Single(slot => slot.Id == outputDefinition.Id);
                        }
                    }

                    SymbolChild.Input input = null;
                    var targetOp = graphCanvas.CompositionOp.Symbol.Children.SingleOrDefault(child => child.Id == connection.TargetParentOrChildId);
                    if (targetOp != null)
                    {
                        input = targetOp.Inputs[connection.TargetSlotId];
                    }

                    if (outputSlot != null && output != null && input != null)
                    {
                        ImGui.PushFont(Fonts.FontSmall);
                        var connectionSource = sourceOp.ReadableName + "." + output.OutputDefinition.Name;
                        ImGui.TextColored(Color.Gray, connectionSource);

                        var connectionTarget = "->" + targetOp.ReadableName + "." + input.InputDefinition.Name;
                        ImGui.TextColored(Color.Gray, connectionTarget);
                        ImGui.PopFont();

                        var width = 160f;
                        ImGui.BeginChild("thumbnail", new Vector2(width, width * 9 / 16f));
                        {
                            TransformGizmoHandling.SetDrawList(drawList);
                            ImageCanvasForTooltips.Update();
                            ImageCanvasForTooltips.SetAsCurrent();

                            //var sourceOpUi = SymbolUiRegistry.Entries[graphCanvas.CompositionOp.Symbol.Id].ChildUis.Single(childUi => childUi.Id == sourceOp.Id);
                            var sourceOpUi = SymbolUiRegistry.Entries[sourceOpInstance.Symbol.Id];
                            IOutputUi outputUi = sourceOpUi.OutputUis[output.OutputDefinition.Id];
                            EvaluationContext.Reset();
                            EvaluationContext.RequestedResolution = new Size2(1280 / 2, 720 / 2);
                            outputUi.DrawValue(outputSlot, EvaluationContext, recompute: UserSettings.Config.HoverMode == GraphCanvas.HoverModes.Live);

                            if (!string.IsNullOrEmpty(sourceOpUi.Description))
                            {
                                ImGui.Spacing();
                                ImGui.PushFont(Fonts.FontSmall);
                                ImGui.PushStyleColor(ImGuiCol.Text, new Color(1, 1, 1, 0.5f).Rgba);
                                ImGui.TextWrapped(sourceOpUi.Description);
                                ImGui.PopStyleColor();
                                ImGui.PopFont();
                            }

                            ImageCanvasForTooltips.Deactivate();
                            TransformGizmoHandling.RestoreDrawList();
                        }
                        ImGui.EndChild();

                        FrameStats.AddHoveredId(targetOp.Id);
                        FrameStats.AddHoveredId(sourceOp.Id);
                    }
                }
                ImGui.EndTooltip();
                ImGui.PopStyleVar();
            }
            else
            {
                _hoverStartTime = -1;
            }

            _bestMatchYetForCurrentFrame = null;
            _bestMatchDistance = float.PositiveInfinity;
        }

        public static void ResetSnapping()
        {
            BestMatchLastFrame = null;
        }

        public static void RegisterAsPotentialSplit(Symbol.Connection connection, Color color, Vector2 position)
        {
            var distance = Vector2.Distance(position, _mousePosition);
            if (distance > SnapDistance || distance > _bestMatchDistance)
            {
                return;
            }

            _bestMatchYetForCurrentFrame = new PotentialConnectionSplit()
                                               {
                                                   Connection = connection,
                                                   PositionOnScreen = position,
                                                   Color = color,
                                               };
            _bestMatchDistance = distance;
        }

        private static readonly ImageOutputCanvas ImageCanvasForTooltips = new ImageOutputCanvas();
        private static readonly EvaluationContext EvaluationContext = new EvaluationContext();

        public static PotentialConnectionSplit BestMatchLastFrame;
        private static PotentialConnectionSplit _bestMatchYetForCurrentFrame;
        private static float _bestMatchDistance = float.PositiveInfinity;
        private const int SnapDistance = 50;
        private static Vector2 _mousePosition;
        private static double _hoverStartTime = -1;

        public class PotentialConnectionSplit
        {
            public Vector2 PositionOnScreen;
            public Symbol.Connection Connection;
            public Color Color;
        }
    }
}