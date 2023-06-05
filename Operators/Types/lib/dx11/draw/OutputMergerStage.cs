﻿using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_5efaf208_ba62_42ce_b3df_059b37fc1382 {
    public class OutputMergerStage : Instance<OutputMergerStage> {
        [Output(Guid = "CEE8C3F0-64EA-4E4D-B967-EC7E3688DD03")]
        public readonly Slot<Command> Output = new Slot<Command>(new Command());

        public OutputMergerStage() {
            Output.UpdateAction = Update;
            Output.Value.RestoreAction = Restore;
        }

        private void Update(EvaluationContext context) {
            var resourceManager = ResourceManager.Instance();
            var device = ResourceManager.Device;
            var deviceContext = device.ImmediateContext;
            var outputMerger = deviceContext.OutputMerger;

            DepthStencilView.GetValue(context);
            DepthStencilReference.GetValue(context);
            BlendFactor.GetValue(context);
            BlendSampleMask.GetValue(context);
            
            
            RenderTargetViews.GetValues(ref _renderTargetViews, context);
            UnorderedAccessViews.GetValues(ref _unorderedAccessViews, context);

            _prevRenderTargetViews = outputMerger.GetRenderTargets(_renderTargetViews.Length);
            outputMerger.GetRenderTargets(out _prevDepthStencilView);
            outputMerger.SetDepthStencilState(DepthStencilState.GetValue(context));
            _prevBlendState = outputMerger.GetBlendState(out _prevBlendFactor, out _prevSampleMask);
            if (_renderTargetViews.Length > 0)
                outputMerger.SetRenderTargets(null, _renderTargetViews);
            
            if (_unorderedAccessViews.Length > 0)
            {
                outputMerger.SetUnorderedAccessViews(1, _unorderedAccessViews);
            }

            outputMerger.BlendState = BlendState.GetValue(context);
        }

        private void Restore(EvaluationContext context) {
            var deviceContext = ResourceManager.Device.ImmediateContext;
            var outputMerger = deviceContext.OutputMerger;

            outputMerger.BlendState = _prevBlendState;
            if (_renderTargetViews.Length > 0)
                outputMerger.SetRenderTargets(_prevDepthStencilView, _prevRenderTargetViews);
            foreach (var rtv in _prevRenderTargetViews)
                rtv?.Dispose();

            // if (_unorderedAccessViews.Length > 0)
                // outputMerger.SetUnorderedAccessViews(1, _prevUnorderedAccessViews);
            // foreach (var uav in _prevUnorderedAccessViews)
                // uav?.Dispose();

            Utilities.Dispose(ref _prevDepthStencilView);
            _prevBlendState = null;
        }

        private RenderTargetView[] _renderTargetViews = new RenderTargetView[0];
        private RenderTargetView[] _prevRenderTargetViews;
        private UnorderedAccessView[] _unorderedAccessViews = new UnorderedAccessView[0];
        //private UnorderedAccessView[] _prevUnorderedAccessViews;
        private DepthStencilView _prevDepthStencilView;
        private BlendState _prevBlendState;
        private RawColor4 _prevBlendFactor;
        private int _prevSampleMask;

        [Input(Guid = "394D374F-2125-4ECB-8A69-CC7B2C3C6CB7")]
        public readonly InputSlot<DepthStencilView> DepthStencilView = new InputSlot<DepthStencilView>();

        [Input(Guid = "9C131DA6-AD56-4E15-9730-754096B3B765")]
        public readonly MultiInputSlot<RenderTargetView> RenderTargetViews = new MultiInputSlot<RenderTargetView>();

        [Input(Guid = "0ED97939-643B-445E-879C-E18C4430AA03")]
        public readonly MultiInputSlot<UnorderedAccessView> UnorderedAccessViews = new MultiInputSlot<UnorderedAccessView>();

        [Input(Guid = "1D5FAAD5-3BE5-426C-B464-AD490EA3D1AA")]
        public readonly InputSlot<DepthStencilState> DepthStencilState = new InputSlot<DepthStencilState>();

        [Input(Guid = "6C7907D7-70F7-4DB7-83EA-22EE48610994")]
        public readonly InputSlot<int> DepthStencilReference = new InputSlot<int>();

        [Input(Guid = "E0BC9CF8-42C8-4632-B958-7A96F6D03BA2")]
        public readonly InputSlot<BlendState> BlendState = new InputSlot<BlendState>();

        [Input(Guid = "CCEE2EC3-586F-4396-8B20-CC99484E1B64")]
        public readonly InputSlot<System.Numerics.Vector4> BlendFactor = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "03166157-1E18-4513-8AF5-398C6F4FCB1E")]
        public readonly InputSlot<int> BlendSampleMask = new InputSlot<int>();
    }
}