using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Operators.Utils;
using T3.Core.IO;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Graph.Interaction;
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

        protected override void DrawContent()
        {
            
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha,1);
            var changed = false;
            ImGui.NewLine();
            if (ImGui.TreeNode("User Interface"))
            {
                FormInputs.AddVerticalSpace();
                FormInputs.SetIndent(20);
                changed |= FormInputs.AddCheckBox("Suspend invalidation of inactive time clips",
                                                  ref ProjectSettings.Config.TimeClipSuspending,
                                                  "An experimental optimization that avoids dirty flag evaluation of graph behind inactive TimeClips. This is only relevant for very complex projects and multiple parts separated by timelines.",
                                                  ProjectSettings.Defaults.TimeClipSuspending);
                
                
                changed |= FormInputs.AddCheckBox("Warn before Lib modifications",
                                                       ref UserSettings.Config.WarnBeforeLibEdit,
                                                       "This warning pops up when you attempt to enter an Operator that ships with the application.\n" +
                                                       "If unsure, this is best left checked.",
                                                   UserSettings.Defaults.WarnBeforeLibEdit);
                
                changed |= FormInputs.AddCheckBox("Use arc connections",
                                                                  ref UserSettings.Config.UseArcConnections,
                                                                  "Affects the shape of the connections between your Operators",
                                                                  UserSettings.Defaults.UseArcConnections);
                
                changed |= FormInputs.AddCheckBox("Show Graph thumbnails",
                                                                  ref UserSettings.Config.ShowThumbnails,
                                                   null,
                                                                  UserSettings.Defaults.ShowThumbnails);

                changed |= FormInputs.AddCheckBox("Drag snapped nodes",
                                                                  ref UserSettings.Config.SmartGroupDragging, 
                                                                  "An experimental features that will drag neighbouring snapped operators.",
                                                                  UserSettings.Defaults.SmartGroupDragging);
                
                changed |= FormInputs.AddCheckBox("Mousewheel adjust flight speed",
                                                  ref UserSettings.Config.AdjustCameraSpeedWithMouseWheel,
                                                  "If enabled, scrolling the mouse wheel while holding left of right mouse button will control navigation speed with WASD keys. This is similar to Unity and Unreal.",
                                                  UserSettings.Defaults.AdjustCameraSpeedWithMouseWheel);
                
                changed |= FormInputs.AddCheckBox("Editing values with mousewheel needs CTRL key",
                                                  ref UserSettings.Config.MouseWheelEditsNeedCtrlKey,
                                                  "In parameter window you can edit numeric values by using the mouse wheel. This setting will prevent accidental modifications while scrolling because by using ctrl key for activation.",
                                                  UserSettings.Defaults.MouseWheelEditsNeedCtrlKey);

                changed |= FormInputs.AddCheckBox("Lock Output canvas",
                                                  ref UserSettings.Config.LockOutputCanvas,
                                                  "In output window you can scroll and scale the canvas. This setting will disable this feature.",
                                                  UserSettings.Defaults.LockOutputCanvas);

                FormInputs.ResetIndent();
                FormInputs.AddVerticalSpace();
                //ImGui.Dummy(new Vector2(20,20));
                changed |= FormInputs.AddEnumDropdown(ref UserSettings.Config.ValueEditGizmo,
                                                      "Value edit control", 
                                                      "This controls the gizmo that pops up when dragging an float value"
                                                     );
    

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
                                                    "Controls the distance until items like keyframes snap in the timeline.",
                                                    UserSettings.Defaults.SnapStrength);
                
                changed |= FormInputs.AddFloat("Click threshold",
                                                               ref UserSettings.Config.ClickThreshold,
                                                               0.0f, 10f, 0.1f, true, 
                                                               "The threshold in pixels until a click becomes a drag. Adjusting this might be useful for stylus input.",
                                                               UserSettings.Defaults.ClickThreshold);
                
                changed |= FormInputs.AddFloat("Timeline Raster Density",
                                                               ref UserSettings.Config.TimeRasterDensity,
                                                               0.0f, 10f, 0.01f, true, 
                                                               "The threshold in pixels until a click becomes a drag. Adjusting this might be useful for stylus input.",
                                                               UserSettings.Defaults.TimeRasterDensity);
                
                changed |= FormInputs.AddEnumDropdown(ref UserSettings.Config.FrameStepAmount,
                                                      "Frame step amount",
                                                      "Controls the next rounding and step amount when jumping between frames.\nDefault shortcut is Shift+Cursor Left/Right");
                
                ImGui.Dummy(new Vector2(20,20));
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Color Theme"))
            {
                ColorThemeEditor.DrawEditor();
                // ColorVariations.DrawSettingsUi();
                ImGui.Dummy(new Vector2(20,20));
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Project settings"))
            {
                var projectSettingsChanged = false;
                CustomComponents.HelpText("These settings only when playback as executable");

                projectSettingsChanged |= FormInputs.AddCheckBox("Enable Playback Control",
                                                                 ref ProjectSettings.Config.EnablePlaybackControlWithKeyboard,
                                                                 "Users can use cursor left/right to skip through time\nand space key to pause playback\nof exported executable.",
                                                                 ProjectSettings.Defaults.EnablePlaybackControlWithKeyboard);
                projectSettingsChanged |= FormInputs.AddCheckBox("Windowed player",
                                                                 ref ProjectSettings.Config.WindowedMode,
                                                                 "Run the executable in windowed mode by default.",
                                                                 ProjectSettings.Defaults.WindowedMode);
                if(projectSettingsChanged)
                    ProjectSettings.Save();
                
                ImGui.TreePop();
            }
            
            if (ImGui.TreeNode("Midi"))
            {
                CustomComponents.HelpText("Only listen to the following Midi devices...\n(This can be useful it avoid capturing devices required by other applications)");
                
                var limitMidiDevices = string.IsNullOrEmpty(ProjectSettings.Config.LimitMidiDeviceCapture) ? string.Empty : ProjectSettings.Config.LimitMidiDeviceCapture;

                if (ImGui.InputTextMultiline("Limit MidiDevices", ref limitMidiDevices, 2000, new Vector2(-1, 100)))
                {
                    changed = true;
                    ProjectSettings.Config.LimitMidiDeviceCapture = string.IsNullOrEmpty(limitMidiDevices) ? null : limitMidiDevices;
                    MidiInConnectionManager.Rescan();
                }
                
                ImGui.Dummy(new Vector2(20,20));
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Space Mouse"))
            {
                CustomComponents.HelpText("These settings only apply with a connected space mouse controller");
                
                changed |= FormInputs.AddFloat("Smoothing",
                                                               ref UserSettings.Config.SpaceMouseDamping,
                                                               0.0f, 10f, 0.01f, true);                

                changed |= FormInputs.AddFloat("Move Speed",
                                                               ref UserSettings.Config.SpaceMouseMoveSpeedFactor,
                                                               0.0f, 10f, 0.01f, true);                

                changed |= FormInputs.AddFloat("Rotation Speed",
                                                               ref UserSettings.Config.SpaceMouseRotationSpeedFactor,
                                                               0.0f, 10f, 0.01f, true);                
                
                ImGui.Dummy(new Vector2(20,20));
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Additional Settings"))
            {
                changed |= FormInputs.AddFloat("Gizmo size",
                                                               ref UserSettings.Config.GizmoSize,
                                                               0.0f, 10f, 0.01f, true);                        
                
                
                ImGui.TreePop();
            }
            if (changed)
                UserSettings.Save();
            
            if (ImGui.TreeNode("Debug information"))
            {
                if (ImGui.TreeNode("Undo history"))
                {
                    int index = 0;
                    foreach (var c in UndoRedoStack.UndoStack)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f/(index+1) + 0.5f));
                        ImGui.PushFont(index == 0 ? Fonts.FontBold : Fonts.FontNormal);
                        if (c is MacroCommand macroCommand)
                        {
                            ImGui.Selectable($"{c.Name} ({macroCommand.Count})");
                        }
                        else
                        {
                            ImGui.Selectable(c.Name);
                        }
                        ImGui.PopFont();
                        ImGui.PopStyleColor();
                        index++;
                    }

                
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Navigation history"))
                {
                    int index = 0;
                    foreach (var c in NavigationHistory.GetPreviouslySelectedInstances())
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f/(index+1) + 0.5f));
                        ImGui.PushFont(index == 0 ? Fonts.FontBold : Fonts.FontNormal);
                        ImGui.Selectable($" {c})");
                        ImGui.PopFont();
                        ImGui.PopStyleColor();
                        index++;
                    }
                    
                    ImGui.TreePop();

                }

                ImGui.TreePop();
            }
            
            ImGui.PopStyleVar();
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}