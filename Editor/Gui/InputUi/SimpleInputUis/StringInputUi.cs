﻿using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Core.Resource;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Serialization;

namespace T3.Editor.Gui.InputUi.SimpleInputUis
{
    public class StringInputUi : InputValueUi<string>
    {
        private const int MaxStringLength = 4000;

        public enum UsageType
        {
            Default,
            FilePath,
            DirectoryPath,
            Multiline,
            CustomDropdown,
        }

        public UsageType Usage { get; private set; } = UsageType.Default;
        public string FileFilter { get; private set; }

        public override IInputUi Clone()
        {
            return new StringInputUi
                       {
                           InputDefinition = InputDefinition,
                           Parent = Parent,
                           PosOnCanvas = PosOnCanvas,
                           Relevancy = Relevancy,
                           Size = Size,
                           Usage = Usage,
                           FileFilter = FileFilter,
                       };
        }

        protected override InputEditStateFlags DrawEditControl(string name, Symbol.Child.Input input, ref string value, bool readOnly)
        {
            if (value == null)
            {
                // value was null!
                ImGui.TextUnformatted(name + " is null?!");
                return InputEditStateFlags.Nothing;
            }

            var inputEditStateFlags = InputEditStateFlags.Nothing;

            switch (Usage)
            {
                case UsageType.Default:
                    inputEditStateFlags = DrawDefaultTextEdit(ref value) ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
                    break;
                case UsageType.Multiline:
                    inputEditStateFlags = DrawMultilineTextEdit(ref value);
                    break;
                case UsageType.FilePath:
                    inputEditStateFlags = DrawTypeAheadSearch(FileOperations.FilePickerTypes.File, ref value);
                    NormalizePathSeparators(inputEditStateFlags, ref value);
                    break;
                case UsageType.DirectoryPath:
                    inputEditStateFlags = DrawTypeAheadSearch(FileOperations.FilePickerTypes.Folder, ref value);
                    NormalizePathSeparators(inputEditStateFlags, ref value);
                    break;
                case UsageType.CustomDropdown:
                    inputEditStateFlags = DrawCustomDropdown(input, ref value);
                    break;
            }

            inputEditStateFlags |= ImGui.IsItemClicked() ? InputEditStateFlags.Started : InputEditStateFlags.Nothing;
            inputEditStateFlags |= ImGui.IsItemDeactivatedAfterEdit() ? InputEditStateFlags.Finished : InputEditStateFlags.Nothing;

            return inputEditStateFlags;

            static void NormalizePathSeparators(InputEditStateFlags inputEditStateFlags, ref string value)
            {
                // normalize path separators when modified
                // use only forward slashes as windows is the only OS that supports backslashes
                if ((inputEditStateFlags & InputEditStateFlags.Modified) == InputEditStateFlags.Modified
                    || (inputEditStateFlags & InputEditStateFlags.Finished) == InputEditStateFlags.Finished)
                {
                    value = value.Replace('\\', '/');
                    
                    // todo: handle trailing slashes
                    //if (value.EndsWith('/'))
                      //  value = value[..^1];
                }
            }
        }

        private InputEditStateFlags DrawTypeAheadSearch(FileOperations.FilePickerTypes type, ref string value)
        {
            return DrawFileInput(type, ref value, FileFilter, Draw);
            
            static InputResult Draw(InputRequest request)
            {
                var filter = request.Filter;
                var value = request.Value;
                
                var drawnItems = ResourceManager.EnumerateResources(filter, request.IsFolder, request.ResourcePackages, ResourceManager.PathMode.Aliased);
                
                var changed = InputWithTypeAheadSearch.Draw("##filePathSearch", ref value, drawnItems, request.ShowWarning, true);
                return new InputResult(changed, value);
            }
        }
        
        private readonly record struct InputResult(bool Modified, string Value);

        private readonly record struct InputRequest(string Value, string[] Filter, bool IsFolder, bool ShowWarning, IEnumerable<IResourcePackage> ResourcePackages);

        private static InputEditStateFlags DrawFileInput(FileOperations.FilePickerTypes type, ref string value, string filter, Func<InputRequest, InputResult> draw)
        {
            ImGui.SetNextItemWidth(-70);

            var selectedInstances = GraphWindow.Focused!.GraphCanvas.NodeSelection.GetSelectedInstances().ToArray();
            var packagesInCommon = selectedInstances.PackagesInCommon().ToArray();
            var isFolder = type == FileOperations.FilePickerTypes.Folder;
            var exists = ResourceManager.TryResolvePath(value, packagesInCommon, out _, out _, isFolder);
            
            var warning = type switch
                              {
                                  FileOperations.FilePickerTypes.File when !exists        => "File doesn't exist:\n",
                                  FileOperations.FilePickerTypes.Folder when !exists => "Directory doesn't exist:\n",
                                  _                                                                   => string.Empty
                              };

            if (warning != string.Empty)
                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.StatusAnimated.Rgba);

