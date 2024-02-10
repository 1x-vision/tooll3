using System;
using System.Collections.Generic;
using System.IO;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.Styling;
using T3.Editor.SystemUi;
using T3.SystemUi;

namespace T3.Editor.Gui.Windows.RenderExport;

public abstract class RenderHelperWindow : Window
{

    protected static int soundtrackChannels()
    {
        var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
        var composition = primaryGraphWindow?.GraphCanvas.CompositionOp;
        PlaybackUtils.FindPlaybackSettingsForInstance(composition, out var compWithSettings, out var settings);
        settings.GetMainSoundtrack(out var soundtrack);
        return AudioEngine.clipChannels(soundtrack);
    }

    protected static int soundtrackSampleRate()
    {
        var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
        var composition = primaryGraphWindow?.GraphCanvas.CompositionOp;
        PlaybackUtils.FindPlaybackSettingsForInstance(composition, out var compWithSettings, out var settings);
        settings.GetMainSoundtrack(out var soundtrack);
        return AudioEngine.clipSampleRate(soundtrack);
    }

    protected static void DrawTimeSetup()
    {
        FormInputs.SetIndentToParameters();
        
        // Convert times if reference time selection changed
        var oldTimeReference = _timeReference;

        if (FormInputs.AddEnumDropdown(ref _timeReference, "Time Format"))
        {
            _startTime = (float)ConvertReferenceTime(_startTime, oldTimeReference, _timeReference);
            _endTime = (float)ConvertReferenceTime(_endTime, oldTimeReference, _timeReference);
        }

        // Change FPS if required
        FormInputs.AddFloat("FPS", ref Fps, 0);
        if (Fps < 0) Fps = -Fps;
        if (Fps != 0)
        {
            _startTime = (float)ConvertFps(_startTime, _lastValidFps, Fps);
            _endTime = (float)ConvertFps(_endTime, _lastValidFps, Fps);
            _lastValidFps = Fps;
        }

        FormInputs.AddEnumDropdown(ref _timeRange, "Use Range");
        ApplyTimeRange();
        
        FormInputs.AddFloat($"Start in {_timeReference}", ref _startTime);
        FormInputs.AddFloat($"End in {_timeReference}", ref _endTime);


        var startTimeInSeconds = ReferenceTimeToSeconds(_startTime, _timeReference);
        var endTimeInSeconds = ReferenceTimeToSeconds(_endTime, _timeReference);
        FrameCount = (int)Math.Round((endTimeInSeconds - startTimeInSeconds) * Fps);

        if (FormInputs.AddInt($"Motion Blur Samples", ref _overrideMotionBlurSamples, -1, 50, 1,
                              "This requires a [RenderWithMotionBlur] operator. Please check its documentation."))
        {
            _overrideMotionBlurSamples = _overrideMotionBlurSamples.Clamp(-1, 50);
        }
    }

    protected static bool ValidateOrCreateTargetFolder(string targetFile)
    {
        var directory = Path.GetDirectoryName(targetFile);
        if (targetFile != directory && File.Exists(targetFile))
        {
            // FIXME: get a nicer popup window here...
            var result = EditorUi.Instance.ShowMessageBox("File exists. Overwrite?", "Render Video", PopUpButtons.YesNo);
            return (result == PopUpResult.Yes);
        }

        if (directory == null || Directory.Exists(directory))
            return true;

        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to create target folder '{directory}': {e.Message}");
            return false;
        }

