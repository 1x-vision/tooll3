using System;
using System.Numerics;
using SharpDX;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_c3d35057_2544_4b82_b2de_b70fe205662b
{
    public class Shear : Instance<Shear>
    {
        [Output(Guid = "e928344e-02a5-4049-bc43-403dac8d805b")]
        public readonly Slot<Command> Output = new();
        

        public Shear()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var shearing = Translation.GetValue(context);
            
            Matrix4x4 m = Matrix4x4.Identity;
            
            m.M12=shearing.Y; 
            m.M21=shearing.X; 
            m.M14=shearing.Z; 

            var previousWorldTobject = context.ObjectToWorld;
            context.ObjectToWorld = Matrix4x4.Multiply(m, context.ObjectToWorld);
            Command.GetValue(context);
            context.ObjectToWorld = previousWorldTobject;
        }

        [Input(Guid = "26404a8b-3e5e-45ac-b19f-692ab99fa2e1")]
        public readonly InputSlot<Command> Command = new();
        
        [Input(Guid = "c3b260f4-4bd1-4964-8874-a1cf400fa1b9")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new();
    }
}