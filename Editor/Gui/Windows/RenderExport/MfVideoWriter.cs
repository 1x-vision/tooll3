/*

Based on the MIT license video writing example at
https://github.com/jtpgames/Kinect-Recorder/blob/master/KinectRecorder/Multimedia/MediaFoundationVideoWriter.cs
Copyright(c) 2016 Juri Tomak
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.MediaFoundation;
using SharpDX.WIC;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Resource;
using MF = SharpDX.MediaFoundation;

namespace T3.Editor.Gui.Windows.RenderExport;

internal abstract class MfVideoWriter : IDisposable
{
    private MfVideoWriter(string filePath, Int2 videoPixelSize, Guid videoInputFormat, bool supportAudio = false)
    {
        if (!_mfInitialized)
        {
            // Initialize MF library. MUST be called before any MF related operations.
            MF.MediaFactory.Startup(MF.MediaFactory.Version, 0);
            _mfInitialized = true;
        }

        // Set initial default values
        FilePath = filePath;
        _videoPixelSize = videoPixelSize;
        _videoInputFormat = videoInputFormat;
        _supportAudio = supportAudio;
        Bitrate = 2000000;
        Framerate = 60; //TODO: is this actually used?
        _frameIndex = -1;
    }    
    
    public string FilePath { get; }

    // skip a certain number of images at the beginning since the
    // final content will only appear after several buffer flips
    public const int SkipImages = 1;

    protected MfVideoWriter(string filePath, Int2 videoPixelSize, bool supportAudio = false)
        : this(filePath, videoPixelSize, _videoInputFormatId, supportAudio)
    {
    }

    public static readonly List<SharpDX.DXGI.Format> SupportedFormats = new List<SharpDX.DXGI.Format>
        { SharpDX.DXGI.Format.R8G8B8A8_UNorm };

    /// <summary>
    /// Returns true if a frame has been written
    /// </summary>
    public bool ProcessFrames(ref Texture2D gpuTexture, ref byte[] audioFrame, int channels, int sampleRate)
    {
        try
        {
            if (gpuTexture == null)
            {
                throw new InvalidOperationException("Handed frame was null");
            }

            var currentDesc = gpuTexture.Description;
            if (currentDesc.Width == 0 || currentDesc.Height == 0)
            {
                throw new InvalidOperationException("Empty image handed over");
            }

            // Setup writer
            if (SinkWriter == null)
            {
                SinkWriter = CreateSinkWriter(FilePath);
                CreateMediaTarget(SinkWriter, _videoPixelSize, out _streamIndex);

                // Configure media type of video
                using (var mediaTypeIn = new MF.MediaType())
                {
                    mediaTypeIn.Set(MF.MediaTypeAttributeKeys.MajorType, MF.MediaTypeGuids.Video);
                    mediaTypeIn.Set(MF.MediaTypeAttributeKeys.Subtype, _videoInputFormat);
                    mediaTypeIn.Set(MF.MediaTypeAttributeKeys.InterlaceMode, (int)MF.VideoInterlaceMode.Progressive);
                    mediaTypeIn.Set(MF.MediaTypeAttributeKeys.FrameSize, MfHelper.GetMfEncodedIntsByValues(_videoPixelSize.Width, _videoPixelSize.Height));
                    mediaTypeIn.Set(MF.MediaTypeAttributeKeys.FrameRate, MfHelper.GetMfEncodedIntsByValues(Framerate, 1));
                    SinkWriter.SetInputMediaType(_streamIndex, mediaTypeIn, null);
                }

                // Create audio support?
                if (_supportAudio)
                {
                    // initialize audio writer
                    var waveFormat = WaveFormatExtension.DefaultIeee;
                    //var waveFormat = WaveFormatExtension.DefaultPcm;
                    waveFormat._nChannels = (ushort)channels;
                    waveFormat._nSamplesPerSec = (uint)sampleRate;
                    waveFormat._nBlockAlign = (ushort)(waveFormat._nChannels * waveFormat._wBitsPerSample / 8);
                    waveFormat._nAvgBytesPerSec = waveFormat._nSamplesPerSec * waveFormat._nBlockAlign;
                    //_audioWriter = new FlacAudioWriter(SinkWriter, ref waveFormat);
                    _audioWriter = new Mp3AudioWriter(SinkWriter, ref waveFormat);
                }

                // Start writing the video file. MUST be called before write operations.
                SinkWriter.BeginWriting();
            }
        }
        catch (Exception e)
        {
            SinkWriter?.Dispose();
            SinkWriter = null;
            throw new InvalidOperationException(e +
                                                "(image size may be unsupported with the requested codec)");
        }

        Sample audioSample = null;
        if (_audioWriter != null)
        {
            if (audioFrame != null && audioFrame.Length != 0)
            {
                //Log.Debug("adding audio");
                audioSample = _audioWriter.CreateSampleFromFrame(ref audioFrame);
            }
            else
            {
                Log.Debug("audio missing");
            }
        }

        // Save last sample (includes image and timing information)
        var savedFrame = false;
        if (_lastSample != null &&
            (!_supportAudio || audioSample != null))
        {
            try
            {
                // Write to stream
                var samples = new Dictionary<int, Sample>();
                if (_lastSample != null)
                    samples.Add(StreamIndex, _lastSample);
                if (_audioWriter != null && audioSample != null)
                    samples.Add(_audioWriter.StreamIndex, audioSample);

                WriteSamples(samples);
                savedFrame = true;
            }
            catch (SharpDXException e)
            {
                Debug.WriteLine(e.Message);
                throw new InvalidOperationException(e.Message);
            }
            finally
            {
                if (_lastSample != null)
                {
                    _lastSample?.Dispose();
                    _lastSample = null;
                }
                audioSample?.Dispose();
            }
        }

        // Initiate reading next frame
        if (!TextureReadAccess.InitiateRead(gpuTexture, SaveSampleAfterReadback))
        {
            Log.Warning("Can't initiate texture readback");
        }

        return savedFrame;
    }



    private void SaveSampleAfterReadback(TextureReadAccess.ReadRequestItem readRequestItem)
    {
        if (_lastSample != null)
        {
             Log.Warning("Discarding previous video sample...");
             _lastSample?.Dispose();
             _lastSample = null;
        }

        var cpuAccessTexture = readRequestItem.CpuAccessTexture;
        if (cpuAccessTexture == null || cpuAccessTexture.IsDisposed)
            return;
        
        // Map image resource to get a stream we can read from
        var dataBox = ResourceManager.Device.ImmediateContext.MapSubresource(cpuAccessTexture,
                                                                             0,
                                                                             0,
                                                                             MapMode.Read,
                                                                             SharpDX.Direct3D11.MapFlags.None,
                                                                             out var inputStream);

        // Create an 8 bit RGBA output buffer to write to
        var width = cpuAccessTexture.Description.Width;
        var height = cpuAccessTexture.Description.Height;
        var formatId = PixelFormat.Format32bppRGBA;
        var rowStride = PixelFormat.GetStride(formatId, width);

        var outBufferSize = height * rowStride;
        var outputStream = new DataStream(outBufferSize, true, true);

        // Write all contents to the MediaBuffer for media foundation
        var mediaBufferLength = RgbaSizeInBytes(ref cpuAccessTexture);
        var mediaBuffer = MF.MediaFactory.CreateMemoryBuffer(mediaBufferLength);
        var mediaBufferPointer = mediaBuffer.Lock(out _, out _);

        // Note: dataBox.RowPitch and outputStream.RowPitch can diverge if width is not divisible by 16.
        for (var loopY = 0; loopY < _videoPixelSize.Height; loopY++)
        {
            if (!FlipY)
                inputStream.Position = (long)(loopY) * dataBox.RowPitch;
            else
                inputStream.Position = (long)(_videoPixelSize.Height - 1 - loopY) * dataBox.RowPitch;

            outputStream.WriteRange(inputStream.ReadRange<byte>(rowStride));
        }

        // Copy our finished BGRA buffer to the media buffer pointer
        for (var loopY = 0; loopY < height; loopY++)
        {
            var index = loopY * rowStride;
            for (var loopX = width; loopX > 0; --loopX)
            {
                var value = Marshal.ReadInt32(outputStream.DataPointer, index);
                Marshal.WriteInt32(mediaBufferPointer, index, value);
                index += 4;
            }
        }
        inputStream?.Dispose();
        outputStream?.Dispose();
        mediaBuffer.Unlock();
        mediaBuffer.CurrentLength = mediaBufferLength;

        // Create the sample (includes image and timing information)
        _lastSample = MF.MediaFactory.CreateSample();
        _lastSample.AddBuffer(mediaBuffer);
        
        mediaBuffer.Dispose();
    }

    private static SinkWriter CreateSinkWriter(string outputFile)
    {
        SinkWriter writer;
        using var attributes = new MediaAttributes();
        MediaFactory.CreateAttributes(attributes, 1);
        attributes.Set(SinkWriterAttributeKeys.ReadwriteEnableHardwareTransforms.Guid, (UInt32)1);
        try
        {
            writer = MediaFactory.CreateSinkWriterFromURL(outputFile, null, attributes);
        }
        catch (COMException e)
        {
            if (e.ErrorCode == unchecked((int)0xC00D36D5))
            {
                throw new ArgumentException("Was not able to create a sink writer for this file extension");
            }

            throw;
        }

        return writer;
    }


    /// <summary>
    /// get minimum image buffer size in bytes if imager is RGBA converted
    /// </summary>
    /// <param name="frame">texture to get information from</param>
    public static int RgbaSizeInBytes(ref Texture2D frame)
    {
        var currentDesc = frame.Description;
        const int bitsPerPixel = 32;
        return (currentDesc.Width * currentDesc.Height * bitsPerPixel + 7) / 8;
    }


    // FIXME: Would possibly need some refactoring not to duplicate code from ScreenshotWriter
    private static float Read2BytesToHalf(DataStream imageStream)
    {
        var low = (byte)imageStream.ReadByte();
        var high = (byte)imageStream.ReadByte();
        return FormatConversion.ToTwoByteFloat(low, high);
    }

    private void WriteSamples(Dictionary<int, Sample> samples)
    {
        ++_frameIndex;

        long frameDuration;
        MediaFactory.FrameRateToAverageTimePerFrame(Framerate, 1, out frameDuration);

        foreach (var item in samples)
        {
            var streamIndex = item.Key;
            var sample = item.Value;
            if (sample != null)
            {
                sample.SampleTime = frameDuration * _frameIndex;
                sample.SampleDuration = frameDuration;
                SinkWriter.WriteSample(streamIndex, sample);
            }
        }
    }

    /// <summary>
    /// Creates a media target.
    /// </summary>
    /// <param name="sinkWriter">The previously created SinkWriter.</param>
    /// <param name="videoPixelSize">The pixel size of the video.</param>
    /// <param name="streamIndex">The stream index for the new target.</param>
    protected abstract void CreateMediaTarget(MF.SinkWriter sinkWriter, Int2 videoPixelSize, out int streamIndex);

    /// <summary>
    /// Internal use: FlipY during rendering?
    /// </summary>
    protected virtual bool FlipY => false;

    public int Bitrate { get; set; }
    public int Framerate { get; set; }

    #region IDisposable Support
    public void Dispose()
    {
        if (SinkWriter != null)
        {
            // since we will try to write on shutdown, things can still go wrong
            try
            {
                SinkWriter.NotifyEndOfSegment(_streamIndex);
                if (_frameIndex > 0)
                {
                    SinkWriter.Finalize();
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(e.Message);
            }
            finally
            {
                SinkWriter.Dispose();
                SinkWriter = null;
            }
        }

        TextureReadAccess.DisposeTextures();
    }
    #endregion


    
    #region Resources for MediaFoundation video rendering
    private MF.Sample _lastSample;
    // private MF.ByteStream outStream;
    private readonly Int2 _videoPixelSize;
    private int _frameIndex;
    private int _streamIndex;
    #endregion

    private int StreamIndex => _streamIndex;

    private MF.SinkWriter SinkWriter { get; set; }
    private MediaFoundationAudioWriter _audioWriter;

    private static readonly Guid _videoInputFormatId = MF.VideoFormatGuids.Rgb32;
    private bool _supportAudio;
    private static bool _mfInitialized = false;
    private readonly Guid _videoInputFormat;
}

internal class Mp4VideoWriter : MfVideoWriter
{
    private static readonly Guid _h264EncodingFormatId = MF.VideoFormatGuids.H264;

    public Mp4VideoWriter(string filePath, Int2 videoPixelSize, bool supportAudio = false)
        : base(filePath, videoPixelSize, supportAudio)
    {
    }

    protected override void CreateMediaTarget(SinkWriter sinkWriter, Int2 videoPixelSize, out int streamIndex)
    {
        using var mediaTypeOut = new MF.MediaType();
        mediaTypeOut.Set(MF.MediaTypeAttributeKeys.MajorType, MF.MediaTypeGuids.Video);
        mediaTypeOut.Set(MF.MediaTypeAttributeKeys.Subtype, _h264EncodingFormatId);
        mediaTypeOut.Set(MF.MediaTypeAttributeKeys.AvgBitrate, Bitrate);
        mediaTypeOut.Set(MF.MediaTypeAttributeKeys.InterlaceMode, (int)MF.VideoInterlaceMode.Progressive);
        mediaTypeOut.Set(MF.MediaTypeAttributeKeys.FrameSize, MfHelper.GetMfEncodedIntsByValues(videoPixelSize.Width, videoPixelSize.Height));
        mediaTypeOut.Set(MF.MediaTypeAttributeKeys.FrameRate, MfHelper.GetMfEncodedIntsByValues(Framerate, 1));
        sinkWriter.AddStream(mediaTypeOut, out streamIndex);
    }

    /// <summary>
    /// Internal use: FlipY during rendering?
    /// </summary>
    protected override bool FlipY => true;
}