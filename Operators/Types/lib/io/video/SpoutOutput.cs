using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpGLTF.Schema2;
using SpoutDX;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using DeviceContext = OpenGL.DeviceContext;
using Resource = SharpDX.DXGI.Resource;

namespace T3.Operators.Types.Id_13be1e3f_861d_4350_a94e_e083637b3e55
{
    public class SpoutOutput : Instance<SpoutOutput>
    {
        [Output(Guid = "297ea260-4486-48f8-b58f-8180acf0c2c5", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> TextureOutput = new();

        public SpoutOutput()
        {
            TextureOutput.UpdateAction = Update;
            _instance++;
        }
        ~SpoutOutput()
        {
            // FIXME: Dispose OpenGL context correctly
            // Dispose();
        }

        private void Update(EvaluationContext context)
        {
            Command.GetValue(context);
            var texture = Texture.GetValue(context);
            var senderName = SenderName.GetValue(context);

            TextureOutput.Value = texture;
            SendTexture(senderName, ref texture);
            SenderName.Update(context);
        }

        private string GetAdapterName()
        {
            IntPtr adapterName = Marshal.AllocHGlobal(1024);
            string adapter;
            unsafe
            {
                sbyte* name = (sbyte*)adapterName;
                _spoutDX.GetAdapterName(_spoutDX.Adapter, name, 1024);
                adapter = new string(name);
            }
            Marshal.FreeHGlobal(adapterName);
            return adapter;
        }

        private Texture2D CreateD3D11Texture2D(Texture2D d3d11Texture2D)
        {
            using (var resource = d3d11Texture2D.QueryInterface<Resource>())
            {
                return ResourceManager.Device.OpenSharedResource<Texture2D>(resource.SharedHandle);
            }
        }

        private bool InitializeSpout(string senderName, uint width, uint height)
        {
            if (!_initialized)
            {
                // create OpenGL context and make this become the primary context
                _deviceContext = DeviceContext.Create();
                _glContext = DeviceContext.GetCurrentContext();
                if (_glContext == IntPtr.Zero)
                {
                    _glContext = _deviceContext.CreateContext(IntPtr.Zero);
                    _deviceContext.MakeCurrent(_glContext);
                }
                _device = ID3D11Device.__CreateInstance(((IntPtr)ResourceManager.Device));
                _initialized = true;
            }
            else if (_glContext != DeviceContext.GetCurrentContext())
            {
                // Make this become the primary context
                if (_deviceContext == null || !_deviceContext.MakeCurrent(_glContext))
                    return false;
            }

            // get rid of old textures?
            if (width != _width || height != _height || senderName != _senderName)
            {
                DisposeTextures();

                if (_spoutDX != null)
                {
                    _spoutDX.CloseDirectX11();
                    _spoutDX.Dispose();
                    _spoutDX = null;
                }
                _width = 0;
                _height = 0;
            }

            // create new spoutDX object
            if (_spoutDX == null)
            {
                _spoutDX = new SpoutDX.SpoutDX();
                _spoutDX.OpenDirectX11(_device);
                Console.WriteLine(@$"Spout output is using adapter {GetAdapterName()}");

                // create new sender and read back the actual name chosen by spout
                // (which may be different if you have multiple senders of the same name)
                _spoutDX.SenderName = senderName;
                _senderName = _spoutDX.SenderName;
                SenderName.SetTypedInputValue(_senderName);
                _width = width;
                _height = height;
            }

            return true;
        }

        private bool SendTexture(string senderName, ref Texture2D frame)
        {
            if (frame == null)
                return false;

            int width, height;
            Texture2DDescription currentDesc;
            try
            {
                currentDesc = frame.Description;
                if (currentDesc.Format != Format.B8G8R8A8_UNorm &&
                    currentDesc.Format != Format.R8G8B8A8_UNorm &&
                    currentDesc.Format != Format.R16G16B16A16_UNorm &&
                    currentDesc.Format != Format.R16G16B16A16_Float)
                {
                    Log.Debug("Spout output supports texture formats B8G8R8A8_UNorm, R8G8B8A8_UNorm, R16G16B16A16_UNorm and R16G16B16A16_Float.");
                    Log.Debug("Please use a render target operator to change the format accordingly.");
                    return false;
                }
                width = currentDesc.Width;
                height = currentDesc.Height;
                if (!InitializeSpout(senderName, (uint)width, (uint)height))
                    return false;
            }
            catch (Exception e)
            {
                Log.Debug("Initialization of Spout failed. Are Spout.dll and SpoutDX.dll present in the executable folder?");
                Log.Debug(e.ToString());
                _spoutDX?.ReleaseSender();
                _spoutDX?.CloseDirectX11();
                _spoutDX?.Dispose();
                _spoutDX = null;
                return false;
            }

            var device = ResourceManager.Device;
            try
            {
                // create several textures with a given format with CPU access
                // to be able to read out the initial texture values
                if (ImagesWithGpuAccess.Count == 0
                    || ImagesWithGpuAccess[0].Description.Format != currentDesc.Format
                    || ImagesWithGpuAccess[0].Description.Width != currentDesc.Width
                    || ImagesWithGpuAccess[0].Description.Height != currentDesc.Height
                    || ImagesWithGpuAccess[0].Description.MipLevels != 1)
                {
                    var imageDesc = new Texture2DDescription
                    {
                        BindFlags = BindFlags.ShaderResource,
                        Format = currentDesc.Format,
                        Width = currentDesc.Width,
                        Height = currentDesc.Height,
                        MipLevels = 1,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        OptionFlags = ResourceOptionFlags.Shared,
                        CpuAccessFlags = CpuAccessFlags.None,
                        ArraySize = 1
                    };

                    DisposeTextures();

                    for (int i = 0; i < NumTextureEntries; ++i)
                    {
                        ImagesWithGpuAccess.Add(new Texture2D(device, imageDesc));
                    }
                    _currentIndex = 0;
                }

                // sanity check
                if (_spoutDX == null || width == 0 || height == 0 || _width != width || _height != height)
                    return false;

                // copy the original texture to a readable image
                var immediateContext = device.ImmediateContext;
                var readableImage = ImagesWithGpuAccess[_currentIndex];
                immediateContext.CopyResource(frame, readableImage);
                _currentIndex = (_currentIndex + 1) % NumTextureEntries;

                _texture = ID3D11Texture2D.__CreateInstance(((IntPtr)readableImage));
                _spoutDX.SendTexture(_texture);
            }
            catch (Exception e)
            {
                Log.Debug("Texture sending failed : " + e.ToString());
            }
            finally
            {
                _texture?.Dispose();
            }

            return true;
        }

        protected void DisposeTextures()
        {
            foreach (var image in ImagesWithGpuAccess)
                image.Dispose();

            ImagesWithGpuAccess.Clear();
        }

        #region IDisposable Support

        public new void Dispose()
        {
            // dispose textures
            DisposeTextures();

            _spoutDX?.ReleaseSender();
            _spoutDX?.CloseDirectX11();
            _spoutDX?.Dispose();

            if (_instance > 0)
                --_instance;

            if (_instance <= 0)
            {
                _device?.Dispose();
                _deviceContext?.MakeCurrent(IntPtr.Zero);
                _deviceContext?.Dispose();
                _initialized = false;
            }
        }

        #endregion

        private static int _instance;                   // number of instances of this object
        private static bool _initialized;               // were static members initialized?
        private static DeviceContext _deviceContext;    // OpenGL device context
        private static IntPtr _glContext;               // OpenGL context
        private static ID3D11Device _device;            // Direct3D11 device

        private SpoutDX.SpoutDX _spoutDX;               // spout object supporting DirectX
        private string _senderName;                     // name of our spout sender
        private uint _width;                            // current width of our sender
        private uint _height;                           // current height of our sender
        private ID3D11Texture2D _texture;               // texture to send

        // hold several textures internally to speed up calculations
        private const int NumTextureEntries = 2;
        private readonly List<Texture2D> ImagesWithGpuAccess = new();
        // current image index (used for circular access of ImagesWithGpuAccess)
        private int _currentIndex;

        [Input(Guid = "FE61FF9E-7F1B-4F69-9F4B-313F30B57124")]
        public readonly InputSlot<Command> Command = new();

        [Input(Guid = "d4b5c642-9cb9-4f41-8739-edbb9c6c4857")]
        public readonly InputSlot<Texture2D> Texture = new();

        [Input(Guid = "7C27EBD7-3746-4B70-A252-DD0AC0445B74")]
        public InputSlot<string> SenderName = new();
    }
}