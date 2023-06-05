﻿using System.Collections.Generic;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_9b28e6b9_1d1f_42d8_8a9e_33497b1df820
{
    public class Draw : Instance<Draw>, IRenderStatsProvider
    {
        [Output(Guid = "49B28DC3-FCD1-4067-BC83-E1CC848AE55C")]
        public readonly Slot<Command> Output = new Slot<Command>();

        
        public Draw()
        {
            lock (_lock)
            {
                if (!_registeredForStats)
                {
                    RenderStatsCollector.RegisterProvider(this);
                    _registeredForStats = true;
                }
            }
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            //Log.Debug("Draw2", this);
            var vertexCount = VertexCount.GetValue(context);
            var startVertexLocation = VertexStartLocation.GetValue(context);
            
            var device = ResourceManager.Device;
            var deviceContext = device.ImmediateContext;

            var setVs = deviceContext.VertexShader.Get();
            var setPs = deviceContext.PixelShader.Get();
            
            if (setVs == null || setPs == null)
            {
                if (!_complainedOnce)
                {
                    Log.Warning("Trying to issue draw call, but pixel and/or vertex shader are null.");
                }
                _complainedOnce = true;
                return;
            }

            _vertexCount += vertexCount;
            _drawCallCount++;

            _complainedOnce = false;

            deviceContext.Draw(vertexCount, startVertexLocation);
        }

        protected override void Dispose(bool disposing)
        {
            RenderStatsCollector.UnregisterProvider(this);
            base.Dispose(disposing);
        }

        public IEnumerable<(string, int)> GetStats()
        {
            yield return ("Triangles", _vertexCount/3);
            yield return ("Draw calls", _drawCallCount);
        }
        

        public void StartNewFrame()
        {
            _vertexCount = 0;
            _drawCallCount = 0;
        }

        private bool _complainedOnce;
        private static int _vertexCount;
        private static int _drawCallCount;
        private static bool _registeredForStats;
        private static readonly Dictionary<string, int> _statResults = new();
        private static readonly object _lock = new ();
        
        [Input(Guid = "8716B11A-EF71-437E-9930-BB747DA818A7")]
        public readonly InputSlot<int> VertexCount = new InputSlot<int>();
        [Input(Guid = "B381B3ED-F043-4001-9BBC-3E3915F38235")]
        public readonly InputSlot<int> VertexStartLocation = new InputSlot<int>();

    }
}