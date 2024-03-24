﻿using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction.TransformGizmos;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.OutputUi
{
    public abstract class OutputUi<T> : IOutputUi
    {
        public Symbol.OutputDefinition OutputDefinition { get; set; }
        public Guid Id => OutputDefinition.Id;
        public Type Type { get; } = typeof(T);
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new(100, 30);
        public abstract IOutputUi Clone();

        public void DrawValue(ISlot slot, EvaluationContext context, bool recompute)
        {
            var drawList = ImGui.GetWindowDrawList();
            drawList.ChannelsSplit(2);
            drawList.ChannelsSetCurrent(1);
            {
                TransformGizmoHandling.SetDrawList(drawList);
                if (recompute)
                {
                    Recompute(slot, context);
                }
            }
            drawList.ChannelsSetCurrent(0);
            {
                DrawTypedValue(slot);
            }
            drawList.ChannelsMerge();
            TransformGizmoHandling.RestoreDrawList();
        }

        protected virtual void Recompute(ISlot slot, EvaluationContext context)
        {
            StartInvalidation(slot);
            slot.Update(context);
        }

        protected abstract void DrawTypedValue(ISlot slot);

        public void StartInvalidation(ISlot slot)
        {
            DirtyFlag.InvalidationRefFrame++;
            slot.Invalidate();
        }
    }
}