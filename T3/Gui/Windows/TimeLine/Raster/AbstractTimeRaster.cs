﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Animation;
using T3.Gui.Interaction.Snapping;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;

namespace T3.Gui.Windows.TimeLine.Raster
{
    public abstract class AbstractTimeRaster : IValueSnapAttractor
    {
        public abstract void Draw(Playback playback);
        protected abstract string BuildLabel(Raster raster, double timeInSeconds);

        protected virtual IEnumerable<Raster> GetRastersForScale(double invertedScale, out float fadeFactor)
        {
            var density = UserSettings.Config.TimeRasterDensity * 0.02f;
            var scaleRange = ScaleRanges.FirstOrDefault(range => range.ScaleMax > invertedScale / density);
            fadeFactor = scaleRange == null
                             ? 1
                             : 1 - (float)MathUtils.RemapAndClamp(invertedScale, scaleRange.ScaleMin * density, scaleRange.ScaleMax * density, 0, 1);

            return scaleRange?.Rasters;
        }

        protected void DrawTimeTicks(double scale, double scroll, ICanvas canvas)
        {
            if (!(scale > Epsilon))
                return;

            var drawList = ImGui.GetWindowDrawList();
            var topLeft = canvas.WindowPos;
            var viewHeight = canvas.WindowSize.Y;
            var width = canvas.WindowSize.X;

            _usedPositions.Clear();

            
            var invertedScale = 1 / scale;

            var rasters = GetRastersForScale(invertedScale, out var fadeFactor);

            if (rasters == null)
                return;

            // Debug string 
            ImGui.PushFont(Fonts.FontSmall);

            foreach (var raster in rasters)
            {
                double t = -scroll % raster.Spacing;

                var lineAlpha = raster.FadeLines ? fadeFactor : 1;
                var lineColor = new Color(0, 0, 0, lineAlpha * 0.9f);

                var textAlpha = raster.FadeLabels ? fadeFactor : 1;
                var textColor = new Color(textAlpha);

                while (t / invertedScale < width)
                {
                    var xIndex = (int)(t / invertedScale);

                    if (xIndex > 0 && xIndex < width && !_usedPositions.ContainsKey(xIndex))
                    {
                        var time = t + scroll;
                        _usedPositions[xIndex] = time;

                        drawList.AddRectFilled(
                                               new Vector2(topLeft.X + xIndex, topLeft.Y),
                                               new Vector2(topLeft.X + xIndex + 1, topLeft.Y + viewHeight), lineColor);

                        if (raster.Label != "")
                        {
                            var output = BuildLabel(raster, time);

                            var p = topLeft + new Vector2(xIndex - 7, viewHeight - 17);
                            drawList.AddText(p, textColor, output);
                        }
                    }

                    t += raster.Spacing;
                }
            }

            ImGui.PopFont();
        }

        #region implement snap attractor

        public virtual SnapResult CheckForSnap(double time, float canvasScale)
        {
            return ValueSnapHandler.FindSnapResult(time, _usedPositions.Values, canvasScale);
        }
        #endregion

        private readonly Dictionary<int, double> _usedPositions = new Dictionary<int, double>();
        protected List<ScaleRange> ScaleRanges;
        private const double Epsilon = 0.001f;

        public class ScaleRange
        {
            public double ScaleMin { get; set; }
            public double ScaleMax { get; set; }
            public List<Raster> Rasters { get; set; }
        }

        public struct Raster
        {
            public string Label { get; set; }
            public double Spacing { get; set; }
            public bool FadeLabels { get; set; }
            public bool FadeLines { get; set; }
        }
    }
}