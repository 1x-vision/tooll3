using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using Vector2 = System.Numerics.Vector2;

namespace T3.Operators.Types.Id_eff2ffff_dc39_4b90_9b1c_3c0a9a0108c6
{
    public class MouseInput : Instance<MouseInput>
    {
        [Output(Guid = "CDC87CE1-FAB8-4B96-9137-9965E064BFA3", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector2> Position = new Slot<Vector2>();
        
        [Output(Guid = "78CAABCF-9C3B-4E50-9D80-BDCBABAEB003", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> IsLeftButtonDown = new Slot<bool>();
        
        public MouseInput()
        {
            Position.UpdateAction = Update;
            IsLeftButtonDown.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var mode = OutputMode.GetEnumValue<OutputModes>(context);
            var aspectRatio = (float)context.RequestedResolution.Width / context.RequestedResolution.Height;
                                         Position.Value = mode == OutputModes.Normalized 
                                                              ? Core.IO.MouseInput.LastPosition
                                                              : (Core.IO.MouseInput.LastPosition - new Vector2(0.5f,0.5f)) * new Vector2(aspectRatio,-1) * 2;
                                         
            IsLeftButtonDown.Value = Core.IO.MouseInput.IsLeftButtonDown;
        }

        [Input(Guid = "49775CC2-35B7-4C9F-A502-59FE8FBBE2A7")]
        public readonly InputSlot<bool> DoUpdate = new InputSlot<bool>();
        
        [Input(Guid = "1327525C-716C-43E4-A5D1-58CF35440462", MappedType = typeof(OutputModes))]
        public readonly InputSlot<int> OutputMode = new ();
        
        private enum OutputModes
        {
            Normalized,
            SignedPosition,
        }
        
    }
}
