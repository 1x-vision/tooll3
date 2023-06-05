using T3.Core.DataTypes;
using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9df0e38e_ccf1_405a_ab18_6586e652cdf1
{
    public class _ParamBlendingOverlay : Instance<_ParamBlendingOverlay>
    {
        [Output(Guid = "9158ca50-6368-4266-9985-0f60b3e2b560")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "d19f4570-225e-4de5-b86e-3499011dd4e1")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();

        [Input(Guid = "2b3dc8be-2ead-40fa-8dab-0f85bb470815")]
        public readonly InputSlot<bool> PreventPlaybackControl = new InputSlot<bool>();

        [Input(Guid = "76343cef-1851-4627-b0a7-477e0b33915c")]
        public readonly InputSlot<bool> DarkOnWhite = new InputSlot<bool>();

        [Input(Guid = "c88b36f7-d8c1-4fa6-9fd1-b002f9850196")]
        public readonly InputSlot<int> SceneIndex = new InputSlot<int>();

        [Output(Guid = "2c1800cd-ea63-4cd5-baf4-582a8127d50f")]
        public readonly Slot<float> NewPlaybackSpeed = new Slot<float>();

        [Output(Guid = "5afc9299-b49a-4d5c-9649-d9aa47604be0")]
        public readonly Slot<bool> DoSetPlaybackSpeed = new Slot<bool>();


    }
}

