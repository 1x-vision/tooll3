using T3.Core.Operator;
using T3.Editor.Gui.Selection;

namespace T3.Editor.UiModel
{
    public partial class SymbolUi
    {
        /// <summary>
        /// Properties needed for visual representation of an instance. Should later be moved to gui component.
        /// </summary>
        public sealed class Child : ISelectableCanvasObject
        {
            internal enum Styles
            {
                Default,
                Expanded,
                Resizable,
                WithThumbnail,
            }

            internal enum ConnectionStyles
            {
                Default,
                FadedOut,
            }

            internal static Vector2 DefaultOpSize { get; } = new(110, 25);

            internal Dictionary<Guid, ConnectionStyles> ConnectionStyleOverrides { get; } = new();

            internal readonly Symbol.Child SymbolChild;
            internal readonly SymbolUi Parent;

            public Guid Id => SymbolChild.Id;
            public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
            public Vector2 Size { get; set; } = DefaultOpSize;

            internal int SnapshotGroupIndex;
            internal Styles Style;
            internal string Comment;

            internal bool IsDisabled { get => SymbolChild.Outputs.FirstOrDefault().Value?.IsDisabled ?? false; set => SetDisabled(value); }

            internal Child(Symbol.Child symbolChild, SymbolUi parent)
            {
                SymbolChild = symbolChild;
                Parent = parent;
            }


            private void SetDisabled(bool shouldBeDisabled)
            {
                var outputDefinitions = SymbolChild.Symbol.OutputDefinitions;

                // Set disabled status on this child's outputs
                foreach (var outputDef in outputDefinitions)
                {
                    if (outputDef == null)
                    {
                        Log.Warning($"{SymbolChild.Symbol.GetType()} {SymbolChild.Symbol.Name} contains a null {typeof(Symbol.OutputDefinition)}", Id);
                        continue;
                    }

                    var hasOutput = SymbolChild.Outputs.TryGetValue(outputDef.Id, out var childOutput);
                    if (!hasOutput)
                    {
                        Log.Warning($"{typeof(Symbol.Child)} {SymbolChild.ReadableName} does not have the following child output as defined: " +
                                    $"{childOutput.OutputDefinition.Name}({nameof(Guid)}{childOutput.OutputDefinition.Id})");
                        continue;
                    }

                    childOutput.IsDisabled = shouldBeDisabled;
                }

                // Set disabled status on outputs of each instanced copy of this child within all parents that contain it
                foreach (var parentInstance in SymbolChild.Parent.InstancesOfSelf)
                {
                    // This parent doesn't have an instance of our SymbolChild. Ignoring and continuing.
                    if (!parentInstance.Children.TryGetValue(Id, out var matchingChildInstance))
                        continue;

                    // Set disabled status on all outputs of each instance
                    foreach (var slot in matchingChildInstance.Outputs)
                    {
                        slot.IsDisabled = shouldBeDisabled;
                    }
                }
            }

            /// <summary>
            /// 
            /// </summary>
            [Flags]
            public enum CustomUiResult
            {
                None = 0,
                Rendered = 1 << 2,
                PreventTooltip = 1 << 3,
                PreventOpenSubGraph = 1 << 4,
                PreventOpenParameterPopUp = 1 << 5,
                PreventInputLabels = 1 << 6,
            }

            internal Child Clone(SymbolUi parent, Symbol.Child symbolChild)
            {
                return new Child(symbolChild, parent)
                           {
                               PosOnCanvas = PosOnCanvas,
                               Size = Size,
                               Style = Style,
                               Comment = Comment
                           };
            }

            public override string ToString()
            {
                return $"{SymbolChild.Parent.Name}>[{SymbolChild.ReadableName}]";
            }
        }
    }
}