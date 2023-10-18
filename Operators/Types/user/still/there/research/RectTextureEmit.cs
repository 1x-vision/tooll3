using T3.Core.DataTypes;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_c6911113_9411_4706_ad16_9e7bf58ad6c6
{
    public class RectTextureEmit : Instance<RectTextureEmit>
    {
        [Output(Guid = "4efe1aa1-fc4c-495d-a25d-bcffe6491611")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "7d0e8a44-367c-4eb0-8fa3-d26a67a5ec35")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "d9e528ba-0aa1-42b2-8169-984d7a340228")]
        public readonly InputSlot<int> EmitCountPerFrame = new InputSlot<int>();

        [Input(Guid = "765b2330-777c-4b7d-bfa0-15f4701bedae")]
        public readonly InputSlot<T3.Core.DataTypes.LegacyParticleSystem> ParticleSystem = new InputSlot<T3.Core.DataTypes.LegacyParticleSystem>();

        [Input(Guid = "ddac1768-0073-4158-9929-9c309d902429")]
        public readonly InputSlot<System.Numerics.Vector2> LifeTime = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "7c63bf62-11d4-48fa-bbd8-2b0c88377ed6")]
        public readonly InputSlot<System.Numerics.Vector2> SizeWithRandom = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "2735e77e-ac99-4290-81b3-9b6a51c9a299")]
        public readonly InputSlot<int> EmitterId = new InputSlot<int>();

        [Input(Guid = "685ade87-f447-421d-af32-3098026f311b")]
        public readonly InputSlot<System.Numerics.Vector3> Velocity = new InputSlot<System.Numerics.Vector3>();

    }
}

