using System;
using System.IO;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Output;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Windows
{
    public class RenderSequenceWindow : RenderHelperWindow
    {
        public RenderSequenceWindow()
        {
            Config.Title = "Render Sequence";
            _lastHelpString = "Hint: Use a [RenderTarget] with format R8G8B8A8_UNorm for faster exports.\n" +
                              "Audio hint: Please ensure your BPM is set corrrectly with the Soundtrack.";
        }


        protected override void DrawContent()
        {
            DrawTimeSetup();

            // Custom parameters for this renderer
            FormInputs.AddEnumDropdown(ref _fileFormat, "FileFormat");
            FormInputs.AddStringInput("Folder", ref _targetFolder);
            ImGui.SameLine();
            FileOperations.DrawFileSelector(FileOperations.FilePickerTypes.Folder, ref _targetFolder);
            ImGui.Separator();

            var mainTexture = OutputWindow.GetPrimaryOutputWindow()?.GetCurrentTexture();
            if (mainTexture == null)
            {
                CustomComponents.HelpText("You have selected an operator that does not render. " +
                                          "Hint: Use a [RenderTarget] with format R8G8B8A8_UNorm for fast exports.");
                return;
            }

            if (!_isExporting)
            {
                if (ImGui.Button("Start Export"))
                {
                    if (ValidateOrCreateTargetFolder(_targetFolder))
                    {
                        _isExporting = true;
                        _exportStartedTime = Playback.RunTimeInSecs;
                        _frameIndex = 0;
                        SetPlaybackTimeForThisFrame();

                        // handle audio although we do not save it
                        var audioFrame = AudioEngine.LastMixDownBuffer(1.0 / _fps);
                        SaveCurrentFrameAndAdvance(mainTexture);
                    }
                }
            }
            else
            {
                // handle audio although we do not save it
                var audioFrame = AudioEngine.LastMixDownBuffer(Playback.LastFrameDuration);
                var success = SaveCurrentFrameAndAdvance(mainTexture);
                ImGui.ProgressBar((float) Progress, new Vector2(-1, 4));

                var currentTime = Playback.RunTimeInSecs;
                var durationSoFar = currentTime - _exportStartedTime;
                if (GetRealFrame() >= _frameCount || !success)
                {
                    var successful = success ? "successfully" : "unsuccessfully";
                    _lastHelpString = $"Sequence export finished {successful} in {durationSoFar:0.00}s";
                    _isExporting = false;
                }
                else if (ImGui.Button("Cancel"))
                {
                    _lastHelpString = $"Sequence export cancelled after {durationSoFar:0.00}s";
                    _isExporting = false;
                }
                else
                {
                    var estimatedTimeLeft = durationSoFar - durationSoFar /  Progress;
                    _lastHelpString = $"Saved {ScreenshotWriter.LastFilename} frame {GetRealFrame()}/{_frameCount}  ";
                    _lastHelpString += $"{Progress * 100.0:0}%  {estimatedTimeLeft:0}s left";
                }

                if (!_isExporting)
                {
                    ScreenshotWriter.Dispose();
                    ReleasePlaybackTime();
                }
            }

            CustomComponents.HelpText(_lastHelpString);
        }

        private static int GetRealFrame()
        {
            // since we are double-buffering and discarding the first few frames,
            // we have to subtract these frames to get the currently really shown framenumber...
            return _frameIndex - ScreenshotWriter.SkipImages;
        }

        private static string GetFilePath()
        {
            return Path.Combine(_targetFolder, $"output_{GetRealFrame():0000}.{Extension}");
        }

        private static bool SaveCurrentFrameAndAdvance(Texture2D mainTexture)
        {
            try
            {
                var success = SaveImage(mainTexture);
                _frameIndex++;
                SetPlaybackTimeForThisFrame();
                return success;
            }
            catch (Exception e)
            {
                _lastHelpString = e.ToString();
                _isExporting = false;
                ReleasePlaybackTime();
                return false;
            }
        }

        private static bool SaveImage(Texture2D mainTexture)
        {
            return ScreenshotWriter.SaveBufferToFile(mainTexture, GetFilePath(), _fileFormat);
        }

        private static string Extension => _fileFormat.ToString().ToLower(); 

        private static double _exportStartedTime;
        private static string _targetFolder = "./Render";

        private static ScreenshotWriter.FileFormats _fileFormat;
        private static string _lastHelpString = string.Empty;
    }
}