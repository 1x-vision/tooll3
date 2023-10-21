﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction.Camera;
using T3.Editor.Gui.Interaction.TransformGizmos;
using T3.Editor.Gui.Styling;
using T3.Operators.Types.Id_746d886c_5ab6_44b1_bb15_f3ce2fadf7e6;

namespace T3.Editor.Gui.Windows.Output
{
    public class CameraSelectionHandling
    {
        public ICamera CameraForRendering { get; private set; }
        public bool BypassCamera { get; private set; }
        
        public bool PreventCameraInteraction { get; private set; }
        
        public bool PreventImageCanvasInteraction { get; private set; }
        
        private enum ControlModes
        {
            /// <summary>
            /// If selected op is Camera, manipulate this.
            /// else
            ///   if playback is running
            ///      move ViewerCamera to first camera in graph  
            ///   else
            ///      manipulate ViewerCamera
            /// </summary>
            SceneViewerFollowing,

            /// <summary>
            /// Always use 3rd person view of ViewerCamera, regardless of selected Operator.
            /// </summary>
            UseViewer,

            /// <summary>
            /// If rendered op is Command-Type and manipulate first found camera in graph
            /// or if rendered Op is ImageType and selected Op is Camera manipulate camera 
            /// </summary>
            AutoUseFirstCam,
            
            /// <summary>
            /// Uses the camera picked from dropdown for manipulation.
            /// If camera wasn't recently updated, fall back to last other mode. 
            /// </summary>
            PickedACamera,
        }
        
        public void Update(Instance drawnInstance, Type drawnType, bool preventInteractions = false)
        {
            _hasAnimationTimeChanged = Math.Abs(_lastUpdateTime - Playback.Current.TimeInBars) > 0.001f;
            _lastUpdateTime = Playback.Current.TimeInBars;
            PreventImageCanvasInteraction = false;
            
            _drawnInstance = drawnInstance;
            if (drawnInstance == null)
            {
                PreventCameraInteraction = true;
                CameraForRendering = _outputWindowViewCamera;
                _recentlyUsedCameras = new List<ICamera>();
                return;
            }

            // Update recently used cameras (this is expensive!)
            UpdateRecentCameras(drawnInstance);

            ICamera cameraForManipulation = null;
            CameraForRendering = null;

            PreventCameraInteraction = preventInteractions;


            if (_controlMode == ControlModes.PickedACamera)
            {
                var isPickStillValid = false;
                foreach (var recentCam in _recentlyUsedCameras)
                {
                    if (recentCam is not Instance camInstance)
                        continue;
                    
                    if (camInstance.SymbolChildId == _pickedCameraId)
                    {
                        cameraForManipulation = recentCam;
                        CameraForRendering = _outputWindowViewCamera;
                        isPickStillValid = true;
                        break;
                    }
                }

                if (!isPickStillValid)
                {
                    _controlMode = _lastControlMode;
                }
            }

            _drawnTypeIsCommand = drawnType == typeof(Command);
            switch (_controlMode)
            {
                case ControlModes.SceneViewerFollowing:
                {
                    if (!_drawnTypeIsCommand)
                    {
                        PreventCameraInteraction = true;
                    }
                    cameraForManipulation = _outputWindowViewCamera;
                    
                    if (_firstCamInGraph != null)
                    {
                        PreventImageCanvasInteraction = true;
                        var isCamOpSelected = NodeSelection.GetFirstSelectedInstance() == _firstCamInGraph;
                        if (isCamOpSelected)
                        {
                            cameraForManipulation = _firstCamInGraph;
                        }
                        
                        if (_hasAnimationTimeChanged)
                        {
                            _outputWindowViewCamera.CameraPosition = _firstCamInGraph.CameraPosition;
                            _outputWindowViewCamera.CameraTarget = _firstCamInGraph.CameraTarget;
                            _outputWindowViewCamera.CameraRoll = _firstCamInGraph.CameraRoll;
                        }
                    }
                    
                    BypassCamera = true;
                    break;
                }
                
                case ControlModes.UseViewer:
                    cameraForManipulation = _outputWindowViewCamera;
                    BypassCamera = true;
                    break;

                case ControlModes.AutoUseFirstCam:
                {
                    if (_firstCamInGraph != null)
                    {
                        var isCamOpSelected = NodeSelection.GetFirstSelectedInstance() == _firstCamInGraph;
                        if (!isCamOpSelected)
                        {
                            cameraForManipulation = _outputWindowViewCamera;
                        }
                        else
                        {
                            PreventImageCanvasInteraction = true;
                            cameraForManipulation = _firstCamInGraph;    
                        }
                        CameraForRendering = _outputWindowViewCamera;
                        _cameraInteraction.ResetCamera(_outputWindowViewCamera);
                    }
                    else
                    {
                        cameraForManipulation = _outputWindowViewCamera;
                    }
                    BypassCamera = false;
                    break;
                }
            }

            if (_controlMode != ControlModes.PickedACamera)
            {
                
                CameraForRendering = cameraForManipulation;
            }
            else
            {
                PreventImageCanvasInteraction = true;
            }

            if (CameraForRendering == null)
            {
                CameraForRendering = _outputWindowViewCamera;
            }

            if (TransformGizmoHandling.IsDragging)
                PreventCameraInteraction = true;
            
            if (!PreventCameraInteraction && cameraForManipulation != null)
            {
                _cameraInteraction.Update(cameraForManipulation, !PreventCameraInteraction);
            }
        }

