﻿using System.Diagnostics;
using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.DataTypes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Editor.App;
using T3.Editor.Gui.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using ComputeShader = SharpDX.Direct3D11.ComputeShader;

namespace T3.Editor.Gui.OutputUi
{
    public class Texture3dOutputUi : OutputUi<Texture3dWithViews>
    {
        public Texture3dOutputUi()
        {
            const string sourcePath = @"internal\render-volume-slice-cs.hlsl";
            const string entryPoint = "main";
            const string debugName = "render-volume-slice";
            var resourceManager = ResourceManager.Instance();
            var success= resourceManager.TryCreateShaderResource(resource: out _shaderResource, 
                                                                                relativePath: sourcePath, 
                                                                                instance: null,
                                                                                errorMessage: out var errorMessage, 
                                                                                name: debugName, 
                                                                                entryPoint: entryPoint, 
                                                                                fileChangedAction: null);

            var texDesc = new Texture2DDescription()
                              {
                                  ArraySize = 1,
                                  BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                                  CpuAccessFlags = CpuAccessFlags.None,
                                  Format = Format.R8G8B8A8_UNorm,
                                  Width = 256,
                                  Height = 256,
                                  MipLevels = 1,
                                  OptionFlags = ResourceOptionFlags.None,
                                  SampleDescription = new SampleDescription(1, 0),
                                  Usage = ResourceUsage.Default,
                              };
            _viewTexture = new Texture2D(ResourceManager.Device, texDesc);
            _viewTextureUav = new UnorderedAccessView(ResourceManager.Device, _viewTexture);
        }

        public override IOutputUi Clone()
        {
            return new Texture3dOutputUi()
                       {
                           OutputDefinition = OutputDefinition,
                           PosOnCanvas = PosOnCanvas,
                           Size = Size
                       };
        }

        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<Texture3dWithViews> typedSlot)
            {
                Texture3dWithViews texture3d = typedSlot.Value;
                Texture2D texture = RenderTo2dTexture(texture3d);
                ImageOutputCanvas.Current.DrawTexture(texture);
                ProgramWindows.Viewer?.SetTexture(texture);
                ImGui.SliderInt("z-pos", ref _zPosIndex, 0, texture3d?.Texture?.Description.Depth - 1 ?? 0);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private Texture2D RenderTo2dTexture(Texture3dWithViews texture3d)
        {
            if (texture3d?.Texture == null)
                return null;

            var resourceManager = ResourceManager.Instance();
            var device = ResourceManager.Device;
            var deviceContext = device.ImmediateContext;
            var csStage = deviceContext.ComputeShader;
            var prevShader = csStage.Get();
            var prevUavs = csStage.GetUnorderedAccessViews(0, 1);
            var prevSrvs = csStage.GetShaderResources(0, 1);
            var prevConstBuffer = csStage.GetConstantBuffers(0, 1);

            ComputeShader resolveShader = _shaderResource.Shader;
            csStage.Set(resolveShader);

            Int4 parameter = new Int4(_zPosIndex, 0, 0, 0);
            ResourceManager.SetupConstBuffer(parameter, ref _paramBuffer);
            
            const int threadNumX = 16, threadNumY = 16;
            csStage.SetShaderResource(0, texture3d.Srv);
            csStage.SetUnorderedAccessView(0, _viewTextureUav, 0);
            csStage.SetConstantBuffer(0, _paramBuffer);
            int dispatchCountX = texture3d.Texture.Description.Width / threadNumX;
            int dispatchCountY = texture3d.Texture.Description.Height / threadNumY;
            deviceContext.Dispatch(dispatchCountX, dispatchCountY, 1);

            // Restore prev setup
            csStage.SetConstantBuffer(0, prevConstBuffer[0]);
            csStage.SetUnorderedAccessView(0, prevUavs[0]);
            csStage.SetShaderResource(0, prevSrvs[0]);
            csStage.Set(prevShader);

            return _viewTexture;
        }

        private readonly Texture2D _viewTexture = null;
        private UnorderedAccessView _viewTextureUav = null;
        private int _zPosIndex = 0;
        private Buffer _paramBuffer = null;
        private ShaderResource<ComputeShader> _shaderResource;
    }
}