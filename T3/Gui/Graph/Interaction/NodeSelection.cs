﻿using System;
using System.Collections.Generic;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Gui;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.TransformGizmos;
using T3.Gui.Selection;

namespace T3.Gui.Graph.Interaction
{
    /// <summary>
    /// Some notes on selection handling:
    /// 
    /// - We can't store references to instances because this leads to null-references and reference leaks. For this
    ///   reason we save id-paths: Basically the nesting path of instances from the dashboard operator down.
    ///
    /// - Frequently we want to select the parent operator when clicking on the background of a composition (e.g. to
    ///   to show it's parameters.  
    /// </summary>
    public static class NodeSelection
    {
        public static void Clear()
        {
            TransformGizmoHandling.ClearSelectedTransformables();
            Selection.Clear();
        }

        /// <summary>
        /// This called when clicking on background
        /// </summary>
        public static void SetSelectionToParent(Instance instance)
        {
            Clear();
            _childUiInstanceIdPaths.Clear();
            _selectedComposition = instance;
        }

        public static void SetSelection(ISelectableCanvasObject node)
        {
            if (node is SymbolChildUi)
            {
                Log.Warning("Setting selection to a SymbolChildUi without providing instance will lead to problems.");
            }

            Clear();
            AddSelection(node);
        }

        public static void AddSelection(ISelectableCanvasObject node)
        {
            _selectedComposition = null;
            if (Selection.Contains(node))
                return;

            Selection.Add(node);
        }
        
        public static void DeselectNode(ISelectableCanvasObject node)
        {
            _selectedComposition = null;
            var index = Selection.IndexOf(node);
            if(index != -1)
                Selection.RemoveAt(index);
        }

        /// <summary>
        /// Replaces current selection with symbol child
        /// </summary>
        public static void SetSelectionToChildUi(SymbolChildUi node, Instance instance)
        {
            Clear();
            AddSymbolChildToSelection(node, instance);
        }

        public static void SelectCompositionChild(Instance compositionOp, Guid id, bool replaceSelection = true)
        {
            if (!NodeOperations.TryGetUiAndInstanceInComposition(id, compositionOp, out var childUi, out var instance))
                return;
            
            if (replaceSelection)
            {
                Clear();
            }

            AddSymbolChildToSelection(childUi, instance);
        }

        public static void AddSymbolChildToSelection(SymbolChildUi childUi, Instance instance)
        {
            _selectedComposition = null;
            if (Selection.Contains(childUi))
                return;

            Selection.Add(childUi);
            if (instance != null)
            {
                _childUiInstanceIdPaths[childUi] = NodeOperations.BuildIdPathForInstance(instance);
                if (instance is ITransformable transformable)
                {
                    TransformGizmoHandling.RegisterSelectedTransformable(childUi, transformable);
                }
            }
        }

        public static IEnumerable<T> GetSelectedNodes<T>() where T : ISelectableCanvasObject
        {
            foreach (var item in Selection)
            {
                if (item is T typedItem)
                    yield return typedItem;
            }
        }
        
        public static bool IsNodeSelected(ISelectableCanvasObject node)
        {
            return Selection.Contains(node);
        }

        public static bool IsAnythingSelected()
        {
            return Selection.Count > 0;
        }

        /// <summary>
        /// Returns null if more than onl
        /// </summary>
        public static Instance GetSelectedInstance()
        {
            if (Selection.Count != 1)
                return null;

            return GetFirstSelectedInstance();
        }
        
        public static Instance GetFirstSelectedInstance()
        {
            if (Selection.Count == 0)
                return _selectedComposition;

            if (Selection[0] is SymbolChildUi firstNode)
            {
                if (!_childUiInstanceIdPaths.ContainsKey(firstNode))
                {
                    Log.Error("Failed to access id-path of selected childUi " + firstNode.SymbolChild.Name);
                    Clear();
                    return null;
                }

                var idPath = _childUiInstanceIdPaths[firstNode];
                return NodeOperations.GetInstanceFromIdPath(idPath);
            }

            return null;
        }

        public static IEnumerable<Instance> GetSelectedInstances()
        {
            foreach (var s in Selection)
            {
                if (s is not SymbolChildUi symbolChildUi)
                    continue;
                
                if (!_childUiInstanceIdPaths.ContainsKey(symbolChildUi))
                {
                    Log.Error("Failed to access id-path of selected childUi " + symbolChildUi.SymbolChild.Name);
                    Clear();
                    break;
                }

                var idPath = _childUiInstanceIdPaths[symbolChildUi];
                yield return NodeOperations.GetInstanceFromIdPath(idPath);

            }
        }

        
        public static Instance GetSelectedComposition()
        {
            return _selectedComposition;
        }

        public static Instance GetCompositionForSelection()
        {
            if (Selection.Count == 0)
                return _selectedComposition;

            if (!(Selection[0] is SymbolChildUi firstNode))
                return null;

            var idPath = _childUiInstanceIdPaths[firstNode];
            var instanceFromIdPath = NodeOperations.GetInstanceFromIdPath(idPath);
            return instanceFromIdPath?.Parent;
        }

        public static IEnumerable<SymbolChildUi> GetSelectedChildUis()
        {
            var result = new List<SymbolChildUi>();
            foreach (var s in Selection)
            {
                if (!(s is SymbolChildUi symbolChildUi))
                    continue;

                result.Add(symbolChildUi);
            }

            return result;
        }

        public static void DeselectCompositionChild(Instance compositionOp, Guid symbolChildId)
        {
            if (!NodeOperations.TryGetUiAndInstanceInComposition(symbolChildId, compositionOp, out var childUi, out var instance))
            {
                Log.Debug($"{nameof(NodeSelection)}: Could not find Ui and Instance in {compositionOp.GetType()}: {symbolChildId}");
                return;
            }

            Selection.Remove(childUi);
            if (instance is ITransformable transformable)
            {
                TransformGizmoHandling.ClearDeselectedTransformableNode(transformable);
            }
        }

        public static void DeselectNode(ISelectableCanvasObject node, Instance instance)
        {
            Selection.Remove(node);
            if (instance is ITransformable transformable)
            {
                TransformGizmoHandling.ClearDeselectedTransformableNode(transformable);
            }
        }

        public static Instance GetInstanceForSymbolChildUi(SymbolChildUi symbolChildUi)
        {
            var idPath = _childUiInstanceIdPaths[symbolChildUi];
            return (NodeOperations.GetInstanceFromIdPath(idPath));
        }

        public static readonly List<ISelectableCanvasObject> Selection = new();
        private static Instance _selectedComposition;
        private static readonly Dictionary<SymbolChildUi, List<Guid>> _childUiInstanceIdPaths = new();
    }
}