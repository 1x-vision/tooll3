﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.InputUi.CombinedInputs;

public class SceneSetupInputUi : InputValueUi<SceneSetup>
{
    // public override IInputUi Clone()
    // {
    //     return new SceneSetupInputUi
    //            {
    //                InputDefinition = InputDefinition,
    //                Parent = Parent,
    //                PosOnCanvas = PosOnCanvas,
    //                Relevancy = Relevancy,
    //                Size = Size,
    //            };
    // }

    public override IInputUi Clone()
    {
        return new SceneSetupInputUi();
    }

    protected override InputEditStateFlags DrawEditControl(string name, SymbolChild.Input input, ref SceneSetup value, bool readOnly)
    {
        if (ImGui.Button("Edit Scene Setup"))
        {
            ImGui.OpenPopup(SceneSetupPopup.EditSceneStructureId);
            return InputEditStateFlags.Modified;
        }

        SceneSetupPopup.DrawPopup(value);

        return InputEditStateFlags.Nothing;
        //throw new System.NotImplementedException();
    }

    protected override void DrawReadOnlyControl(string name, ref SceneSetup value)
    {
        ImGui.NewLine();
    }
}

public static class SceneSetupPopup
{
    private static ImDrawListPtr drawList;
    public const string EditSceneStructureId = "Edit Scene Structure";

    public static void DrawPopup(SceneSetup setup)
    {
        if (setup == null)
            return;


        ImGui.SetNextWindowSize(new Vector2(600, 600));
        if (ImGui.BeginPopup(EditSceneStructureId))
        {
            drawList = ImGui.GetWindowDrawList();

            if (setup.RootNodes == null)
            {
                ImGui.TextUnformatted("node structure undefined");
            }
            else
            {
                foreach (var node in setup.RootNodes)
                {
                    DrawNode(node, setup);
                }
            }

            ImGui.EndPopup();
        }
    }
    
    

    private static void DrawNode(SceneSetup.SceneNode node, SceneSetup sceneSetup, bool parentVisible = true)
    {
        var label = string.IsNullOrEmpty(node.Name) ? "???" : node.Name;

        if (sceneSetup.NodeSettings == null)
        {
            sceneSetup.NodeSettings = new List<SceneSetup.NodeSetting>();
        }

        var nodeSettings = sceneSetup.NodeSettings.SingleOrDefault(s => s.NodeHashId == node.Name.GetHashCode());

        if (nodeSettings == null)
        {
        }

        var isNodeVisible = parentVisible && (nodeSettings == null || nodeSettings.Visibility == SceneSetup.NodeSetting.NodeVisibilities.Visible);
        var fade = isNodeVisible ? 1 : 0.5f;
        var icon = isNodeVisible
                       ? Icon.Visible
                       : Icon.Hidden;

        parentVisible &= isNodeVisible;
        
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, fade);

        // ImGui.SetNextItemOpen(true);
        var isOpen = ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.DefaultOpen);
        ImGui.SameLine(1);


        if (CustomComponents.IconButton(icon, new Vector2(16, 16), CustomComponents.ButtonStates.Dimmed))
        {
            if (nodeSettings == null)
            {
                nodeSettings = new SceneSetup.NodeSetting()
                                   {
                                       NodeHashId = label.GetHashCode(),
                                       Visibility = SceneSetup.NodeSetting.NodeVisibilities.HiddenBranch,
                                   };
                sceneSetup.NodeSettings.Add(nodeSettings);
            }
            else
            {
                nodeSettings.Visibility = nodeSettings.Visibility == SceneSetup.NodeSetting.NodeVisibilities.Visible
                                              ? SceneSetup.NodeSetting.NodeVisibilities.HiddenBranch
                                              : SceneSetup.NodeSetting.NodeVisibilities.Visible;
            }
        }

        if (isOpen)
        {
            // Mesh Label
            var meshLabel = string.IsNullOrEmpty(node.MeshName) ? "-" : $"  {node.MeshName} ({node.MeshBuffers.FaceCount.FormatCount()})";
            ImGui.SameLine(200);
            ImGui.TextColored(UiColors.TextMuted, meshLabel);
            
            // Material
            if (node.Material != null)
            {
                ImGui.SameLine(430);
                ImGui.TextColored(UiColors.TextMuted, node.Material.Name);
                
                var h = ImGui.GetFrameHeight();
                var p = ImGui.GetItemRectMin() + new Vector2(-h *0.5f,0.4f *h);
                var r = h * 0.3f;
                
                var baseColor = (Color)(node.Material.PbrParameters.BaseColor);
                drawList.AddCircleFilled(p, r , baseColor.Fade(0.5f));

                var blurSteps = 2;
                var spec = Color.White.Fade(0.2f);
                for (int i = 0; i < blurSteps; i++)
                {
                    var f = (float)i / (blurSteps - 1);
                    drawList.AddCircleFilled(p - Vector2.One *r * (0.2f+f) * 0.1f,
                                             r* 0.5f* (node.Material.PbrParameters.Roughness * f + 0.6f), 
                                             spec);
                }
            }
            else
            {
                ImGui.SameLine(400);
                ImGui.TextUnformatted("no material");
            }

            foreach (var child in node.ChildNodes)
            {
                DrawNode(child, sceneSetup, parentVisible);
            }

            ImGui.TreePop();
        }
        ImGui.PopStyleVar();
    }
    
    static string FormatCount(this int num)
    {
        return num switch
                   {
                       >= 100000 => FormatCount(num / 1000) + "K",
                       >= 10000  => (num / 1000D).ToString("0.#") + "K",
                       _         => num.ToString("#,0")
                   };
    }
}