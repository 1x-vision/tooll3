using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.MediaFoundation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.DataTypes.DataSet;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator.Interfaces;
using T3.Core.Utils;
using ResourceManager = T3.Core.Resource.ResourceManager;

namespace T3.Operators.Types.Id_914fb032_d7eb_414b_9e09_2bdd7049e049
{
    /**
     * This code is strongly inspired by
     *
     * https://github.com/vvvv/VL.Video.MediaFoundation/blob/master/src/VideoPlayer.cs
     */
    public class PlayVideo : Instance<PlayVideo>, IStatusProvider
    {
        [Output(Guid = "fa56b47f-1b16-45d5-80cd-32c5a872acf4", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> Texture = new();

        [Output(Guid = "2F16BE73-226B-47E7-B7EE-BF4F3738FA13")]
        public readonly Slot<float> Duration = new();

        [Output(Guid = "C89EA3AE-82FF-4791-B755-7B7D9EDDF8A7", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> HasCompleted = new();

        [Output(Guid = "732FC715-A8B5-438F-A607-EE1B8B080C04", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> UpdateCount = new();

        public PlayVideo()
        {
            Texture.UpdateAction = Update;
            UpdateCount.UpdateAction = Update;
            _playbackController = new(this);
        }

        private void Update(EvaluationContext context)
        {
            var requestedTime = OverrideTimeInSecs.IsConnected
                                    ? OverrideTimeInSecs.GetValue(context)
                                    : context.Playback.SecondsFromBars(context.LocalTime);

            if (_playbackController.HandleGettingFrames(Path.GetValue(context),
                                                        requestedTime,
                                                        ResyncThreshold.GetValue(context),
                                                        Loop.GetValue(context),
                                                        Volume.GetValue(context),
                                                        IsPreciseAtPlayback.GetValue(context)))
            {
                UpdateCount.Value++;
            }

            HasCompleted.Value = _playbackController.HasPlaybackCompleted;
            Texture.Value = _playbackController.Texture;
            Duration.Value = _playbackController.Duration;

            //Playback.OpNotReady |= !_playbackController.IsReadyForRendering;
            
            Texture.DirtyFlag.Clear();
            Duration.DirtyFlag.Clear();
            HasCompleted.DirtyFlag.Clear();
            UpdateCount.DirtyFlag.Clear();
        }

        private readonly PlaybackController _playbackController;

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            _playbackController.Dispose();
        }

        // Input parameters
        [Input(Guid = "0e255347-08bc-4363-9ffa-ab863a1cea8e")]
        public readonly InputSlot<string> Path = new();

        [Input(Guid = "2FECFBB4-F7D9-4C53-95AE-B64CCBB6FBAD")]
        public readonly InputSlot<float> Volume = new();

        [Input(Guid = "E9C15B3F-8C4A-411D-B9B3-795D64D6BD20")]
        public readonly InputSlot<float> ResyncThreshold = new();

        [Input(Guid = "48E62A3C-A903-4A9B-A44A-148C6C07AC1E")]
        public readonly InputSlot<float> OverrideTimeInSecs = new();

        [Input(Guid = "21B5671B-862F-4CEA-A355-FA019996C936")]
        public readonly InputSlot<bool> Loop = new();
        
        [Input(Guid = "B62C208C-3735-4130-87DE-8C03C8A9B5FA")]
        public readonly InputSlot<bool> IsPreciseAtPlayback = new();

        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_playbackController?.ErrorMessageForStatus) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Error;
        }

        public string GetStatusMessage() => _playbackController?.ErrorMessageForStatus;

        private class PlaybackController : IDisposable
        {
            public string ErrorMessageForStatus;

            // TODO:
            public bool HasPlaybackCompleted { get; private set; }
            public float Duration { get; private set; }
            public Texture2D Texture { get; private set; }
            public bool IsReadyForRendering => Texture != null && !_isSeeking;

            public bool HandleGettingFrames(string url, double requestedTime, float resyncThreshold, bool loop, float volume, bool precisePlayback)
            {
                requestedTime = (Math.Floor(requestedTime*60)/60).Clamp(0, Duration);
                //Log.Debug($"requested time {requestedTime:0.000}  {requestedTime*60:0.000}", this);
                if (!_initialized)
                {
                    SetupMediaFoundation();
                    _initialized = true;
                }

                if (_engine == null)
                {
                    ErrorMessageForStatus = "Initialization of MediaEngine failed";
                    return false;
                }

                _lastUpdateTimeInSecs = Playback.RunTimeInSecs;

                SetMediaUrl(url);
                
                const float completionThreshold = 0.016f; // A hack to prevent engine missing the end of playback
                var durationWithMargin = _engine.Duration - completionThreshold;
                HasPlaybackCompleted = !loop && _engine.CurrentTime > durationWithMargin || requestedTime > durationWithMargin;

                var isPlayingForward = Math.Abs(Playback.Current.PlaybackSpeed - 1) < 0.001f && !Playback.Current.IsRenderingToFile;
                _lastRequestedTime = requestedTime;

                /***
                 * Mute video if audio engine is muted
                 * FIXME: does not work when the video is not updating...
                 *
                 * Fixing this will require some thought: To managed audio-levels and playback centrally we probably need
                 * an interfaces to register all audio sources and provides functions like muting, stop, setting audio level, etc.
                 */
                _engine.Volume = AudioEngine.IsMuted ? 0 : volume.Clamp(0f, 1f);

                if ((ReadyStates)_engine.ReadyState <= ReadyStates.HaveNothing)
                    return false;

                if ((ReadyStates)_engine.ReadyState <= ReadyStates.HaveMetadata)
                    return false;

                Duration = _engine.ReadyState >= (int)ReadyStates.HaveMetadata ? (float)_engine.Duration : -1;

                Trace("02b-engine.CurrentTime", _engine.CurrentTime);
                Trace("02c-engine.PlayState", (ReadyStates)_engine.ReadyState);
                Trace("02d-engine.IsSeeking", _engine.IsSeeking);
                Trace("02e-engine.IsPaused", _engine.IsPaused);
                Trace("08e-engine.PlaybackRate", _engine.PlaybackRate);

                
                // This is very unfortunate: Starting the video without seeking is only possible with a slight delay.
                // But adding this offset will cause is also visible when pausing the video.
                // To still have precise timing during pause (e.g. for setting keyframes) we added this option. 
                var playbackOffset = precisePlayback ? 2/60f : 0;
                var clampedSeekTime = LoopOrClampTimeToVideoDuration(requestedTime + playbackOffset);
                
                var clampedVideoTime = LoopOrClampTimeToVideoDuration(_engine.CurrentTime);
                var deltaTime = clampedSeekTime - clampedVideoTime;
                
                Trace("00-VideoTime", clampedVideoTime);
                Trace("02a-RequestedTime", clampedSeekTime);
                Trace("02a_-delta", deltaTime);

                double seekThreshold = resyncThreshold;
                if (!isPlayingForward)
                {
                    const float thresholdWhenPaused = 0.5f / 60f;
                    seekThreshold = thresholdWhenPaused;
                }
                
                var shouldSeek = !_isSeeking && !_engine.IsSeeking && Math.Abs(deltaTime) > seekThreshold;
                
                if (shouldSeek)
                {
                    const float averageSeekTime = 0.05f;
                    var lookAheadOffsetDuringPlayback = isPlayingForward ? averageSeekTime : 0;

                    var seekTargetDuringPlayback = clampedSeekTime + lookAheadOffsetDuringPlayback;
                    //Log.Debug($"Seeking to {seekTargetDuringPlayback:0.000} from {clampedVideoTime:0.000}   dt |{deltaTime:0.0000}| > {seekThreshold} ", this);
                    
                    _engine.CurrentTime = seekTargetDuringPlayback;
                    _seekOperationStartTime = Playback.RunTimeInSecs;
                    _isSeeking = true;
                }

 
                _engine.Loop = loop;

                if (!_engine.Loop)
                {
                    var currentTime = (float)_engine.CurrentTime;
                    const float loopStartTime = 0f;
                    var loopEndTime = (float)_engine.Duration;
                    if (currentTime < loopStartTime || currentTime > loopEndTime)
                    {
                        _engine.CurrentTime = (float)_engine.PlaybackRate >= 0 ? loopStartTime : loopEndTime;
                    }
                }

                // Finishes seeking operation?
                if (_isSeeking && !_engine.IsSeeking)
                {
                    Trace("05-Seeking took", Playback.RunTimeInSecs - _seekOperationStartTime);

                    //Log.Debug($"Seeking took {(Playback.RunTimeInSecs - _seekOperationStartTime) * 1000:0}ms");
                    _isSeeking = false;
                }
                
                if(Playback.Current.IsRenderingToFile)
                    Playback.OpNotReady |= _isSeeking;
                
                if (isPlayingForward && _engine.IsPaused)
                {
                    Log.Debug("Starting playback", _instance);
                    _engine.Play();
                    _playbackStartTime = Playback.RunTimeInSecs;
                }
                else if (!isPlayingForward && !_engine.IsPaused)
                {
                    Log.Debug("Paused playback", _instance);
                    _engine.Pause();
                }
                
                var hasNewFrame = _engine.OnVideoStreamTick(out var presentationTimeTicks);

                if ((ReadyStates)_engine.ReadyState < ReadyStates.HaveCurrentData || !hasNewFrame)
                    return false;

                if (presentationTimeTicks < 0)
                {
                    //Log.Warning("Ignoring invalid time tick: " + presentationTimeTicks, _instance);
                    return false;
                }

                Trace("04-HasNewFrameTick", presentationTimeTicks / 1000);

                if (presentationTimeTicks == _lastStreamTick)
                    return false;

                _lastStreamTick = presentationTimeTicks;

                return TryTransferFrame();

                // -----------------------------------------------------------
                // Local functions

                double LoopOrClampTimeToVideoDuration(double time)
                {
                    return loop
                               ? time % _engine.Duration
                               : Math.Clamp(time, 0.0, _engine.Duration);
                }
            }

            private int _framesAfterPlaybackStart;

            private bool TryTransferFrame()
            {
                try
                {
                    if (_contentReloaded || Texture == null)
                    {
                        _contentReloaded = false;

                        _engine.GetNativeVideoSize(out var width, out var height);
                        SetupTexture(new Int2(width, height));

                        // _SRGB doesn't work :/ Getting invalid argument exception in TransferVideoFrame
                        //_renderTarget = Texture.New2D(graphicsDevice, width, height, PixelFormat.B8G8R8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
                    }

                    if (Texture == null)
                    {
                        ErrorMessageForStatus = "Failed to setup texture";
                        return false;
                    }

                    _engine.TransferVideoFrame(
                                               Texture,
                                               ToVideoRect(default),
                                               //new RawRectangle(0, 0, renderTarget.ViewWidth, renderTarget.ViewHeight),
                                               new RawRectangle(0, 0, _textureSize.Width, _textureSize.Height),
                                               ToRawColorBgra(default));
                }
                catch (Exception e)
                {
                    Log.Warning("Using video texture image failed: " + e.Message);
                    DisposeTexture();
                    return false;
                }

                return true;
            }

            private void SetupMediaFoundation()
            {
                using var mediaEngineAttributes = new MediaEngineAttributes
                                                      {
                                                          // _SRGB doesn't work :/ Getting invalid argument exception later in TransferVideoFrame
                                                          AudioCategory = SharpDX.Multimedia.AudioStreamCategory.GameMedia,
                                                          AudioEndpointRole = SharpDX.Multimedia.AudioEndpointRole.Multimedia,
                                                          VideoOutputFormat = (int)SharpDX.DXGI.Format.B8G8R8A8_UNorm
                                                      };

                var device = ResourceManager.Device;
                if (device != null)
                {
                    // Add multi thread protection on device (MediaFoundation is multi-threaded)
                    using var deviceMultithread = device.QueryInterface<DeviceMultithread>();
                    deviceMultithread.SetMultithreadProtected(true);

                    // Reset device
                    using var manager = new DXGIDeviceManager();
                    manager.ResetDevice(device);
                    mediaEngineAttributes.DxgiManager = manager;
                }

                // Setup Media Engine attributes and create a DXGI Device Manager
                _dxgiDeviceManager = new DXGIDeviceManager();
                _dxgiDeviceManager.ResetDevice(device);
                var attributes = new MediaEngineAttributes
                                     {
                                         DxgiManager = _dxgiDeviceManager,
                                         VideoOutputFormat = (int)SharpDX.DXGI.Format.B8G8R8A8_UNorm
                                         //VideoOutputFormat = (int)SharpDX.DXGI.Format.NV12                                     
                                     };

                MediaManager.Startup();

                using var factory = new MediaEngineClassFactory();
                _engine = new MediaEngine(factory, attributes, MediaEngineCreateFlags.None, EnginePlaybackEventHandler);
                _engine.Preload = MediaEnginePreload.Automatic;
            }

            private void SetupTexture(Int2 size)
            {
                if (size.Width <= 0 || size.Height <= 0 || size.Width > 16383 || size.Height > 16383)
                {
                    Log.Warning($"Texture size {size} is invalid, using 512x512 instead.");
                    size = new Int2(512, 512);
                }

                if (Texture != null && size == _textureSize)
                    return;

                Texture?.Dispose();

                var device = ResourceManager.Device;
                try
                {
                    Texture = new Texture2D(device,
                                            new Texture2DDescription
                                                {
                                                    ArraySize = 1,
                                                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                                                    CpuAccessFlags = CpuAccessFlags.None,
                                                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                                                    Width = size.Width,
                                                    Height = size.Height,
                                                    MipLevels = 0,
                                                    OptionFlags = ResourceOptionFlags.None,
                                                    SampleDescription = new SampleDescription(1, 0),
                                                    Usage = ResourceUsage.Default
                                                });
                    _textureSize = size;
                }
                catch (Exception e)
                {
                    ErrorMessageForStatus = $"Failed to create texture for {size}: {e.Message}";
                    DisposeTexture();
                }
            }

            private void EnginePlaybackEventHandler(MediaEngineEvent mediaEvent, long param1, int param2)
            {
                Trace("04-MediaEngineEvent", mediaEvent);

                switch (mediaEvent)
                {
                    case MediaEngineEvent.LoadStart:
                        _lastMediaEngineError = MediaEngineErr.Noerror;
                        break;
                    case MediaEngineEvent.Error:
                        _lastMediaEngineError = (MediaEngineErr)param1;
                        ErrorMessageForStatus = _lastMediaEngineError.ToString();
                        break;

                    case MediaEngineEvent.LoadedMetadata:
                        _contentReloaded = true;
                        _engine.Volume = 0.0;
                        _engine.Pause();
                        break;

                    case MediaEngineEvent.FirstFrameReady:
                    case MediaEngineEvent.TimeUpdate:
                        _lastMediaEngineError = MediaEngineErr.Noerror;

                        // Pause the video to (mute audio) if Update hasn't been
                        // called for a while. This will happen when using PlayVideo
                        // with TimeClips.. 
                        var timeSinceLastUpdate = Playback.RunTimeInSecs - _lastUpdateTimeInSecs;
                        if (timeSinceLastUpdate > 10 / 60f)
                        {
                            Log.Debug($"Pausing video due to {timeSinceLastUpdate:0.00}s inactivity", this);
                            _engine.Pause(); // Maybe we should dispose the video instead?
                        }

                        ErrorMessageForStatus = null;

                        break;
                }
            }

            private void SetMediaUrl(string url)
            {
                if (url == _url)
                    return;

                _url = url;
                try
                {
                    DisposeTexture();
                    _engine.Pause();
                    _engine.Source = url;
                }
                catch (SharpDXException e)
                {
                    var unableToSwitchVideoSourceError = "unable to switch video source..." + e.Message;
                    ErrorMessageForStatus = unableToSwitchVideoSourceError;
                    Log.Debug(unableToSwitchVideoSourceError, this);
                }
            }

            // private void InvalidateTexture()
            // {
            //     _hasValidTexture = false;
            // }

            private static VideoNormalizedRect? ToVideoRect(RectangleF? rect)
            {
                if (!rect.HasValue)
                    return default;

                var r = rect.Value;
                return new VideoNormalizedRect()
                           {
                               Left = r.Left.Clamp(0f, 1f),
                               Bottom = r.Bottom.Clamp(0f, 1f),
                               Right = r.Right.Clamp(0f, 1f),
                               Top = r.Top.Clamp(0f, 1f)
                           };
            }

            private static RawColorBGRA? ToRawColorBgra(Color4? color)
            {
                if (!color.HasValue)
                    return default;

                color.Value.ToBgra(out var r, out var g, out var b, out var a);
                return new RawColorBGRA(b, g, r, a);
            }

            // private enum Timing
            // {
            //     Forward,
            //     Backward,
            //     Paused,
            //     Stepped,
            // }

            private enum ReadyStates : short
            {
                /** information is available about the media resource. */
                HaveNothing,

                /** ugh of the media resource has been retrieved that the metadata attributes are initialized. Seeking will no longer raise an exception. */
                HaveMetadata,

                /** a is available for the current playback position, but not enough to actually play more than one frame. */
                HaveCurrentData,

                /** a for the current playback position as well as for at least a little bit of time into the future is available (in other words, at least two frames of video, for example). */
                HaveFutureData,

                /** ugh data is available—and the download rate is high enough—that the media can be played through to the end without interruption.*/
                HaveEnoughData
            }

            public void DisposeTexture()
            {
                Texture?.Dispose();
                Texture = null;
            }

            public void Dispose()
            {
                _engine?.Shutdown();
                _engine?.Dispose();
                Texture?.Dispose();
            }

            private readonly PlayVideo _instance;
            private string _url;

            private double _lastUpdateTimeInSecs;
            private double _lastRequestedTime;
            private long _lastStreamTick;

            private bool _initialized;
            private MediaEngine _engine;
            private bool _contentReloaded;
            private DXGIDeviceManager _dxgiDeviceManager;
            private MediaEngineErr _lastMediaEngineError;

            private Int2 _textureSize = new(0, 0);
            private bool _isSeeking;

            // private float _requestedSeekTime;
            private double _seekOperationStartTime;

            private readonly Dictionary<string, DataChannel> _profilingChannels = new();
            private double _playbackStartTime;

            private void Trace(string key, object o)
            {
                var exists = _profilingChannels.TryGetValue(key, out var channel);
                DebugDataRecording.KeepTraceData(_instance, key, o, ref channel);
                if (!exists)
                    _profilingChannels.Add(key, channel);
            }

            public PlaybackController(PlayVideo playVideo)
            {
                _instance = playVideo;
            }
        }
    }
}