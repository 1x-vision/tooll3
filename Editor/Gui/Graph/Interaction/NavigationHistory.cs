﻿using System;
using System.Collections.Generic;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Helpers;

namespace T3.Editor.Gui.Graph.Interaction;

/// <summary>
/// Manage the navigation between previously selected instances.
/// This can also be used sort items by relevance in search dialog.
/// </summary>
static class NavigationHistory
{
        
    /// <summary>
    /// abC   select D -> append...
    /// abcD  select C -> keep order if new selection is next to current index 
    /// abCd  select A -> resort
    /// bcdA
    /// 
    /// </summary>
    public static void UpdateSelectedInstance(Instance instance)
    {
        var path = OperatorUtils.BuildIdPathForInstance(instance);
        var hash = GetHashForIdPath(path);

        if (!_idPathsByHash.TryGetValue(hash, out var previousPath))
        {
            _idPathsByHash[hash] = path;
            _previousSelections.Insert(0, path);
            _currentIndex = 0;
            return;
        }

        var index = _previousSelections.IndexOf(previousPath);
        if (index == -1)
        {
            Log.Warning("Inconsistent navigation path");
            return;
            
        }

        if (index == 0)
        {
            return;
        }
            

        // Keep order
        if (Math.Abs(index - _currentIndex) == 1)
        {
            _currentIndex = index;
            return;
        }
        
        // Rearrange
        _previousSelections.RemoveAt(index);
        _previousSelections.Insert(0, previousPath);
        _currentIndex = 0;
    }

    public static IEnumerable<Instance> GetPreviouslySelectedInstances()
    {
        foreach (var path in _previousSelections)
        {
            var instance = Structure.GetInstanceFromIdPath(path);
            if (instance == null)
                continue;

            yield return instance;
        }
    }


    private static Instance GetPreviousNavigationInstance()
    {
        return null;
    }

    private static Instance GetNextNavigationInstance()
    {
        return null;
    }

    private static long GetHashForIdPath(List<Guid> path)
    {
        long hash = 31.GetHashCode();
        foreach (var id in path)
        {
            hash = hash * 31 + id.GetHashCode();
        }

        return hash;
    }

    private static int _currentIndex;
    
    private static readonly List<List<Guid>> _previousSelections = new();
    private static readonly Dictionary<long, List<Guid>> _idPathsByHash = new();


}