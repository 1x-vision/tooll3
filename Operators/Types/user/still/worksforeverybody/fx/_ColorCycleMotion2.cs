using T3.Core.DataTypes;
using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7b18587a_e75a_47c0_88de_c92ac6442c4c
{
    public class _ColorCycleMotion2 : Instance<_ColorCycleMotion2>
    {
        [Output(Guid = "0340f641-d087-42e6-831f-ae6083d834b1")]
        public readonly Slot<Command> Result = new Slot<Command>();

        [Input(Guid = "71c3818f-bd7a-4979-bf83-56b56ccea6b0")]
        public readonly InputSlot<Object> CameraReference = new InputSlot<Object>();

        [Input(Guid = "2b794031-698c-461c-9bbc-23125ef475a0")]
        public readonly InputSlot<float> TimeOverride = new InputSlot<float>();

        [Input(Guid = "66bbb8f3-866f-4024-bd73-8e40e7d439da")]
        public readonly InputSlot<float> CycleSpeed = new InputSlot<float>();

        [Input(Guid = "6ab9cc37-e7c2-46c7-803d-5c436eedf64e")]
        public readonly InputSlot<float> ColorPhaseSpread = new InputSlot<float>();

        [Input(Guid = "1d0bee55-4745-4499-b837-ee410e1a1b47")]
        public readonly InputSlot<System.Numerics.Vector3> FakeTranslate = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "1ec97fba-0de0-40c2-a8bc-c9dcfdc5aa32")]
        public readonly InputSlot<System.Numerics.Vector3> FakeRotate = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "e2c0218d-3dda-4770-8b47-e44a6b716c34")]
        public readonly InputSlot<float> FakeAmount = new InputSlot<float>();

        [Input(Guid = "4d3e9f64-8307-4def-bfe6-abc23d2aaf5d")]
        public readonly InputSlot<float> TimeSpread = new InputSlot<float>();

        [Input(Guid = "496edb83-3ce4-4815-b82a-7e3b2ec51cd9")]
        public readonly InputSlot<float> ZOffsetCenterPass = new InputSlot<float>();

        [Input(Guid = "ce87be78-6466-4265-b65c-80811dc9fdd7")]
        public readonly InputSlot<System.Numerics.Vector4> FadeOutColor = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "f8bd71d3-22cc-4b66-822f-6c0bbee6fbb1")]
        public readonly InputSlot<int> LoopCount = new InputSlot<int>();

        [Input(Guid = "78c7396f-3ce8-4565-b043-ec92602c0ae7")]
        public readonly InputSlot<T3.Core.DataTypes.Command> Command = new InputSlot<T3.Core.DataTypes.Command>();

    }
}