            var fileFiltersInCommon = selectedInstances
                                     .Where(x => x is IDescriptiveFilename)
                                     .Cast<IDescriptiveFilename>()
                                     .Select(x => x.FileFilter)
                                     .Aggregate((a, b) => a.Intersect(b))
                                     .Append(filter != null && filter.Contains('|') ? filter.Split('|')[1] : filter)
                                     .Where(s => !string.IsNullOrWhiteSpace(s))
                                     .Distinct()
                                     .ToArray();

            var result = draw(new InputRequest(value, fileFiltersInCommon, isFolder, ShowWarning: !exists, packagesInCommon));
            value = result.Value;
            var inputEditStateFlags = result.Modified ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;

            if (warning != string.Empty)
                ImGui.PopStyleColor();

            if (ImGui.IsItemHovered() && ImGui.CalcTextSize(value).X > ImGui.GetItemRectSize().X)
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(warning + value);
                ImGui.EndTooltip();
            }

            ImGui.SameLine();
            var modifiedByPicker = FileOperations.DrawFileSelector(type, ref value, filter);
            if (modifiedByPicker)
            {
                inputEditStateFlags = InputEditStateFlags.Modified | InputEditStateFlags.Finished;
            }
            return inputEditStateFlags;
        }

        private static bool DrawDefaultTextEdit(ref string value)
        {
            return ImGui.InputText("##textEdit", ref value, MaxStringLength);
        }

        private static InputEditStateFlags DrawMultilineTextEdit(ref string value)
        {
            ImGui.Dummy(new Vector2(1, 1));
            var changed = ImGui.InputTextMultiline("##textEdit", ref value, MaxStringLength, new Vector2(-1, 3 * ImGui.GetFrameHeight()));
            return changed ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
        }

        private static InputEditStateFlags DrawCustomDropdown(Symbol.Child.Input input, ref string value)
        {
            var instance = GraphWindow.Focused?.GraphCanvas.NodeSelection.GetSelectedInstanceWithoutComposition();
            if (instance != null && instance is ICustomDropdownHolder customValueHolder)
            {
                var changed = false;

                var currentValue = customValueHolder.GetValueForInput(input.Id);
                if (ImGui.BeginCombo("##customDropdown", currentValue, ImGuiComboFlags.HeightLarge))
                {
                    foreach (var value2 in customValueHolder.GetOptionsForInput(input.Id))
                    {
                        if (value2 == null)
                            continue;

                        var isSelected = value2 == currentValue;
                        if (!ImGui.Selectable($"{value2}", isSelected, ImGuiSelectableFlags.DontClosePopups))
                            continue;

                        ImGui.CloseCurrentPopup();
                        customValueHolder.HandleResultForInput(input.Id, value2);
                        changed = true;
                    }

                    ImGui.EndCombo();
                }

                return changed ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
            }
            else
            {
                ImGui.NewLine();
                //Log.Warning($"{instance?.Parent?.Symbol?.Name} doesn't support custom inputs");
                return InputEditStateFlags.Nothing;
            }
        }

        protected override void DrawReadOnlyControl(string name, ref string value)
        {
            if (value != null)
            {
                ImGui.InputText(name, ref value, MaxStringLength, ImGuiInputTextFlags.ReadOnly);
            }
            else
            {
                string nullString = "<null>";
                ImGui.InputText(name, ref nullString, MaxStringLength, ImGuiInputTextFlags.ReadOnly);
            }
        }

        public override void DrawSettings()
        {
            base.DrawSettings();
            FormInputs.AddVerticalSpace();

            {
                var tmpForRef = Usage;
                if (FormInputs.AddEnumDropdown(ref tmpForRef, "Usage"))
                    Usage = tmpForRef;
            }

            if (Usage == UsageType.FilePath)
            {
                var tmp = FileFilter;
                var warning = !string.IsNullOrEmpty(tmp) && !tmp.Contains('|')
                                  ? "Filter must include at least one | symbol.\nPlease read tooltip for examples"
                                  : null;

                if (FormInputs.AddStringInput("File Filter", ref tmp, null, warning,
                                              "This will only work for file FilePath-Mode.\nThe filter has to be in following format:\n\n Your Description (*.ext)|*.ext"))
                {
                    FileFilter = tmp;
                }
            }
        }

        public override void Write(JsonTextWriter writer)
        {
            base.Write(writer);

            writer.WriteObject(nameof(Usage), Usage.ToString());

            if (!string.IsNullOrEmpty(FileFilter))
                writer.WriteObject(nameof(FileFilter), FileFilter);
        }

        public override void Read(JToken inputToken)
        {
            if (inputToken == null)
                return;

            base.Read(inputToken);

            if (Enum.TryParse<UsageType>(inputToken[nameof(Usage)].Value<string>(), out var enumValue))
            {
                Usage = enumValue;
            }

            FileFilter = inputToken[nameof(FileFilter)]?.Value<string>();
        }
    }
}