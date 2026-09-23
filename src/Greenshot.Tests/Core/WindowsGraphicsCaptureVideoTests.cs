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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Interfaces.Video;
using Greenshot.Base.Recipes;
using Greenshot.Native;
using Greenshot.Native.DirectX;
using Greenshot.Pipeline.Steps;
using Greenshot.Video;
using Xunit;
using Xunit.Abstractions;

namespace Greenshot.Tests.Core
{
    public class WindowsGraphicsCaptureVideoTests
    {
        private readonly ITestOutputHelper _output;

        public WindowsGraphicsCaptureVideoTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void VideoCaptureOptions_Presets_ConfigureExpectedValues()
        {
            var options = new VideoCaptureOptions();

            options.ApplyPreset(VideoEncodingPreset.SmallSize);
            Assert.Equal(15, options.FrameRate);
            Assert.Equal(800_000, options.Bitrate);
            Assert.Equal(new Size(1280, 720), options.TargetSize);

            options.ApplyPreset(VideoEncodingPreset.Balanced);
            Assert.Equal(30, options.FrameRate);
            Assert.Equal(2_500_000, options.Bitrate);
            Assert.Null(options.TargetSize);

            options.ApplyPreset(VideoEncodingPreset.HighQuality);
            Assert.Equal(60, options.FrameRate);
            Assert.Equal(8_000_000, options.Bitrate);

            options.ApplyPreset(VideoEncodingPreset.HighFrameRateGaming);
            Assert.Equal(144, options.FrameRate);
            Assert.Equal(25_000_000, options.Bitrate);
        }

        [Fact]
        public void VideoCaptureTarget_FromMonitor_ValidatesHandle()
        {
            Assert.Throws<ArgumentException>(() => VideoCaptureTarget.FromMonitor(IntPtr.Zero));

            var target = VideoCaptureTarget.FromMonitor(new IntPtr(1234));
            Assert.Equal(VideoCaptureTargetType.Monitor, target.TargetType);
            Assert.Equal(new IntPtr(1234), target.MonitorHandle);
        }

        [Fact]
        public void VideoCaptureTarget_FromWindow_ValidatesHandle()
        {
            Assert.Throws<ArgumentException>(() => VideoCaptureTarget.FromWindow(IntPtr.Zero));

            var target = VideoCaptureTarget.FromWindow(new IntPtr(5678));
            Assert.Equal(VideoCaptureTargetType.Window, target.TargetType);
            Assert.Equal(new IntPtr(5678), target.WindowHandle);
        }

        [Fact]
        public void VideoCaptureTarget_FromRegion_ValidatesBounds()
        {
            Assert.Throws<ArgumentException>(() => VideoCaptureTarget.FromRegion(new NativeRect(0, 0, 0, 0)));
            Assert.Throws<ArgumentException>(() => VideoCaptureTarget.FromRegion(new NativeRect(0, 0, -10, 50)));

            var target = VideoCaptureTarget.FromRegion(new NativeRect(100, 100, 640, 480));
            Assert.Equal(VideoCaptureTargetType.Region, target.TargetType);
            Assert.Equal(640, target.RegionBounds.Width);
            Assert.Equal(480, target.RegionBounds.Height);
        }

