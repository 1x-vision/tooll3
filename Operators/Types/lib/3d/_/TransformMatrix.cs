using System;
using System.Numerics;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using Vector4 = System.Numerics.Vector4;

namespace T3.Operators.Types.Id_17324ce1_8920_4653_ac67_c211ad507a81 
{
    public class TransformMatrix : Instance<TransformMatrix>
    {
        [Output(Guid = "751E97DE-C418-48C7-823E-D4660073A559")]
        public readonly Slot<Vector4[]> Result = new Slot<Vector4[]>();
        

        [Output(Guid = "ECA8121B-2A7F-4ECC-9143-556DCF78BA33")]
        public readonly Slot<Vector4[]> ResultInverted = new Slot<Vector4[]>();
        
        public TransformMatrix()
        {
            Result.UpdateAction = Update;
            ResultInverted.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var s = Scale.GetValue(context) * UniformScale.GetValue(context);
            var r = Rotation.GetValue(context);
            float yaw = MathUtils.ToRad * r.Y;
            float pitch = MathUtils.ToRad * r.X;
            float roll = MathUtils.ToRad * r.Z;
            var pivot = Pivot.GetValue(context);
            var t = Translation.GetValue(context);
            var objectToParentObject = Math3DUtils.CreateTransformationMatrix(scalingCenter: pivot, 
                                                             scalingRotation: Quaternion.Identity, 
                                                             scaling: new Vector3(s.X, s.Y, s.Z), 
                                                             rotationCenter: pivot,
                                                             rotation: Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll), 
                                                             translation: new Vector3(t.X, t.Y, t.Z));

            var shearing = Shear.GetValue(context);
            
              
            
            Matrix4x4 m = Matrix4x4.Identity;
            m.M12=shearing.Y; 
            m.M21=shearing.X; 
            m.M13=shearing.Z;             
            objectToParentObject = Matrix4x4.Multiply(objectToParentObject,m);
            
            // transpose all as mem layout in hlsl constant buffer is row based
            objectToParentObject.Transpose();
            
            if (Invert.GetValue(context))
            {
                objectToParentObject.Invert(); 
            }
            
            _matrix[0] = objectToParentObject.Row1();
            _matrix[1] = objectToParentObject.Row2();
            _matrix[2] = objectToParentObject.Row3();
            _matrix[3] = objectToParentObject.Row4();
            Result.Value = _matrix;

            Matrix4x4.Invert(objectToParentObject, out var invertedMatrix);
            
            _invertedMatrix[0] = invertedMatrix.Row1();
            _invertedMatrix[1] = invertedMatrix.Row2();
            _invertedMatrix[2] = invertedMatrix.Row3();
            _invertedMatrix[3] = invertedMatrix.Row4();
            ResultInverted.Value = _invertedMatrix;
            
        }

        private Vector4[] _matrix = new Vector4[4];
        private Vector4[] _invertedMatrix = new Vector4[4];
        
        
        
        [Input(Guid = "3B817E6C-F532-4A8C-A2FF-A00DC926EEB2")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new InputSlot<System.Numerics.Vector3>();
        
        [Input(Guid = "5339862D-5A18-4D0C-B908-9277F5997563")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();
        
        [Input(Guid = "58B9DFB6-0596-4F0D-BAF6-7FB3AE426C94")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "566F1619-1DE0-4B41-B167-7FC261730D62")]
        public readonly InputSlot<float> UniformScale = new InputSlot<float>();
        
        [Input(Guid = "F53F3311-E1FC-418B-8861-74ADC175D5FA")]
        public readonly InputSlot<System.Numerics.Vector3> Shear = new();

        [Input(Guid = "279730B7-C427-4924-9FDE-77EB65A3076C")]
        public readonly InputSlot<System.Numerics.Vector3> Pivot = new();


        
        [Input(Guid = "E19808D8-6D73-4638-B5F2-DDDDC49AD815")]
        public readonly InputSlot<bool> Invert = new InputSlot<bool>();        
    }
}
