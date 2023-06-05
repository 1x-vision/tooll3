using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Interaction.Snapping;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.TimeLine;

namespace T3.Editor.Gui.Interaction.WithCurves
{
    public abstract class CurveEditCanvas : ScalableCanvas, ITimeObjectManipulation
    {
        protected CurveEditCanvas()
        {
            ScrollTarget = new Vector2(-2.5f, 0.0f);
            ScaleTarget = new Vector2(80, -1);
        }

        public string ImGuiTitle = "timeline";

        
        protected void DrawCurveCanvas(Action drawAdditionalCanvasContent, float height = 0, T3Ui.EditingFlags flags = T3Ui.EditingFlags.None)
        {

            ImGui.BeginChild(ImGuiTitle, new Vector2(0, height), true,
                             ImGuiWindowFlags.NoScrollbar | 
                             ImGuiWindowFlags.NoMove | 
                             ImGuiWindowFlags.NoScrollWithMouse);
            {
                Drawlist = ImGui.GetWindowDrawList();
                UpdateCanvas(flags);
                SetScaleToParentCanvas(GraphCanvas.Current);
                Drawlist = ImGui.GetWindowDrawList();

                drawAdditionalCanvasContent();
                HandleFenceUpdate();
                SnapHandlerForU.DrawSnapIndicator(this, ValueSnapHandler.Mode.VerticalLinesForU);
                SnapHandlerForV.DrawSnapIndicator(this, ValueSnapHandler.Mode.HorizontalLinesForV);
            }
            ImGui.EndChild();
        }

        private void HandleFenceUpdate()
        {
            FenceState = SelectionFence.UpdateAndDraw(FenceState);
            switch (FenceState)
            {
                case SelectionFence.States.Updated:
                case SelectionFence.States.CompletedAsClick:
                    UpdateSelectionForArea(SelectionFence.BoundsInScreen, SelectionFence.SelectMode);
                    break;
            }
        }


        protected void HandleCreateNewKeyframes(Curve curve)
        {
            var hoverNewKeyframe = !ImGui.IsAnyItemActive()
                                   && ImGui.IsWindowHovered()
                                   && ImGui.GetIO().KeyAlt
                                   && ImGui.IsWindowHovered();
            if (!hoverNewKeyframe)
                return;
            
            var hoverTime = InverseTransformX(ImGui.GetIO().MousePos.X);
            SnapHandlerForU.CheckForSnapping(ref hoverTime, Scale.X);

            if (ImGui.IsMouseReleased(0))
            {
                var dragDistance = ImGui.GetIO().MouseDragMaxDistanceAbs[0].Length();
                if (dragDistance < 2)
                {
                    ClearSelection();

                    InsertNewKeyframe(curve, hoverTime);
                }
            }
            else
            {
                var sampledValue = (float)curve.GetSampledValue(hoverTime);
                var posOnCanvas = new Vector2(hoverTime, sampledValue);
                var posOnScreen = TransformPosition(posOnCanvas)
                                - new Vector2(KeyframeIconWidth / 2 + 1, KeyframeIconWidth / 2 + 1);
                Icons.Draw(Icon.CurveKeyframe, posOnScreen);
                var drawlist = ImGui.GetWindowDrawList();
                drawlist.AddText(posOnScreen + Vector2.One*20, Color.Gray, $"Insert at\n{hoverTime:0.00}  {sampledValue:0.00}");
            }

            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        private const float KeyframeIconWidth = 16;

        private void InsertNewKeyframe(Curve curve, float u)
        {
            var value = curve.GetSampledValue(u);
            var previousU = curve.GetPreviousU(u);

            var key = (previousU != null)
                          ? curve.GetV(previousU.Value).Clone()
                          : new VDefinition();

            key.Value = value;
            key.U = u;
            curve.AddOrUpdateV(u, key);
        }

        #region implement ITimeObjectManipulation to forward interaction to children
        public void ClearSelection()
        {
            foreach (var sh in TimeObjectManipulators)
            {
                sh.ClearSelection();
            }
        }

        public void UpdateSelectionForArea(ImRect screenArea, SelectionFence.SelectModes selectMode)
        {
            foreach (var sh in TimeObjectManipulators)
            {
                sh.UpdateSelectionForArea(screenArea, selectMode);
            }
        }

        private MacroCommand _macro;
        private readonly List<ICommand> _commands = new List<ICommand>();
        
        public ICommand StartDragCommand()
        {
            _commands.Clear();
            foreach (var manipulators in TimeObjectManipulators)
            {
                _commands.Add(manipulators.StartDragCommand());
            }
            _macro = new MacroCommand("Manipulate Keyframes", _commands);
            return null;
        }

        public void UpdateDragCommand(double dt, double dv)
        {
            foreach (var manipulators in TimeObjectManipulators)
            {
                manipulators.UpdateDragCommand(dt, dv);
            }
        }

        public void UpdateDragAtStartPointCommand(double dt, double dv)
        {
            foreach (var manipulators in TimeObjectManipulators)
            {
                manipulators.UpdateDragAtStartPointCommand(dt, dv);
            }
        }

        public void UpdateDragAtEndPointCommand(double dt, double dv)
        {
            foreach (var manipulators in TimeObjectManipulators)
            {
                manipulators.UpdateDragAtEndPointCommand(dt, dv);
            }
        }

        public void UpdateDragStretchCommand(double scaleU, double scaleV, double originU, double originV)
        {
            foreach (var manipulators in TimeObjectManipulators)
            {
                manipulators.UpdateDragStretchCommand(scaleU, scaleV, originU, originV);
            }
        }

        public TimeRange GetSelectionTimeRange()
        {
            var timeRange = new TimeRange(float.PositiveInfinity, float.NegativeInfinity);

            foreach (var sh in TimeObjectManipulators)
            {
                timeRange.Unite(sh.GetSelectionTimeRange());
            }

            return timeRange;
        }

        public void CompleteDragCommand()
        {
            foreach (var manipulators in TimeObjectManipulators)
            {
                manipulators.CompleteDragCommand();
            }

            if (_macro == null)
            {
                Log.Warning("Can't complete no valid valid drag command?");
                return;
            }
            UndoRedoStack.AddAndExecute(_macro);
        }

        public void DeleteSelectedElements()
        {
            foreach (var s in TimeObjectManipulators)
            {
                s.DeleteSelectedElements();
            }
        }

        protected readonly List<ITimeObjectManipulation> TimeObjectManipulators = new List<ITimeObjectManipulation>();
        #endregion
        
        protected SelectionFence.States FenceState;
        
        public readonly ValueSnapHandler SnapHandlerForU = new ValueSnapHandler();
        public readonly ValueSnapHandler SnapHandlerForV = new ValueSnapHandler();
        protected ImDrawListPtr Drawlist;
    }
}