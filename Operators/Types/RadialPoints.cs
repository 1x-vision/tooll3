using System;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Point = T3.Core.DataTypes.Point;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace T3.Operators.Types.Id_a38626d8_3145_4aa9_820f_ca16b3411985
{
    public class RadialPoints : Instance<RadialPoints>
    {
        [Output(Guid = "F270E4C2-3E5A-4F3E-B474-09E9291999E1")]
        public readonly Slot<StructuredList> ResultList = new Slot<StructuredList>();

        public RadialPoints()
        {
            ResultList.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var closeCircle = CloseCircle.GetValue(context);
            var circleOffset = closeCircle ? 1 : 0;
            var corners = Count.GetValue(context).Clamp(1, 10000);
            var pointCount = corners + circleOffset;
            var listCount = corners + 2 *circleOffset;    // Separator

            if (_pointList.NumElements != listCount)
            {
                //_points = new T3.Core.DataTypes.Point[count];
                _pointList.SetLength(listCount);
            }

            var axis = Axis.GetValue(context);
            var center = Center.GetValue(context);
            var offset = Offset.GetValue(context);
            var radius = Radius.GetValue(context);
            var radiusOffset = RadiusOffset.GetValue(context);
            var thickness = W.GetValue(context);
            var thicknessOffset = WOffset.GetValue(context);

            var angelInRads = StartAngle.GetValue(context) * MathUtils.ToRad + (float)Math.PI/2;
            var deltaAngle = -Cycles.GetValue(context) * MathUtils.Pi2 / (pointCount- circleOffset);
            
            for (var index = 0; index < pointCount; index++)
            {
                var f = corners == 1 
                            ? 1 
                            : (float)index / pointCount;
                var length = MathUtils.Lerp(radius, radius + radiusOffset, f);
                var v = Vector3.UnitX * length;
                var rot = Quaternion.CreateFromAxisAngle(axis, angelInRads);
                var vInAxis = Vector3.Transform(v, rot) + Vector3.Lerp(center, center + offset, f);

                var p = new Point
                            {
                                Position = vInAxis,
                                W = MathUtils.Lerp(thickness, thickness + thicknessOffset, f),
                                Orientation = rot
                            };
                _pointList[index] = p;
                angelInRads += deltaAngle;
            }

            if (closeCircle)
            {
                _pointList[listCount - 1] = Point.Separator();
            }

            ResultList.Value = _pointList;
        }

        private readonly StructuredList<Point> _pointList = new StructuredList<Point>(10);
        //private readonly Point Separator;

        [Input(Guid = "cb697476-36df-44ae-bd1d-138cc49467c2")]
        public readonly InputSlot<int> Count = new InputSlot<int>();

        [Input(Guid = "9C26FCAD-EF7D-46AA-9A7E-EB853E88E955")]
        public readonly InputSlot<float> Radius = new InputSlot<float>();

        [Input(Guid = "BCE00400-5951-4574-AF61-B24FF0AD5E23")]
        public readonly InputSlot<float> RadiusOffset = new InputSlot<float>();

        [Input(Guid = "D68DCBA4-D713-4BC7-A418-85042EFC26D3")]
        public readonly InputSlot<Vector3> Center = new InputSlot<Vector3>();

        [Input(Guid = "03A54164-8EF9-4CC8-88F3-55AA5DB3640C")]
        public readonly InputSlot<Vector3> Offset = new InputSlot<Vector3>();

        [Input(Guid = "E3736CED-D10D-41F3-92ED-DDFC3EDD1BC6")]
        public readonly InputSlot<float> StartAngle = new InputSlot<float>();

        [Input(Guid = "C9341B17-5F56-4112-BA87-FE734B7BF0BA")]
        public readonly InputSlot<float> Cycles = new InputSlot<float>();

        [Input(Guid = "D1E78447-E110-4CDF-B761-DFF32F05140D")]
        public readonly InputSlot<Vector3> Axis = new InputSlot<Vector3>(Vector3.UnitZ);

        [Input(Guid = "DEE89AD4-1516-40D0-A682-98E05A8B7C12")]
        public readonly InputSlot<float> W = new InputSlot<float>();

        [Input(Guid = "C02152E3-D643-4E1D-99CA-11AB6BC8A5FB")]
        public readonly InputSlot<float> WOffset = new InputSlot<float>();
        
        [Input(Guid = "75ACDFBD-176B-4E65-BD33-AC10F8373EB2")]
        public readonly InputSlot<bool> CloseCircle = new InputSlot<bool>();

    }
}