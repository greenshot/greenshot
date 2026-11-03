/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Capturing;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Native;
using Greenshot.Base.Native.DirectX;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Sources;
using Greenshot.Base.Threading;
using Greenshot.Editor.FileFormatHandlers;
using Greenshot.Recipes.Steps;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Greenshot.Tests.Core;

/// <summary>
/// HDR captures saved as HDR files: the HDR image are read from the FP16 texture (WARP, no HDR monitor needed), cropped and composed,
/// and written lossless as JPEG XR. scRGB 1.0 is 80 nits, with an SDR white level of 240 nits SDR white is 3.0.
/// </summary>
public class HdrCaptureTests
{
    private const float SdrWhiteNits = 240f;
    private const float SdrWhite = SdrWhiteNits / 80f;

    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(0.5f)]
    [InlineData(3f)]
    [InlineData(8.625f)]
    [InlineData(-2.25f)]
    [InlineData(65504f)]
    [InlineData(0.0001f)] // a subnormal half
    public void FloatToHalf_RoundTrips(float value)
    {
        float back = HalfHelper.HalfToFloat(HalfHelper.FloatToHalf(value));

        Assert.True(Math.Abs(back - value) <= Math.Abs(value) * 0.001f + 0.000001f, $"{value} became {back}");
    }

    [Fact]
    public void FloatToHalf_TooLarge_IsInfinity_NaN_StaysNaN()
    {
        Assert.True(float.IsPositiveInfinity(HalfHelper.HalfToFloat(HalfHelper.FloatToHalf(100000f))));
        Assert.True(float.IsNaN(HalfHelper.HalfToFloat(HalfHelper.FloatToHalf(float.NaN))));
    }

    [Fact]
    public void Crop_KeepsTheArea()
    {
        var pixels = CreatePixels(4, 2, (x, y) => Rgba(x, y, 0));

        var cropped = pixels.Crop(new NativeRect(1, 1, 2, 5));

        Assert.Equal(2, cropped.Width);
        Assert.Equal(1, cropped.Height);
        Assert.Equal(1f, Get(cropped, 0, 0)[0]);
        Assert.Equal(2f, Get(cropped, 1, 0)[0]);
        Assert.Equal(1f, Get(cropped, 1, 0)[1]);
        Assert.Null(pixels.Crop(new NativeRect(10, 10, 2, 2)));
    }

    [Fact]
    public void ScreenCaptureResult_LinksTheHdrImageToItsImage()
    {
        var pixels = CreatePixels(2, 2, (x, y) => Rgba(1, 1, 1));
        using var image = new Bitmap(2, 2);
        using var otherImage = new Bitmap(2, 2);

        _ = new ScreenCaptureResult(image, new NativePoint(0, 0), new GraphicsCaptureBackend(), pixels);

        Assert.True(pixels.BelongsTo(image));
        // An effect or the editor replaces the image: the HDR image no longer show it
        Assert.False(pixels.BelongsTo(otherImage));
        Assert.False(pixels.BelongsTo(null));
    }

    [Fact]
    public void CaptureCrop_CropsTheHdrImage_AndLinksItToTheCroppedImage()
    {
        using var capture = new Capture { Image = new Bitmap(4, 2) };
        var pixels = CreatePixels(4, 2, (x, y) => Rgba(x, y, 0));
        pixels.BelongTo(capture.Image);
        capture.HdrImage = pixels;

        Assert.True(capture.Crop(new NativeRect(1, 0, 2, 2)));

        var cropped = capture.HdrImage;
        Assert.Equal(2, cropped.Width);
        Assert.Equal(2, cropped.Height);
        Assert.Equal(1f, Get(cropped, 0, 0)[0]);
        Assert.True(cropped.BelongsTo(capture.Image));
        // The capture owns its HDR image: the uncropped one is gone
        Assert.Throws<ObjectDisposedException>(() => Get(pixels, 0, 0));
    }

    [Fact]
    public void CaptureClone_SharesTheHdrPixels_ForTheClonedImage()
    {
        var capture = new Capture { Image = new Bitmap(2, 1) };
        capture.HdrImage = CreatePixels(2, 1, (x, y) => Rgba(5f, 5f, 5f));
        capture.HdrImage.BelongTo(capture.Image);
        var original = capture.HdrImage;

        using var clone = capture.Clone();

        // Its own HdrImage (own link to its own image), but the same pixels: no copy
        Assert.NotSame(original, clone.HdrImage);
        Assert.Same(original.Pixels, clone.HdrImage.Pixels);
        Assert.True(clone.HdrImage.BelongsTo(clone.Image));
        Assert.False(original.BelongsTo(clone.Image));

        // The pixels stay while a copy uses them
        capture.Dispose();
        Assert.Throws<ObjectDisposedException>(() => original.Pixels);
        Assert.Equal(5f, Get(clone.HdrImage, 1, 0)[0], 2);
    }

    [Fact]
    public void Crop_ToTheWholeImage_SharesThePixels()
    {
        using var hdrImage = CreatePixels(2, 1, (x, y) => Rgba(1, 1, 1));

        using var cropped = hdrImage.Crop(new NativeRect(0, 0, 5, 5));

        Assert.Same(hdrImage.Pixels, cropped.Pixels);
    }

    [Fact]
    public void Surface_TakesOverTheHdrImage_AndDisposesIt()
    {
        var capture = new Capture { Image = new Bitmap(2, 1) };
        var hdrImage = CreatePixels(2, 1, (x, y) => Rgba(5f, 5f, 5f));
        hdrImage.BelongTo(capture.Image);
        capture.HdrImage = hdrImage;

        var surface = WinFormsContextGuard.CreateWithoutContext(() => new Greenshot.Editor.Drawing.Surface(capture));
        capture.Dispose();

        // Taken over with the image, the capture doesn't dispose it any more
        Assert.Null(capture.HdrImage);
        Assert.Same(hdrImage, surface.HdrImage);
        Assert.True(hdrImage.BelongsTo(surface.Image));
        Assert.Equal(5f, Get(hdrImage, 0, 0)[0], 2);

        surface.Dispose();
        Assert.Throws<ObjectDisposedException>(() => Get(hdrImage, 0, 0));
    }

    [Fact]
    public void ReadHdrImage_FromTheTexture_KeepsHighlights()
    {
        float[][] row = { Rgba(SdrWhite, SdrWhite, SdrWhite), Rgba(4 * SdrWhite, 2 * SdrWhite, 0.5f), Rgba(0.25f, 0.5f, 1f) };

        var pixels = OnWarp(row, (texture, device, context) => WindowsGraphicsCaptureInterop.ReadHdrImage(texture, device, context, new Rectangle(1, 0, 5, 1), SdrWhiteNits, 400));

        // Clipped to the texture: the last two pixels
        Assert.Equal(2, pixels.Width);
        Assert.Equal(SdrWhiteNits, pixels.SdrWhiteLevelInNits);
        Assert.Equal(4 * SdrWhite, Get(pixels, 0, 0)[0], 2);
        Assert.Equal(2 * SdrWhite, Get(pixels, 0, 0)[1], 2);
        Assert.Equal(1f, Get(pixels, 1, 0)[2], 2);
    }

    [Fact]
    public void CombineHdrImages_KeepsOnlyTheHdrMonitor_ComposePutsTheSdrMonitorAtTheSdrWhiteLevel()
    {
        // The area spans an HDR monitor (left, 2 pixels) and an SDR monitor (right, 2 pixels) whose white is in the result bitmap
        using var result = new Bitmap(4, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        result.SetPixel(0, 0, System.Drawing.Color.Black);
        result.SetPixel(2, 0, System.Drawing.Color.White);
        result.SetPixel(3, 0, System.Drawing.Color.White);
        using var hdr = CreatePixels(2, 1, (x, y) => Rgba(5f, 5f, 5f));
        var requests = new[]
        {
            new WindowsGraphicsCaptureInterop.CaptureRequest(null, null, "hdr") { HdrImage = hdr, TargetLocation = Point.Empty, TargetSourceRect = new Rectangle(0, 0, 2, 1) },
            new WindowsGraphicsCaptureInterop.CaptureRequest(null, null, "sdr") { TargetLocation = new Point(2, 0), TargetSourceRect = new Rectangle(0, 0, 2, 1) }
        };

        using var combined = WindowsGraphicsCaptureInterop.CombineHdrImages(requests, 4, 1);

        // Only the HDR monitor, its pixels shared, not copied
        Assert.Equal(4, combined.Width);
        Assert.False(combined.IsComplete);
        Assert.Equal(new[] { new NativeRect(0, 0, 2, 1) }, combined.TileAreas);
        Assert.Throws<InvalidOperationException>(() => combined.Pixels);

        // Written: the SDR monitor comes from the 8-bit image, the HDR monitor keeps its highlights (not the black of the 8-bit image)
        using var composed = combined.Compose(result);
        Assert.True(composed.IsComplete);
        Assert.Equal(5f, Get(composed, 0, 0)[0], 2);
        Assert.Equal(5f, Get(composed, 1, 0)[0], 2);
        Assert.Equal(SdrWhite, Get(composed, 2, 0)[0], 2);
        Assert.Equal(SdrWhite, Get(composed, 3, 0)[2], 2);
        Assert.Equal(1f, Get(composed, 3, 0)[3], 2);
    }

    [Fact]
    public void CombineHdrImages_WithoutHdrMonitor_IsNull()
    {
        var requests = new[] { new WindowsGraphicsCaptureInterop.CaptureRequest(null, null, "sdr") { TargetSourceRect = new Rectangle(0, 0, 2, 1) } };

        Assert.Null(WindowsGraphicsCaptureInterop.CombineHdrImages(requests, 2, 1));
    }

    [Fact]
    public void Compose_OfACompleteHdrImage_SharesIt()
    {
        using var hdrImage = CreatePixels(2, 1, (x, y) => Rgba(5f, 5f, 5f));

        using var composed = hdrImage.Compose(null);

        Assert.Same(hdrImage.Pixels, composed.Pixels);
    }

    [Fact]
    public void Crop_OfTiles_KeepsWhatIsInTheArea_NothingOfAnHdrDisplay_IsNull()
    {
        // A frozen screen of an SDR monitor (left, 4 pixels) and an HDR monitor (right, 4 pixels)
        using var hdrMonitor = CreatePixels(4, 2, (x, y) => Rgba(x + 1, y, 0));
        using var screen = HdrImage.Combine(8, 2, new[] { (hdrMonitor, new NativePoint(4, 0)) });

        // Across both: the part of the HDR monitor at its new place
        using (var across = screen.Crop(new NativeRect(2, 0, 4, 2)))
        {
            Assert.Equal(4, across.Width);
            Assert.Equal(new[] { new NativeRect(2, 0, 2, 2) }, across.TileAreas);
        }

        // Only on the HDR monitor: complete, the pixels from x 1
        using (var onHdr = screen.Crop(new NativeRect(5, 0, 2, 2)))
        {
            Assert.True(onHdr.IsComplete);
            Assert.Equal(2f, Get(onHdr, 0, 0)[0]);
            Assert.Equal(1f, Get(onHdr, 1, 1)[1]);
        }

        // Only on the SDR monitor: no HDR content
        Assert.Null(screen.Crop(new NativeRect(0, 0, 3, 2)));
    }

    [Fact]
    public void JpegXr_IsWrittenLossless_WithTheHighlights()
    {
        var encoder = new WicHdrImageEncoder();
        Assert.True(encoder.CanEncode(WellKnownFileFormats.Jxr));

        var values = RoundTrip(encoder, WellKnownFileFormats.Jxr);

        // JPEG XR keeps the half-floats exactly
        Assert.Equal(SdrWhite, values[0], 2);
        Assert.Equal(4 * SdrWhite, values[4], 2);
        Assert.Equal(2 * SdrWhite, values[5], 2);
        Assert.Equal(0.5f, values[6], 2);
    }

    [Fact]
    public void EncodedImage_OfABuffer_IsOnlyItsLength()
    {
        var buffer = new byte[] { 1, 2, 3, 0, 0, 0 };

        var encoded = new EncodedImage(buffer, 3, WellKnownFileFormats.Jxr);

        Assert.Equal(3, encoded.Length);
        Assert.Equal(new byte[] { 1, 2, 3 }, encoded.ToArray());
        using var stream = encoded.OpenRead();
        Assert.Equal(3, stream.Length);
    }

    [Fact]
    public void TryEncode_UnknownFormat_Fails()
    {
        var pixels = CreatePixels(1, 1, (x, y) => Rgba(1, 1, 1));

        Assert.False(new WicHdrImageEncoder().TryEncode(pixels, "png", null, false, out var encoded, out string error));
        Assert.Null(encoded);
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData("Auto", "jxr")]
    [InlineData("Jxr", "jxr")]
    [InlineData("None", null)]
    [InlineData(null, null)]
    public void ResolveFormatId_PicksTheFormat(string hdrFormat, string expected)
    {
        Assert.Equal(expected, HdrOutput.ResolveFormatId(hdrFormat));
    }

    [Fact]
    public void SavesHdrFile_OnlyForASaveFileStepWithAnHdrFormat_CapturesHdr_ForASourceWithoutHdrFalse()
    {
        var recipe = new CaptureRecipe("hdr_test", "HDR test");
        recipe.Nodes.Add(new RecipeNodeConfig("source", WellKnownStepTypes.Source));
        recipe.Nodes.Add(new RecipeNodeConfig("save", WellKnownStepTypes.SaveFile).Set(HdrOutput.ParameterName, "None"));
        Assert.False(HdrOutput.SavesHdrFile(recipe));
        // Without Hdr the setting decides: it may
        Assert.True(HdrOutput.CapturesHdr(recipe));

        recipe.Nodes.Add(new RecipeNodeConfig("save_hdr", WellKnownStepTypes.SaveFile).Set(HdrOutput.ParameterName, "Jxr"));
        Assert.True(HdrOutput.SavesHdrFile(recipe));
        Assert.False(HdrOutput.SavesHdrFile(null));

        recipe.Nodes[0].Set(HdrOutput.SourceParameterName, true);
        Assert.True(HdrOutput.CapturesHdr(recipe));
        // An option may switch it on
        recipe.Nodes[0].Set(HdrOutput.SourceParameterName, "${option.hdr}");
        Assert.True(HdrOutput.CapturesHdr(recipe));
        recipe.Nodes[0].Set(HdrOutput.SourceParameterName, "false");
        Assert.False(HdrOutput.CapturesHdr(recipe));
    }

    [Fact]
    public void Validator_WarnsAboutAnHdrFileWithoutAnHdrCapture()
    {
        var recipe = new CaptureRecipe("hdr_test", "HDR test");
        recipe.Nodes.Add(new RecipeNodeConfig("source", WellKnownStepTypes.Source).Set("SourceType", "ActiveWindow"));
        recipe.Nodes.Add(new RecipeNodeConfig("save", WellKnownStepTypes.SaveFile).Set(HdrOutput.ParameterName, "Auto"));
        recipe.Flow = new RecipeFlowConfig("source").AddTransition("source", "save");

        // Without Hdr the setting decides
        Assert.DoesNotContain(RecipeValidator.Validate(recipe).Warnings, warning => warning.Contains("Hdr is false"));

        recipe.Nodes[0].Set(HdrOutput.SourceParameterName, false);
        Assert.Contains(RecipeValidator.Validate(recipe).Warnings, warning => warning.Contains("Hdr is false"));

        recipe.Nodes[0].Set(HdrOutput.SourceParameterName, true);
        Assert.DoesNotContain(RecipeValidator.Validate(recipe).Warnings, warning => warning.Contains("Hdr is false"));
    }

    [Fact]
    public void ScreenMode_Frozen_NeverSelectsLive_Live_SelectsLiveWhatTheSettingSays()
    {
        var recipe = new CaptureRecipe("hdr_region", "HDR region");
        recipe.Nodes.Add(RecipeStepConfig.CreateSelection("select", CaptureMode.Region, false));
        var context = new CaptureFlowContext(recipe);

        Assert.False(SourceAcquisitionStep.UseLiveSelection(context, CaptureSourceType.Region, false, "Frozen"));
        if (!LiveScreenCaptureSource.IsSupported)
        {
            Assert.Skip("Selecting on the live screen needs Windows 10 2004 or newer.");
        }
        // Live is used also when the Freeze screen setting is on
        Assert.True(SourceAcquisitionStep.UseLiveSelection(context, CaptureSourceType.Region, false, "Live"));
        // Not for a capture of the whole screen
        Assert.False(SourceAcquisitionStep.UseLiveSelection(context, CaptureSourceType.FullScreen, false, "Live"));
    }

    /// <summary>
    /// A white pixel and an orange highlight at 4x SDR white, written and read back with WIC as floats
    /// </summary>
    private static float[] RoundTrip(WicHdrImageEncoder encoder, string formatId)
    {
        var pixels = new HdrImage(2, 1, SdrWhiteNits, 400);
        Set(pixels, 0, 0, Rgba(SdrWhite, SdrWhite, SdrWhite));
        Set(pixels, 1, 0, Rgba(4 * SdrWhite, 2 * SdrWhite, 0.5f));
        Assert.True(encoder.TryEncode(pixels, formatId, null, false, out var encoded, out string error), error);

        using var stream = encoded.OpenRead();
        var frame = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        var converted = new FormatConvertedBitmap(frame, PixelFormats.Rgba128Float, null, 0);
        var values = new float[8];
        converted.CopyPixels(values, 32, 0);
        return values;
    }

    private static HdrImage CreatePixels(int width, int height, Func<int, int, float[]> pixel)
    {
        var pixels = new HdrImage(width, height, SdrWhiteNits, 400);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Set(pixels, x, y, pixel(x, y));
            }
        }
        return pixels;
    }

    private static void Set(HdrImage pixels, int x, int y, float[] rgba) => pixels.Pixels[x, y] = new HalfVector4(rgba[0], rgba[1], rgba[2], rgba[3]);

    private static float[] Get(HdrImage pixels, int x, int y)
    {
        var rgba = pixels.Pixels[x, y].ToVector4();
        return new[] { rgba.X, rgba.Y, rgba.Z, rgba.W };
    }

    private static float[] Rgba(float r, float g, float b, float a = 1) => new[] { r, g, b, a };

    /// <summary>
    /// Calls the function with a one row FP16 texture of the pixels on the WARP device
    /// </summary>
    private static T OnWarp<T>(float[][] row, Func<ID3D11Texture2D, ID3D11Device, ID3D11DeviceContext, T> function)
    {
        int hr = WindowsGraphicsCaptureInterop.CreateD3D11Device(WindowsGraphicsCaptureInterop.WarpDriverType, out var device, out var context);
        Assert.Equal(0, hr);
        var halfs = new ushort[row.Length * 4];
        for (int i = 0; i < row.Length; i++)
        {
            for (int channel = 0; channel < 4; channel++)
            {
                halfs[i * 4 + channel] = HalfHelper.FloatToHalf(row[i][channel]);
            }
        }
        var handle = GCHandle.Alloc(halfs, GCHandleType.Pinned);
        var initialData = new D3D11_SUBRESOURCE_DATA { SysMem = handle.AddrOfPinnedObject(), SysMemPitch = row.Length * 8 };
        var initialDataHandle = GCHandle.Alloc(initialData, GCHandleType.Pinned);
        ID3D11Texture2D texture = null;
        try
        {
            var desc = new D3D11_TEXTURE2D_DESC
            {
                Width = row.Length,
                Height = 1,
                MipLevels = 1,
                ArraySize = 1,
                Format = 10, // DXGI_FORMAT_R16G16B16A16_FLOAT
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT,
                BindFlags = 0x8, // D3D11_BIND_SHADER_RESOURCE
                CPUAccessFlags = 0,
                MiscFlags = 0
            };
            device.CreateTexture2D(ref desc, initialDataHandle.AddrOfPinnedObject(), out texture);
            return function(texture, device, context);
        }
        finally
        {
            if (texture != null) Marshal.ReleaseComObject(texture);
            initialDataHandle.Free();
            handle.Free();
            Marshal.ReleaseComObject(context);
            Marshal.ReleaseComObject(device);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D3D11_SUBRESOURCE_DATA
    {
        public IntPtr SysMem;
        public int SysMemPitch;
        public int SysMemSlicePitch;
    }
}