        private void UpdateRecentCameras(Instance drawnInstance)
        {
            _recentlyUsedCameras.Clear();
            _firstCamInGraph = null;
            
            var parentInstance = drawnInstance.Parent;
            if (parentInstance != null)
            {
                var children = parentInstance.Children;
                foreach (var child in children)
                {
                    if (child is not ICamera cam2)
                        continue;

                    if (child.Outputs[0].DirtyFlag.FramesSinceLastUpdate > 1)
                        continue;

                    if (_firstCamInGraph == null)
                        _firstCamInGraph = cam2;

                    _recentlyUsedCameras.Add(cam2);
                }
            }

            var selectedInstance = NodeSelection.GetSelectedInstance();
            if (selectedInstance is Camera selectedCamera
                && !_recentlyUsedCameras.Contains(selectedCamera))
            {
                if (_recentlyUsedCameras.Count == 0)
                {
                    _firstCamInGraph = selectedCamera;
                }
                _recentlyUsedCameras.Add(selectedCamera);
            }
        }

        public void DrawCameraControlSelection()
        {
            ImGui.SetNextItemWidth(145);

            var label = String.Empty;
            
            switch (_controlMode)
            {
                case ControlModes.AutoUseFirstCam:
                    label = "Auto";
                    break;
                
                case ControlModes.SceneViewerFollowing:
                    label = "Follow";
                    break;
                
                case ControlModes.UseViewer:
                    label = "Viewer";
                    break;
                
                case ControlModes.PickedACamera:
                    label = "Locked to Op";
                    break;
            }

            var isAuto = _controlMode == ControlModes.AutoUseFirstCam;

            
            //var min = ImGui.GetCursorScreenPos();
            // var isOpen = ImGui.BeginCombo("##CameraSelection", label);

            var width = ImGui.GetFrameHeight() * 5;
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, Vector2.Zero);
            var labelColor = isAuto ? UiColors.TextMuted : UiColors.ForegroundFull;
            ImGui.PushStyleColor(ImGuiCol.Text, labelColor.Rgba);
            var isClicked = ImGui.Button($"     {label}##CameraMode", new Vector2(width, ImGui.GetFrameHeight()));
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            if (isClicked)
            {
                ImGui.OpenPopup("cameraPopup");
                ImGui.SetNextWindowPos(new Vector2(min.X,max.Y));
                
            }
            Icons.DrawIconAtScreenPosition(Icon.Camera,min + new Vector2(4,7), ImGui.GetWindowDrawList(), labelColor);
            
