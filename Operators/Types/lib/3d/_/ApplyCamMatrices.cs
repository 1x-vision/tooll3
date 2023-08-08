using System;
using System.Numerics;
using ManagedBass;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_3dae14a8_3d0b_432f_b951_bdd7afd7e5f8
{
    public class ApplyCamMatrices : Instance<ApplyCamMatrices>
    {
        [Output(Guid = "bd6cd982-c99e-4366-906a-2f0280cf32de")]
        public readonly Slot<Command> Output = new();

        public ApplyCamMatrices()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var worldToCam = MatrixFromRows(WorldToCamRows.GetValue(context));
            var camToClipSpace = MatrixFromRows(CamToClipSpaceRows.GetValue(context));
            var aspect = camToClipSpace.M22 / camToClipSpace.M11;
            //Log.Debug($" M11: {camToClipSpace.M11:0.00}  M22: {camToClipSpace.M22:0.00} ", this);
            
            var x = 1;            
            worldToCam.M11 /= camToClipSpace.M11 / x;
            worldToCam.M22 /= camToClipSpace.M22 / x;

            var previousWorldTobject = context.ObjectToWorld;
            
            context.ObjectToWorld = Matrix4x4.Multiply(worldToCam, context.ObjectToWorld);
            
            Command.GetValue(context);
            context.ObjectToWorld = previousWorldTobject;
        }

        private static Matrix4x4 MatrixFromRows(Vector4[] rows)
        {
            if(rows == null || rows.Length != 4)
                return Matrix4x4.Identity;
            
            return new Matrix4x4(
                              rows[0].X, rows[0].Y, rows[0].Z, rows[0].W,
                       rows[1].X, rows[1].Y, rows[1].Z, rows[1].W,
                       rows[2].X, rows[2].Y, rows[2].Z, rows[2].W,
                       rows[3].X, rows[3].Y, rows[3].Z, rows[3].W);
        }

        [Input(Guid = "aad90ca4-5085-4b90-bf58-b87bdc852d62")]
        public readonly InputSlot<Command> Command = new();

        [Input(Guid = "86FA7A95-C6DF-4BB8-A447-E92882A99037")]
        public readonly InputSlot<Vector4[]> WorldToCamRows = new();
        
        [Input(Guid = "6DD0EC68-5B73-4C19-8043-1CA91BBD0AF3")]
        public readonly InputSlot<Vector4[]> CamToClipSpaceRows = new();
        
    }
}