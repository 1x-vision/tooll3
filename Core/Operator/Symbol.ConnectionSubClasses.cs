using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using T3.Core.Logging;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator;

public sealed partial class Symbol
{
    /// <summary>
    /// Options on the visual presentation of <see cref="Symbol"/> input.
    /// </summary>
    public sealed class InputDefinition
    {
        public Guid Id { get; internal init; }
        public string Name { get; internal set; }
        public InputValue DefaultValue { get; set; }
        public bool IsMultiInput { get; internal set; }
    }

    public sealed class OutputDefinition
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public Type ValueType { get; init; }
        public Type OutputDataType { get; init; }
        public DirtyFlagTrigger DirtyFlagTrigger { get; init; }

        private static readonly ConcurrentDictionary<Type, Func<object>> OutputValueConstructors = new();

        public static bool TryGetNewValueType(OutputDefinition def, out IOutputData newData)
        {
            return TryCreateOutputType(def, out newData, def.ValueType);
        }

        public static bool TryGetNewOutputDataType(OutputDefinition def, out IOutputData newData)
        {
            return TryCreateOutputType(def, out newData, def.OutputDataType);
        }

        private static bool TryCreateOutputType(OutputDefinition def, out IOutputData newData, Type valueType)
        {
            if (valueType == null)
            {
                newData = null;
                return false;
            }

            if (OutputValueConstructors.TryGetValue(valueType, out var constructor))
            {
                newData = (IOutputData)constructor();
                return true;
            }

            if (!valueType.IsAssignableTo(typeof(IOutputData)))
            {
                Log.Warning($"Value type {valueType} for output {def.Name} is not an {nameof(IOutputData)}");
                newData = null;
                return false;
            }

            constructor = Expression.Lambda<Func<object>>(Expression.New(valueType)).Compile();
            OutputValueConstructors[valueType] = constructor;
            newData = (IOutputData)constructor();
            return true;
        }
    }

    public class Connection
    {
        public Guid SourceParentOrChildId { get; }
        public Guid SourceSlotId { get; }
        public Guid TargetParentOrChildId { get; }
        public Guid TargetSlotId { get; }

        private readonly int _hashCode;

        public Connection(in Guid sourceParentOrChildId, in Guid sourceSlotId, in Guid targetParentOrChildId, in Guid targetSlotId)
        {
            SourceParentOrChildId = sourceParentOrChildId;
            SourceSlotId = sourceSlotId;
            TargetParentOrChildId = targetParentOrChildId;
            TargetSlotId = targetSlotId;

            // pre-compute hash code as this is read-only
            _hashCode = CalculateHashCode(sourceParentOrChildId, sourceSlotId, targetParentOrChildId, targetSlotId);
        }

        public sealed override int GetHashCode() => _hashCode;

        private int CalculateHashCode(in Guid sourceParentOrChildId, in Guid sourceSlotId, in Guid targetParentOrChildId, in Guid targetSlotId)
        {
            int hash = sourceParentOrChildId.GetHashCode();
            hash = hash * 31 + sourceSlotId.GetHashCode();
            hash = hash * 31 + targetParentOrChildId.GetHashCode();
            hash = hash * 31 + targetSlotId.GetHashCode();
            return hash;
        }

        public sealed override bool Equals(object other)
        {
            return GetHashCode() == other?.GetHashCode();
        }

        public static bool operator ==(Connection a, Connection b) => a?.GetHashCode() == b?.GetHashCode();
        public static bool operator !=(Connection a, Connection b) => a?.GetHashCode() != b?.GetHashCode();

        public bool IsSourceOf(Guid sourceParentOrChildId, Guid sourceSlotId)
        {
            return SourceParentOrChildId == sourceParentOrChildId && SourceSlotId == sourceSlotId;
        }

        public bool IsTargetOf(Guid targetParentOrChildId, Guid targetSlotId)
        {
            return TargetParentOrChildId == targetParentOrChildId && TargetSlotId == targetSlotId;
        }

        public bool IsConnectedToSymbolOutput => TargetParentOrChildId == Guid.Empty;
        public bool IsConnectedToSymbolInput => SourceParentOrChildId == Guid.Empty;
    }
}