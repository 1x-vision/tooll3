using SharpDX.Direct3D11;
using SharpDX.DXGI;
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

namespace T3.Operators.Types.Id_25307357_6f6c_45b1_a38d_de635510a845
{
    public class SpoutInput : Instance<SpoutInput>
    {
        [Output(Guid = "10955469-F5C0-476D-8A1B-9CB2803820A9", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> Texture = new();

        public SpoutInput()
        {
            Texture.UpdateAction = Update;
            _instance++;
        }
        ~SpoutInput()
        {
            // FIXME: Dispose OpenGL context correctly
            // Dispose();
        }

        private void Update(EvaluationContext context)
        {
            Command.GetValue(context);
            var receiverName = ReceiverName.GetValue(context);

            if (!ReceiveTexture(receiverName))
                Texture.Value = null;

            // ReceiverName.Update(context);
        }

        private string GetAdapterName(int adapterIndex)
        {
            IntPtr adapterName = Marshal.AllocHGlobal(1024);
            string adapter;
            unsafe
            {
                sbyte* name = (sbyte*)adapterName;
                _spoutDX.GetAdapterName(adapterIndex, name, 1024);
                adapter = new string(name);
            }
            Marshal.FreeHGlobal(adapterName);
            return adapter;
        }

        private string GetSenderAdapter()
        {
            IntPtr adapterName = Marshal.AllocHGlobal(1024);
            string adapter;
            unsafe
            {
                sbyte* name = (sbyte*)adapterName;
                _spoutDX.GetSenderAdapter(_receiverName, name, 1024);
                adapter = new string(name);
            }
            Marshal.FreeHGlobal(adapterName);
            return adapter;
        }

        private unsafe bool GetSenderInfo(ref uint width, ref uint height, ref IntPtr dxSharedHandle, ref uint dwFormat)
        {
            fixed (IntPtr* handle = &dxSharedHandle)
                return _spoutDX.GetSenderInfo(_receiverName, ref width, ref height, handle, ref dwFormat);
        }

        private static Texture2D CreateD3D11Texture2D(Texture2D d3d11Texture2D)
        {
            using (var resource = d3d11Texture2D.QueryInterface<Resource>())
            {
                return ResourceManager.Device.OpenSharedResource<Texture2D>(resource.SharedHandle);
            }
        }

        private bool InitializeSpout(string receiverName, bool useWidthAndHeight, uint width, uint height)
        {
            // TODO: combine OpenGL context creation of Spout input and output
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
                if (_deviceContext == null ||!_deviceContext.MakeCurrent(_glContext))
                    return false;
            }

            // get rid of old textures?
            if ((useWidthAndHeight && (width != _width || height != _height))
                || receiverName != _receiverName)
            {
                DisposeTextures();
            }

            // create new spoutDX object
            if (_spoutDX == null)
            {
                _spoutDX = new SpoutDX.SpoutDX();
                _spoutDX.AdapterAuto = true;
                _spoutDX.OpenDirectX11(_device);

                // set new receiver
                _spoutDX.SetReceiverName(receiverName);
                _spoutDX.CheckSenderFormat(receiverName);
                _receiverName = receiverName;
                ReceiverName.SetTypedInputValue(_receiverName);
            }
            else if (receiverName != _receiverName || !_spoutDX.IsConnected)
            {
                // set new receiver
                _spoutDX.SetReceiverName(receiverName);
                _spoutDX.CheckSenderFormat(receiverName);
                _receiverName = receiverName;
                ReceiverName.SetTypedInputValue(_receiverName);
            }

            // switch to sender adapter if connected and adapter is not automatically chosen
            if (!_adapterSet && _spoutDX != null && _spoutDX.IsConnected && !_spoutDX.AdapterAuto)
            {
                string adapterName = GetSenderAdapter();
                for (var i = 0; i < _spoutDX.NumAdapters; i++)
                {
                    if (GetAdapterName(i) == adapterName)
                    {
                        if (_spoutDX.Adapter != i)
                        {
                            _spoutDX.SetAdapter(i);
                            Console.WriteLine(@$"Spout input switched to sender adapter");
                            break;
                        }
                    }
                }

                Console.WriteLine(@$"Spout input is using adapter {GetAdapterName(_spoutDX.Adapter)}");
                _adapterSet = true;
            }

            return true;
        }

        private bool ReceiveTexture(string receiverName)
        {
            uint width = 0;
            uint height = 0;
            try
            {
                // initialize spout to get width and height
                if (!InitializeSpout(receiverName, false, 0, 0))
                    return false;

                // re-initialize with correct receiver width and height
                if (_spoutDX != null)
                {
                    _spoutDX.ReceiveTexture();
                    width = _spoutDX.SenderWidth;
                    height = _spoutDX.SenderHeight;
                    if (width == 0 || height == 0)
                        return false;

                    if (!InitializeSpout(receiverName, true, width, height))
                        return false;
                }
            }
            catch (Exception e)
            {
                Log.Debug("Initialization of Spout failed. Are Spout.dll and SpoutDX.dll present in the executable folder?");
                Log.Debug(e.ToString());
                _spoutDX?.ReleaseReceiver();
                _spoutDX?.CloseDirectX11();
                _spoutDX?.Dispose();
                _spoutDX = null;
                return false;
            }

            var device = ResourceManager.Device;
            try
            {
                if (!_spoutDX.IsFrameNew)
                    return true;

                Texture2D readTexture = new Texture2D(_spoutDX.SenderTexture.__Instance);

                // check the input format
                uint senderWidth = 0;
                uint senderHeight = 0;
                IntPtr senderHandle = IntPtr.Zero;
                uint directXFormat = 0;

                // default to RGBA input
                Format textureFormat = Format.R8G8B8A8_UNorm;
                // check the currently received format
                if (GetSenderInfo(ref senderWidth, ref senderHeight, ref senderHandle, ref directXFormat))
                {
                    // if a format is provided, use that,
                    // otherwise use the format provided in the image that was received
                    if (directXFormat != 0)
                        textureFormat = (Format)directXFormat;
                    else
                        textureFormat = readTexture.Description.Format;
                }

                // create several textures with a given format with CPU access
                // to be able to read out the initial texture values
                if (ImagesWithGpuAccess.Count == 0
                    || ImagesWithGpuAccess[0].Description.Format != textureFormat
                    || ImagesWithGpuAccess[0].Description.Width != (int)width
                    || ImagesWithGpuAccess[0].Description.Height != (int)height
                    || ImagesWithGpuAccess[0].Description.MipLevels != 1)
                {
                    var imageDesc = new Texture2DDescription
                    {
                        BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                        Format = textureFormat,
                        Width = (int)width,
                        Height = (int)height,
                        MipLevels = 1,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        OptionFlags = ResourceOptionFlags.None,
                        CpuAccessFlags = CpuAccessFlags.None,
                        ArraySize = 1
                    };

                    DisposeTextures();

                    Log.Debug($"Spout input wxh = {senderWidth}x{senderHeight}, " +
                              $"handle = {senderHandle}, " +
                              $"format = {textureFormat} ({directXFormat})");

                    for (var i = 0; i < NumTextureEntries; ++i)
                    {
                        ImagesWithGpuAccess.Add(new Texture2D(device, imageDesc));
                    }
                    _width = width;
                    _height = height;

                    _currentIndex = 0;
                }

                // sanity check
                if (_spoutDX == null || width == 0 || height == 0 || _width != width || _height != height)
                    return false;

                // copy the spout texture to an internal image
                var immediateContext = device.ImmediateContext;
                var readableImage = ImagesWithGpuAccess[_currentIndex];
                immediateContext.CopyResource(readTexture, readableImage);
                _currentIndex = (_currentIndex + 1) % NumTextureEntries;

                Texture.Value = readableImage;
            }
            catch (Exception e)
            {
                Log.Debug("Spout input texture reception failed: " + e.ToString());
            }

            return true;
        }

        protected void DisposeTextures()
        {
            foreach (var image in ImagesWithGpuAccess)
                image?.Dispose();

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
            _adapterSet = false;

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
        private static bool _adapterSet;                // was adapter set to sender's adapter?
        private string _receiverName;                   // name of our spout receiver
        private uint _width;                            // current width of our receiver
        private uint _height;                           // current height of our receiver

        // hold several textures internally to speed up calculations
        private const int NumTextureEntries = 2;
        private readonly List<Texture2D> ImagesWithGpuAccess = new();
        // current image index (used for circular access of ImagesWithGpuAccess)
        private int _currentIndex;

        [Input(Guid = "F7A7A410-FB91-448C-A0FE-BA4278D82107")]
        public readonly InputSlot<Command> Command = new();

        [Input(Guid = "C5C487D2-3EC0-49E3-9EA5-0B1238E82233")]
        public InputSlot<string> ReceiverName = new();
    }
}
