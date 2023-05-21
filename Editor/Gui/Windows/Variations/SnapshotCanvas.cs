﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Windows.Variations
{
    public class SnapshotCanvas : VariationBaseCanvas
    {
        protected override Instance InstanceForBlendOperations => VariationHandling.ActiveInstanceForSnapshots;
        protected override SymbolVariationPool PoolForBlendOperations => VariationHandling.ActivePoolForSnapshots;

        public override void DrawToolbarFunctions()
        {
            var s = ImGui.GetFrameHeight();

            if (CustomComponents.IconButton(Icon.Plus, new Vector2(s, s)))
            {
                CreateVariation();
            }
        }

        public override string GetTitle()
        {
            return VariationHandling.ActiveInstanceForSnapshots != null 
                       ? $"...for {VariationHandling.ActiveInstanceForSnapshots.Symbol.Name}" 
                       : string.Empty;
        }

        protected override void DrawAdditionalContextMenuContent()
        {
            var oneSelected = Selection.SelectedElements.Count == 1;
            var oneOrMoreSelected = Selection.SelectedElements.Count > 0;

            if (ImGui.MenuItem("Select affected Operators",
                               "",
                               false,
                               oneOrMoreSelected))
            {
                NodeSelection.Clear();

                foreach (var element in Selection.SelectedElements)
                {
                    if (element is not Variation selectedVariation)
                        continue;

                    var parentSymbolUi = SymbolUiRegistry.Entries[InstanceForBlendOperations.Symbol.Id];

                    foreach (var symbolChildUi in parentSymbolUi.ChildUis)
                    {
                        if (!selectedVariation.ParameterSetsForChildIds.ContainsKey(symbolChildUi.Id))
                            continue;

                        var instance = InstanceForBlendOperations.Children.FirstOrDefault(c => c.SymbolChildId == symbolChildUi.Id);
                        if (instance != null)
                            NodeSelection.AddSymbolChildToSelection(symbolChildUi, instance);
                    }
                }

                FitViewToSelectionHandling.FitViewToSelection();
            }

            if (ImGui.MenuItem("Remove selected Ops from Variations",
                               "",
                               false,
                               oneOrMoreSelected))
            {
                var selectedInstances = NodeSelection.GetSelectedInstances().ToList();
                var selectedThumbnails = new List<Variation>();
                foreach (var thumbnail in Selection.SelectedElements)
                {
                    if (thumbnail is Variation v)
                    {
                        selectedThumbnails.Add(v);
                    }
                }

                VariationHandling.RemoveInstancesFromVariations(selectedInstances, selectedThumbnails);
            }
        }

        public override Variation CreateVariation()
        {
            var newVariation = VariationHandling.CreateOrUpdateSnapshotVariation();
            if (newVariation == null)
                return new Variation();

            PoolForBlendOperations.SaveVariationsToFile();
            Selection.SetSelection(newVariation);
            ResetView();
            TriggerThumbnailUpdate();
            VariationThumbnail.VariationForRenaming = newVariation;
            return new Variation();
        }
    }
}