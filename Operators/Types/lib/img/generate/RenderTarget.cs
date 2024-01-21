using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Color = SharpDX.Color;
using Device = SharpDX.Direct3D11.Device;
using Utilities = T3.Core.Utils.Utilities;
using Vector4 = System.Numerics.Vector4;

namespace T3.Operators.Types.Id_f9fe78c5_43a6_48ae_8e8c_6cdbbc330dd1
{
    public class RenderTarget : Instance<RenderTarget>, IRenderStatsProvider
    {
        [Output(Guid = "7A4C4FEB-BE2F-463E-96C6-CD9A6BAD77A2")]
        public readonly Slot<Texture2D> ColorBuffer = new();

        [Output(Guid = "8bb0b18f-4fad-4348-a4fa-95b40c4167a4")]
        public readonly Slot<Texture2D> DepthBuffer = new();

        [Output(Guid = "152312A6-729B-49CB-9AC5-A63105694A6B")]
        public readonly Slot<Texture2D> VelocityBuffer = new();

        public RenderTarget()
        {
            ColorBuffer.UpdateAction = Update;
            DepthBuffer.UpdateAction = Update;
            SetupShaderResources();

            lock (_lock)
            {
                if (!_registeredStats)
                {
                    RenderStatsCollector.RegisterProvider(this);
                    _registeredStats = true;
                }
            }
        }

        private const int MaximumTexture2DSize = SharpDX.Direct3D11.Resource.MaximumTexture2DSize;

