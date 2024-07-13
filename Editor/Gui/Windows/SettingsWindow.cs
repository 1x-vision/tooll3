﻿using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Operators.Utils;
using T3.Core.IO;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Interaction.Midi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows
{
    public class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            Config.Title = "Settings";
        }

        private enum Categories
        {
            Interface,
            Theme,
            Project,
            Midi,
            OSC,
            SpaceMouse,
            Keyboard,
            Profiling,
        }

        private Categories _activeCategory;

        protected override void DrawContent()
        {
            var changed = false;

            ImGui.BeginChild("categories", new Vector2(120 * T3Ui.UiScaleFactor, -1), true, ImGuiWindowFlags.NoScrollbar);
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
                FormInputs.AddSegmentedButtonWithLabel(ref _activeCategory, "", 110 * T3Ui.UiScaleFactor);
                ImGui.PopStyleVar();
            }
            ImGui.EndChild();

            ImGui.SameLine();
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(20, 5));
            ImGui.BeginChild("content", new Vector2(-1, -1), true);
            {
                FormInputs.SetIndentToParameters();
                switch (_activeCategory)
                {
                    case Categories.Interface:
                        FormInputs.SetIndentToLeft();

                        FormInputs.AddSectionHeader("User Interface");
                        FormInputs.AddVerticalSpace();
                        changed |= FormInputs.AddCheckBox("Editing values with mousewheel needs CTRL key",
                                                          ref UserSettings.Config.MouseWheelEditsNeedCtrlKey,
                                                          "In parameter window you can edit numeric values by using the mouse wheel. This setting will prevent accidental modifications while scrolling because by using ctrl key for activation.",
                                                          UserSettings.Defaults.MouseWheelEditsNeedCtrlKey);

                        changed |= FormInputs.AddCheckBox("Mousewheel adjust flight speed",
                                                          ref UserSettings.Config.AdjustCameraSpeedWithMouseWheel,
                                                          "If enabled, scrolling the mouse wheel while holding left of right mouse button will control navigation speed with WASD keys. This is similar to Unity and Unreal",
                                                          UserSettings.Defaults.AdjustCameraSpeedWithMouseWheel);

                        changed |= FormInputs.AddCheckBox("Use arc connections",
                                                          ref UserSettings.Config.UseArcConnections,
                                                          "Affects the shape of the connections between your operators",
                                                          UserSettings.Defaults.UseArcConnections);

                        changed |= FormInputs.AddCheckBox("Drag snapped nodes",
                                                          ref UserSettings.Config.SmartGroupDragging,
                                                          "An experimental features that will drag neighbouring snapped operators",
                                                          UserSettings.Defaults.SmartGroupDragging);

                        FormInputs.AddVerticalSpace();
                        FormInputs.SetIndentToParameters();
                        changed |= FormInputs.AddFloat("UI Scale",
                                                       ref UserSettings.Config.UiScaleFactor,
                                                       0.1f, 5f, 0.01f, true,
                                                       "The global scale of all rendered UI in the application",
                                                       UserSettings.Defaults.UiScaleFactor);

                        changed |= FormInputs.AddFloat("Scroll smoothing",
                                                       ref UserSettings.Config.ScrollSmoothing,
                                                       0.0f, 0.2f, 0.01f, true,
                                                       null,
                                                       UserSettings.Defaults.ScrollSmoothing);

                        changed |= FormInputs.AddFloat("Snap strength",
                                                       ref UserSettings.Config.SnapStrength,
                                                       0.0f, 0.2f, 0.01f, true,
                                                       "Controls the distance until items such as keyframes snap in the timeline",
                                                       UserSettings.Defaults.SnapStrength);

                        changed |= FormInputs.AddFloat("Click threshold",
                                                       ref UserSettings.Config.ClickThreshold,
                                                       0.0f, 10f, 0.1f, true,
                                                       "The threshold in pixels until a click becomes a drag. Adjusting this might be useful for stylus input",
                                                       UserSettings.Defaults.ClickThreshold);

                        changed |= FormInputs.AddFloat("Timeline marks density",
                                                       ref UserSettings.Config.TimeRasterDensity,
                                                       0.0f, 10f, 0.01f, true,
                                                       "Density/opacity of the marks (time or beat) at the bottom of the timeline",
                                                       UserSettings.Defaults.TimeRasterDensity);

                        changed |= FormInputs.AddFloat("Gizmo size",
                                                       ref UserSettings.Config.GizmoSize,
                                                       0.0f, 10f, 0.01f, true);

                        changed |= FormInputs.AddEnumDropdown(ref UserSettings.Config.FrameStepAmount,
                                                              "Frame step amount",
                                                              "Controls the next rounding and step amount when jumping between frames.\nDefault shortcut is Shift+Cursor Left/Right");

                        changed |= FormInputs.AddEnumDropdown(ref UserSettings.Config.ValueEditMethod,
                                                              "Value input method",
                                                              "The control that pops up when dragging on a number value"
                                                             );
                        
                        


                        FormInputs.SetIndentToLeft();
                        FormInputs.AddVerticalSpace();
                        FormInputs.AddSectionHeader("Previews");
                        FormInputs.AddVerticalSpace();
                        changed |= FormInputs.AddCheckBox("Show Graph thumbnails",
                                                          ref UserSettings.Config.ShowThumbnails, null,
                                                          UserSettings.Defaults.ShowThumbnails);

                        changed |= FormInputs.AddCheckBox("Show nodes thumbnails when hovering",
                                                          ref UserSettings.Config.EditorHoverPreview, null,
                                                          UserSettings.Defaults.EditorHoverPreview);

                        FormInputs.AddVerticalSpace();
                        FormInputs.AddSectionHeader("Advanced");
                        FormInputs.AddVerticalSpace();
                        changed |= FormInputs.AddCheckBox("Middle mouse button zooms canvas",
                                                          ref UserSettings.Config.MiddleMouseButtonZooms,
                                                          "This can be useful if you're working with tablets or other input devices that lack a mouse wheel.",
                                                          UserSettings.Defaults.MiddleMouseButtonZooms);
                        changed |= FormInputs.AddCheckBox("Reset time after playback",
                                                          ref UserSettings.Config.ResetTimeAfterPlayback,
                                                          "After the playback is halted, the time will reset to the moment when the playback began. This feature proves beneficial for iteratively reviewing animations without requiring manual rewinding.",
                                                          UserSettings.Defaults.ResetTimeAfterPlayback);
                        changed |= FormInputs.AddCheckBox("Suspend invalidation of inactive time clips",
                                                          ref ProjectSettings.Config.TimeClipSuspending,
                                                          "An experimental optimization that avoids dirty flag evaluation of graph behind inactive TimeClips. This is only relevant for very complex projects and multiple parts separated by timelines.",
                                                          ProjectSettings.Defaults.TimeClipSuspending);

                        changed |= FormInputs.AddCheckBox("Warn before Lib modifications",
                                                          ref UserSettings.Config.WarnBeforeLibEdit,
                                                          "This warning pops up when you attempt to enter an Operator that ships with the application.\n" +
                                                          "If unsure, this is best left checked.",
                                                          UserSettings.Defaults.WarnBeforeLibEdit);

                        changed |= FormInputs.AddCheckBox("Suspend rendering when hidden",
                                                          ref UserSettings.Config.SuspendRenderingWhenHidden,
                                                          "Suspend rendering and update when Tooll's editor window is minimized. This will reduce energy consumption significantly.",
                                                          UserSettings.Defaults.SuspendRenderingWhenHidden);

                        break;
                    case Categories.Theme:
                        FormInputs.AddSectionHeader("Color Theme");
                        FormInputs.AddVerticalSpace();

                        ColorThemeEditor.DrawEditor();
                        break;
                    case Categories.Project:
                    {
                        FormInputs.AddSectionHeader("Project specific settings");

                        var projectSettingsChanged = false;
                        CustomComponents.HelpText("These settings only when playback as executable");
                        FormInputs.AddVerticalSpace();

                        FormInputs.SetIndentToLeft();

                        projectSettingsChanged |= FormInputs.AddCheckBox("Enable Playback Control",
                                                                         ref ProjectSettings.Config.EnablePlaybackControlWithKeyboard,
                                                                         "Users can use cursor left/right to skip through time\nand space key to pause playback\nof exported executable.",
                                                                         ProjectSettings.Defaults.EnablePlaybackControlWithKeyboard);
                        projectSettingsChanged |= FormInputs.AddCheckBox("Windowed player",
                                                                         ref ProjectSettings.Config.WindowedMode,
                                                                         "Run the executable in windowed mode by default.",
                                                                         ProjectSettings.Defaults.WindowedMode);
                        if (projectSettingsChanged)
                            ProjectSettings.Save();

                        FormInputs.SetIndentToParameters();

                        break;
                    }
                    case Categories.Midi:
                    {
                        FormInputs.AddSectionHeader("Midi");

                        if (ImGui.Button("Rescan devices"))
                        {
                            MidiConnectionManager.Rescan();
                            //MidiOutConnectionManager.Init();
                            CompatibleMidiDeviceHandling.InitializeConnectedDevices();
                        }

                        {
                            FormInputs.AddVerticalSpace();
                            ImGui.TextUnformatted("Limit captured MIDI devices...");
                            CustomComponents
                               .HelpText("This can be useful it avoid capturing devices required by other applications.\nEnter one search string per line...");

                            var limitMidiDevices = string.IsNullOrEmpty(ProjectSettings.Config.LimitMidiDeviceCapture)
                                                       ? string.Empty
                                                       : ProjectSettings.Config.LimitMidiDeviceCapture;

                            if (ImGui.InputTextMultiline("##Limit MidiDevices", ref limitMidiDevices, 2000, new Vector2(-1, 100)))
                            {
                                changed = true;
                                ProjectSettings.Config.LimitMidiDeviceCapture = string.IsNullOrEmpty(limitMidiDevices) ? null : limitMidiDevices;
                                MidiConnectionManager.Rescan();
                            }

                            FormInputs.AddVerticalSpace();
                        }

                        FormInputs.AddVerticalSpace();
                        break;
                    }
                    case Categories.OSC:
                    {
                        FormInputs.AddSectionHeader("OSC");
                        
                        CustomComponents
                           .HelpText("On startup, Tooll will listen for OSC messages on the default port." +
                                     "The IO indicator in the timeline will show incoming messages.\n" +
                                     "You can also use the OscInput operator to receive OSC from other ports.");
                            
                        CustomComponents
                           .HelpText("Changing the port will require a restart of Tooll.");
                        
                        FormInputs.AddInt("Default Port", ref ProjectSettings.Config.DefaultOscPort,
                                          0, 65535, 1,
                                          "If a valid port is set, Tooll will listen for OSC messages on this port by default.",
                                          -1);
                        
                        FormInputs.AddVerticalSpace();
                        break;
                    }

                    case Categories.SpaceMouse:
                        FormInputs.AddSectionHeader("Space Mouse");

                        CustomComponents.HelpText("These settings only apply with a connected space mouse controller");
                        FormInputs.AddVerticalSpace();

                        changed |= FormInputs.AddFloat("Smoothing",
                                                       ref UserSettings.Config.SpaceMouseDamping,
                                                       0.0f, 10f, 0.01f, true);

                        changed |= FormInputs.AddFloat("Move Speed",
                                                       ref UserSettings.Config.SpaceMouseMoveSpeedFactor,
                                                       0.0f, 10f, 0.01f, true);

                        changed |= FormInputs.AddFloat("Rotation Speed",
                                                       ref UserSettings.Config.SpaceMouseRotationSpeedFactor,
                                                       0.0f, 10f, 0.01f, true);
                        break;

                    case Categories.Keyboard:
                        FormInputs.AddSectionHeader("Keyboard Shortcuts");
                        CustomComponents.HelpText("The keyboard layout can't be edited yet.");

                        if (ImGui.BeginTable("Shortcuts", 2,
                                             ImGuiTableFlags.BordersInnerH))
                        {
                            foreach (var value in Enum.GetValues<UserActions>())
                            {
                                ImGui.TableNextRow();
                                ImGui.TableSetColumnIndex(0);
                                var actionName = CustomComponents.HumanReadablePascalCase(Enum.GetName(value));
                                var shortcuts = KeyboardBinding.ListKeyboardShortcuts(value, false);
                                var hasShortcut = !string.IsNullOrEmpty(shortcuts);
                                ImGui.PushStyleColor(ImGuiCol.Text, hasShortcut ? UiColors.Text : UiColors.TextMuted.Rgba);
                                ImGui.TextUnformatted(actionName);

                                ImGui.TableSetColumnIndex(1);

                                if (hasShortcut)
                                {
                                    ImGui.PushFont(Fonts.FontBold);
                                    ImGui.TextUnformatted(shortcuts);
                                    ImGui.PopFont();
                                    ImGui.PopStyleColor();
                                }
                            }

                            ImGui.EndTable();
                        }
                        break;
                    case Categories.Profiling:
                    {
                        FormInputs.AddSectionHeader("Profiling and debugging");

                        CustomComponents.HelpText("Enabling this will add slight performance overhead.\nChanges will require a restart of Tooll.");
                        FormInputs.AddVerticalSpace();

                        FormInputs.SetIndentToLeft();
                        
                        changed |= FormInputs.AddCheckBox("Enable Frame Profiling",
                                                                         ref UserSettings.Config.EnableFrameProfiling,
                                                                         "A basic frame profile for the duration of frame processing. Overhead is minimal.",
                                                                         UserSettings.Defaults.EnableFrameProfiling);
                        changed |= FormInputs.AddCheckBox("Keep Log Messages",
                                                          ref UserSettings.Config.KeepTraceForLogMessages,
                                                          "Store log messages in the profiling data. This can be useful to see correlation between frame drops and log messages.",
                                                          UserSettings.Defaults.KeepTraceForLogMessages);
                        
                        changed |= FormInputs.AddCheckBox("Log GC Profiling",
                                                          ref UserSettings.Config.EnableGCProfiling,
                                                          "Log garbage collection information. This can be useful to see correlation between frame drops and GC activity.",
                                                          UserSettings.Defaults.EnableGCProfiling);

                        FormInputs.SetIndentToParameters();
                        break;
                    }
                }

                if (changed)
                    UserSettings.Save();
            }
            ImGui.EndChild();
            ImGui.PopStyleVar();
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}