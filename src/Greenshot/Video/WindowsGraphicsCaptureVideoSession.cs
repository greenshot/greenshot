/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Interfaces.Video;
using Greenshot.Native;
using Greenshot.Native.Audio;
using Greenshot.Native.DirectX;
using log4net;
using Microsoft.Win32;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage.Streams;

namespace Greenshot.Video
{
    /// <summary>
    /// Executes and manages a hardware-accelerated video recording session using
    /// Windows.Graphics.Capture, Direct3D 11, and Windows.Media.Transcoding.
    /// </summary>
    public sealed class WindowsGraphicsCaptureVideoSession : IVideoRecordingSession
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsGraphicsCaptureVideoSession));

        private readonly VideoCaptureOptions _options;
        private readonly object _lock = new object();
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private RecordingState _state = RecordingState.Unstarted;
        private TimeSpan _pausedOffset = TimeSpan.Zero;
        private DateTime? _pauseStartTime;
        private System.Threading.Timer _durationTimer;
        private PowerHelper _powerHelper;

        // DirectX & WGC
        private ID3D11Device _d3d11Device;
        private ID3D11DeviceContext _context;
        private IDirect3DDevice _winrtDevice;
        private GraphicsCaptureItem _captureItem;
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _captureSession;

        // Staging ring buffer resources for thread-safe encoding & GPU cropping
        private const int StagingBufferCount = 3;
        private readonly ID3D11Texture2D[] _stagingTextures = new ID3D11Texture2D[StagingBufferCount];
        private readonly IDirect3DSurface[] _stagingSurfaces = new IDirect3DSurface[StagingBufferCount];
        private int _stagingIndex = 0;
        private IDirect3DSurface _lastFrameSurface;
        private TimeSpan _lastSampleTimestamp = TimeSpan.MinValue;

        private int _finalWidth;
        private int _finalHeight;
        private bool _isCroppingRequired;
        private D3D11_BOX _cropBox;

        // Synchronization events
        private readonly ManualResetEventSlim _frameEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _stopEvent = new ManualResetEventSlim(false);

        // Media Transcoder & Stream Source
        private MediaStreamSource _mediaStreamSource;
        private Task _transcodeTask;
        private FileStream _fileStream;
        private IRandomAccessStream _randomAccessStream;

        // Audio
        private IAudioCaptureProvider _audioProvider;
        private readonly ConcurrentQueue<AudioSampleEventArgs> _audioQueue = new ConcurrentQueue<AudioSampleEventArgs>();

        // Lifecycle & State
        private readonly TaskCompletionSource<bool> _recordingFinishedTcs = new TaskCompletionSource<bool>();
        private bool _disposed;

        public RecordingState State => _state;
        public TimeSpan Duration => _stopwatch.Elapsed;
        public string OutputFilePath => _options.OutputFilePath;
        public VideoCaptureOptions Options => _options;

        public event EventHandler<RecordingStateChangedEventArgs> StateChanged;
        public event EventHandler<RecordingDurationChangedEventArgs> DurationChanged;
        public event EventHandler<RecordingErrorEventArgs> ErrorOccurred;

        public WindowsGraphicsCaptureVideoSession(VideoCaptureOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (_options.Target == null) throw new ArgumentException("Capture target must be specified.", nameof(options));

            EnsureOutputFilePath();
        }

        private void EnsureOutputFilePath()
        {
            if (string.IsNullOrWhiteSpace(_options.OutputFilePath))
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Greenshot");
                Directory.CreateDirectory(directory);
                _options.OutputFilePath = Path.Combine(directory, $"Greenshot_Recording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
            }
            else
            {
                var dir = Path.GetDirectoryName(_options.OutputFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        internal async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                WindowsGraphicsCaptureInterop.CreateD3D11Device(out _d3d11Device, out _context);
                _winrtDevice = WindowsGraphicsCaptureInterop.CreateID3DDeviceFromD3D11Device(_d3d11Device);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize dedicated Direct3D 11 device for video recording: {ex.Message}", ex);
                throw new InvalidOperationException("Failed to acquire or initialize Direct3D 11 device for video recording.", ex);
            }

            // Prevent system sleep during recording
            if (_options.PreventSleepWhileRecording)
            {
                _powerHelper = new PowerHelper();
            }

            // Hook lock and power events
            if (_options.AutoPauseOnSessionLock)
            {
                SystemEvents.SessionSwitch += OnSessionSwitch;
            }
            SystemEvents.PowerModeChanged += OnPowerModeChanged;

            // 1. Initialize capture item
            InitializeCaptureItem();

            // 2. Compute final dimensions (even values required for H.264/HEVC)
            ComputeFinalDimensions();

            // 3. Allocate staging textures ring buffer for lock-free GPU rendering
            InitializeStagingTextures();

            // 4. Initialize audio provider if enabled
            InitializeAudio();

            // 5. Initialize FramePool & start capture session FIRST so initial frames are buffered
            InitializeFramePool();
            _captureSession.StartCapture();

            // Wait up to 2 seconds for the very first frame to arrive from compositor
            int initialWait = WaitHandle.WaitAny(new[] { _stopEvent.WaitHandle, _frameEvent.WaitHandle }, 2000);
            if (initialWait == 0)
            {
                throw new OperationCanceledException("Capture was cancelled before the first frame arrived.");
            }

            // 6. Initialize Media Transcoder & MediaStreamSource
            await InitializeTranscoderAsync();

            // 7. Start recording state and timer
            _stopwatch.Restart();
            ChangeState(RecordingState.Recording);

            _audioProvider?.Start();

            // Start timer to emit duration updates
            _durationTimer = new System.Threading.Timer(OnDurationTimerTick, null, 1000, 1000);
        }

        private void InitializeCaptureItem()
        {
            switch (_options.Target.TargetType)
            {
                case VideoCaptureTargetType.Monitor:
                    _captureItem = WindowsGraphicsCaptureInterop.CreateCaptureItemForMonitor(_options.Target.MonitorHandle);
                    break;
                case VideoCaptureTargetType.Window:
                    _captureItem = WindowsGraphicsCaptureInterop.CreateCaptureItemForWindow(_options.Target.WindowHandle);
                    break;
                case VideoCaptureTargetType.Region:
                    // Find monitor containing region
                    var bounds = _options.Target.RegionBounds;
                    var display = DisplayInfo.AllDisplayInfos
                        .Select(d => new { Display = d, Intersection = d.Bounds.Intersect(bounds) })
                        .Where(x => !x.Intersection.IsEmpty)
                        .OrderByDescending(x => x.Intersection.Width * x.Intersection.Height)
                        .Select(x => x.Display)
                        .FirstOrDefault()
                        ?? DisplayInfo.AllDisplayInfos.FirstOrDefault(d => d.IsPrimary)
                        ?? DisplayInfo.AllDisplayInfos.First();

                    IntPtr hMonitor = display.MonitorHandle;
                    _captureItem = WindowsGraphicsCaptureInterop.CreateCaptureItemForMonitor(hMonitor);

                    // Compute crop box relative to monitor
                    int cropX = Math.Max(0, bounds.X - display.Bounds.X);
                    int cropY = Math.Max(0, bounds.Y - display.Bounds.Y);
                    int cropWidth = Math.Min(bounds.Width, display.Bounds.Width - cropX);
                    int cropHeight = Math.Min(bounds.Height, display.Bounds.Height - cropY);
                    _cropBox = new D3D11_BOX((uint)cropX, (uint)cropY, (uint)(cropX + cropWidth), (uint)(cropY + cropHeight));
                    _isCroppingRequired = true;
                    break;
            }

            if (_captureItem == null)
            {
                throw new InvalidOperationException($"Failed to create GraphicsCaptureItem for {_options.Target}.");
            }
        }

        private void ComputeFinalDimensions()
        {
            int baseWidth = _isCroppingRequired ? _options.Target.RegionBounds.Width : _captureItem.Size.Width;
            int baseHeight = _isCroppingRequired ? _options.Target.RegionBounds.Height : _captureItem.Size.Height;

            int targetW = baseWidth;
            int targetH = baseHeight;

            if (_options.TargetSize.HasValue)
            {
                targetW = _options.TargetSize.Value.Width;
                targetH = _options.TargetSize.Value.Height;
            }
            else if (_options.ScaleFactor.HasValue && _options.ScaleFactor.Value > 0)
            {
                targetW = (int)(baseWidth * _options.ScaleFactor.Value);
                targetH = (int)(baseHeight * _options.ScaleFactor.Value);
            }

            // Enforce even dimensions for video macroblocks
            _finalWidth = Math.Max(64, targetW & ~1);
            _finalHeight = Math.Max(64, targetH & ~1);
        }

        private void InitializeStagingTextures()
        {
            var desc = new D3D11_TEXTURE2D_DESC
            {
                Width = _finalWidth,
                Height = _finalHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = 87, // DXGI_FORMAT_B8G8R8A8_UNORM
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = 0x20 | 0x08, // D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE
                CPUAccessFlags = 0,
                MiscFlags = 0
            };

            for (int i = 0; i < StagingBufferCount; i++)
            {
                _d3d11Device.CreateTexture2D(ref desc, IntPtr.Zero, out _stagingTextures[i]);
                if (_stagingTextures[i] == null)
                {
                    throw new InvalidOperationException($"Failed to create Direct3D 11 staging texture {i}.");
                }
                _stagingSurfaces[i] = WindowsGraphicsCaptureInterop.CreateDirect3D11SurfaceFromTexture2D(_stagingTextures[i]);
                if (_stagingSurfaces[i] == null)
                {
                    throw new InvalidOperationException($"Failed to create Direct3D 11 surface for staging texture {i}.");
                }
            }
        }

        private void InitializeAudio()
        {
            if (_options.AudioSource == AudioCaptureSource.None) return;

            _audioProvider = _options.CustomAudioProvider ?? new WasapiAudioCaptureProvider(_options.AudioSource);
            _audioProvider.SampleAvailable += OnAudioSampleAvailable;
        }

        private void OnAudioSampleAvailable(object sender, AudioSampleEventArgs e)
        {
            if (_state == RecordingState.Recording)
            {
                _audioQueue.Enqueue(e);
            }
        }

        private async Task InitializeTranscoderAsync()
        {
            MediaEncodingProfile profile;
            if (_options.Format == VideoFormat.Mp4_HEVC)
            {
                profile = MediaEncodingProfile.CreateHevc(VideoEncodingQuality.Auto);
            }
            else
            {
                profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
            }

            profile.Video.Width = (uint)_finalWidth;
            profile.Video.Height = (uint)_finalHeight;
            profile.Video.FrameRate.Numerator = (uint)_options.FrameRate;
            profile.Video.FrameRate.Denominator = 1;
            profile.Video.PixelAspectRatio.Numerator = 1;
            profile.Video.PixelAspectRatio.Denominator = 1;

            if (_options.Bitrate.HasValue && _options.Bitrate.Value > 0)
            {
                profile.Video.Bitrate = (uint)_options.Bitrate.Value;
            }
            else
            {
                // Sensible default bitrate
                profile.Video.Bitrate = (uint)Math.Max(1_500_000, (_finalWidth * _finalHeight * (long)_options.FrameRate / 20));
            }

            // Input descriptor for MediaStreamSource MUST describe uncompressed raw frames (Bgra8)
            var videoProperties = VideoEncodingProperties.CreateUncompressed(MediaEncodingSubtypes.Bgra8, (uint)_finalWidth, (uint)_finalHeight);
            videoProperties.FrameRate.Numerator = (uint)_options.FrameRate;
            videoProperties.FrameRate.Denominator = 1;
            videoProperties.PixelAspectRatio.Numerator = 1;
            videoProperties.PixelAspectRatio.Denominator = 1;

            var videoDescriptor = new VideoStreamDescriptor(videoProperties);

            if (_audioProvider != null)
            {
                profile.Audio = AudioEncodingProperties.CreateAac((uint)_audioProvider.SampleRate, (uint)_audioProvider.Channels, 192000);
                var audioProperties = AudioEncodingProperties.CreatePcm((uint)_audioProvider.SampleRate, (uint)_audioProvider.Channels, (uint)_audioProvider.BitsPerSample);
                var audioDescriptor = new AudioStreamDescriptor(audioProperties);
                _mediaStreamSource = new MediaStreamSource(videoDescriptor, audioDescriptor);
            }
            else
            {
                _mediaStreamSource = new MediaStreamSource(videoDescriptor);
            }

            _mediaStreamSource.BufferTime = TimeSpan.Zero;
            _mediaStreamSource.Starting += OnMediaStreamSourceStarting;
            _mediaStreamSource.SampleRequested += OnMediaStreamSourceSampleRequested;
            _mediaStreamSource.Closed += OnMediaStreamSourceClosed;

            _fileStream = new FileStream(_options.OutputFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            _randomAccessStream = _fileStream.AsRandomAccessStream();

            var transcoder = new MediaTranscoder
            {
                HardwareAccelerationEnabled = true
            };

            var prepareOp = await transcoder.PrepareMediaStreamSourceTranscodeAsync(_mediaStreamSource, _randomAccessStream, profile);
            if (!prepareOp.CanTranscode)
            {
                throw new InvalidOperationException($"Cannot transcode video: {prepareOp.FailureReason}");
            }

            _transcodeTask = prepareOp.TranscodeAsync().AsTask();
        }

        private void InitializeFramePool()
        {
            var pixelFormat = DirectXPixelFormat.B8G8R8A8UIntNormalized;
            _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(_winrtDevice, pixelFormat, 2, _captureItem.Size);
            _captureSession = _framePool.CreateCaptureSession(_captureItem);

            _captureSession.IsCursorCaptureEnabled = _options.CaptureCursor;

            if ((object)_captureSession is IGraphicsCaptureSession3 session3)
            {
                try
                {
                    session3.IsBorderRequired = _options.ShowCaptureBorder;
                }
                catch { }
            }

            _framePool.FrameArrived += OnFrameArrived;
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            if (_disposed || _stopEvent.IsSet) return;

            try
            {
                using var frame = sender.TryGetNextFrame();
                if (frame == null) return;

                lock (_lock)
                {
                    _stagingIndex = (_stagingIndex + 1) % StagingBufferCount;
                    var targetTexture = _stagingTextures[_stagingIndex];
                    var targetSurface = _stagingSurfaces[_stagingIndex];

                    var sourceTexture = WindowsGraphicsCaptureInterop.CreateTexture2DFromID3DSurface(frame.Surface);
                    try
                    {
                        if (sourceTexture != null && targetTexture != null)
                        {
                            if (_isCroppingRequired)
                            {
                                _context.CopySubresourceRegion(
                                    targetTexture, 0, 0, 0, 0,
                                    sourceTexture, 0, ref _cropBox);
                            }
                            else if (frame.ContentSize.Width == _finalWidth && frame.ContentSize.Height == _finalHeight)
                            {
                                _context.CopyResource(targetTexture, sourceTexture);
                            }
                            else
                            {
                                uint copyW = (uint)Math.Min(frame.ContentSize.Width, _finalWidth);
                                uint copyH = (uint)Math.Min(frame.ContentSize.Height, _finalHeight);
                                var box = new D3D11_BOX(0, 0, copyW, copyH);
                                _context.CopySubresourceRegion(
                                    targetTexture, 0, 0, 0, 0,
                                    sourceTexture, 0, ref box);
                            }

                            _lastFrameSurface = targetSurface;
                        }
                    }
                    finally
                    {
                        if (sourceTexture != null) Marshal.ReleaseComObject(sourceTexture);
                    }
                }

                _frameEvent.Set();
            }
            catch (Exception ex)
            {
                Log.Warn("Exception in OnFrameArrived: " + ex.Message, ex);
            }
        }

        private void OnMediaStreamSourceStarting(MediaStreamSource sender, MediaStreamSourceStartingEventArgs args)
        {
            args.Request.SetActualStartPosition(TimeSpan.Zero);
        }

        private void OnMediaStreamSourceSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
        {
            if (_stopEvent.IsSet || _state == RecordingState.Finalizing || _state == RecordingState.Stopped || _state == RecordingState.Cancelled)
            {
                args.Request.Sample = null;
                return;
            }

            var deferral = args.Request.GetDeferral();
            try
            {
                if (args.Request.StreamDescriptor is VideoStreamDescriptor)
                {
                    // If paused, wait until resumed or stopped
                    while (_state == RecordingState.Paused && !_stopEvent.IsSet)
                    {
                        Thread.Sleep(30);
                    }

                    if (_stopEvent.IsSet || (_state != RecordingState.Recording && _state != RecordingState.Unstarted))
                    {
                        args.Request.Sample = null;
                        return;
                    }

                    // Frame pacing: wait for next frame arrived event or frame interval timeout for static scenes
                    int frameIntervalMs = Math.Max(5, (int)(1000.0 / _options.FrameRate));
                    int timeoutMs = _lastFrameSurface == null ? 1000 : frameIntervalMs;

                    _frameEvent.Reset();
                    int waitResult = WaitHandle.WaitAny(new[] { _stopEvent.WaitHandle, _frameEvent.WaitHandle }, timeoutMs);

                    if (waitResult == 0 || _stopEvent.IsSet)
                    {
                        // Stop signaled -> End of Stream
                        args.Request.Sample = null;
                        return;
                    }

                    IDirect3DSurface surface;
                    lock (_lock)
                    {
                        surface = _lastFrameSurface;
                    }

                    if (surface != null)
                    {
                        TimeSpan timestamp = _stopwatch.Elapsed;
                        if (timestamp <= _lastSampleTimestamp)
                        {
                            timestamp = _lastSampleTimestamp + TimeSpan.FromTicks(10);
                        }
                        _lastSampleTimestamp = timestamp;

                        var sample = MediaStreamSample.CreateFromDirect3D11Surface(surface, timestamp);
                        sample.Duration = TimeSpan.FromSeconds(1.0 / _options.FrameRate);
                        args.Request.Sample = sample;
                    }
                    else
                    {
                        args.Request.Sample = null;
                    }
                }
                else if (args.Request.StreamDescriptor is AudioStreamDescriptor)
                {
                    // Audio descriptor handling
                    args.Request.Sample = null;
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Error fulfilling sample request: " + ex.Message, ex);
                ErrorOccurred?.Invoke(this, new RecordingErrorEventArgs(ex, false));
                args.Request.Sample = null;
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void OnMediaStreamSourceClosed(MediaStreamSource sender, MediaStreamSourceClosedEventArgs args)
        {
            _recordingFinishedTcs.TrySetResult(true);
        }

        public Task PauseAsync()
        {
            lock (_lock)
            {
                if (_state != RecordingState.Recording) return Task.CompletedTask;

                _stopwatch.Stop();
                _pauseStartTime = DateTime.UtcNow;
                _audioProvider?.Pause();

                ChangeState(RecordingState.Paused);
            }
            return Task.CompletedTask;
        }

        public Task ResumeAsync()
        {
            lock (_lock)
            {
                if (_state != RecordingState.Paused) return Task.CompletedTask;

                if (_pauseStartTime.HasValue)
                {
                    _pausedOffset += (DateTime.UtcNow - _pauseStartTime.Value);
                    _pauseStartTime = null;
                }

                _audioProvider?.Resume();
                _stopwatch.Start();

                ChangeState(RecordingState.Recording);
            }
            return Task.CompletedTask;
        }

        public async Task<VideoRecordingResult> StopAsync()
        {
            lock (_lock)
            {
                if (_state == RecordingState.Stopped || _state == RecordingState.Finalizing)
                {
                    return CreateResult();
                }
                ChangeState(RecordingState.Finalizing);
            }

            _durationTimer?.Dispose();
            _durationTimer = null;
            _stopwatch.Stop();
            _audioProvider?.Stop();

            // 1. Signal stop to unblock any pending SampleRequested and deliver EOS to MediaTranscoder
            _stopEvent.Set();
            _frameEvent.Set();

            // 2. Await transcode task completion (flushes MP4 container and writes moov atom)
            if (_transcodeTask != null)
            {
                try
                {
                    var completed = await Task.WhenAny(_transcodeTask, Task.Delay(5000));
                    if (completed == _transcodeTask)
                    {
                        if (_transcodeTask.IsFaulted)
                        {
                            Log.Warn($"Transcode task faulted: {_transcodeTask.Exception?.GetBaseException()}", _transcodeTask.Exception);
                        }
                    }
                    else
                    {
                        Log.Warn($"Transcode task timed out after 5 seconds (Status: {_transcodeTask.Status}).");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn("Exception awaiting transcode completion: " + ex.Message, ex);
                }
            }

            // 3. Now clean up capture session and frame pool
            try
            {
                _captureSession?.Dispose();
                _captureSession = null;
                _framePool?.Dispose();
                _framePool = null;
            }
            catch (Exception ex)
            {
                Log.Warn("Error disposing capture session: " + ex.Message, ex);
            }

            // 4. Close streams after transcode has finished writing
            CloseStreams();
            ChangeState(RecordingState.Stopped);

            return CreateResult();
        }

        public async Task CancelAsync()
        {
            lock (_lock)
            {
                if (_state == RecordingState.Cancelled || _state == RecordingState.Stopped) return;
                ChangeState(RecordingState.Cancelled);
            }

            _durationTimer?.Dispose();
            _durationTimer = null;
            _stopwatch.Stop();
            _audioProvider?.Stop();

            _stopEvent.Set();
            _frameEvent.Set();

            if (_transcodeTask != null)
            {
                try
                {
                    await Task.WhenAny(_transcodeTask, Task.Delay(1000));
                }
                catch { }
            }

            try
            {
                _captureSession?.Dispose();
                _captureSession = null;
                _framePool?.Dispose();
                _framePool = null;
            }
            catch { }

            CloseStreams();

            // Delete partial output file
            try
            {
                if (File.Exists(_options.OutputFilePath))
                {
                    File.Delete(_options.OutputFilePath);
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Failed to delete cancelled recording file: " + ex.Message);
            }
        }

        private VideoRecordingResult CreateResult()
        {
            long fileSize = 0;
            try
            {
                if (File.Exists(_options.OutputFilePath))
                {
                    fileSize = new FileInfo(_options.OutputFilePath).Length;
                }
            }
            catch { }

            return new VideoRecordingResult(
                _options.OutputFilePath,
                _stopwatch.Elapsed,
                fileSize,
                _finalWidth,
                _finalHeight,
                _options.FrameRate,
                _options.Format,
                _options.AudioSource != AudioCaptureSource.None);
        }

        private void CloseStreams()
        {
            try
            {
                _randomAccessStream?.Dispose();
                _randomAccessStream = null;
            }
            catch { }

            try
            {
                _fileStream?.Dispose();
                _fileStream = null;
            }
            catch { }
        }

        private void OnDurationTimerTick(object state)
        {
            if (_state == RecordingState.Recording)
            {
                DurationChanged?.Invoke(this, new RecordingDurationChangedEventArgs(_stopwatch.Elapsed));
            }
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                Log.Info("Session locked (Win+L detected), pausing video recording.");
                PauseAsync();
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                Log.Info("Session unlocked, resuming video recording.");
                ResumeAsync();
            }
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                Log.Warn("System entering suspend/sleep, auto-finalizing recording to prevent corruption.");
                StopAsync().GetAwaiter().GetResult();
            }
        }

        private void ChangeState(RecordingState newState)
        {
            var old = _state;
            _state = newState;
            StateChanged?.Invoke(this, new RecordingStateChangedEventArgs(old, newState));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;

            _powerHelper?.Dispose();
            _powerHelper = null;

            _durationTimer?.Dispose();
            _durationTimer = null;

            _audioProvider?.Dispose();
            _audioProvider = null;

            _stopEvent.Set();
            _frameEvent.Set();

            try { _captureSession?.Dispose(); } catch { }
            try { _framePool?.Dispose(); } catch { }

            for (int i = 0; i < StagingBufferCount; i++)
            {
                if (_stagingSurfaces[i] != null)
                {
                    try { _stagingSurfaces[i].Dispose(); } catch { }
                    _stagingSurfaces[i] = null;
                }
                if (_stagingTextures[i] != null)
                {
                    try { Marshal.ReleaseComObject(_stagingTextures[i]); } catch { }
                    _stagingTextures[i] = null;
                }
            }
            _lastFrameSurface = null;

            if (_winrtDevice != null)
            {
                try { (_winrtDevice as IDisposable)?.Dispose(); } catch { }
                _winrtDevice = null;
            }
            if (_context != null)
            {
                try { Marshal.ReleaseComObject(_context); } catch { }
                _context = null;
            }
            if (_d3d11Device != null)
            {
                try { Marshal.ReleaseComObject(_d3d11Device); } catch { }
                _d3d11Device = null;
            }

            CloseStreams();
        }
    }
}