        [Fact]
        public void TestCopyResource_Direct3D11()
        {
            if (!WindowsGraphicsCaptureInterop.GetOrCreateDevice(out var d3d11Device, out var context, out var winrtDevice))
            {
                return;
            }

            var desc = new D3D11_TEXTURE2D_DESC
            {
                Width = 256,
                Height = 256,
                MipLevels = 1,
                ArraySize = 1,
                Format = 87,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = 0x20 | 0x08,
                CPUAccessFlags = 0,
                MiscFlags = 0
            };

            d3d11Device.CreateTexture2D(ref desc, IntPtr.Zero, out var tex1);
            d3d11Device.CreateTexture2D(ref desc, IntPtr.Zero, out var tex2);

            _output.WriteLine($"tex1 type: {((object)tex1).GetType().FullName}");
            _output.WriteLine($"tex1 is ID3D11Resource: {tex1 is ID3D11Resource}");
            _output.WriteLine($"tex1 is ID3D11Texture2D: {tex1 is ID3D11Texture2D}");

            context.CopyResource(tex1, tex2);

            var surface = WindowsGraphicsCaptureInterop.CreateDirect3D11SurfaceFromTexture2D(tex1);
            Assert.NotNull(surface);

            var texFromSurface = WindowsGraphicsCaptureInterop.CreateTexture2DFromID3DSurface(surface);
            Assert.NotNull(texFromSurface);

            _output.WriteLine($"texFromSurface type: {((object)texFromSurface).GetType().FullName}");
            _output.WriteLine($"texFromSurface is ID3D11Resource: {texFromSurface is ID3D11Resource}");
            _output.WriteLine($"texFromSurface is ID3D11Texture2D: {texFromSurface is ID3D11Texture2D}");

            context.CopyResource(tex2, texFromSurface);

            // Now test STA to MTA cross-apartment invocation
            ID3D11Texture2D staTex = null;
            var staThread = new System.Threading.Thread(() =>
            {
                d3d11Device.CreateTexture2D(ref desc, IntPtr.Zero, out staTex);
            });
            staThread.SetApartmentState(System.Threading.ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            _output.WriteLine("Calling context.CopyResource with staTex from MTA thread...");
            context.CopyResource(tex2, staTex);
        }

        [Fact]
        public async Task VideoCaptureService_Validation_ThrowsOnInvalidArguments()
        {
            var service = new WindowsGraphicsCaptureVideoService();

            if (!service.IsSupported)
            {
                _output.WriteLine("WGC not supported on this environment, skipping service validation.");
                return;
            }

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.StartRecordingAsync(null));

            var optionsWithoutTarget = new VideoCaptureOptions();
            await Assert.ThrowsAsync<ArgumentException>(() => service.StartRecordingAsync(optionsWithoutTarget));
        }

        [Fact]
        public void RecordVideoRecipeStep_BuildsFromConfig()
        {
            var nodeConfig = new RecipeNodeConfig
            {
                Id = "rec_video_node",
                StepType = WellKnownStepTypes.RecordVideo,
                Name = "Record Video Step"
            };
            nodeConfig.Parameters["Target"] = "Region";
            nodeConfig.Parameters["FrameRate"] = 60;
            nodeConfig.Parameters["Bitrate"] = 5_000_000;
            nodeConfig.Parameters["DurationSeconds"] = 2;

            var step = new RecordVideoRecipeStep(nodeConfig);
            Assert.Equal("Record Video Step", step.Name);
            Assert.Equal(nodeConfig, step.Config);
        }

        [Fact]
        public async Task VideoCaptureService_RecordShortRegion_ProducesValidMp4()
        {
            var service = new WindowsGraphicsCaptureVideoService();
            if (!service.IsSupported)
            {
                _output.WriteLine("Windows Graphics Capture is not supported on this platform. Skipping live recording test.");
                return;
            }

            var primaryDisplay = DisplayInfo.AllDisplayInfos.FirstOrDefault(d => d.IsPrimary) ?? DisplayInfo.AllDisplayInfos.FirstOrDefault();
            if (primaryDisplay == null || primaryDisplay.MonitorHandle == IntPtr.Zero)
            {
                _output.WriteLine("No active display found. Skipping test.");
                return;
            }

            string tempFile = Path.Combine(Path.GetTempPath(), $"GreenshotTest_{Guid.NewGuid():N}.mp4");

            try
            {
                var options = new VideoCaptureOptions
                {
                    Target = VideoCaptureTarget.FromRegion(new NativeRect(100, 100, 320, 240)),
                    OutputFilePath = tempFile,
                    FrameRate = 15,
                    Bitrate = 500_000,
                    AudioSource = AudioCaptureSource.None,
                    CaptureCursor = false,
                    PreventSleepWhileRecording = false,
                    AutoPauseOnSessionLock = false
                };

                using var session = await service.StartRecordingAsync(options);
                Assert.Equal(RecordingState.Recording, session.State);

                // Record for 1.5 seconds
                await Task.Delay(1500);

                var result = await session.StopAsync();
                _output.WriteLine($"Stopped. Result: {result?.FileSizeBytes} bytes");
                Console.WriteLine($"Stopped. Result: {result?.FileSizeBytes} bytes");

                Assert.Equal(RecordingState.Stopped, session.State);
                Assert.NotNull(result);
                Assert.True(File.Exists(tempFile), "Output video file should exist.");
                Assert.True(result.FileSizeBytes > 0, $"File size should be greater than 0, got {result.FileSizeBytes}");
                Assert.Equal(320, result.Width);
                Assert.Equal(240, result.Height);

                _output.WriteLine($"Successfully recorded video to {tempFile} (Size: {result.FileSizeBytes} bytes, Duration: {result.Duration})");
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }
    }
}