        private void Update(EvaluationContext context)
        {
            var device = ResourceManager.Device;
            
            Int2 size = Resolution.GetValue(context);
            bool generateMips = GenerateMips.GetValue(context);
            var withDepthBuffer = WithDepthBuffer.GetValue(context);
            var clear = Clear.GetValue(context);
            var clearColor = ClearColor.GetValue(context);
            var reference = TextureReference.GetValue(context);
            var enableUpdate = EnableUpdate.GetValue(context);
            
            if (size.Width == 0 || size.Height == 0)
            {
                size = context.RequestedResolution;
            }
            
            if (size.Width <= 0 || size.Height <= 0 || size.Width > MaximumTexture2DSize || size.Height > MaximumTexture2DSize)
            {
                Log.Warning("Ignoring invalid texture size: " + size, this);
                return;
            }

            _sampleCount = Multisampling.GetValue(context).Clamp(1, 32);

            var textureFormat = TextureFormat.GetValue(context);
            if (textureFormat == Format.Unknown)
            {
                Log.Warning("Texture format unknown is not supported. Falling back to default", this);
                textureFormat = TextureFormat.TypedDefaultValue.Value;
                TextureFormat.SetTypedInputValue(textureFormat);
            }
            
            var formatChanged = UpdateTextures(device, size, textureFormat, withDepthBuffer ? Format.R32_Typeless : Format.Unknown, generateMips);

            if (formatChanged || enableUpdate)
            {
                var deviceContext = device.ImmediateContext;

                // Save settings in context
                var prevRequestedResolution = context.RequestedResolution;
                var prevViewports = deviceContext.Rasterizer.GetViewports<RawViewportF>();
                var prevTargets = deviceContext.OutputMerger.GetRenderTargets(2, out var prevDepthStencilView);
                var prevObjectToWorld = context.ObjectToWorld;
                var prevWorldToCamera = context.WorldToCamera;
                var prevCameraToClipSpace = context.CameraToClipSpace;
                var keepCameraBypass = context.BypassCameras;
                var keepBackgroundColor = context.BackgroundColor;
                var keepForegroundColor = context.ForegroundColor;
                context.BypassCameras = false; 
                context.BackgroundColor = Vector4.One;
                context.ForegroundColor = Vector4.One;
                
                
                deviceContext.Rasterizer.SetViewport(new SharpDX.Viewport(0, 0, size.Width, size.Height, 0.0f, 1.0f));
                deviceContext.OutputMerger.SetTargets(_multiSampledDepthBufferDsv, _multiSampledColorBufferRtv);

                // Clear

                if (clear || !_wasClearedOnce)
                {
                    try
                    {
                        deviceContext.ClearRenderTargetView(_multiSampledColorBufferRtv, new Color(clearColor.X, clearColor.Y, clearColor.Z, clearColor.W));
                        if (_multiSampledDepthBufferDsv != null)
                        {
                            deviceContext.ClearDepthStencilView(_multiSampledDepthBufferDsv, DepthStencilClearFlags.Depth, 1.0f, 0);
                        }

                        _wasClearedOnce = true;
                    } 
                    catch
                    {
                        Log.Error($"{Parent.Symbol.Name}: Error clearing actual render target.", this);
                    }
                }
                
                

                // Set default values for new sub tree
                context.RequestedResolution = size;
                context.SetDefaultCamera();

                if (TextureReference.IsConnected && TextureReference != null)
                {
                    reference.ColorTexture = ColorTexture;
                    reference.DepthTexture = DepthTexture;
                }

                // Render
                Command.GetValue(context);

                // Restore context
                context.BypassCameras = keepCameraBypass;
                context.ObjectToWorld = prevObjectToWorld;
                context.WorldToCamera = prevWorldToCamera;
                context.CameraToClipSpace = prevCameraToClipSpace;
                context.RequestedResolution = prevRequestedResolution;
                context.BackgroundColor = keepBackgroundColor;
                context.ForegroundColor = keepForegroundColor;
                deviceContext.Rasterizer.SetViewports(prevViewports);
                deviceContext.OutputMerger.SetTargets(prevDepthStencilView, prevTargets);
                

                if (_sampleCount > 1)
                {
                    try
                    {
                        device.ImmediateContext.ResolveSubresource(_multiSampledColorBuffer,
                                                                   0,
                                                                   _resolvedColorBuffer, 0,
                                                                   _resolvedColorBuffer.Description.Format);

                        if (withDepthBuffer)
                        {
                            ResolveDepthBuffer();
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("resolving render target buffer failed:" + e.Message, this);
                    }
                }
                
                if (generateMips)
                {
                    try
                    {
                        deviceContext.GenerateMips(DownSamplingRequired ? _resolvedColorBufferSrv : _multiSampledColorBufferSrv);
                    }
                    catch (SEHException e)
                    {
                        Log.Warning("Failed to generate mipmaps: " + e.Message);
                    }
                }

                // Clean up ref counts for RTVs
                for (int i = 0; i < prevTargets.Length; i++)
                {
                    prevTargets[i]?.Dispose();
                }

                prevDepthStencilView?.Dispose();
            }
            
            ColorBuffer.Value = ColorTexture;
            ColorBuffer.DirtyFlag.Clear();
            DepthBuffer.Value = DepthTexture;
            DepthBuffer.DirtyFlag.Clear();
            
            VelocityBuffer.DirtyFlag.Clear();

            _statsCount++;
            _statsCountPixels += size.Height * size.Width;
            if (withDepthBuffer)
            {
                _statsCountPixels += size.Height * size.Width;
            }

            if (_sampleCount > 1)
                _statsCountWithMsaa++;
            
        }
        
        private void SetupShaderResources()
        {
            if (_resolveComputeShaderResource == null)
            {
                string sourcePath = @"Resources\lib\img\internal\resolve-multisampled-depth-buffer-cs.hlsl";
                string entryPoint = "main";
                string debugName = "resolve-multisampled-depth-buffer";
                var resourceManager = ResourceManager.Instance();
                var success = resourceManager.TryCreateShaderResource(out _resolveComputeShaderResource, 
                                                                      fileName: sourcePath, 
                                                                      entryPoint: entryPoint, 
                                                                      name: debugName,
                                                                      errorMessage: out var errorMessage);
                
                if(!string.IsNullOrWhiteSpace(errorMessage))
                    Log.Error($"{nameof(RenderTarget)}: {errorMessage}");
            }
        }
        
        private void ResolveDepthBuffer()
        {
            var resourceManager = ResourceManager.Instance();
            var device = ResourceManager.Device;
            var deviceContext = device.ImmediateContext;
            var csStage = deviceContext.ComputeShader;
            var prevShader = csStage.Get();
            var prevUavs = csStage.GetUnorderedAccessViews(0, 1);
            var prevSrvs = csStage.GetShaderResources(0, 1);

            ComputeShader resolveShader = _resolveComputeShaderResource.Shader;
            csStage.Set(resolveShader);

            const int threadNumX = 16, threadNumY = 16;
            csStage.SetShaderResource(0, _multiSampledDepthBufferSrv);
            csStage.SetUnorderedAccessView(0, _resolvedDepthBufferUav, 0);
            int dispatchCountX = (_multiSampledDepthBuffer.Description.Width / threadNumX) + 1;
            int dispatchCountY = (_multiSampledDepthBuffer.Description.Height / threadNumY) + 1;
            deviceContext.Dispatch(dispatchCountX, dispatchCountY, 1);
            
            // Restore prev setup
            csStage.SetUnorderedAccessView(0, prevUavs[0]);
            csStage.SetShaderResource(0, prevSrvs[0]);
            csStage.Set(prevShader);
        }
        
        
        private bool UpdateTextures(Device device, Int2 size, Format colorFormat, Format depthFormat, bool generateMips)
        {
            int w = Math.Max(size.Width, size.Height);
            int mipLevels = generateMips ? (int)MathUtils.Log2(w) + 1 : 1;
            var multiSampleTexture2dMipLevels=  DownSamplingRequired ? 1 : mipLevels;
            // Log.Debug($"miplevel: {mipLevels}, w: {w}", this);
            bool wasChanged= false;

            bool colorFormatChanged = _multiSampledColorBuffer == null
                                      || _multiSampledColorBuffer.Description.Format != colorFormat
                                      || _multiSampledColorBuffer.Description.MipLevels != multiSampleTexture2dMipLevels
                                      || _multiSampledColorBuffer.Description.Width != size.Width
                                      || _multiSampledColorBuffer.Description.Height != size.Height
                                      || _multiSampledColorBuffer.Description.SampleDescription.Count != _sampleCount;

            //bool useMultiSampling = _sampleCount > 1;

            if (colorFormatChanged)
            {
                wasChanged = true;
                // Color / Multi sampling
                Utilities.Dispose(ref _multiSampledColorBufferSrv);
                Utilities.Dispose(ref _multiSampledColorBufferRtv);
                Utilities.Dispose(ref _multiSampledColorBuffer);

                try
                {
                    
                    var texture2DDescription = new Texture2DDescription
                                                   {
                                                       ArraySize = 1,
                                                       BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                                                       CpuAccessFlags = CpuAccessFlags.None,
                                                       Format = colorFormat,
                                                       Width = size.Width,
                                                       Height = size.Height,
                                                       MipLevels = !DownSamplingRequired ? mipLevels : 1,
                                                       OptionFlags = !DownSamplingRequired && generateMips 
                                                                         ? ResourceOptionFlags.GenerateMipMaps 
                                                                         : ResourceOptionFlags.None,
                                                       SampleDescription = new SampleDescription(_sampleCount, 0),
                                                       Usage = ResourceUsage.Default,
                                                   };
                    _multiSampledColorBuffer = new Texture2D(device, texture2DDescription);

                    _multiSampledColorBufferSrv = new ShaderResourceView(device, _multiSampledColorBuffer);
                    _multiSampledColorBufferRtv = new RenderTargetView(device, _multiSampledColorBuffer,
                                                                       new RenderTargetViewDescription
                                                                           {
                                                                               Format = colorFormat,
                                                                               Dimension = DownSamplingRequired
                                                                                               ? RenderTargetViewDimension.Texture2DMultisampled
                                                                                               : RenderTargetViewDimension.Texture2D
                                                                           });

                    //_multiSampledColorBufferRtv = new RenderTargetView(device, _multiSampledColorBuffer);
                    _wasClearedOnce = false;
                }
                catch (Exception e)
                {
                    Log.Error("Error creating color render target." + e.Message, this);
                    Utilities.Dispose(ref _multiSampledColorBufferSrv);
                    Utilities.Dispose(ref _multiSampledColorBufferRtv);
                    Utilities.Dispose(ref _multiSampledColorBuffer);
                }

                // Color / Down sampled
                if (_resolvedColorBuffer != null)
                {
                    Utilities.Dispose(ref _resolvedColorBufferSrv);
                    Utilities.Dispose(ref _resolvedColorBufferRtv);
                    Utilities.Dispose(ref _resolvedColorBuffer);
                }

                if (DownSamplingRequired)
                {
                    try
                    {
                        _resolvedColorBuffer = new Texture2D(device,
                                                             new Texture2DDescription
                                                                 {
                                                                     ArraySize = 1,
                                                                     BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                                                                     CpuAccessFlags = CpuAccessFlags.None,
                                                                     Format = colorFormat,
                                                                     Width = size.Width,
                                                                     Height = size.Height,
                                                                     MipLevels = mipLevels,
                                                                     OptionFlags =
                                                                         generateMips ? ResourceOptionFlags.GenerateMipMaps : ResourceOptionFlags.None,
                                                                     SampleDescription = new SampleDescription(1, 0),
                                                                     Usage = ResourceUsage.Default
                                                                 });
                        _resolvedColorBufferSrv = new ShaderResourceView(device, _resolvedColorBuffer);
                        _resolvedColorBufferRtv = new RenderTargetView(device, _resolvedColorBuffer);
                        _wasClearedOnce = false;
                    }
                    catch
                    {
                        Log.Error("Error creating color render target.", this);
                        Utilities.Dispose(ref _resolvedColorBufferSrv);
                        Utilities.Dispose(ref _resolvedColorBufferRtv);
                        Utilities.Dispose(ref _resolvedColorBuffer);
                    }
                }

                _wasClearedOnce = false;
            }

            var depthRequired = depthFormat != Format.Unknown;
            var depthInitialized = _multiSampledDepthBuffer != null;

            bool depthFormatChanged = (_multiSampledDepthBuffer == null)
                                      || _multiSampledDepthBuffer.Description.Width != size.Width
                                      || _multiSampledDepthBuffer.Description.Height != size.Height
                                      || _multiSampledDepthBuffer.Description.SampleDescription.Count != _sampleCount
                                      || _multiSampledDepthBuffer.Description.Format != depthFormat;
                
            
            if (depthFormatChanged || (!depthRequired && depthInitialized))
            {
                Utilities.Dispose(ref _multiSampledDepthBufferDsv);
                Utilities.Dispose(ref _multiSampledDepthBufferSrv);
                Utilities.Dispose(ref _multiSampledDepthBuffer);
                Utilities.Dispose(ref _resolvedDepthBufferUav);
                Utilities.Dispose(ref _resolvedDepthBuffer);
            }

            if (depthRequired && (depthFormatChanged || !depthInitialized))
            {
                wasChanged = true;
                // Depth / Multi sampled
                try
                {
                    _multiSampledDepthBuffer = new Texture2D(device,
                                                             new Texture2DDescription
                                                                 {
                                                                     ArraySize = 1,
                                                                     BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                                                                     CpuAccessFlags = CpuAccessFlags.None,
                                                                     Format = Format.R32_Typeless,
                                                                     Width = size.Width,
                                                                     Height = size.Height,
                                                                     MipLevels = DownSamplingRequired ? 1: mipLevels,
                                                                     OptionFlags = ResourceOptionFlags.None,
                                                                     SampleDescription = new SampleDescription(_sampleCount, 0),
                                                                     Usage = ResourceUsage.Default
                                                                 });

                    _multiSampledDepthBufferDsv = new DepthStencilView(device,
                                                                       _multiSampledDepthBuffer,
                                                                       new DepthStencilViewDescription
                                                                           {
                                                                               Format = Format.D32_Float,
                                                                               Dimension = DownSamplingRequired
                                                                                               ? DepthStencilViewDimension.Texture2DMultisampled
                                                                                               : DepthStencilViewDimension.Texture2D
                                                                           });
                    if (DownSamplingRequired)
                    {
                        var viewDesc = new ShaderResourceViewDescription
                                           {
                                               Format = Format.R32_Float,
                                               Dimension = ShaderResourceViewDimension.Texture2DMultisampled
                                           };
                        _multiSampledDepthBufferSrv = new ShaderResourceView(device, _multiSampledDepthBuffer, viewDesc);
                    }
                }
                catch
                {
                    Utilities.Dispose(ref _multiSampledDepthBufferDsv);
                    Utilities.Dispose(ref _multiSampledDepthBufferSrv);
                    Utilities.Dispose(ref _multiSampledDepthBuffer);
                    Log.Error("Error  creating multisampled depth/stencil buffer.", this);
                }

                if (DownSamplingRequired)
                {
                    try
                    {
                        _resolvedDepthBuffer = new Texture2D(device,
                                                             new Texture2DDescription
                                                                 {
                                                                     ArraySize = 1,
                                                                     BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                                                                     CpuAccessFlags = CpuAccessFlags.None,
                                                                     Format = Format.R32_Float,
                                                                     Width = size.Width,
                                                                     Height = size.Height,
                                                                     MipLevels = mipLevels,
                                                                     OptionFlags = ResourceOptionFlags.None,
                                                                     SampleDescription = new SampleDescription(1, 0),
                                                                     Usage = ResourceUsage.Default
                                                                 });

                        _resolvedDepthBufferUav = new UnorderedAccessView(device, _resolvedDepthBuffer);
                    }
                    catch
                    {
                        Utilities.Dispose(ref _resolvedDepthBufferUav);
                        Utilities.Dispose(ref _resolvedDepthBuffer);
                        Log.Error("Error creating depth/stencil downsampling buffer.", this);
                    }
                }
            }

            return wasChanged;
        }

        private enum Samples
        {
            No_MSAA = 1,
            MSAA_2Samples =2,
            MSAA_4Samples = 4,
            MSAA_8Samples = 8,
        }
        
        private Texture2D _multiSampledColorBuffer;
        private ShaderResourceView _multiSampledColorBufferSrv;
        private RenderTargetView _multiSampledColorBufferRtv;

        private Texture2D _resolvedColorBuffer;
        private ShaderResourceView _resolvedColorBufferSrv;
        private RenderTargetView _resolvedColorBufferRtv;

        private Texture2D _multiSampledDepthBuffer;
        private DepthStencilView _multiSampledDepthBufferDsv;
        private ShaderResourceView _multiSampledDepthBufferSrv;

        private Texture2D _resolvedDepthBuffer;
        private UnorderedAccessView _resolvedDepthBufferUav;

        private bool _wasClearedOnce;

        private Texture2D ColorTexture => _sampleCount > 1 ? _resolvedColorBuffer : _multiSampledColorBuffer ;
        private Texture2D DepthTexture => _sampleCount > 1 ? _resolvedDepthBuffer : _multiSampledDepthBuffer;
        private bool DownSamplingRequired => _sampleCount > 1;
        private int _sampleCount;

        [Input(Guid = "4da253b7-4953-439a-b03f-1d515a78bddf")]
        public readonly InputSlot<Command> Command = new();

        [Input(Guid = "03749b41-cc3c-4f38-aea6-d7cea19fc073")]
        public readonly InputSlot<Int2> Resolution = new();

        [Input(Guid = "aacafc4d-f47f-4893-9a6e-98db306a8901")]
        public readonly InputSlot<bool> Clear = new();
        
        [Input(Guid = "8bb4a4e5-0c88-4d99-a5b2-2c9e22bd301f")]
        public readonly InputSlot<System.Numerics.Vector4> ClearColor = new();
        
        [Input(Guid = "E882E0F0-03F9-46E6-AC7A-709E6FA66613", MappedType = typeof(Samples))]
        public readonly InputSlot<int> Multisampling = new();
        
        [Input(Guid = "ec46bef4-8dce-4eb4-bfe8-e35a5ac416ec")]
        public readonly InputSlot<SharpDX.DXGI.Format> TextureFormat = new();

        // [Input(Guid = "2d54adbb-04c7-4f92-babd-9822953aa4cd")]
        // public readonly InputSlot<SharpDX.DXGI.Format> DepthFormat = new InputSlot<SharpDX.DXGI.Format>();
        
        [Input(Guid = "6EA4F801-FF52-4266-A41F-B9EF02C68510")]
        public readonly InputSlot<bool> WithDepthBuffer = new();
        

        [Input(Guid = "f0cf3325-4967-4419-9beb-036cd6dbfd6a")]
        public readonly InputSlot<bool> GenerateMips = new();

        [Input(Guid = "07AD28AD-FF5F-4CA9-B7BB-F7F8B16A6434")]
        public readonly InputSlot<RenderTargetReference> TextureReference = new();
        
        [Input(Guid = "ABEBB02B-BCAA-42C7-8EB8-DA3C1B2FC840")]
        public readonly InputSlot<bool> EnableUpdate = new();

        public IEnumerable<(string, int)> GetStats()
        {
            yield return ("RenderTargets", _statsCount);
            yield return ("with MSAA", _statsCountWithMsaa);
            yield return ("pixels", _statsCountPixels);
        }

        private static readonly object _lock = new ();
        
        public void StartNewFrame()
        {
            _statsCount = 0;
            _statsCountPixels = 0;
            _statsCountWithMsaa = 0;
        }
        
        private static int _statsCount;
        private static int _statsCountWithMsaa;
        private static int _statsCountPixels;
        private static bool _registeredStats;

        private ShaderResource<SharpDX.Direct3D11.ComputeShader> _resolveComputeShaderResource;
    }
}