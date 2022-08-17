using ImGuiNET;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Numerics;
using SharpDX.DXGI;
using T3.Core;
using T3.Core.Logging;
using T3.Gui.Graph;
using T3.Gui.Graph.Rendering;
using T3.Gui.Interaction;
using T3.Gui.OutputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using T3.Operators.Types.Id_eff2ffff_dc39_4b90_9b1c_3c0a9a0108c6;
using UiHelpers;

namespace T3.Gui.Windows
{
    public class ImageOutputCanvas : ScalableCanvas
    {
        public void Update()
        {
            
            UpdateCanvas();
            UpdateViewMode();
            
        }

        public void SetAsCurrent()
        {
            Current = this;
        }

        public void Deactivate()
        {
            Current = null;
        }

        /// <summary>
        /// The image canvas that is currently being drawn from the UI.
        /// Note that <see cref="ImageOutputCanvas"/> is NOT a singleton so you can't rely on this to be valid outside of the Draw()ing context.
        /// It is used by <see cref="Texture2dOutputUi"/> to draw its content.
        /// </summary>
        public static ImageOutputCanvas Current = null;

        public Texture2D LastTexture;
        
        public void DrawTexture(Texture2D texture)
        {
            CustomComponents.FillWithStripes(ImGui.GetWindowDrawList(), THelpers.GetContentRegionArea());
            LastTexture = texture;
            
            if (texture == null || texture.IsDisposed)
                return;

            var size = new Vector2(texture.Description.Width, texture.Description.Height);
            var area = new ImRect(0, 0, size.X, size.Y);

            if (_viewMode == Modes.Fitted)
                ImageOutputCanvas.Current.FitAreaOnCanvas(area);

            var topLeft = Vector2.Zero;
            var topLeftOnScreen = ImageOutputCanvas.Current.TransformPosition(topLeft);
            ImGui.SetCursorScreenPos(topLeftOnScreen);

            var sizeOnScreen = ImageOutputCanvas.Current.TransformDirection(size);
            var srv = SrvManager.GetSrvForTexture(texture);
            ImGui.Image((IntPtr)srv, sizeOnScreen);

            if (ImGui.IsMouseHoveringRect(topLeftOnScreen, topLeftOnScreen + sizeOnScreen))
            {
                var relativePosition = (ImGui.GetMousePos() - topLeftOnScreen) / sizeOnScreen;
                MouseInput.Set(relativePosition, ImGui.IsMouseDown(ImGuiMouseButton.Left));
            }

            if (UserSettings.Config.ShowGraphOverContent)
                return;
            
            string format = "";
            if (srv == null)
            {
                format = "null?";
            } 
            else if (UserSettings.Config.ShowExplicitTextureFormatInOutputWindow)
            {
                format = srv.Description.Format.ToString();
            }
            else
            {
                switch (srv.Description.Format)
                {
                    case Format.R16G16B16A16_Float:
                        format = "RGBA:16";
                        break;
                    case Format.R8G8B8A8_SNorm:
                        format = "RGBA:8";
                        break;
                    default:
                        format = srv.Description.Format.ToString();
                        break;
                }
            }
                
                
                
            ImGui.PushFont(Fonts.FontSmall);
            var zoom = Math.Abs(Scale.X) < 0.001f ? "" : $" ×{Scale.X:G2}";
            var description = $"{size.X}x{size.Y}  {format} {zoom}";
            var descriptionWidth = ImGui.CalcTextSize(description).X;

            var textPos = new Vector2(WindowPos.X + (WindowSize.X - descriptionWidth) / 2,
                                      WindowPos.Y + WindowSize.Y - 16);

            var drawList = ImGui.GetWindowDrawList();
            drawList.AddText(textPos + new Vector2(1,0), ShadowColor, description );
            drawList.AddText(textPos + new Vector2(-1,0), ShadowColor, description );
            drawList.AddText(textPos + new Vector2(0,1), ShadowColor, description );
            drawList.AddText(textPos + new Vector2(0,-1), ShadowColor, description );
            drawList.AddText(textPos, Color.White, description );
            ImGui.PopFont();
        }
        
        private static readonly Color ShadowColor = new Color(0.0f, 0.0f, 0.0f, 0.6f);


        public void SetViewMode(Modes newMode)
        {
            _viewMode = newMode;
            if (newMode == Modes.Pixel)
            {
                SetScaleToMatchPixels();
            }
        }


        /// <summary>
        /// Updated the view mode if user interacted 
        /// </summary>
        private void UpdateViewMode()
        {
            switch (_viewMode)
            {
                case Modes.Fitted:
                    if (UserScrolledCanvas || UserZoomedCanvas)
                        _viewMode = Modes.Custom;
                    break;

                case Modes.Pixel:
                    if (UserZoomedCanvas)
                        _viewMode = Modes.Custom;
                    break;
            }
        }


        public enum Modes
        {
            Fitted,
            Pixel,
            Custom,
        }

        public Modes ViewMode => _viewMode;
        private Modes _viewMode = Modes.Fitted;
    }
}
