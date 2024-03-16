﻿using System;

namespace T3.Core.IO
{
    /// <summary>
    /// Saves view layout and currently open node 
    /// </summary>
    public class ProjectSettings : Settings<ProjectSettings.ConfigData>
    {
        public ProjectSettings(bool saveOnQuit) : base("projectSettings.json", saveOnQuit)
        {
        }
        
        public class ConfigData
        {
            public bool TimeClipSuspending = true;
            public float AudioResyncThreshold = 0.04f;
            public bool EnablePlaybackControlWithKeyboard = true;
            
            public string LimitMidiDeviceCapture = null; 
            public bool EnableMidiSnapshotIndication = false;
            public WindowMode DefaultWindowMode = WindowMode.Fullscreen;
        }
    }

    [Serializable]
    public record ExportSettings(Guid OperatorId, string ApplicationTitle, WindowMode WindowMode, ProjectSettings.ConfigData ConfigData, string Author);
    
    public enum WindowMode { Windowed, Fullscreen }
}