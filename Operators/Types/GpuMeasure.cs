using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Utils;
using Utilities = T3.Core.Utilities;

namespace T3.Operators.Types.Id_000e08d0_669f_48df_9083_7aa0a43bbc05
{
    public class GpuMeasure : Instance<GpuMeasure>
    {
        [Output(Guid = "a506c67c-2e17-49ef-9ac3-990404ce76eb")]
        public readonly Slot<Command> Output = new Slot<Command>();

        public GpuMeasure()
        {
            Output.UpdateAction = Update;

            _d3dDevice = ResourceManager.Instance().Device;
            _queryTimeStampDisjoint = new GpuQuery(_d3dDevice, new QueryDescription() { Type = QueryType.TimestampDisjoint });
            _queryTimeStampFrameBegin = new GpuQuery(_d3dDevice, new QueryDescription() { Type = QueryType.Timestamp });
            _queryTimeStampFrameEnd = new GpuQuery(_d3dDevice, new QueryDescription() { Type = QueryType.Timestamp });
        }

        protected override void Dispose(bool disposing)
        {
            Utilities.Dispose(ref _queryTimeStampDisjoint);
            Utilities.Dispose(ref _queryTimeStampFrameBegin);
            Utilities.Dispose(ref _queryTimeStampFrameEnd);
        }

        private void Update(EvaluationContext context)
        {
            var deviceContext = _d3dDevice.ImmediateContext;
            bool enabled = Enabled.GetValue(context);
            bool logToConsole = LogToConsole.GetValue(context);

            if (enabled && _readyToMeasure)
            {
                _queryTimeStampDisjoint.Begin(deviceContext);
                _queryTimeStampFrameBegin.End(deviceContext);
            }

            Command.GetValue(context);

            if (!enabled)
                return;

            if (_readyToMeasure)
            {
                _queryTimeStampFrameEnd.End(deviceContext);
                _queryTimeStampDisjoint.End(deviceContext);
                _readyToMeasure = false;
            }
            else
            {
                // check if last measurement is ready
                bool dataFetched = true;
                dataFetched &= _queryTimeStampDisjoint.GetData(deviceContext, AsynchronousFlags.None, out QueryDataTimestampDisjoint disjointData);
                dataFetched &= _queryTimeStampFrameBegin.GetData(deviceContext, AsynchronousFlags.None, out long timeStampframeBegin);
                dataFetched &= _queryTimeStampFrameEnd.GetData(deviceContext, AsynchronousFlags.None, out long timeStampframeEnd);

                if (dataFetched && !disjointData.Disjoint)
                {
                    float durationInS = (float)(timeStampframeEnd - timeStampframeBegin) / disjointData.Frequency;
                    int usDuration = (int)(durationInS * 1000f * 1000f);
                    if (logToConsole)
                    {
                        Log.Debug($" localTime: {context.LocalTime:0.00}  GPUMeasure {usDuration}us on GPU.");
                    }
                    LastMeasureInMicroSeconds = usDuration;
                    _readyToMeasure = true;
                }

                LastMeasureInMs = MathUtils.Lerp(LastMeasureInMs, (float)(LastMeasureInMicroSeconds / 1000.0), 0.03f);
            }
        }

        public int LastMeasureInMicroSeconds { get; private set; }
        
        public float LastMeasureInMs { get; private set; }

        private readonly Device _d3dDevice;
        private GpuQuery _queryTimeStampDisjoint;
        private GpuQuery _queryTimeStampFrameBegin;
        private GpuQuery _queryTimeStampFrameEnd;
        private bool _readyToMeasure = true;

        [Input(Guid = "76c017f1-5474-44a5-b881-d581f7038ca5")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();

        [Input(Guid = "0c7ec1ae-e8d0-4acb-8050-44768f827b56")]
        public readonly InputSlot<bool> Enabled = new InputSlot<bool>();

        [Input(Guid = "e430cc80-9003-4a56-af5d-f5820434c074")]
        public readonly InputSlot<bool> LogToConsole = new InputSlot<bool>();
    }
}