﻿using ImGuiNET;
using T3.Core.SystemUi;
using T3.Editor.Compilation;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;

namespace T3.Editor.Gui.Graph.Dialogs
{
    internal sealed class NewProjectDialog : ModalDialog
    {
        protected override void OnShowNextFrame()
        {
            _shareResources = true;
            _newName = string.Empty;
            _userName = UserSettings.Config.UserName;
            _newNamespace = _userName;
            _needsAutoFocus = true;
        }
        
        public void Draw()
        {
            DialogSize = new Vector2(500, 300);

            if (BeginDialog("Create new project"))
            {
                // Name and namespace
                string namespaceWarningText = null;
                bool namespaceCorrect = true;
                if (!_newNamespace.StartsWith(_userName) || _newNamespace.Length > _userName.Length && _newNamespace[_userName.Length] != '.')
                {
                    namespaceCorrect = false;
                    namespaceWarningText = $"Namespace must be within the \"{_userName}\" namespace";
                }
                else if(!GraphUtils.IsNamespaceValid(_newNamespace))
                {
                    namespaceCorrect = false;
                    namespaceWarningText = "Namespace must be a valid C# namespace";
                }
                
                FormInputs.AddStringInput("Namespace", ref _newNamespace, 
                                          warning: namespaceWarningText, autoFocus: _needsAutoFocus);
                _needsAutoFocus = false;
                
                var nameCorrect = GraphUtils.IsIdentifierValid(_newName);
                FormInputs.AddStringInput("Name", ref _newName,
                                          warning: !nameCorrect ? "Name must be a valid C# identifier" : null);
                
                FormInputs.AddCheckBox("Share Resources", ref _shareResources, "Enabling this allows anyone with this package to reference shaders, " +
                                                                               "images, and other resources that belong to this package in other projects.\n" +
                                                                               "It is recommended that you leave this option enabled.");

                if (_shareResources == false)
                {
                    ImGui.TextColored(UiColors.StatusWarning, "Warning: there is no way to change this without editing the project code at this time.");
                }
                
                if (CustomComponents.DisablableButton(label: "Create",
                                                      isEnabled: namespaceCorrect && nameCorrect,
                                                      enableTriggerWithReturn: false))
                {
                    if (ProjectSetup.TryCreateProject(_newName, _newNamespace + '.' + _newName, _shareResources, out var project))
                    {
                        T3Ui.Save(false); // todo : this is probably not needed
                        ImGui.CloseCurrentPopup();
                    }
                    else
                    {
                        var message = $"Failed to create project \"{_newName}\" in \"{_newNamespace}\".\n\n" +
                                      "This should never happen - please file a bug report.\n" +
                                      "Currently this error is unhandled, so you will want to manually delete the project from disk.";
                        
                        Log.Error(message);
                        BlockingWindow.Instance.ShowMessageBox(message, "Failed to create new project");
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                EndDialogContent();
            }

            EndDialog();
        }

        private string _newName = string.Empty;
        private string _newNamespace = string.Empty;
        private string _userName = string.Empty;
        private bool _shareResources = true;
        private bool _needsAutoFocus;
    }
}