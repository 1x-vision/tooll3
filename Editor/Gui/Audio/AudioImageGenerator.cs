using System;
using System.Drawing;
using System.IO;
using ManagedBass;
using Newtonsoft.Json;
using T3.Core.Audio;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Audio
{
    public class AudioImageGenerator
    {
        public AudioImageGenerator(AudioClip audioClip)
        {
            SoundFilePath = audioClip.FilePath;
            
            const string imageExtension = ".waveform.png";
            ImageFilePath = SoundFilePath + imageExtension;
            if(!audioClip.TryGetAbsoluteFilePath(out SoundFilePathAbsolute))
                throw new Exception($"Could not get absolute path for audio clip: {SoundFilePath}");
            
            ImageFilePathAbsolute = SoundFilePathAbsolute + imageExtension;
        }

        public bool TryGenerateSoundSpectrumAndVolume()
        {
            
            try
            {
                if (string.IsNullOrEmpty(SoundFilePathAbsolute) || !File.Exists(SoundFilePathAbsolute))
                    return false;

                if (File.Exists(ImageFilePathAbsolute))
                {
                    Log.Debug($"Reusing sound image file: {ImageFilePath}");
                    return true;
                }
            }
            catch(Exception e)
            {
                Log.Warning($"Failed to generated image for soundtrack {SoundFilePath}: " + e.Message);
                return false;
            }

            Log.Debug($"Generating {ImageFilePath}...");

            Bass.Init(-1, 44100, 0, IntPtr.Zero);
            var stream = Bass.CreateStream(SoundFilePathAbsolute, 0, 0, BassFlags.Decode | BassFlags.Prescan);

            var streamLength = Bass.ChannelGetLength(stream);

            const double samplingResolution = 1.0 / 100;

            var sampleLength = Bass.ChannelSeconds2Bytes(stream, samplingResolution);
            var numSamples = streamLength / sampleLength;

            const int maxSamples = 16384;
            if (numSamples > maxSamples)
            {
                sampleLength = (long)(sampleLength * numSamples / (double)maxSamples) + 100;
                numSamples = streamLength / sampleLength;
                Log.Debug($"Limiting texture size to {numSamples} samples");
            }

            Bass.ChannelPlay(stream);

            var spectrumImage = new Bitmap((int)numSamples, ImageHeight);

            int a, b, r, g;
            var palette = new System.Drawing.Color[PaletteSize];
            
            const float upperThreshold = PaletteSize * 2 / 3f;
            const float lowerThreshold = PaletteSize / 3f;

            for (var palettePos = 0; palettePos < PaletteSize; ++palettePos)
            {
                a = 255;
                if (palettePos < upperThreshold)
                    a = (int)(palettePos * 255 / upperThreshold);

                b = 0;
                if (palettePos < lowerThreshold)
                    b = palettePos;
                else if (palettePos < upperThreshold)
                    b = -palettePos + 510;

                r = 0;
                if (palettePos > upperThreshold)
                    r = 255;
                else if (palettePos > lowerThreshold)
                    r = palettePos - 255;

                g = 0;
                if (palettePos > upperThreshold)
                    g = palettePos - 510;

                palette[palettePos] = System.Drawing.Color.FromArgb(a, r, g, b);
            }

            foreach (var region in _regions)
            {
                region.Levels = new float[numSamples];
            }

            var f = (float)(SpectrumLength / Math.Log(ImageHeight + 1));
            var f2 = (float)((PaletteSize - 1) / Math.Log(MaxIntensity + 1));
            //var f3 = (float)((ImageHeight - 1) / Math.Log(32768.0f + 1));

            var logarithmicExponent = UserSettings.Config.ExpandSpectrumVisualizerVertically ? 10d : Math.E;

            for (var sampleIndex = 0; sampleIndex < numSamples; ++sampleIndex)
            {
                Bass.ChannelSetPosition(stream, sampleIndex * sampleLength);
                Bass.ChannelGetData(stream, _fftBuffer, (int)DataFlags.FFT2048);

                for (var rowIndex = 0; rowIndex < ImageHeight; ++rowIndex)
                {
                    var j = (int)(f * Math.Log(rowIndex + 1));
                    var pj = (int)(rowIndex > 0 ? f * Math.Log(rowIndex - 1 + 1, logarithmicExponent) : j);
                    var nj = (int)(rowIndex < ImageHeight - 1 ? f * Math.Log(rowIndex + 1 + 1, logarithmicExponent) : j);
                    var intensity = 125.0f * _fftBuffer[SpectrumLength - pj - 1] +
                                    750.0f * _fftBuffer[SpectrumLength - j - 1] +
                                    125.0f * _fftBuffer[SpectrumLength - nj - 1];
                    intensity = Math.Min(MaxIntensity, intensity);
                    intensity = Math.Max(0.0f, intensity);

                    var palettePos = (int)(f2 * Math.Log(intensity + 1));
                    spectrumImage.SetPixel(sampleIndex, rowIndex, palette[palettePos]);
                }

                if (sampleIndex % 1000 == 0)
                {
                    var percentage = (int)(100.0 * sampleIndex / (float)numSamples);
                    Log.Debug($"   computing sound image {percentage}%% complete");
                }

                // foreach (var region in _regions)
                // {
                //     region.ComputeUpLevelForCurrentFft(sampleIndex, ref _fftBuffer);
                // }
            }

            // foreach (var region in _regions)
            // {
            //     region.SaveToFile(_soundFilePath);
            // }

            bool success;
            try
            {
                spectrumImage.Save(ImageFilePathAbsolute);
                success = true;
            }
            catch(Exception e)
            {
                success = false;
                Log.Error(e.Message);
            }

            Bass.ChannelStop(stream);
            Bass.StreamFree(stream);

            return success;
        }

        private class FftRegion
        {
            public string Title;
            public float[] Levels;
            public float LowerLimit;
            public float UpperLimit;

            public void ComputeUpLevelForCurrentFft(int index, ref float[] fftBuffer)
            {
                var level = 0f;

                var startIndex = (int)MathUtils.Lerp(0, SpectrumLength, MathUtils.Clamp(this.LowerLimit, 0, 1));
                var endIndex = (int)MathUtils.Lerp(0, SpectrumLength, MathUtils.Lerp(this.UpperLimit, 0, 1));

                for (int i = startIndex; i < endIndex; i++)
                {
                    level += fftBuffer[i];
                }

                Levels[index] = level;
            }

            public void SaveToFile(string basePath)
            {
                using (var sw = new StreamWriter(basePath + "." + Title + ".json"))
                {
                    sw.Write(JsonConvert.SerializeObject(Levels, Formatting.Indented));
                }
            }
        }

        public readonly string SoundFilePath;
        public readonly string SoundFilePathAbsolute;
        public readonly string ImageFilePath;
        public readonly string ImageFilePathAbsolute;

        private readonly FftRegion[] _regions =
            {
                new() { Title = "levels", LowerLimit = 0f, UpperLimit = 1f },
                new() { Title = "highlevels", LowerLimit = 0.3f, UpperLimit = 1f },
                new() { Title = "midlevels", LowerLimit = 0.06f, UpperLimit = 0.3f },
                new() { Title = "lowlevels", LowerLimit = 0.0f, UpperLimit = 0.02f },
            };

        private const int SpectrumLength = 1024;
        private const int ImageHeight = 256;
        private const float MaxIntensity = 500;
        private const int ColorSteps = 255;
        private const int PaletteSize = 3 * ColorSteps;

        private float[] _fftBuffer = new float[SpectrumLength];

        public AudioImageGenerator(string soundFilePath)
        {
            SoundFilePath = soundFilePath;
        }
    }
}