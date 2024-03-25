using System.Linq;
using System.Numerics;
using ImGuiNET;
using lib.exec.context;
using T3.Core.Operator;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi
{
    public static class GetFloatVarUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance1, ImDrawListPtr drawList, ImRect area, Vector2 canvasScale)
        {
            if (!(instance1 is GetFloatVar instance))
                return SymbolChildUi.CustomUiResult.PreventOpenSubGraph;

            drawList.PushClipRect(area.Min, area.Max, true);

            var value = instance.Result.Value;

            var name = instance1.SymbolChild.Name;
            if (!string.IsNullOrWhiteSpace(instance1.SymbolChild.Name))
            {
                WidgetElements.DrawPrimaryTitle(drawList, area, name, canvasScale);
            }
            else
            {
                WidgetElements.DrawPrimaryTitle(drawList, area, "Get " + instance.Variable.TypedInputValue.Value, canvasScale);
            }

            WidgetElements.DrawSmallValue(drawList, area, $"{value:0.000}", canvasScale);

            drawList.PopClipRect();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels | SymbolChildUi.CustomUiResult.PreventOpenSubGraph;
        }
    }
}