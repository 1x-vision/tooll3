using System;
using System.Collections.Generic;
using System.Resources;
using Microsoft.Win32;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Point = T3.Core.DataTypes.Point;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace T3.Operators.Types.Id_478522e1_5683_4db1_a7dc_db59838eca2a
{
    public class RepeatAtPointsCpu : Instance<RepeatAtPointsCpu>
    {
        [Output(Guid = "0e874f9d-352c-435f-a6d2-d7c8d01e2205")]
        public readonly Slot<StructuredList> ResultList = new Slot<StructuredList>();

        public RepeatAtPointsCpu()
        {
            ResultList.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var sourcePoints = SourcePoints.GetValue(context) as StructuredList<Point>;
            var destinationPoints = DestinationsPoints.GetValue(context) as StructuredList<Point>;

            if (sourcePoints == null || destinationPoints == null
                                     || sourcePoints.NumElements == 0 || destinationPoints.NumElements == 0)
            {
                _pointList.SetLength(0);
                ResultList.Value = _pointList;
                return;
            }

            var count = sourcePoints.NumElements * destinationPoints.NumElements;

            if (_pointList.NumElements != count)
            {
                _pointList.SetLength(count);
            }
            
            for (var destinationIndex = 0; destinationIndex < destinationPoints.NumElements; destinationIndex++)
            {
                var destination = destinationPoints.TypedElements[destinationIndex];
                
                for (var sourceIndex = 0; sourceIndex < sourcePoints.NumElements; sourceIndex++)
                {
                    var source = sourcePoints.TypedElements[sourceIndex];
                    _pointList.TypedElements[destinationIndex * sourcePoints.NumElements + sourceIndex]
                        = new Point()
                              {
                                  Position = destination.Position + Vector3.Transform(source.Position, destination.Orientation),
                                  W = source.W,
                                  Orientation = Quaternion.Multiply(destination.Orientation, source.Orientation),
                              };
                }
            }

            ResultList.Value = _pointList;
        }

        private readonly StructuredList<Point> _pointList = new StructuredList<Point>(10);

        [Input(Guid = "6FFEC88A-F5B9-4D2F-8B03-A2D3DA9C6B8C")]
        public readonly InputSlot<StructuredList> SourcePoints = new InputSlot<StructuredList>();

        [Input(Guid = "26EA6E28-E093-484C-9635-5C4AC0EFDFB7")]
        public readonly InputSlot<StructuredList> DestinationsPoints = new InputSlot<StructuredList>();
    }
}