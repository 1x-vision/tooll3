﻿using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_a9600440_4203_4315_bdb1_4dfd603b4515
{
    public class VertexShaderStage : Instance<VertexShaderStage>
    {
        [Output(Guid = "65B394A9-06DC-4D9B-8819-15394EDE2997")]
        public readonly Slot<Command> Output = new Slot<Command>(new Command());

        public VertexShaderStage()
        {
            Output.UpdateAction = Update;
            Output.Value.RestoreAction = Restore;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var device = ResourceManager.Device;
            var deviceContext = device.ImmediateContext;
            var vsStage = deviceContext.VertexShader;

            // ConstantBuffers.GetValues(ref _constantBuffers, context, clearDirty:false);
            // ShaderResources.GetValues(ref _shaderResourceViews, context, clearDirty:false);
            // SamplerStates.GetValues(ref _samplerStates, context, clearDirty:false);

            ConstantBuffers.GetValues(ref _constantBuffers, context);
            ShaderResources.GetValues(ref _shaderResourceViews, context);
            SamplerStates.GetValues(ref _samplerStates, context);

            _prevConstantBuffers = vsStage.GetConstantBuffers(0, _constantBuffers.Length);
            _prevShaderResourceViews = vsStage.GetShaderResources(0, _shaderResourceViews.Length);
            _prevVertexShader = vsStage.Get();

            var vs = VertexShader.GetValue(context);
            if (vs == null)
                return;

            vsStage.Set(vs);
            vsStage.SetConstantBuffers(0, _constantBuffers.Length, _constantBuffers);
            vsStage.SetShaderResources(0, _shaderResourceViews.Length, _shaderResourceViews);
        }

        private void Restore(EvaluationContext context)
        {
            var deviceContext = ResourceManager.Device.ImmediateContext;
            var vsStage = deviceContext.VertexShader;
            vsStage.Set(_prevVertexShader);
            vsStage.SetConstantBuffers(0, _prevConstantBuffers.Length, _prevConstantBuffers);
            vsStage.SetShaderResources(0, _prevShaderResourceViews.Length, _prevShaderResourceViews);
        }

        private Buffer[] _constantBuffers = new Buffer[0];
        private ShaderResourceView[] _shaderResourceViews = new ShaderResourceView[0];
        private SamplerState[] _samplerStates = new SamplerState[0];

        private SharpDX.Direct3D11.VertexShader _prevVertexShader;
        private Buffer[] _prevConstantBuffers;
        private ShaderResourceView[] _prevShaderResourceViews;

        [Input(Guid = "B1C236E5-6757-4D77-9911-E3ACD5EA9FE9")]
        public readonly InputSlot<SharpDX.Direct3D11.VertexShader> VertexShader = new InputSlot<SharpDX.Direct3D11.VertexShader>();

        [Input(Guid = "BBA8F6EB-7CFF-435B-AB47-FEBF58DD8FBA")]
        public readonly MultiInputSlot<Buffer> ConstantBuffers = new MultiInputSlot<Buffer>();

        [Input(Guid = "3A0BEA89-BD93-4594-B1B6-3E25689C67E6")]
        public readonly MultiInputSlot<ShaderResourceView> ShaderResources = new MultiInputSlot<ShaderResourceView>();

        [Input(Guid = "2BC7584D-A347-4954-9120-C1841AF76650")]
        public readonly MultiInputSlot<SamplerState> SamplerStates = new MultiInputSlot<SamplerState>();
    }
}