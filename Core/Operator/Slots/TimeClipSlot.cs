using System;
using T3.Core.Animation;
using T3.Core.IO;
using T3.Core.Logging;
// ReSharper disable ForCanBeConvertedToForeach

namespace T3.Core.Operator.Slots
{
    public interface ITimeClipProvider
    {
        TimeClip TimeClip { get; }
    }

    public interface IOutputDataUser
    {
        void SetOutputData(IOutputData data);
    }

    // This interface is mainly to extract the output data type while no instance of an implementer exists.
    internal interface IOutputDataUser<T> : IOutputDataUser
    {
    }

    public sealed class TimeClipSlot<T> : Slot<T>, ITimeClipProvider, IOutputDataUser<TimeClip>
    {
        public TimeClip TimeClip { get; private set; }

        public TimeClipSlot()
        {
            HasInvalidationOverride = true;
        }

        public void SetOutputData(IOutputData data)
        {
            TimeClip = (TimeClip)data;
            TimeClip.Id = Parent.SymbolChildId;
        }

        public UpdateStates LastUpdateStatus;

        private void UpdateWithTimeRangeCheck(EvaluationContext context)
        {
            if ((context.LocalTime < TimeClip.TimeRange.Start) || (context.LocalTime >= TimeClip.TimeRange.End))
            {
                LastUpdateStatus = ProjectSettings.Config.TimeClipSuspending ? UpdateStates.Suspended : UpdateStates.Active;
                return;
            }

            // TODO: Setting local time should flag time accessors as dirty 
            var prevTime = context.LocalTime;
            double factor = (context.LocalTime - TimeClip.TimeRange.Start) / (TimeClip.TimeRange.End - TimeClip.TimeRange.Start);
            context.LocalTime = factor * (TimeClip.SourceRange.End - TimeClip.SourceRange.Start) + TimeClip.SourceRange.Start;

            if (_baseUpdateAction == null)
            {
                Log.Warning("Ignoring invalid time clip update action", Parent);
            }
            else
            {
                _baseUpdateAction(context);
            }

            context.LocalTime = prevTime;
            LastUpdateStatus = UpdateStates.Active;
        }

        private Action<EvaluationContext> _baseUpdateAction;

        public enum UpdateStates
        {
            Undefined,
            Active,
            Inactive, // Out of range
            Suspended,
        }

        public override Action<EvaluationContext> UpdateAction
        {
            set
            {
                _baseUpdateAction = value;
                base.UpdateAction = UpdateWithTimeRangeCheck;
            }
        }

        protected override void SetDisabled(bool isDisabled)
        {
            if (isDisabled == _isDisabled)
                return;

            if (isDisabled)
            {
                _keepOriginalUpdateAction = _baseUpdateAction;
                base.UpdateAction = EmptyAction;
                DirtyFlag.Invalidate();
            }
            else
            {
                RestoreUpdateAction();
                DirtyFlag.Invalidate();
            }

            _isDisabled = isDisabled;
        }

        protected override int InvalidationOverride()
        {
            // Slot is an output of an composition op
            if (IsConnected)
            {
                return InputConnections[0].Invalidate();
            }

            if (LastUpdateStatus == UpdateStates.Suspended)
            {
                return _dirtyFlag.Invalidate();
            }

            var isOutputDirty = _dirtyFlag.IsDirty;
            var parentInputs = Parent.Inputs;
            var parentInputCount = parentInputs.Count;
            for (var index = 0; index < parentInputCount; index++)
            {
                var inputSlot = parentInputs[index];
                var inputDirtyFlag = inputSlot.DirtyFlag;
                if (inputSlot.TryGetFirstConnection(out var inputSlotConnection))
                {
                    inputDirtyFlag.Target = inputSlotConnection.Invalidate();
                }
                else if (inputDirtyFlag.TriggerIsAnimated)
                {
                    inputDirtyFlag.Invalidate();
                }

                inputSlot.SetVisited();
                isOutputDirty |= inputDirtyFlag.IsDirty;
            }

            return isOutputDirty ? _dirtyFlag.Invalidate() : _dirtyFlag.Target;
        }
    }
}