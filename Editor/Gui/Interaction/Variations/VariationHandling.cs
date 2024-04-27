﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction.Midi;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.Gui.Windows.Variations;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Interaction.Variations;

/// <summary>
/// Applies actions on variations to the currently active pool.
/// </summary>
/// <remarks>
/// Variations are a sets of symbolChild.input-parameters combinations defined for an Symbol.
/// These input slots can also include the symbols out inputs which thus can be used for defining
/// and applying "presets" to instances of that symbol.
///
/// Most variations will modify(!) the parent symbol. This is great while working within a single symbol
/// and tweaking and blending parameters. However it's potentially unintended (or dangerous) if the
/// modified symbol has many instances. That's why applying symbol-variations is not allowed for Symbols
/// in the lib-namespace.  
/// </remarks>
internal static class VariationHandling
{
    public static SymbolVariationPool ActivePoolForSnapshots { get; private set; }
    public static Instance ActiveInstanceForSnapshots { get; private set; }

    public static SymbolVariationPool ActivePoolForPresets { get; private set; }
    public static Instance ActiveInstanceForPresets { get; private set; }

    /// <summary>
    /// Update variation handling
    /// </summary>
    public static void Update()
    {
        // Sync with composition selected in UI
        var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
        if (primaryGraphWindow == null)
            return;

        var singleSelectedInstance = NodeSelection.GetSelectedInstance();
        if (singleSelectedInstance != null)
        {
            var selectedSymbolId = singleSelectedInstance.Symbol.Id;
            ActivePoolForPresets = GetOrLoadVariations(selectedSymbolId);
            ActivePoolForSnapshots = GetOrLoadVariations(singleSelectedInstance.Parent.Symbol.Id);
            ActiveInstanceForPresets = singleSelectedInstance;
            ActiveInstanceForSnapshots = singleSelectedInstance.Parent;
        }
        else
        {
            ActivePoolForPresets = null;

            var activeCompositionInstance = primaryGraphWindow.GraphCanvas.CompositionOp;
            if (activeCompositionInstance == null)
                return;

            ActiveInstanceForSnapshots = activeCompositionInstance;

            // Prevent variations for library operators
            if (activeCompositionInstance.Symbol.Namespace.StartsWith("lib."))
            {
                ActivePoolForSnapshots = null;
            }
            else
            {
                ActivePoolForSnapshots = GetOrLoadVariations(activeCompositionInstance.Symbol.Id);
            }

            if (!NodeSelection.IsAnythingSelected())
            {
                ActiveInstanceForPresets = ActiveInstanceForSnapshots;
            }
        }

        CompatibleMidiDeviceHandling.UpdateConnectedDevices();
        BlendActions.SmoothVariationBlending.UpdateBlend();
    }

    public static SymbolVariationPool GetOrLoadVariations(Guid symbolId)
    {
        if (_variationPoolForOperators.TryGetValue(symbolId, out var variationForComposition))
        {
            return variationForComposition;
        }

        var newOpVariation = SymbolVariationPool.InitVariationPoolForSymbol(symbolId);
        _variationPoolForOperators[newOpVariation.SymbolId] = newOpVariation;
        return newOpVariation;
    }

    private const int AutoIndex = -1;

    public static Variation CreateOrUpdateSnapshotVariation(int activationIndex = AutoIndex)
    {
        // Only allow for snapshots.
        if (ActivePoolForSnapshots == null || ActiveInstanceForSnapshots == null)
        {
            return null;
        }

        // Delete previous snapshot for that index.
        if (activationIndex != AutoIndex && SymbolVariationPool.TryGetSnapshot(activationIndex, out var existingVariation))
        {
            ActivePoolForSnapshots.DeleteVariation(existingVariation);
        }

        _affectedInstances.Clear();

        AddSnapshotEnabledChildrenToList(ActiveInstanceForSnapshots, _affectedInstances);

        var newVariation = ActivePoolForSnapshots.CreateVariationForCompositionInstances(_affectedInstances);
        if (newVariation == null)
            return null;

        newVariation.PosOnCanvas = VariationBaseCanvas.FindFreePositionForNewThumbnail(ActivePoolForSnapshots.Variations);
        if (activationIndex != AutoIndex)
            newVariation.ActivationIndex = activationIndex;

        newVariation.State = Variation.States.Active;
        ActivePoolForSnapshots.SaveVariationsToFile();
        return newVariation;
    }

    // TODO: Implement undo/redo!
    public static void RemoveInstancesFromVariations(List<Instance> instances, List<Variation> variations)
    {
        if (ActivePoolForSnapshots == null || ActiveInstanceForSnapshots == null)
        {
            return;
        }

        foreach (var variation in variations)
        {
            foreach (var instance in instances)
            {
                if (!variation.ParameterSetsForChildIds.ContainsKey(instance.SymbolChildId))
                    continue;

                variation.ParameterSetsForChildIds.Remove(instance.SymbolChildId);
            }
        }

        ActivePoolForSnapshots.SaveVariationsToFile();
    }

    private static void AddSnapshotEnabledChildrenToList(Instance instance, List<Instance> list)
    {
        var compositionUi = SymbolUiRegistry.Entries[instance.Symbol.Id];
        foreach (var childInstance in instance.Children)
        {
            var symbolChildUi = compositionUi.ChildUis.SingleOrDefault(cui => cui.Id == childInstance.SymbolChildId);
            Debug.Assert(symbolChildUi != null);

            if (symbolChildUi.SnapshotGroupIndex == 0)
                continue;

            list.Add(childInstance);
        }
    }

    private static IEnumerable<Instance> GetSnapshotEnabledChildren(Instance instance)
    {
        var compositionUi = SymbolUiRegistry.Entries[instance.Symbol.Id];
        foreach (var childInstance in instance.Children)
        {
            var symbolChildUi = compositionUi.ChildUis.SingleOrDefault(cui => cui.Id == childInstance.SymbolChildId);
            Debug.Assert(symbolChildUi != null);

            if (symbolChildUi.SnapshotGroupIndex == 0)
                continue;

            yield return childInstance;
        }
    }
    
    private static readonly Dictionary<Guid, SymbolVariationPool> _variationPoolForOperators = new();
    private static readonly List<Instance> _affectedInstances = new(100);
}