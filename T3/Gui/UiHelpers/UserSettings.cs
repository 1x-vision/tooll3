﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.IO;
using T3.Gui.Graph;
using T3.Gui.Interaction;
using T3.Gui.Windows;

namespace T3.Gui.UiHelpers
{
    /// <summary>
    /// Saves view layout, currently open node and other user settings 
    /// </summary>
    public class UserSettings : Settings<UserSettings.ConfigData>
    {
        public UserSettings(bool saveOnQuit) : base("userSettings.json", saveOnQuit:saveOnQuit)
        {
        }

        public class ConfigData
        {
            public readonly Dictionary<Guid, ScalableCanvas.Scope> OperatorViewSettings = new Dictionary<Guid, ScalableCanvas.Scope>();
            public readonly Dictionary<string, Guid> LastOpsForWindows = new Dictionary<string, Guid>();

            [JsonConverter(typeof(StringEnumConverter))]
            public GraphCanvas.HoverModes HoverMode = GraphCanvas.HoverModes.Live;

            public bool AudioMuted;
            
            // UI-Elements
            public bool ShowThumbnails = true;
            public bool ShowMainMenu = true;
            public bool ShowTitleAndDescription = true;
            public bool ShowToolbar = true;
            public bool ShowTimeline = true;
            
            // UI-State
            public float UiScaleFactor = 1;
            public bool FullScreen = false;
            public bool ShowGraphOverContent = false;
            public int WindowLayoutIndex = 0;
            public bool EnableIdleMotion = true;
            
            // Interaction
            public bool SmartGroupDragging = false;
            public bool ShowExplicitTextureFormatInOutputWindow = false;
            public bool UseArcConnections = true;
            public float SnapStrength = 5;
            public bool UseJogDialControl = true;
            public float ScrollSmoothing = 0.06f;
            public float TooltipDelay = 1.2f;
            public float ClickThreshold = 5; // Increase for high-res display and pen tablets

            public bool VariationLiveThumbnails = true;
            public bool VariationHoverPreview = true;
            
            // Load Save
            public bool AutoSaveAfterSymbolCreation = true;
            public bool EnableAutoBackup = true;
            public bool SaveOnlyModified = false;
            
            // Other settings
            public float GizmoSize = 100;
            public bool SwapMainAnd2ndWindowsWhenFullscreen = false;
            public bool EnableStartupConsistencyCheck = true;

            // Timeline
            public bool CountBarsFromZero = true;
            public float TimeRasterDensity = 1f;

            // Space mouse
            public float SpaceMouseRotationSpeedFactor = 1f;
            public float SpaceMouseMoveSpeedFactor = 1f;
            public float SpaceMouseDamping = 0.5f;

            [JsonConverter(typeof(StringEnumConverter))]
            public TimeFormat.TimeDisplayModes TimeDisplayMode = TimeFormat.TimeDisplayModes.Bars;
            
            public List<GraphBookmarkNavigation.Bookmark> Bookmarks = new();
            public List<Gradient> GradientPresets = new();
            
        }

        public static Guid GetLastOpenOpForWindow(string windowTitle)
        {
            return Config.LastOpsForWindows.TryGetValue(windowTitle, out var id) ? id : Guid.Empty;
        }

        public static void SaveLastViewedOpForWindow(GraphWindow window, Guid opInstanceId)
        {
            Config.LastOpsForWindows[window.Config.Title] = opInstanceId;
        }
    }
}