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

namespace T3.Operators.Types.Id_a98d7796_6e09_45d1_a372_f3ea55abd359
{
    public class LinePointsCpu : Instance<LinePointsCpu>
    {
        [Output(Guid = "ba2e400c-8880-4a4e-9e5b-983ef4846165")]
        public readonly Slot<StructuredList> ResultList = new Slot<StructuredList>();

        public LinePointsCpu()
        {
            ResultList.UpdateAction = Update;
            _pointListWithSeparator.TypedElements[2] = Point.Separator();
        }

        private void Update(EvaluationContext context)
        {
            var from = From.GetValue(context);
            var to = To.GetValue(context);
            var w = W.GetValue(context);
            var wOffset = WOffset.GetValue(context);
            var addSeparator = AddSeparator.GetValue(context);

            var rot = Quaternion.CreateFromAxisAngle(new Vector3(0,1,0), (float)Math.Atan2(from.X - to.X, from.Y - to.Y) );
            var array = addSeparator ? _pointListWithSeparator : _pointList;
            
            array.TypedElements[0].Position = from;
            array.TypedElements[0].W = w;
            array.TypedElements[0].Orientation = rot;
            array.TypedElements[1].Position = to;
            array.TypedElements[1].W = w + wOffset;
            array.TypedElements[1].Orientation = rot;
            
            ResultList.Value = array;
        }

        private readonly StructuredList<Point> _pointListWithSeparator = new StructuredList<Point>(3);
        private readonly StructuredList<Point> _pointList = new StructuredList<Point>(2);
        //private readonly Point Separator;

        [Input(Guid = "CD2B1F6F-1964-4D15-92E0-57B77584301B")]
        public readonly InputSlot<Vector3> From = new InputSlot<Vector3>();

        [Input(Guid = "37B9A120-F79D-481C-BEA2-A48E3B2A05B7")]
        public readonly InputSlot<Vector3> To = new InputSlot<Vector3>();
        
        [Input(Guid = "A670BD01-B3BB-4795-9F86-2AB9010F2BEC")]
        public readonly InputSlot<float> W = new InputSlot<float>();

        [Input(Guid = "4E3D7F66-FA22-4780-835B-1C72DDEC16CE")]
        public readonly InputSlot<float> WOffset = new InputSlot<float>();

        [Input(Guid = "8ED772FD-182B-455F-BD82-C9585019D035")]
        public readonly InputSlot<bool> AddSeparator = new InputSlot<bool>();
    }
}