            //ImGui.SetNextWindowSize(new Vector2(width,-1));
            if(ImGui.BeginPopup("cameraPopup", ImGuiWindowFlags.None))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6) * T3Ui.UiScaleFactor);
                if (ImGui.MenuItem("Auto", "", _controlMode == ControlModes.AutoUseFirstCam, true ))
                {
                    _controlMode = ControlModes.AutoUseFirstCam;
                    _pickedCameraId = Guid.Empty;
                }
                CustomComponents.TooltipForLastItem("Shows the first scene camera. For manipulation select the camera operator in graph or in the list below.");
                if (ImGui.IsItemHovered() && _firstCamInGraph is Instance camInstance)
                    FrameStats.AddHoveredId(camInstance.SymbolChildId);

                if (ImGui.MenuItem("Viewer",
                                   "", 
                                   _controlMode == ControlModes.UseViewer, 
                                   _drawnTypeIsCommand ))
                {
                    _controlMode = ControlModes.UseViewer;
                    _pickedCameraId = Guid.Empty;
                }
                CustomComponents.TooltipForLastItem("Ignores scene cameras. This can be useful in combination with [ShowCamGizmos].");
                
                if (ImGui.MenuItem("Viewer (Following)",
                                   "", 
                                   _controlMode == ControlModes.SceneViewerFollowing,
                                   _drawnTypeIsCommand
                                   ))
                {
                    _controlMode = ControlModes.SceneViewerFollowing;
                    _pickedCameraId = Guid.Empty;
                }
                if (ImGui.IsItemHovered() && _firstCamInGraph is Instance camInstance2)
                    FrameStats.AddHoveredId(camInstance2.SymbolChildId);
                
                CustomComponents.TooltipForLastItem("During playback the scene viewer is following the scene camera. Otherwise it can be independently manipulated without affecting the scene camera.");

                if (_recentlyUsedCameras.Count > 0)
                {
                    ImGui.Separator();
                    CustomComponents.HintLabel("Active Cameras...");
                }
                
                foreach (var cam in _recentlyUsedCameras)
                {
                    if (cam is not Instance cameraInstance)
                        continue;
                    
                    ImGui.PushID(cameraInstance.SymbolChildId.GetHashCode());
                    {
                        // This is expensive, but happens only if dropdown is open...
                        var symbolChild = SymbolRegistry.Entries[_drawnInstance.Parent.Symbol.Id].Children
                                                        .SingleOrDefault(child => child.Id == cameraInstance.SymbolChildId);

                        if (symbolChild == null)
                            continue;
                        
                        if(ImGui.MenuItem(symbolChild.ReadableName, "",cameraInstance.SymbolChildId == _pickedCameraId, true))
                        {
                            _lastControlMode = _controlMode;
                            _controlMode = ControlModes.PickedACamera;
                            _pickedCameraId = cameraInstance.SymbolChildId;
                            T3Ui.SelectAndCenterChildIdInView(symbolChild.Id);
                        }

                        if (ImGui.IsItemHovered())
                        {
                            FrameStats.AddHoveredId(cameraInstance.SymbolChildId);
                        }
                    }
                    ImGui.PopID();
                }

                ImGui.PopStyleVar();
                ImGui.EndPopup();
            }
            else
            {
                CustomComponents.TooltipForLastItem("Camera control mode", "This affects which camera will be manipulated by the view controls. Please also review to the tooltips of the dropdown options.");

            }

            ImGui.SameLine();
        }

        public void ResetView()
        {
            _cameraInteraction.ResetView();
        }

        private bool _drawnTypeIsCommand;
        private ControlModes _controlMode = ControlModes.AutoUseFirstCam;
        private readonly ViewCamera _outputWindowViewCamera = new();
        private readonly CameraInteraction _cameraInteraction = new();

        private double _lastUpdateTime;
        private bool _hasAnimationTimeChanged;

        private Guid _pickedCameraId = Guid.Empty;
        
        private Instance _drawnInstance;
        private List<ICamera> _recentlyUsedCameras = new();

        private ControlModes _lastControlMode;

        private ICamera _firstCamInGraph;
    }
}