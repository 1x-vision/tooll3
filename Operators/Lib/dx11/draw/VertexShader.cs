using System.Runtime.InteropServices;
using lib.Utils;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using VertexShaderD3D = T3.Core.DataTypes.VertexShader;

namespace lib.dx11.draw
{
	[Guid("646f5988-0a76-4996-a538-ba48054fd0ad")]
    public class VertexShader : Instance<VertexShader>, IDescriptiveFilename, IStatusProvider, IShaderOperator<VertexShaderD3D>
    {
        [Output(Guid = "ED31838B-14B5-4875-A0FC-DC427E874362")]
        public readonly Slot<VertexShaderD3D> Shader = new();

        public VertexShader()
        {
            ShaderOperatorImpl.Initialize();
        }

        public InputSlot<string> SourcePathSlot => Source;


        [Input(Guid = "78FB7501-74D9-4A27-8DB2-596F25482C87")]
        public readonly InputSlot<string> Source = new();

        [Input(Guid = "9A8B500E-C3B1-4BE1-8270-202EF3F90793")]
        public readonly InputSlot<string> EntryPoint = new();

        [Input(Guid = "C8A59CF8-6612-4D57-BCFD-3AEEA351BA50")]
        public readonly InputSlot<string> DebugName = new();
        
        public IEnumerable<string> FileFilter => FileFilters;
        private static readonly string[] FileFilters = ["*.hlsl"];

        #region IShaderOperator implementation
        private IShaderOperator<VertexShaderD3D> ShaderOperatorImpl => this;
        InputSlot<string> IShaderOperator<VertexShaderD3D>.Path => Source;
        InputSlot<string> IShaderOperator<VertexShaderD3D>.EntryPoint => EntryPoint;
        InputSlot<string> IShaderOperator<VertexShaderD3D>.DebugName => DebugName;
        Slot<VertexShaderD3D> IShaderOperator<VertexShaderD3D>.ShaderSlot => Shader;
        #endregion
        
        
        #region IStatusProvider implementation
        private readonly DefaultShaderStatusProvider _statusProviderImplementation = new ();
        public void SetWarning(string message) => _statusProviderImplementation.Warning = message;
        string IShaderOperator<VertexShaderD3D>.CachedEntryPoint { get; set; }
        IStatusProvider.StatusLevel IStatusProvider.GetStatusLevel() => _statusProviderImplementation.GetStatusLevel();
        string IStatusProvider.GetStatusMessage() => _statusProviderImplementation.GetStatusMessage();
        #endregion
    }
}