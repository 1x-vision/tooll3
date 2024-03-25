﻿using ImGuiNET;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Dialogs
{
    internal class EditCommentDialog : ModalDialog
    {
        public void Draw(NodeSelection selection)
        {
            DialogSize = new Vector2(500, 450);
            
            if (BeginDialog("Edit comment"))
            {
                var instance = selection.GetSelectedInstanceWithoutComposition();

                if (instance?.Parent == null)
                {
                    CustomComponents.EmptyWindowMessage("Please select operator\nto add comment");
                }
                else
                {
                    var symbolUi = instance.Parent.Symbol.GetSymbolUi();
                    var symbolChildUi = symbolUi.ChildUis[instance.SymbolChildId];
                    if (symbolChildUi == null)
                    {
                        CustomComponents.EmptyWindowMessage("Sorry, can't find UI definition for operator.");
                    }
                    else
                    {
                        var comment = symbolChildUi.Comment ?? string.Empty;
                        
                        ImGui.PushFont(Fonts.FontLarge);
                        ImGui.Text(symbolChildUi.SymbolChild.Symbol.Name);
                        ImGui.PopFont();
                        
                        if (ImGui.IsWindowAppearing())
                            ImGui.SetKeyboardFocusHere();

                        if (ImGui.InputTextMultiline("##comment", ref comment, 2000, new Vector2(-1, 300), ImGuiInputTextFlags.None))
                        {
                            symbolChildUi.Comment = comment;
                            symbolUi.FlagAsModified();
                        }
                    }
                }
                if (ImGui.Button("Close"))
                {
                    ImGui.CloseCurrentPopup();
                }
                
                EndDialogContent();
            }
            EndDialog();
        }
    }
}