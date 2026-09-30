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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Threading;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// These tests change the real clipboard: they only run in an active desktop session, and never in parallel with other tests.
    /// Every test places its own content first, so the order doesn't matter.
    /// </summary>
    [Collection(TestCollections.Clipboard)]
    public class ClipboardTests
    {
        private static readonly string FormatDib = StandardClipboardFormats.DeviceIndependentBitmap.AsString();
        private static readonly string FormatDibV5 = StandardClipboardFormats.DeviceIndependentBitmapV5.AsString();
        private static readonly string FormatUnicodeText = StandardClipboardFormats.UnicodeText.AsString();
        private static readonly string FormatText = StandardClipboardFormats.Text.AsString();

        // Opaque red, green, blue, a half transparent white and a fully transparent pixel
        private static readonly Color[] TestColors =
        {
            Color.FromArgb(255, 255, 0, 0), Color.FromArgb(255, 0, 255, 0), Color.FromArgb(255, 0, 0, 255), Color.FromArgb(128, 255, 255, 255), Color.FromArgb(0, 0, 0, 0)
        };

        public ClipboardTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static Bitmap CreateTestBitmap(PixelFormat pixelFormat = PixelFormat.Format32bppArgb)
        {
            var bitmap = new Bitmap(5, 3, pixelFormat);
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    bitmap.SetPixel(x, y, TestColors[(x + y) % TestColors.Length]);
                }
            }
            return bitmap;
        }

        private static void AssertSamePixels(Bitmap expected, Image actualImage, bool compareAlpha)
        {
            Assert.NotNull(actualImage);
            var actual = Assert.IsAssignableFrom<Bitmap>(actualImage);
            Assert.Equal(expected.Size, actual.Size);
            for (int y = 0; y < expected.Height; y++)
            {
                for (int x = 0; x < expected.Width; x++)
                {
                    var e = expected.GetPixel(x, y);
                    var a = actual.GetPixel(x, y);
                    if (compareAlpha)
                    {
                        Assert.Equal(e.A, a.A);
                    }
                    // Colors of fully transparent pixels are undefined
                    if (e.A == 0)
                    {
                        continue;
                    }
                    // Premultiplied storage can round a bit
                    Assert.InRange(a.R, e.R - 2, e.R + 2);
                    Assert.InRange(a.G, e.G - 2, e.G + 2);
                    Assert.InRange(a.B, e.B - 2, e.B + 2);
                }
            }
        }

        private static void Place(Bitmap bitmap, params ClipboardFormat[] formats)
        {
            using var content = ClipboardHelper.CreateContent(bitmap, formats);
            Assert.True(content.HasData);
            ClipboardHelper.SetClipboardData(content.Contents);
        }

        /// <summary>
        /// Read the image from the clipboard like Greenshot does: only the needed formats, then the first image
        /// </summary>
        private static Bitmap ReadImage() => ClipboardHelper.GetFirstImage(Snapshot(ClipboardHelper.SelectImageReadFormats().ToArray()));

        private static ClipboardSnapshot Snapshot(params string[] formats)
        {
            var snapshot = ClipboardHelper.ReadSnapshot(formats);
            Assert.NotNull(snapshot);
            return snapshot;
        }

        #region Round trips for every ClipboardFormat

        [InteractiveDesktopFact]
        public void RoundTrip_Png()
        {
            using var bitmap = CreateTestBitmap();
            Place(bitmap, ClipboardFormat.PNG);

            // Win32: a real PNG file in the registered "PNG" format
            Assert.True(ClipboardNative.HasFormat("PNG"));
            var png = Snapshot("PNG").GetAsBytes("PNG");
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png.Take(4).ToArray());

            // Greenshot
            Assert.True(ClipboardHelper.ContainsImage());
            using var image = ReadImage();
            AssertSamePixels(bitmap, image, compareAlpha: true);
        }

        [InteractiveDesktopFact]
        public void RoundTrip_Dib()
        {
            using var bitmap = CreateTestBitmap();
            Place(bitmap, ClipboardFormat.DIB);

            // Win32: CF_DIB, and Windows synthesizes CF_BITMAP and CF_DIBV5
            Assert.True(ClipboardNative.HasFormat(StandardClipboardFormats.DeviceIndependentBitmap));
            Assert.True(ClipboardNative.HasFormat(StandardClipboardFormats.Bitmap));
            Assert.False(ClipboardNative.HasFormat("PNG"));
            var dib = Snapshot(FormatDib).GetAsBytes(FormatDib);
            Assert.Equal(40, BitConverter.ToInt32(dib, 0));
            Assert.True(DibImage.TryDecode(dib, out var dibImage));
            Assert.Equal(5, dibImage.Width);
            Assert.Equal(3, dibImage.Height);

            // Greenshot, CF_DIB has no reliable alpha. GetImage takes the CF_DIBV5 Windows synthesizes, so also read CF_DIB itself
            using var image = ReadImage();
            AssertSamePixels(bitmap, image, compareAlpha: false);
            using var fromDib = ClipboardHelper.GetFirstImage(Snapshot(FormatDib));
            AssertSamePixels(bitmap, fromDib, compareAlpha: false);
        }

        [InteractiveDesktopFact]
        public void RoundTrip_DibV5_KeepsAlpha()
        {
            using var bitmap = CreateTestBitmap();
            Place(bitmap, ClipboardFormat.DIBV5);

            // Win32: CF_DIBV5 with a BITMAPV5HEADER and alpha
            Assert.True(ClipboardNative.HasFormat(StandardClipboardFormats.DeviceIndependentBitmapV5));
            var dibV5 = Snapshot(FormatDibV5).GetAsBytes(FormatDibV5);
            Assert.Equal(124, BitConverter.ToInt32(dibV5, 0));
            Assert.True(DibImage.TryDecode(dibV5, out var dibImage));
            Assert.True(dibImage.HasAlpha);

            // Greenshot
            using var image = ReadImage();
            AssertSamePixels(bitmap, image, compareAlpha: true);
        }

        [InteractiveDesktopFact]
        public void RoundTrip_DibV5_FromPremultipliedBitmap()
        {
            using var bitmap = CreateTestBitmap(PixelFormat.Format32bppPArgb);
            Place(bitmap, ClipboardFormat.DIBV5);

            using var image = ReadImage();
            // The half transparent white must stay white, not become gray
            AssertSamePixels(bitmap, image, compareAlpha: true);
        }

        [InteractiveDesktopFact]
        public void RoundTrip_Bitmap_IsPlacedAsDib()
        {
            using var bitmap = CreateTestBitmap();
            Place(bitmap, ClipboardFormat.BITMAP);

            // No .NET serialized object anymore, native applications get CF_BITMAP from CF_DIB
            Assert.False(ClipboardNative.HasFormat("System.Drawing.Bitmap"));
            Assert.True(ClipboardNative.HasFormat(StandardClipboardFormats.DeviceIndependentBitmap));
            Assert.True(ClipboardNative.HasFormat(StandardClipboardFormats.Bitmap));

            using var image = ReadImage();
            AssertSamePixels(bitmap, image, compareAlpha: false);
        }

        [InteractiveDesktopFact]
        public void RoundTrip_Html_ReferencesTemporaryFile()
        {
            using var bitmap = CreateTestBitmap();
            Place(bitmap, ClipboardFormat.HTML);

            // Win32: CF_HTML with consistent byte offsets
            var raw = Snapshot(ClipboardHtml.FormatName).GetAsBytes(ClipboardHtml.FormatName);
            var header = Encoding.UTF8.GetString(raw);
            int startFragment = ReadOffset(header, "StartFragment:");
            int endFragment = ReadOffset(header, "EndFragment:");
            string fragment = Encoding.UTF8.GetString(raw, startFragment, endFragment - startFragment);
            Assert.StartsWith("<img", fragment.Trim());

            // Greenshot
            var urls = ClipboardHelper.GetHtmlImageUrls(Snapshot(ClipboardHtml.FormatName));
            var url = Assert.Single(urls);
            var fileUri = new Uri(url);
            Assert.True(fileUri.IsFile);
            Assert.True(File.Exists(fileUri.LocalPath));
            using var fromFile = Image.FromFile(fileUri.LocalPath);
            AssertSamePixels(bitmap, fromFile, compareAlpha: true);
        }

        [InteractiveDesktopFact]
        public void RoundTrip_HtmlDataUrl_ContainsThePng()
        {
            using var bitmap = CreateTestBitmap();
            Place(bitmap, ClipboardFormat.HTMLDATAURL);

            Assert.True(ClipboardNative.HasFormat(ClipboardHtml.FormatName));
            var url = Assert.Single(ClipboardHelper.GetHtmlImageUrls(Snapshot(ClipboardHtml.FormatName)));
            const string prefix = "data:image/png;base64,";
            Assert.StartsWith(prefix, url);
            using var pngStream = new MemoryStream(Convert.FromBase64String(url.Substring(prefix.Length)));
            using var fromDataUrl = Image.FromStream(pngStream);
            AssertSamePixels(bitmap, fromDataUrl, compareAlpha: true);
        }

        [InteractiveDesktopFact]
        public void RoundTrip_Text()
        {
            const string text = "Greenshot ✓ äöü";
            ClipboardHelper.SetClipboardData(text);

            // Win32: CF_UNICODETEXT, Windows synthesizes CF_TEXT
            Assert.True(ClipboardNative.HasFormat(StandardClipboardFormats.UnicodeText));
            Assert.True(ClipboardNative.HasFormat(StandardClipboardFormats.Text));
            var snapshot = Snapshot(FormatUnicodeText, FormatText);
            Assert.Equal(text, snapshot.GetAsUnicodeString());

            // Greenshot
            Assert.True(ClipboardHelper.ContainsText());
            Assert.False(ClipboardHelper.ContainsImage());
            Assert.Equal(text, ClipboardHelper.GetText(snapshot));
        }

        [InteractiveDesktopFact]
        public void RoundTrip_AllFormats_RichestFirst()
        {
            using var bitmap = CreateTestBitmap();
            using (var content = ClipboardHelper.CreateContent(bitmap,
                       new[] { ClipboardFormat.PNG, ClipboardFormat.DIB, ClipboardFormat.HTMLDATAURL, ClipboardFormat.BITMAP, ClipboardFormat.DIBV5 }, "alt text"))
            {
                // The order in which the formats are placed
                var formatNames = content.Contents.FormatIds.Select(ClipboardFormatExtensions.MapIdToFormat).ToList();
                Assert.Equal(new[] { "PNG", FormatDibV5, FormatDib, ClipboardHtml.FormatName, FormatUnicodeText }, formatNames);
                ClipboardHelper.SetClipboardData(content.Contents);
            }

            // Only the two best image formats and the file formats are read, not HTML
            var readFormats = ClipboardHelper.SelectImageReadFormats();
            Assert.Equal(new[] { "PNG", FormatDibV5 }, readFormats.Take(2));
            Assert.DoesNotContain(ClipboardHtml.FormatName, readFormats);

            // PNG wins
            using var image = ReadImage();
            AssertSamePixels(bitmap, image, compareAlpha: true);
            Assert.Equal("alt text", ClipboardHelper.GetText(Snapshot(FormatUnicodeText)));
        }

        [InteractiveDesktopFact]
        public async Task ClipboardService_SetImageAsync_FromBackgroundThread()
        {
            using var bitmap = CreateTestBitmap();
            var service = new ClipboardService(InlineUiDispatcher.Instance);
            // No UI thread involved: run on a pool (MTA) thread
            await Task.Run(() => service.SetImageAsync(bitmap, new[] { ClipboardFormat.PNG }));

            Assert.True(await Task.Run(() => service.ContainsImageAsync()));
            using var image = await Task.Run(() => service.GetImageAsync());
            AssertSamePixels(bitmap, image, compareAlpha: true);
        }

        private static int ReadOffset(string header, string name)
        {
            int start = header.IndexOf(name, StringComparison.Ordinal) + name.Length;
            int end = header.IndexOfAny(new[] { '\r', '\n' }, start);
            return int.Parse(header.Substring(start, end - start));
        }

        #endregion

        #region Reading content of other applications

        [InteractiveDesktopFact]
        public void Read_PngOnly()
        {
            using var bitmap = CreateTestBitmap();
            using var pngStream = new MemoryStream();
            bitmap.Save(pngStream, ImageFormat.Png);
            ClipboardHelper.SetClipboardData(new ClipboardContents().AddBytes(pngStream.ToArray(), "PNG"));

            Assert.True(ClipboardHelper.ContainsImage());
            using var image = ReadImage();
            AssertSamePixels(bitmap, image, compareAlpha: true);
        }

        [InteractiveDesktopFact]
        public void Read_DibV5WithAlpha()
        {
            using var bitmap = CreateTestBitmap();
            var pixels = ClipboardBitmapConverter.ToBgra32(bitmap);
            // Only CF_DIBV5, like applications which place a bitmap with alpha
            ClipboardHelper.SetClipboardData(new ClipboardContents()
                .AddBytes(DibImage.CreateDibV5(pixels.Pixels, pixels.Width, pixels.Height, pixels.Stride, false), StandardClipboardFormats.DeviceIndependentBitmapV5));

            using var image = ReadImage();
            AssertSamePixels(bitmap, image, compareAlpha: true);
            using var drawable = ClipboardHelper.GetDrawables(Snapshot(ClipboardHelper.ImageReadFormats.ToArray())).Single();
            Assert.Equal(bitmap.Width, drawable.Width);
        }

        [InteractiveDesktopFact]
        public void Read_Dib24Bpp()
        {
            // 3x2 pixels, 24 bpp bottom-up: rows are padded to 4 bytes (9 -> 12)
            var dib = new byte[40 + 12 * 2];
            using (var writer = new BinaryWriter(new MemoryStream(dib)))
            {
                writer.Write(40); // biSize
                writer.Write(3); // biWidth
                writer.Write(2); // biHeight, bottom-up
                writer.Write((ushort)1); // biPlanes
                writer.Write((ushort)24); // biBitCount
                writer.Write(0); // BI_RGB
                writer.Write(0); // biSizeImage, may be 0 for BI_RGB
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                // Bottom row (y = 1): blue, blue, blue
                for (int x = 0; x < 3; x++)
                {
                    writer.Write(new byte[] { 255, 0, 0 });
                }
                writer.Write(new byte[3]);
                // Top row (y = 0): red, green, white
                writer.Write(new byte[] { 0, 0, 255, 0, 255, 0, 255, 255, 255 });
                writer.Write(new byte[3]);
            }
            ClipboardHelper.SetClipboardData(new ClipboardContents().AddBytes(dib, StandardClipboardFormats.DeviceIndependentBitmap));

            // Read CF_DIB itself, not the CF_DIBV5 Windows synthesizes from it
            using var image = ClipboardHelper.GetFirstImage(Snapshot(FormatDib));
            var result = Assert.IsAssignableFrom<Bitmap>(image);
            Assert.Equal(new Size(3, 2), result.Size);
            Assert.Equal(Color.FromArgb(255, 255, 0, 0), result.GetPixel(0, 0));
            Assert.Equal(Color.FromArgb(255, 0, 255, 0), result.GetPixel(1, 0));
            Assert.Equal(Color.FromArgb(255, 255, 255, 255), result.GetPixel(2, 0));
            Assert.Equal(Color.FromArgb(255, 0, 0, 255), result.GetPixel(1, 1));
        }

        [InteractiveDesktopFact]
        public void Read_HtmlWithImageUrl()
        {
            ClipboardHelper.SetClipboardData(new ClipboardContents()
                .AddHtml("<p>A picture <img src=\"images/picture.png?a=1&amp;b=2\" alt=\"x\"></p>", new Uri("https://example.com/articles/page.html")));

            Assert.True(ClipboardHelper.ContainsImage());
            Assert.True(ClipboardHelper.ContainsImageExact());
            var url = Assert.Single(ClipboardHelper.GetHtmlImageUrls(Snapshot(ClipboardHelper.ImageReadFormats.ToArray())));
            // Relative to the SourceURL, entities decoded
            Assert.Equal("https://example.com/articles/images/picture.png?a=1&b=2", url);
        }

        [InteractiveDesktopFact]
        public void Read_HtmlRelativeUrl_IsNotResolvedAgainstAFileSource()
        {
            // A relative src must not become a file:// (UNC) url which would be downloaded
            ClipboardHelper.SetClipboardData(new ClipboardContents().AddHtml("<img src=\"picture.png\">", new Uri(@"\\server\share\page.html")));

            var url = Assert.Single(ClipboardHelper.GetHtmlImageUrls(Snapshot(ClipboardHtml.FormatName)));
            Assert.Equal("picture.png", url);
        }

        [InteractiveDesktopFact]
        public void Read_NonImageFileList_IsOnlyAPossibleImage()
        {
            ClipboardHelper.SetClipboardData(new ClipboardContents().AddFileNames(new[] { Path.Combine(Path.GetTempPath(), "not-an-image.txt") }));
            // Cheap check for menus: a file list might be an image
            Assert.True(ClipboardHelper.ContainsImage());
            // The exact check reads the file names
            Assert.False(ClipboardHelper.ContainsImageExact());
        }

        [InteractiveDesktopFact]
        public void Read_FirefoxTextHtml()
        {
            ClipboardHelper.SetClipboardData(new ClipboardContents()
                .AddBytes(Encoding.UTF8.GetBytes("<img src=\"https://example.com/a.png\">"), "text/html"));

            var url = Assert.Single(ClipboardHelper.GetHtmlImageUrls(Snapshot(ClipboardHelper.ImageReadFormats.ToArray())));
            Assert.Equal("https://example.com/a.png", url);
        }

        [InteractiveDesktopFact]
        public void Read_FileList()
        {
            using var bitmap = CreateTestBitmap();
            string directory = Path.Combine(Path.GetTempPath(), "GreenshotClipboardTests");
            Directory.CreateDirectory(directory);
            string imageFile = Path.Combine(directory, "image.PNG");
            string textFile = Path.Combine(directory, "notes.txt");
            bitmap.Save(imageFile, ImageFormat.Png);
            File.WriteAllText(textFile, "not an image");
            try
            {
                ClipboardHelper.SetClipboardData(new ClipboardContents().AddFileNames(new[] { textFile, imageFile }));

                Assert.True(ClipboardHelper.ContainsImage());
                var snapshot = Snapshot(ClipboardHelper.ImageReadFormats.ToArray());
                // Only the file Greenshot can load, the extension is compared case insensitive
                Assert.Equal(new[] { imageFile }, ClipboardHelper.GetImageFilenames(snapshot));
                using var image = ClipboardHelper.GetFirstImage(snapshot);
                AssertSamePixels(bitmap, image, compareAlpha: true);
            }
            finally
            {
                File.Delete(imageFile);
                File.Delete(textFile);
            }
        }

        [InteractiveDesktopFact]
        public void Read_CraftedDibV5_IsRejectedWithoutCrash()
        {
            // The header claims a huge image, the data is tiny: this was an out-of-bounds read in the old reader
            var dib = new byte[124 + 16];
            BitConverter.GetBytes(124).CopyTo(dib, 0);
            BitConverter.GetBytes(30000).CopyTo(dib, 4);
            BitConverter.GetBytes(30000).CopyTo(dib, 8);
            BitConverter.GetBytes((ushort)1).CopyTo(dib, 12);
            BitConverter.GetBytes((ushort)32).CopyTo(dib, 14);
            BitConverter.GetBytes(uint.MaxValue).CopyTo(dib, 20);
            ClipboardHelper.SetClipboardData(new ClipboardContents().AddBytes(dib, StandardClipboardFormats.DeviceIndependentBitmapV5));

            Assert.Null(ReadImage());
        }

        #endregion

        #region Drop: virtual files through DataObjectReader

        [Fact]
        public void Drop_VirtualFile_IsReadWithSafeFileName()
        {
            using var bitmap = CreateTestBitmap();
            using var pngStream = new MemoryStream();
            bitmap.Save(pngStream, ImageFormat.Png);
            byte[] png = pngStream.ToArray();

            Exception threadException = null;
            Bitmap result = null;
            string safeFileName = null;
            // OLE needs an STA thread, like a drop on the UI thread
            var thread = new Thread(() =>
            {
                try
                {
                    // Like an Outlook attachment: FileGroupDescriptorW + FileContents; the name tries to escape the directory
                    var dataObject = new System.Windows.Forms.DataObject();
                    dataObject.SetData(DataObjectReader.FileGroupDescriptorWFormat, false, new MemoryStream(CreateFileGroupDescriptor(@"..\..\evil\attachment.png", png.Length)));
                    dataObject.SetData(DataObjectReader.FileContentsFormat, false, new MemoryStream(png));

                    using var reader = new DataObjectReader(dataObject);
                    safeFileName = reader.GetVirtualFiles().Single().SafeFileName;
                    Assert.True(ClipboardHelper.ContainsImage(reader));
                    result = ClipboardHelper.GetFirstImage(reader);
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadException);
            Assert.Equal("attachment.png", safeFileName);
            using (result)
            {
                AssertSamePixels(bitmap, result, compareAlpha: true);
            }
        }

        [Fact]
        public void Drop_AnsiTextOnly_IsRead()
        {
            string text = null;
            Exception threadException = null;
            var thread = new Thread(() =>
            {
                try
                {
                    // Only CF_TEXT, Windows doesn't synthesize CF_UNICODETEXT for a data object
                    var dataObject = new System.Windows.Forms.DataObject();
                    dataObject.SetData(System.Windows.Forms.DataFormats.Text, false, new MemoryStream(Encoding.Default.GetBytes("dropped text\0")));
                    using var reader = new DataObjectReader(dataObject);
                    Assert.True(ClipboardHelper.ContainsText(reader));
                    text = ClipboardHelper.GetText(reader);
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadException);
            Assert.Equal("dropped text", text);
        }

        /// <summary>
        /// FILEGROUPDESCRIPTORW with one FILEDESCRIPTORW (592 bytes) with FD_FILESIZE
        /// </summary>
        private static byte[] CreateFileGroupDescriptor(string fileName, int size)
        {
            var descriptor = new byte[4 + 592];
            BitConverter.GetBytes(1).CopyTo(descriptor, 0);
            const int fdFileSize = 0x40;
            BitConverter.GetBytes(fdFileSize).CopyTo(descriptor, 4);
            // nFileSizeHigh at 4 + 64, nFileSizeLow at 4 + 68, cFileName at 4 + 72
            BitConverter.GetBytes(size).CopyTo(descriptor, 4 + 68);
            Encoding.Unicode.GetBytes(fileName).CopyTo(descriptor, 4 + 72);
            return descriptor;
        }

        #endregion

        #region Busy clipboard

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [InteractiveDesktopFact]
        public async Task Busy_MessageNamesTheBlockingApplication()
        {
            using var opened = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            Exception holderException = null;
            // Another thread keeps the clipboard open with its own window, without Dapplo's in-process lock: like another application
            var holder = new Thread(() =>
            {
                var window = new System.Windows.Forms.NativeWindow();
                try
                {
                    window.CreateHandle(new System.Windows.Forms.CreateParams());
                    // Clipboard history or another monitor can have the clipboard open for a moment after the previous test
                    int attempt = 0;
                    while (!OpenClipboard(window.Handle))
                    {
                        if (++attempt >= 50)
                        {
                            throw new InvalidOperationException($"OpenClipboard failed: {Marshal.GetLastWin32Error()}");
                        }
                        Thread.Sleep(50);
                    }
                    opened.Set();
                    release.Wait(TimeSpan.FromSeconds(30));
                    CloseClipboard();
                }
                catch (Exception ex)
                {
                    holderException = ex;
                    opened.Set();
                }
                finally
                {
                    window.DestroyHandle();
                }
            });
            holder.Start();
            try
            {
                Assert.True(opened.Wait(TimeSpan.FromSeconds(10)));
                Assert.Null(holderException);

                var contents = new ClipboardContents().AddUnicodeString("blocked");
                var exception = await Assert.ThrowsAsync<ClipboardException>(() =>
                    ClipboardHelper.SetClipboardDataAsync(contents, ClipboardHelper.CreateAccessOptions(2, TimeSpan.FromMilliseconds(50))));

                using var me = Process.GetCurrentProcess();
                string expectedName = Path.GetFileName(me.MainModule.FileName);
                Assert.Equal(expectedName, exception.OwnerProcess);
                Assert.Contains(expectedName, exception.Message);
                var accessDenied = Assert.IsType<ClipboardAccessDeniedException>(exception.InnerException);
                Assert.Equal(me.Id, accessDenied.BlockingProcessId);

                // The service reports it the same way, with its configured attempts
                var service = new ClipboardService(InlineUiDispatcher.Instance, maxAttempts: 2, retryDelay: TimeSpan.FromMilliseconds(50));
                var serviceException = await Assert.ThrowsAsync<ClipboardException>(() => service.SetTextAsync("blocked"));
                Assert.Equal(expectedName, serviceException.OwnerProcess);
            }
            finally
            {
                release.Set();
                holder.Join();
            }

            // Afterwards the clipboard works again
            await ClipboardHelper.SetClipboardDataAsync(new ClipboardContents().AddUnicodeString("free again"));
            Assert.Equal("free again", ClipboardHelper.GetText(Snapshot(FormatUnicodeText)));
        }

        #endregion
    }
}
