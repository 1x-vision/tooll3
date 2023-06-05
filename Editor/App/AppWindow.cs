﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using T3.Editor.Gui.Styling;
using Device = SharpDX.Direct3D11.Device;
using Icon = System.Drawing.Icon;
using Resource = SharpDX.Direct3D11.Resource;

namespace T3.Editor.App
{
    /// <summary>
    /// Functions and properties related to rendering DX11 content into  RenderForm windows
    /// </summary>
    internal class AppWindow
    {
        public IntPtr HwndHandle => Form.Handle;
        public System.Numerics.Vector2 Size => new(Width, Height);
        public int Width => Form.ClientSize.Width;
        public int Height => Form.ClientSize.Height;
        public bool IsFullScreen => Form.FormBorderStyle == FormBorderStyle.None;
        
        internal SwapChain SwapChain { get => _swapChain; private set => _swapChain = value; }
        internal RenderTargetView RenderTargetView { get => _renderTargetView; private set => _renderTargetView = value; }
        internal ImGuiDx11RenderForm Form { get; private set; }
        internal SwapChainDescription SwapChainDescription { get; private set; }

        internal bool IsMinimized => Form.WindowState == FormWindowState.Minimized;
        internal bool IsCursorOverWindow => Form.Bounds.Contains(Cursor.Position);

        internal AppWindow(string windowTitle, bool disableClose)
        {
            CreateRenderForm(windowTitle, disableClose);
        }

        public void SetVisible(bool isVisible) => Form.Visible = isVisible;

        public void SetSizeable() => Form.FormBorderStyle = FormBorderStyle.Sizable;

        public void Show() => Form.Show();

        public Vector2 GetDpi()
        {
            using Graphics graphics = Form.CreateGraphics();
            Vector2 dpi = new(graphics.DpiX, graphics.DpiY);
            return dpi;
        }

        internal void SetFullScreen(int screenIndex)
        {
            Form.FormBorderStyle = FormBorderStyle.Sizable;
            Form.WindowState = FormWindowState.Normal;
            Form.FormBorderStyle = FormBorderStyle.None;
            Form.Bounds = Screen.AllScreens[screenIndex].Bounds;
        }

        internal void InitViewSwapChain(Factory factory)
        {
            SwapChain = new SwapChain(factory, _device, SwapChainDescription);
            SwapChain.ResizeBuffers(bufferCount: 3, Form.ClientSize.Width, Form.ClientSize.Height,
                                    SwapChain.Description.ModeDescription.Format, SwapChain.Description.Flags);
        }

        internal void PrepareRenderingFrame()
        {
            _deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _deviceContext.Rasterizer.SetViewport(new Viewport(0, 0, Form.ClientSize.Width, Form.ClientSize.Height, 0.0f, 1.0f));
            _deviceContext.OutputMerger.SetTargets(RenderTargetView);
            _deviceContext.ClearRenderTargetView(RenderTargetView, T3Style.Colors.WindowBackground.AsSharpDx);
        }
        
        internal void RunRenderLoop(RenderLoop.RenderCallback callback) => RenderLoop.Run(Form, callback);

        internal void SetSize(int width, int height) => Form.ClientSize = new Size(width, height);

        internal void SetBorderStyleSizable() => Form.FormBorderStyle = FormBorderStyle.Sizable;

        internal void InitializeWindow(FormWindowState windowState, KeyEventHandler handleKeyDown, KeyEventHandler handleKeyUp, CancelEventHandler handleClose)
        {
            InitRenderTargetsAndEventHandlers();

            if (handleKeyDown != null)
                Form.KeyDown += handleKeyDown;

            if (handleKeyUp != null)
                Form.KeyUp += handleKeyUp;

            if (handleClose != null)
                Form.Closing += handleClose;

            Form.WindowState = windowState;
        }

        internal void SetDevice(Device device, DeviceContext deviceContext, SwapChain swapChain = null)
        {
            if (_hasSetDevice)
                throw new InvalidOperationException("Device has already been set");

            _hasSetDevice = true;
            _device = device;
            _deviceContext = deviceContext;
            _swapChain = swapChain;
        }

        internal void Release()
        {
            _renderTargetView.Dispose();
            _backBufferTexture.Dispose();
            _swapChain.Dispose();
        }

        private void CreateRenderForm(string windowTitle, bool disableClose)
        {
            Form = disableClose
                       ? new NoCloseRenderForm(windowTitle)
                             {
                                 ClientSize = new Size(640, 360 + 20),
                                 Icon = new Icon(@"Resources\t3-editor\images\t3.ico", 48, 48),
                                 FormBorderStyle = FormBorderStyle.None,
                             }
                       : new ImGuiDx11RenderForm(windowTitle)
                             {
                                 ClientSize = new Size(640, 480),
                                 Icon = new Icon(@"Resources\t3-editor\images\t3.ico", 48, 48)
                             };

            SwapChainDescription = new SwapChainDescription()
                                       {
                                           BufferCount = 3,
                                           ModeDescription = new ModeDescription(Form.ClientSize.Width,
                                                                                 Form.ClientSize.Height,
                                                                                 new Rational(60, 1),
                                                                                 Format.R8G8B8A8_UNorm),
                                           IsWindowed = true,
                                           OutputHandle = Form.Handle,
                                           SampleDescription = new SampleDescription(1, 0),
                                           SwapEffect = SwapEffect.Discard,
                                           Usage = Usage.RenderTargetOutput
                                       };
        }

        private void InitRenderTargetsAndEventHandlers()
        {
            var device = _device;
            _backBufferTexture = Resource.FromSwapChain<Texture2D>(SwapChain, 0);
            RenderTargetView = new RenderTargetView(device, _backBufferTexture);

            Form.ResizeBegin += (sender, args) => _isResizingRightNow = true;
            Form.ResizeEnd += (sender, args) =>
                              {
                                  RebuildBackBuffer(Form, device, ref _renderTargetView, ref _backBufferTexture, ref _swapChain);
                                  _isResizingRightNow = false;
                              };
            Form.ClientSizeChanged += (sender, args) =>
                                      {
                                          if (_isResizingRightNow)
                                              return;

                                          RebuildBackBuffer(Form, device, ref _renderTargetView, ref _backBufferTexture, ref _swapChain);
                                      };
        }

        private static void RebuildBackBuffer(Form form, Device device, ref RenderTargetView rtv, ref Texture2D buffer, ref SwapChain swapChain)
        {
            rtv.Dispose();
            buffer.Dispose();
            swapChain.ResizeBuffers(3, form.ClientSize.Width, form.ClientSize.Height, Format.Unknown, 0);
            buffer = Resource.FromSwapChain<Texture2D>(swapChain, 0);
            rtv = new RenderTargetView(device, buffer);
        }        
        
        /// <summary>
        /// We prevent closing the secondary viewer window for now because
        /// this will cause a SwapChain related crash
        /// </summary>
        private class NoCloseRenderForm : ImGuiDx11RenderForm
        {
            private const int CP_NOCLOSE_BUTTON = 0x200;

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams myCp = base.CreateParams;
                    myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                    return myCp;
                }
            }

            public NoCloseRenderForm(string title) : base(title)
            {
            }
        }
        
        private bool _hasSetDevice;
        private Device _device;
        private DeviceContext _deviceContext;
        private SwapChain _swapChain;
        private RenderTargetView _renderTargetView;
        private Texture2D _backBufferTexture;
        private bool _isResizingRightNow;
    }
}