        return true;
    }
    

    private static void ApplyTimeRange()
    {
        switch (_timeRange)
        {
            case TimeRanges.Custom:
                break;
            case TimeRanges.Loop:
            {
                var playback = Playback.Current; // TODO, this should be non-static eventually
                var startInSeconds = playback.SecondsFromBars(playback.LoopRange.Start);
                var endInSeconds = playback.SecondsFromBars(playback.LoopRange.End);
                _startTime = (float)SecondsToReferenceTime(startInSeconds, _timeReference);
                _endTime = (float)SecondsToReferenceTime(endInSeconds, _timeReference);
                break;
            }
            case TimeRanges.Soundtrack:
            {
                if (PlaybackUtils.TryFindingSoundtrack(out var soundtrack))
                {
                    var playback = Playback.Current; // TODO, this should be non-static eventually
                    _startTime = (float)SecondsToReferenceTime(playback.SecondsFromBars(soundtrack.StartTime), _timeReference);
                    if (soundtrack.EndTime > 0)
                    {
                        _endTime = (float)SecondsToReferenceTime(playback.SecondsFromBars(soundtrack.EndTime), _timeReference);
                    }
                    else
                    {
                        _endTime = (float)SecondsToReferenceTime(soundtrack.LengthInSeconds, _timeReference);
                    }
                }
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static double ConvertReferenceTime(double time,
                                               TimeReference oldTimeReference,
                                               TimeReference newTimeReference)
    {
        // Only convert time value if time reference changed
        if (oldTimeReference == newTimeReference) return time;

        var seconds = ReferenceTimeToSeconds(time, oldTimeReference);
        return SecondsToReferenceTime(seconds, newTimeReference);
    }

    private static double ConvertFps(double time, double oldFps, double newFps)
    {
        // Only convert FPS if values are valid
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
                if (Fps != 0)
                    return time / Fps;
                else
                    return time / 60.0;
        }

        // This is an error, don't change the value
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
                if (Fps != 0)
                    return timeInSeconds * Fps;
                else
                    return timeInSeconds * 60.0;
        }

        // This is an error, don't change the value
        return timeInSeconds;
    }

    protected static void SetPlaybackTimeForThisFrame()
    {
        // get playback settings
        var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
        var composition = primaryGraphWindow?.GraphCanvas.CompositionOp;
        PlaybackUtils.FindPlaybackSettingsForInstance(composition, out var compWithSettings, out var settings);

        // change settings for all playback before calculating times
        Playback.Current.Bpm = settings.Bpm;
        Playback.Current.PlaybackSpeed = 0.0;
        Playback.Current.Settings = settings;

        // set user time in secs for video playback
        double startTimeInSeconds = ReferenceTimeToSeconds(_startTime, _timeReference);
        double endTimeInSeconds = startTimeInSeconds + (FrameCount - 1) / Fps;
        var oldTimeInSecs = Playback.Current.TimeInSecs;
        Playback.Current.TimeInSecs = MathUtils.Lerp(startTimeInSeconds, endTimeInSeconds, Progress);
        var adaptedDeltaTime = Math.Max(Playback.Current.TimeInSecs - oldTimeInSecs + _timingOverhang, 0.0);

        // set user time in secs for audio playback
        settings.GetMainSoundtrack(out var soundtrack);
        if (soundtrack != null)
            AudioEngine.UseAudioClip(soundtrack, Playback.Current.TimeInSecs);

        if (!_recording)
        {
            _timingOverhang = 0.0;
            adaptedDeltaTime = 1.0 / Fps;

            Playback.Current.IsLive = false;
            Playback.Current.PlaybackSpeed = 1.0;

            AudioEngine.prepareRecording(Playback.Current, Fps);

            double requestedEndTimeInSeconds = ReferenceTimeToSeconds(_endTime, _timeReference);
            double actualEndTimeInSeconds = startTimeInSeconds + FrameCount / Fps;

            Log.Debug($"Requested recording from {startTimeInSeconds:0.0000} to {requestedEndTimeInSeconds:0.0000} seconds");
            Log.Debug($"Actually recording from {startTimeInSeconds:0.0000} to {actualEndTimeInSeconds:0.0000} seconds due to frame raster");
            Log.Debug($"Using {Playback.Current.Bpm} bpm");

            _recording = true;
        }

        // update audio parameters, respecting looping etc.
        Playback.Current.Update(false);

        var bufferLengthInMS = (int)Math.Floor(1000.0 * adaptedDeltaTime);
        _timingOverhang = adaptedDeltaTime - (double)bufferLengthInMS / 1000.0;
        _timingOverhang = Math.Max(_timingOverhang, 0.0);

        AudioEngine.CompleteFrame(Playback.Current, (double)bufferLengthInMS / 1000.0);
    }

    protected static void ReleasePlaybackTime()
    {
        AudioEngine.endRecording(Playback.Current, Fps);

        Playback.Current.TimeInSecs = ReferenceTimeToSeconds(_endTime, _timeReference);
        Playback.Current.IsLive = true;
        Playback.Current.PlaybackSpeed = 0.0;
        Playback.Current.Update(false);

        _recording = false;
    }

    public override List<Window> GetInstances()
    {
        return new List<Window>();
    }

    protected static bool FindIssueWithTexture(Texture2D texture, List<Format> supportedInputFormats, out string warning)
    {
        if (texture == null || texture.IsDisposed)
        {
            warning = "You have selected an operator that does not render. " +
                      "Hint: Use a [RenderTarget] with format B8G8R8A8_UNorm for fast exports.";
            return true;
        }

        // if (!supportedInputFormats.Contains(texture.Description.Format))
        // {
        //     warning = $"Texture format {texture.Description.Format} is not supported. Please use [ConvertFormat] with "
        //               + string.Join(", ", supportedInputFormats);
        //     return true;
        // }

        warning = string.Empty;
        return false;
    }

    protected const string PreferredInputFormatHint = "Hint: Use a [ConvertFormat] with format B8G8R8A8_UNorm for fast exports.";

    protected static double Progress => (FrameCount <= 1) ? 0 :
        ((double)FrameIndex / (double)(FrameCount - 1)).Clamp(0, 1);

    private static TimeRanges _timeRange = TimeRanges.Custom;
    private static TimeReference _timeReference;
    private static float _startTime;
    private static float _endTime = 4.0f; // four Bars
    protected static float Fps = 60.0f;
    private static float _lastValidFps = Fps;

    private static double _timingOverhang = 0.0; // time that could not be updated due to MS resolution (in seconds)
    private static bool _recording = false; // are we recording?
    public static bool IsExporting => _isExporting;

    // ReSharper disable once InconsistentNaming
    protected static bool _isExporting;
    public static int OverrideMotionBlurSamples => _overrideMotionBlurSamples;
    private static int _overrideMotionBlurSamples = -1;

    protected static int FrameIndex;
    protected static int FrameCount;
    
    private enum TimeReference
    {
        Bars,
        Seconds,
        Frames
    }

    private enum TimeRanges
    {
        Custom,
        Loop,
        Soundtrack,
    }

}