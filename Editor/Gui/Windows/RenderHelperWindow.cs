using System;
using System.Collections.Generic;
using System.IO;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.SystemUi;
using T3.SystemUi;

namespace T3.Editor.Gui.Windows
{
    public abstract class RenderHelperWindow : Window
    {
        public enum TimeReference
        {
            Bars,
            Seconds,
            Frames
        }

        protected static void DrawTimeSetup()
        {
            // convert times if reference time selection changed
            var oldTimeReference = _timeReference;
            
            if (FormInputs.AddEnumDropdown(ref _timeReference, "Time reference"))
            {
                _startTime = (float)ConvertReferenceTime(_startTime, oldTimeReference, _timeReference);
                _endTime = (float)ConvertReferenceTime(_endTime, oldTimeReference, _timeReference);
            }

            // change FPS if required
            FormInputs.AddFloat("FPS", ref _fps, 0);
            if (_fps < 0) _fps = -_fps;
            if (_fps != 0)
            {
                _startTime = (float)ConvertFPS(_startTime, _lastValidFps, _fps);
                _endTime = (float)ConvertFPS(_endTime, _lastValidFps, _fps);
                _lastValidFps = _fps;
            }
            FormInputs.AddFloat($"Start in {_timeReference}", ref _startTime);
            FormInputs.AddFloat($"End in {_timeReference}", ref _endTime);
            
            // use our loop range instead of entered values?
            FormInputs.AddCheckBox("Use Loop Range", ref _useLoopRange);
            if (_useLoopRange) UseLoopRange();
            
            double startTimeInSeconds = ReferenceTimeToSeconds(_startTime, _timeReference);
            double endTimeInSeconds = ReferenceTimeToSeconds(_endTime, _timeReference);
            _frameCount = (int)Math.Round((endTimeInSeconds - startTimeInSeconds) * _fps);
            
            if (FormInputs.AddInt($"Motion Blur Samples", ref _overrideMotionBlurSamples, -1, 50, 1, "This requires a [RenderWithMotionBlur] operator. Please check its documentation."))
            {
                _overrideMotionBlurSamples = _overrideMotionBlurSamples.Clamp(-1, 50);
            }            
        }

        protected static bool ValidateOrCreateTargetFolder(string targetFile)
        {
            string directory = Path.GetDirectoryName(targetFile);
            if (targetFile != directory && File.Exists(targetFile))
            {
                // FIXME: get a nicer popup window here...
                var result = EditorUi.Instance.ShowMessageBox("File exists. Overwrite?", "Render Video", PopUpButtons.YesNo);
                return (result == PopUpResult.Yes);
            }

            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception e)
                {
                    Log.Warning($"Failed to create target folder '{directory}': {e.Message}");
                    return false;
                }
            }
            return true;
        }

        private static void UseLoopRange()
        {
            var playback = Playback.Current; // TODO, this should be non-static eventually
            var startInSeconds = playback.SecondsFromBars(playback.LoopRange.Start);
            var endInSeconds = playback.SecondsFromBars(playback.LoopRange.End);
            _startTime = (float)SecondsToReferenceTime(startInSeconds, _timeReference);
            _endTime = (float)SecondsToReferenceTime(endInSeconds, _timeReference);
        }

        private static double ConvertReferenceTime(double time,
                                                   TimeReference oldTimeReference,
                                                   TimeReference newTimeReference)
        {
            // only convert time value if time reference changed
            if (oldTimeReference == newTimeReference) return time;

            var seconds = ReferenceTimeToSeconds(time, oldTimeReference);
            return SecondsToReferenceTime(seconds, newTimeReference);
        }

        private static double ConvertFPS(double time, double oldFps, double newFps)
        {
            // only convert FPS if values are valid
            if (oldFps == 0 || newFps == 0) return time;

            return time / oldFps * newFps;
        }

        private static double ReferenceTimeToSeconds(double time, TimeReference timeReference)
        {
            var playback = Playback.Current; // TODO, this should be non-static eventually
            switch (timeReference)
            {
                case TimeReference.Bars:
                    return playback.SecondsFromBars(time);
                case TimeReference.Seconds:
                    return time;
                case TimeReference.Frames:
                    if (_fps != 0)
                        return time / _fps;
                    else
                        return time / 60.0;
            }

            // this is an error, don't change the value
            return time;
        }

        private static double SecondsToReferenceTime(double timeInSeconds, TimeReference timeReference)
        {
            var playback = Playback.Current; // TODO, this should be non-static eventually
            switch (timeReference)
            {
                case TimeReference.Bars:
                    return playback.BarsFromSeconds(timeInSeconds);
                case TimeReference.Seconds:
                    return timeInSeconds;
                case TimeReference.Frames:
                    if (_fps != 0)
                        return timeInSeconds * _fps;
                    else
                        return timeInSeconds * 60.0;
            }

            // this is an error, don't change the value
            return timeInSeconds;
        }

        protected static void SetPlaybackTimeForNextFrame()
        {
            double startTimeInSeconds = ReferenceTimeToSeconds(_startTime, _timeReference);
            double endTimeInSeconds = ReferenceTimeToSeconds(_endTime, _timeReference);
            Playback.Current.TimeInSecs = MathUtils.Lerp(startTimeInSeconds, endTimeInSeconds, Progress);
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        protected static float Progress => (float)((double)_frameIndex / (double)_frameCount).Clamp(0, 1);

        private static bool _useLoopRange;
        private static TimeReference _timeReference;
        private static float _startTime;
        private static float _endTime = 1.0f; // one Bar
        protected static float _fps = 60.0f;
        private static float _lastValidFps = _fps;

        public static bool IsExporting => _isExporting;
        public static int OverrideMotionBlurSamples => _overrideMotionBlurSamples;
        private static int _overrideMotionBlurSamples = -1;
        
        protected static bool _isExporting;
        protected static int _frameIndex;
        protected static int _frameCount;
    }
}