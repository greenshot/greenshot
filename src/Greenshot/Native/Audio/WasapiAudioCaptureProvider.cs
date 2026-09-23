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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces.Video;
using log4net;

namespace Greenshot.Native.Audio
{
    /// <summary>
    /// Captures audio (system loopback or microphone) using Windows Core Audio (WASAPI).
    /// Pure native implementation with zero third-party dependencies.
    /// </summary>
    internal sealed class WasapiAudioCaptureProvider : IAudioCaptureProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WasapiAudioCaptureProvider));

        private readonly AudioCaptureSource _source;
        private IMMDevice _device;
        private IAudioClient _audioClient;
        private IAudioCaptureClient _captureClient;

        private IntPtr _pFormat = IntPtr.Zero;
        private int _sampleRate = 48000;
        private int _channels = 2;
        private int _bitsPerSample = 16;
        private int _bytesPerFrame = 4;

        private Thread _captureThread;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private volatile bool _isPaused;
        private volatile bool _isDisposed;

        public int SampleRate => _sampleRate;
        public int Channels => _channels;
        public int BitsPerSample => _bitsPerSample;

        public event EventHandler<AudioSampleEventArgs> SampleAvailable;

        public WasapiAudioCaptureProvider(AudioCaptureSource source)
        {
            _source = source;
            InitializeWasapi();
        }

        private void InitializeWasapi()
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            try
            {
                var dataFlow = _source == AudioCaptureSource.Microphone ? EDataFlow.eCapture : EDataFlow.eRender;
                int hr = enumerator.GetDefaultAudioEndpoint(dataFlow, ERole.eMultimedia, out _device);
                if (hr != 0 || _device == null)
                {
                    Log.Warn($"Could not get default audio endpoint for {dataFlow} (HRESULT: 0x{hr:X8})");
                    return;
                }

                var iidAudioClient = WasapiGuids.IID_IAudioClient;
                hr = _device.Activate(ref iidAudioClient, 1 /* CLSCTX_INPROC_SERVER */, IntPtr.Zero, out var clientObj);
                if (hr != 0 || clientObj == null)
                {
                    Log.Warn($"Could not activate IAudioClient (HRESULT: 0x{hr:X8})");
                    return;
                }

                _audioClient = (IAudioClient)clientObj;

                hr = _audioClient.GetMixFormat(out _pFormat);
                if (hr != 0 || _pFormat == IntPtr.Zero)
                {
                    Log.Warn($"GetMixFormat failed (HRESULT: 0x{hr:X8})");
                    return;
                }

                var waveFormat = Marshal.PtrToStructure<WAVEFORMATEX>(_pFormat);
                _sampleRate = (int)waveFormat.nSamplesPerSec;
                _channels = waveFormat.nChannels;
                _bitsPerSample = waveFormat.wBitsPerSample;
                _bytesPerFrame = waveFormat.nBlockAlign;

                uint streamFlags = (uint)AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM |
                                   (uint)AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY;

                if (_source == AudioCaptureSource.SystemAudio || _source == AudioCaptureSource.SystemAudioAndMicrophone)
                {
                    streamFlags |= (uint)AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_LOOPBACK;
                }

                // Request 100ms buffer (10,000,000 hns = 1 second)
                long hnsBufferDuration = 1_000_000;
                hr = _audioClient.Initialize(
                    AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
                    streamFlags,
                    hnsBufferDuration,
                    0,
                    _pFormat,
                    IntPtr.Zero);

                if (hr != 0)
                {
                    Log.Warn($"IAudioClient.Initialize failed (HRESULT: 0x{hr:X8})");
                    return;
                }

                var iidCaptureClient = WasapiGuids.IID_IAudioCaptureClient;
                hr = _audioClient.GetService(ref iidCaptureClient, out var captureObj);
                if (hr != 0 || captureObj == null)
                {
                    Log.Warn($"IAudioClient.GetService(IAudioCaptureClient) failed (HRESULT: 0x{hr:X8})");
                    return;
                }

                _captureClient = (IAudioCaptureClient)captureObj;
                Log.Debug($"WASAPI capture initialized: {_sampleRate}Hz, {_channels} channels, {_bitsPerSample} bits");
            }
            finally
            {
                if (enumerator != null) Marshal.ReleaseComObject(enumerator);
            }
        }

        public void Start()
        {
            if (_audioClient == null || _captureClient == null || _isDisposed) return;

            int hr = _audioClient.Start();
            if (hr != 0)
            {
                Log.Warn($"IAudioClient.Start failed (HRESULT: 0x{hr:X8})");
                return;
            }

            _stopwatch.Restart();
            _captureThread = new Thread(CaptureLoop)
            {
                IsBackground = true,
                Name = "Greenshot-WasapiCapture"
            };
            _captureThread.Start();
        }

        public void Pause()
        {
            _isPaused = true;
            _stopwatch.Stop();
        }

        public void Resume()
        {
            _isPaused = false;
            _stopwatch.Start();
        }

        public void Stop()
        {
            if (_isDisposed) return;
            _cts.Cancel();

            try
            {
                _audioClient?.Stop();
            }
            catch (Exception ex)
            {
                Log.Debug("Error stopping audio client: " + ex.Message);
            }

            _captureThread?.Join(500);
            _captureThread = null;
        }

        private void CaptureLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                if (_isPaused)
                {
                    Thread.Sleep(20);
                    continue;
                }

                try
                {
                    int hr = _captureClient.GetNextPacketSize(out uint packetLength);
                    if (hr != 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    while (packetLength > 0 && !_cts.IsCancellationRequested)
                    {
                        hr = _captureClient.GetBuffer(
                            out IntPtr pData,
                            out uint numFramesRead,
                            out uint flags,
                            out _,
                            out _);

                        if (hr != 0) break;

                        if (numFramesRead > 0 && pData != IntPtr.Zero)
                        {
                            int byteCount = (int)(numFramesRead * _bytesPerFrame);
                            byte[] buffer = new byte[byteCount];

                            if ((flags & 0x01 /* AUDCLNT_BUFFERFLAGS_SILENT */) != 0)
                            {
                                Array.Clear(buffer, 0, byteCount);
                            }
                            else
                            {
                                Marshal.Copy(pData, buffer, 0, byteCount);
                            }

                            TimeSpan timestamp = _stopwatch.Elapsed;
                            TimeSpan duration = TimeSpan.FromSeconds((double)numFramesRead / _sampleRate);

                            SampleAvailable?.Invoke(this, new AudioSampleEventArgs(buffer, timestamp, duration));
                        }

                        _captureClient.ReleaseBuffer(numFramesRead);
                        hr = _captureClient.GetNextPacketSize(out packetLength);
                        if (hr != 0) break;
                    }

                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    if (!_cts.IsCancellationRequested)
                    {
                        Log.Warn("Error in WASAPI capture loop: " + ex.Message, ex);
                    }
                    Thread.Sleep(20);
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            Stop();
            _cts.Dispose();

            if (_pFormat != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(_pFormat);
                _pFormat = IntPtr.Zero;
            }

            if (_captureClient != null)
            {
                Marshal.ReleaseComObject(_captureClient);
                _captureClient = null;
            }

            if (_audioClient != null)
            {
                Marshal.ReleaseComObject(_audioClient);
                _audioClient = null;
            }

            if (_device != null)
            {
                Marshal.ReleaseComObject(_device);
                _device = null;
            }
        }
    }
}
