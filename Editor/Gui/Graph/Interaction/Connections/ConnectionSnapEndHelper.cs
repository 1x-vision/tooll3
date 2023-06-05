﻿using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Graph.Interaction.Connections
{
    /// <summary>
    /// A helper that collects potential collection targets during connection drag operations.
    /// </summary>
    public static class ConnectionSnapEndHelper
    {
        public static void PrepareNewFrame()
        {
            _mousePosition = ImGui.GetMousePos();
            BestMatchLastFrame = _bestMatchYetForCurrentFrame;
            if (BestMatchLastFrame != null)
            {
                // drawList.AddRect(_bestMatchLastFrame.Area.Min, _bestMatchLastFrame.Area.Max, Color.Orange);
                var textSize = ImGui.CalcTextSize(BestMatchLastFrame.Name);
                ImGui.SetNextWindowPos(_mousePosition + new Vector2(-textSize.X - 20, -textSize.Y / 2));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(BestMatchLastFrame.Name);
                ImGui.EndTooltip();
                ImGui.PopStyleVar();
            }

            _bestMatchYetForCurrentFrame = null;
            _bestMatchDistance = float.PositiveInfinity;
        }

        public static void ResetSnapping()
        {
            BestMatchLastFrame = null;
        }

        public static void RegisterAsPotentialTarget(SymbolChildUi childUi, IInputUi inputUi, int slotIndex, ImRect areaOnScreen)
        {
            if (ConnectionMaker.TempConnections == null || ConnectionMaker.TempConnections.Count == 0)
                return;

            if (T3Ui.IsAnyPopupOpen)
                return;
            
            if (ConnectionMaker.TempConnections.All(c => c.ConnectionType != inputUi.Type))
                return;

            var distance = Vector2.Distance(areaOnScreen.Min, _mousePosition);
            if (distance > SnapDistance || distance > _bestMatchDistance)
            {
                return;
            }

            _bestMatchYetForCurrentFrame = new PotentialConnectionTarget()
                                               {
                                                   TargetParentOrChildId = childUi.SymbolChild.Id,
                                                   TargetInputId = inputUi.InputDefinition.Id,
                                                   Area = areaOnScreen,
                                                   Name = inputUi.InputDefinition.Name,
                                                   SlotIndex = slotIndex
                                               };
            _bestMatchDistance = distance;
        }

        public static void RegisterAsPotentialTarget(IOutputUi outputUi, ImRect areaOnScreen)
        {
            if (ConnectionMaker.TempConnections == null || ConnectionMaker.TempConnections.Count == 0)
                return;

            if (!string.IsNullOrEmpty(FrameStats.Last.OpenedPopUpName))
                return;
            
            if (ConnectionMaker.TempConnections.All(c => c.ConnectionType != outputUi.Type))
                return;

            var distance = Vector2.Distance(areaOnScreen.Min, _mousePosition);
            if (distance > SnapDistance || distance > _bestMatchDistance)
            {
                return;
            }

            _bestMatchYetForCurrentFrame = new PotentialConnectionTarget()
                                               {
                                                   TargetParentOrChildId = ConnectionMaker.UseSymbolContainerId,
                                                   TargetInputId = outputUi.OutputDefinition.Id,
                                                   Area = areaOnScreen,
                                                   Name = outputUi.OutputDefinition.Name,
                                                   SlotIndex = 0
                                               };
            _bestMatchDistance = distance;
        }

        public static bool IsNextBestTarget(SymbolChildUi childUi, Guid inputDefinitionId, int socketIndex)
        {
            return BestMatchLastFrame != null && BestMatchLastFrame.TargetParentOrChildId == childUi.SymbolChild.Id
                                              && BestMatchLastFrame.TargetInputId == inputDefinitionId
                                              && BestMatchLastFrame.SlotIndex == socketIndex;
        }

        public static bool IsNextBestTarget(IOutputUi outputUi)
        {
            return BestMatchLastFrame != null && BestMatchLastFrame.TargetParentOrChildId == ConnectionMaker.UseSymbolContainerId
                                              && BestMatchLastFrame.TargetInputId == outputUi.Id
                                              && BestMatchLastFrame.SlotIndex == 0;
        }

        public static PotentialConnectionTarget BestMatchLastFrame;
        private static PotentialConnectionTarget _bestMatchYetForCurrentFrame;
        private static float _bestMatchDistance = float.PositiveInfinity;
        private const int SnapDistance = 50;
        private static Vector2 _mousePosition;

        public class PotentialConnectionTarget
        {
            public Guid TargetParentOrChildId;
            public Guid TargetInputId;
            public ImRect Area;
            public string Name;
            public int SlotIndex;
        }
    }
}