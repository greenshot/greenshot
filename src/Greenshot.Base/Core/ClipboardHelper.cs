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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Dapplo.Windows.Clipboard;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;
using log4net;
using Greenshot.Base.Languages;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// All clipboard reading and writing of Greenshot, done with Dapplo.Windows.Clipboard.
    /// The clipboard can be used from any thread: content is prepared before the clipboard is opened, and read data is decoded
    /// after it was closed again. Only reading virtual files (e.g. Outlook attachments) from the clipboard needs OLE, and so the UI thread.
    /// </summary>
    public static class ClipboardHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ClipboardHelper));

        #region Formats

        private const string FormatPng = "PNG";
        private const string FormatPngOfficeArt = "PNG+Office Art";
        private const string FormatJpg = "JPG";
        private const string FormatJpeg = "JPEG";
        private const string FormatJfif = "JFIF";
        private const string FormatJfifOfficeArt = "JFIF+Office Art";
        private const string FormatGif = "GIF";
        // Firefox places the HTML also as plain UTF-8 with its MIME type
        private const string FormatHtmlMime = "text/html";

        private static readonly string FormatText = StandardClipboardFormats.Text.AsString();
        private static readonly string FormatUnicodeText = StandardClipboardFormats.UnicodeText.AsString();
        private static readonly string FormatBitmap = StandardClipboardFormats.Bitmap.AsString();
        private static readonly string FormatDib = StandardClipboardFormats.DeviceIndependentBitmap.AsString();
        private static readonly string FormatDibV5 = StandardClipboardFormats.DeviceIndependentBitmapV5.AsString();
        private static readonly string FormatTiff = StandardClipboardFormats.Tiff.AsString();
        private static readonly string FormatEnhancedMetafile = StandardClipboardFormats.EnhancedMetafile.AsString();
        private static readonly string FormatDrop = StandardClipboardFormats.Drop.AsString();

        /// <summary>
        /// The image formats in the order Greenshot prefers them
        /// </summary>
        private static readonly string[] ImageFormats =
        {
            FormatPngOfficeArt, FormatPng, FormatDibV5, FormatJfifOfficeArt, FormatJpg, FormatJpeg, FormatJfif, FormatTiff, FormatDib, FormatGif
        };

        /// <summary>
        /// Outlook (2010) places a clipped PNG, when it's there together with a DIB the DIB is used first
        /// </summary>
        private static readonly string[] OutlookImageFormats =
        {
            FormatDib, FormatPngOfficeArt, FormatPng, FormatJfifOfficeArt, FormatJpg, FormatJpeg, FormatJfif, FormatTiff, FormatGif
        };

        /// <summary>
        /// Formats which are an image by themselves (CF_BITMAP and CF_ENHMETAFILE are GDI handles, Windows synthesizes CF_DIB from CF_BITMAP)
        /// </summary>
        private static readonly string[] DirectImageFormats =
        {
            FormatBitmap, FormatDib, FormatDibV5, FormatTiff, FormatEnhancedMetafile, FormatPng, FormatJpg, FormatJfif, FormatJpeg, FormatGif
        };

        private static readonly string[] VirtualFileFormats = { DataObjectReader.FileGroupDescriptorWFormat, DataObjectReader.FileGroupDescriptorFormat };

        /// <summary>
        /// Formats which might contain images, and need to be read to know
        /// </summary>
        private static readonly string[] IndirectImageFormats = new[] { FormatDrop, ClipboardHtml.FormatName, FormatHtmlMime }.Concat(VirtualFileFormats).ToArray();

        /// <summary>
        /// Every format which can give an image
        /// </summary>
        internal static IReadOnlyList<string> ImageReadFormats { get; } = ImageFormats.Concat(IndirectImageFormats).Distinct().ToList();

        /// <summary>
        /// The formats for text
        /// </summary>
        public static IReadOnlyList<string> TextReadFormats { get; } = new[] { FormatUnicodeText, FormatText };

        // Limit for formats which are only read to check something (file names, HTML)
        private const long SmallFormatLimit = 16L * 1024 * 1024;

        /// <summary>
        /// The formats needed to get an image from the current clipboard content: the first two image formats which are available
        /// (the second is a fallback when the first can't be decoded), the file formats, and HTML only when there is no image format.
        /// Reading a format makes the application which copied render it, so not every image format is requested.
        /// This doesn't open the clipboard.
        /// </summary>
        public static IReadOnlyList<string> SelectImageReadFormats()
        {
            var formats = ClipboardNative.AvailableFormats(ImageFormatOrder(ClipboardNative.HasFormat), 2).ToList();
            bool hasImageFormat = formats.Count > 0;
            formats.Add(FormatDrop);
            formats.AddRange(VirtualFileFormats);
            if (!hasImageFormat)
            {
                formats.Add(ClipboardHtml.FormatName);
                formats.Add(FormatHtmlMime);
            }
            return formats;
        }

        /// <summary>
        /// The image formats in the order to try them. Outlook (2010) places a clipped PNG: with a DIB next to it, the DIB is used first.
        /// </summary>
        private static string[] ImageFormatOrder(Func<string, bool> hasFormat) =>
            hasFormat(FormatPngOfficeArt) && hasFormat(FormatDib) ? OutlookImageFormats : ImageFormats;

        /// <summary>
        /// The file extension the file format handlers know for the clipboard format
        /// </summary>
        private static string ExtensionForFormat(string format)
        {
            if (format == FormatPng || format == FormatPngOfficeArt)
            {
                return ".png";
            }
            if (format == FormatJpg || format == FormatJpeg || format == FormatJfif || format == FormatJfifOfficeArt)
            {
                return ".jpg";
            }
            if (format == FormatGif)
            {
                return ".gif";
            }
            if (format == FormatTiff)
            {
                return ".tiff";
            }
            if (format == FormatDib || format == FormatDibV5)
            {
                return ".dib";
            }
            return format;
        }

        /// <summary>
        /// Map a WinForms format name (e.g. "Text", "DeviceIndependentBitmap", "Format17") to the name Dapplo.Windows.Clipboard uses (e.g. "CF_TEXT").
        /// Other names are returned as they are.
        /// </summary>
        public static string NormalizeFormatName(string format)
        {
            switch (format)
            {
                case "Text": return FormatText;
                case "UnicodeText": return FormatUnicodeText;
                case "Bitmap": return FormatBitmap;
                case "DeviceIndependentBitmap": return FormatDib;
                case "Format17": return FormatDibV5;
                case "TaggedImageFileFormat": return FormatTiff;
                case "EnhancedMetafile": return FormatEnhancedMetafile;
                case "FileDrop": return FormatDrop;
                case "Html": return ClipboardHtml.FormatName;
                default: return format;
            }
        }

        #endregion

        #region Access options and errors

        private const int DefaultWriteRetries = 15;
        private static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromMilliseconds(200);
        private const int DefaultReadRetries = 5;
        private static readonly TimeSpan DefaultReadRetryInterval = TimeSpan.FromMilliseconds(100);
        // How long we wait for another thread of Greenshot which has the clipboard open
        private static readonly TimeSpan InProcessLockTimeout = TimeSpan.FromSeconds(2);
        // Synchronous reads mostly run on the UI thread: don't wait long for another thread of Greenshot (e.g. a write which is retrying)
        private static readonly TimeSpan SyncReadLockTimeout = TimeSpan.FromMilliseconds(200);
        // Largest virtual file (e.g. Outlook attachment) which is read
        private const long MaxVirtualFileSize = 256L * 1024 * 1024;

        /// <summary>
        /// The ClipboardAccessOptions Greenshot uses to write: 15 attempts, 200 ms apart (the retries run asynchronously with UseAsync)
        /// </summary>
        public static ClipboardAccessOptions CreateAccessOptions(int retries = DefaultWriteRetries, TimeSpan? retryInterval = null)
        {
            return new ClipboardAccessOptions
            {
                Retries = Math.Max(0, retries),
                RetryInterval = retryInterval ?? DefaultRetryInterval,
                LockTimeout = InProcessLockTimeout
            };
        }

        /// <summary>
        /// The ClipboardException with the message the user sees, naming the application which keeps the clipboard open
        /// </summary>
        private static ClipboardException CreateClipboardException(Exception exception)
        {
            string blocker = (exception as ClipboardAccessDeniedException)?.BlockingProcessName;
            string message = blocker != null
                ? string.Format(Texts.Core.ClipboardInuse, blocker)
                : Texts.Core.ClipboardError;
            Log.Warn(message, exception);
            return new ClipboardException(message, blocker, exception);
        }

        #endregion

        #region Writing

        /// <summary>
        /// Replace the clipboard content, retrying (blocking) while another application has the clipboard open.
        /// Prefer <see cref="SetClipboardDataAsync"/> from async code.
        /// </summary>
        /// <param name="contents">ClipboardContents, prepared before</param>
        /// <exception cref="ClipboardException">when the clipboard couldn't be written, the message names the blocking application</exception>
        public static void SetClipboardData(ClipboardContents contents)
        {
            if (contents == null)
            {
                throw new ArgumentNullException(nameof(contents));
            }

            try
            {
                ClipboardNative.ReplaceContents(contents, IntPtr.Zero, DefaultWriteRetries, DefaultRetryInterval, InProcessLockTimeout);
            }
            catch (Exception ex) when (ex is not ArgumentException and not OperationCanceledException)
            {
                throw CreateClipboardException(ex);
            }
        }

        /// <summary>
        /// Replace the clipboard content, waiting asynchronously while another application has the clipboard open.
        /// </summary>
        /// <param name="contents">ClipboardContents, prepared before</param>
        /// <param name="options">ClipboardAccessOptions, default <see cref="CreateAccessOptions"/></param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <exception cref="ClipboardException">when the clipboard couldn't be written, the message names the blocking application</exception>
        public static async Task SetClipboardDataAsync(ClipboardContents contents, ClipboardAccessOptions options = null, CancellationToken cancellationToken = default)
        {
            if (contents == null)
            {
                throw new ArgumentNullException(nameof(contents));
            }

            try
            {
                // The work only places prepared data, it doesn't await
                await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(contents), options ?? CreateAccessOptions(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not ArgumentException and not OperationCanceledException)
            {
                throw CreateClipboardException(ex);
            }
        }

        /// <summary>
        /// Set text to the clipboard
        /// </summary>
        /// <param name="text">string</param>
        /// <exception cref="ClipboardException">when the clipboard couldn't be written</exception>
        public static void SetClipboardData(string text)
        {
            SetClipboardData(new ClipboardContents().AddUnicodeString(text ?? string.Empty));
        }

        /// <summary>
        /// Attempts to set text on the clipboard. Returns true on success, or false with an errorMessage on failure.
        /// </summary>
        public static bool TrySetClipboardData(string text, out string errorMessage)
        {
            try
            {
                SetClipboardData(text);
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Everything to put on the clipboard for an image and/or text, created on any thread before the clipboard is opened.
        /// Dispose it after it was placed: the clipboard has its own copy then.
        /// </summary>
        public sealed class ClipboardContent : IDisposable
        {
            private readonly List<IDisposable> _resources = new List<IDisposable>();

            internal ClipboardContent()
            {
            }

            /// <summary>
            /// The formats to place on the clipboard, in the order of preference
            /// </summary>
            public ClipboardContents Contents { get; } = new ClipboardContents();

            /// <summary>
            /// False when there is nothing to place (no text, no image formats)
            /// </summary>
            public bool HasData { get; internal set; }

            internal T Own<T>(T resource) where T : IDisposable
            {
                _resources.Add(resource);
                return resource;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                foreach (var resource in _resources)
                {
                    resource?.Dispose();
                }
                _resources.Clear();
            }
        }

        /// <summary>
        /// Create the clipboard content for the image (borrowed, not disposed) in the requested formats (default: configured formats), and text.
        /// This doesn't touch the clipboard and can run on any thread. The formats are placed in this order, richest first:
        /// PNG, DIBV5, DIB, HTML (or HTML with a data URL), text.
        /// </summary>
        public static ClipboardContent CreateContent(Image imageToSave, IEnumerable<ClipboardFormat> formats = null, string text = null)
        {
            var activeFormats = formats?.ToList() ?? CoreConfiguration?.ClipboardFormats ?? new List<ClipboardFormat>();
            var content = new ClipboardContent();
            if (imageToSave != null && activeFormats.Count > 0)
            {
                AddImageFormats(content, imageToSave, activeFormats);
            }

            if (!string.IsNullOrEmpty(text))
            {
                // Last: applications which take the first format they understand should get the image. Windows synthesizes CF_TEXT and CF_OEMTEXT.
                content.Contents.AddUnicodeString(text);
                content.HasData = true;
            }

            return content;
        }

        private static ICoreConfiguration CoreConfiguration
        {
            get
            {
                try
                {
                    return IniConfigRegistry.GetSection<ICoreConfiguration>();
                }
                catch
                {
                    return null;
                }
            }
        }

        private static void AddImageFormats(ClipboardContent content, Image imageToSave, IList<ClipboardFormat> activeFormats)
        {
            var contents = content.Contents;
            // The PNG, HTML and HTMLDATAURL formats all use the same PNG, it's encoded only once
            MemoryStream pngStream = null;
            MemoryStream GetPngStream()
            {
                if (pngStream == null)
                {
                    pngStream = content.Own(CreatePngStream(imageToSave));
                }
                return pngStream;
            }

            if (activeFormats.Contains(ClipboardFormat.PNG))
            {
                try
                {
                    // PNG works for e.g. Powerpoint
                    var png = GetPngStream();
                    contents.AddStream(FormatPng, png, png.Length);
                    content.HasData = true;
                }
                catch (Exception pngEx)
                {
                    Log.Error("Error creating PNG for the Clipboard.", pngEx);
                }
            }

            // BITMAP used to place a .NET Bitmap object (and CF_BITMAP), Windows synthesizes CF_BITMAP from CF_DIB
            var dibFormats = DibFormats.None;
            if (activeFormats.Contains(ClipboardFormat.DIBV5))
            {
                dibFormats |= DibFormats.DibV5;
            }
            // Without alpha, Windows creates the same CF_DIB from CF_DIBV5 when an application asks for it
            bool hasAlpha = Image.IsAlphaPixelFormat(imageToSave.PixelFormat);
            if ((activeFormats.Contains(ClipboardFormat.DIB) || activeFormats.Contains(ClipboardFormat.BITMAP)) && (hasAlpha || dibFormats == DibFormats.None))
            {
                dibFormats |= DibFormats.Dib;
            }

            if (dibFormats != DibFormats.None)
            {
                try
                {
                    var pixels = ClipboardBitmapConverter.ToBgra32(imageToSave);
                    // The DIBs are encoded straight into the clipboard memory when the contents are placed, CF_DIBV5 before CF_DIB
                    if (hasAlpha && (dibFormats & DibFormats.Dib) != 0)
                    {
                        // CF_DIB has no defined alpha channel, many applications show transparent pixels black: place it on white
                        contents.AddDib(new ReadOnlyMemory<byte>(pixels.Pixels), pixels.Width, pixels.Height, pixels.Stride, pixels.PremultipliedAlpha, dibFormats & DibFormats.DibV5);
                        contents.AddDib(new ReadOnlyMemory<byte>(ClipboardBitmapConverter.FlattenOnWhite(pixels)), pixels.Width, pixels.Height, pixels.Stride, false, DibFormats.Dib);
                    }
                    else
                    {
                        contents.AddDib(new ReadOnlyMemory<byte>(pixels.Pixels), pixels.Width, pixels.Height, pixels.Stride, pixels.PremultipliedAlpha, dibFormats);
                    }

                    content.HasData = true;
                }
                catch (Exception dibEx)
                {
                    Log.Error("Error creating DIB for the Clipboard.", dibEx);
                }
            }

            try
            {
                if (activeFormats.Contains(ClipboardFormat.HTML))
                {
                    string tmpFile = ImageIO.SaveEncodedToTmpFile(GetPngStream(), WellKnownFileFormats.Png, null);
                    contents.AddHtml(CreateImageFragment(new Uri(tmpFile).AbsoluteUri, imageToSave.Size));
                    content.HasData = true;
                }
                else if (activeFormats.Contains(ClipboardFormat.HTMLDATAURL))
                {
                    var png = GetPngStream();
                    string dataUrl = "data:image/png;base64," + Convert.ToBase64String(png.GetBuffer(), 0, (int)png.Length);
                    contents.AddHtml(CreateImageFragment(dataUrl, imageToSave.Size));
                    content.HasData = true;
                }
            }
            catch (Exception htmlEx)
            {
                Log.Error("Error creating HTML for the Clipboard.", htmlEx);
            }
        }

        /// <summary>
        /// Encode the image as PNG for the clipboard, used for the PNG, HTML and HTMLDATAURL formats
        /// </summary>
        private static MemoryStream CreatePngStream(Image image)
        {
            var pngStream = RecyclableMemoryStreamFactory.GetStream("ClipboardHelper.PNG");
            var pngOutputSettings = new SurfaceOutputSettings(WellKnownFileFormats.Png, 100, false)
            {
                // Do not allow to reduce the colors, some applications dislike 256 color images
                // reported with bug #3594681
                DisableReduceColors = true
            };
            try
            {
                if (image.PixelFormat != PixelFormat.Format8bppIndexed)
                {
                    ImageIO.SaveToStream(image, null, pngStream, pngOutputSettings);
                }
                else
                {
                    // A 256 color image is converted first, some applications dislike them
                    using var fullColorImage = ImageHelper.Clone(image, PixelFormat.Format32bppArgb);
                    ImageIO.SaveToStream(fullColorImage, null, pngStream, pngOutputSettings);
                }
            }
            catch
            {
                pngStream.Dispose();
                throw;
            }
            pngStream.Position = 0;
            return pngStream;
        }

        /// <summary>
        /// The HTML fragment with the img element, Dapplo creates the CF_HTML header
        /// </summary>
        private static string CreateImageFragment(string source, Size imageSize)
        {
            return $"<img border='0' src='{WebUtility.HtmlEncode(source)}' width='{imageSize.Width}' height='{imageSize.Height}'>";
        }

        #endregion

        #region Reading the clipboard

        /// <summary>
        /// Copy the formats from the clipboard in one short clipboard session, retrying (blocking) a few times when it's in use.
        /// </summary>
        /// <param name="formats">the formats to read, null for all formats</param>
        /// <param name="maxBytesPerFormat">larger formats are skipped</param>
        /// <returns>ClipboardSnapshot, or null when the clipboard couldn't be opened</returns>
        public static ClipboardSnapshot ReadSnapshot(IEnumerable<string> formats, long maxBytesPerFormat = long.MaxValue)
        {
            using var clipboard = ClipboardNative.Access(IntPtr.Zero, DefaultReadRetries, DefaultReadRetryInterval, SyncReadLockTimeout);
            if (!clipboard.CanAccess)
            {
                Log.WarnFormat("Couldn't read the clipboard, it's in use by {0}", clipboard.GetBlockingProcessName() ?? "an unknown application");
                return null;
            }

            return clipboard.ReadSnapshot(formats, maxBytesPerFormat);
        }

        /// <summary>
        /// Copy the formats from the clipboard in one short clipboard session, waiting asynchronously when it's in use.
        /// </summary>
        /// <param name="formats">the formats to read, null for all formats</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>ClipboardSnapshot, or null when the clipboard couldn't be opened</returns>
        public static Task<ClipboardSnapshot> ReadSnapshotAsync(IEnumerable<string> formats, CancellationToken cancellationToken = default) =>
            ReadSnapshotAsync(formats, long.MaxValue, cancellationToken);

        /// <summary>
        /// Copy the formats from the clipboard in one short clipboard session, waiting asynchronously when it's in use.
        /// The clipboard is read on the context of the caller.
        /// </summary>
        /// <param name="formats">the formats to read, null for all formats</param>
        /// <param name="maxBytesPerFormat">larger formats are skipped</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>ClipboardSnapshot, or null when the clipboard couldn't be opened</returns>
        public static async Task<ClipboardSnapshot> ReadSnapshotAsync(IEnumerable<string> formats, long maxBytesPerFormat, CancellationToken cancellationToken = default)
        {
            try
            {
                // No ConfigureAwait(false): callers may continue with work which needs their thread
                return await ClipboardNative.ReadSnapshotAsync(formats, maxBytesPerFormat, CreateAccessOptions(DefaultReadRetries, DefaultReadRetryInterval), cancellationToken);
            }
            catch (ClipboardAccessDeniedException ex)
            {
                Log.Warn($"Couldn't read the clipboard, it's in use by {ex.BlockingProcessName ?? "an unknown application"}", ex);
                return null;
            }
        }

        /// <summary>
        /// The result of the last detailed image check, with the clipboard sequence number it belongs to
        /// </summary>
        private static Tuple<uint, bool> _lastImageCheck;

        /// <summary>
        /// Phase 1 of the image check, without opening the clipboard (only IsClipboardFormatAvailable), fast enough for the UI thread:
        /// true when there is an image format, false when there is nothing which could contain an image, null when a file list,
        /// virtual files or HTML have to be looked at (phase 2, <see cref="ContainsImageAsync"/>). A detailed result for the current
        /// clipboard content is remembered, so null is only returned once per clipboard change.
        /// </summary>
        /// <returns>bool? true: image, false: no image, null: unknown without reading the clipboard</returns>
        public static bool? ContainsImageQuick()
        {
            if (DirectImageFormats.Any(ClipboardNative.HasFormat))
            {
                return true;
            }

            if (!IndirectImageFormats.Any(ClipboardNative.HasFormat))
            {
                return false;
            }

            var lastCheck = Volatile.Read(ref _lastImageCheck);
            if (lastCheck != null && lastCheck.Item1 == ClipboardNative.SequenceNumber)
            {
                return lastCheck.Item2;
            }
            return null;
        }

        /// <summary>
        /// Two phase image check: <see cref="ContainsImageQuick"/>, and only when that can't tell, phase 2 copies the file names and HTML
        /// in a short snapshot (waiting asynchronously when the clipboard is busy) and checks them without decoding anything:
        /// supported file extensions, an img element in the HTML, and on an STA thread (the UI thread) the names of virtual files
        /// (e.g. Outlook attachments), their content isn't read.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task with true when the clipboard has an image</returns>
        public static async Task<bool> ContainsImageAsync(CancellationToken cancellationToken = default)
        {
            var quick = ContainsImageQuick();
            if (quick.HasValue)
            {
                return quick.Value;
            }

            // Waiting for a busy clipboard is asynchronous, copying the small formats takes only a moment.
            // No ConfigureAwait(false): virtual files are checked on the calling (UI, STA) thread.
            var snapshot = await ReadSnapshotAsync(IndirectImageFormats, SmallFormatLimit, cancellationToken);
            return snapshot != null && CompleteImageCheck(snapshot, ContainsImageFileOrHtml(snapshot));
        }

        /// <summary>
        /// Two phase image check like <see cref="ContainsImageAsync"/>, synchronous: phase 2 runs on the calling thread.
        /// Virtual files are only checked when that is an STA thread.
        /// </summary>
        public static bool ContainsImageExact()
        {
            var quick = ContainsImageQuick();
            if (quick.HasValue)
            {
                return quick.Value;
            }

            var snapshot = ReadSnapshot(IndirectImageFormats, SmallFormatLimit);
            return snapshot != null && CompleteImageCheck(snapshot, ContainsImageFileOrHtml(snapshot));
        }

        private static bool ContainsImageFileOrHtml(IClipboardDataSource source) =>
            GetImageFilenames(source).Count > 0 || (TryGetHtml(source, out var html, out _) && html.IndexOf("<img", StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>
        /// Check the virtual file names when needed (only possible on an STA thread), and remember the result when it's complete
        /// </summary>
        private static bool CompleteImageCheck(ClipboardSnapshot snapshot, bool foundInFilesOrHtml)
        {
            bool result = foundInFilesOrHtml;
            bool complete = true;
            if (!result && snapshot.HasVirtualFiles())
            {
                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                {
                    result = ContainsVirtualImageFile(snapshot);
                }
                else
                {
                    complete = false;
                }
            }

            if (complete)
            {
                Volatile.Write(ref _lastImageCheck, Tuple.Create(snapshot.SequenceNumber, result));
            }
            return result;
        }

        /// <summary>
        /// True when a virtual file of the source has an extension Greenshot can load, only the names are read
        /// </summary>
        private static bool ContainsVirtualImageFile(IClipboardDataSource source)
        {
            var supportedExtensions = SupportedExtensions(FileFormatHandlerActions.LoadDrawableFromStream);
            return UseVirtualFiles(source,
                virtualFiles => virtualFiles.Any(file => !file.IsDirectory && supportedExtensions.Contains(SafeExtension(file.SafeFileName))), false);
        }

        /// <summary>
        /// True when the clipboard has text, the clipboard isn't opened
        /// </summary>
        public static bool ContainsText() => TextReadFormats.Any(ClipboardNative.HasFormat);

        #endregion

        #region Reading any IClipboardDataSource (snapshot, drop, OLE clipboard)

        private static IEnumerable<IFileFormatHandler> FileFormatHandlers => SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();

        /// <summary>
        /// Test if the source contains text
        /// </summary>
        public static bool ContainsText(IClipboardDataSource source) => source != null && TextReadFormats.Any(source.HasFormat);

        /// <summary>
        /// Get the text of the source
        /// </summary>
        /// <returns>string or null</returns>
        public static string GetText(IClipboardDataSource source)
        {
            // Falls back to CF_TEXT / CF_OEMTEXT for sources where Windows doesn't synthesize CF_UNICODETEXT, e.g. a drop
            return source?.GetAsUnicodeString();
        }

        /// <summary>
        /// Check if the source has an image: an image format, an image file, a virtual image file or HTML with an img element.
        /// Virtual files of a ClipboardSnapshot are only checked on an STA thread.
        /// </summary>
        public static bool ContainsImage(IClipboardDataSource source)
        {
            if (source == null)
            {
                return false;
            }

            Log.DebugFormat("Found formats: {0}", string.Join(",", source.Formats));
            if (DirectImageFormats.Any(source.HasFormat) || GetImageFilenames(source).Any())
            {
                return true;
            }

            return ContainsVirtualImageFile(source) || GetHtmlImageUrls(source).Count > 0;
        }

        /// <summary>
        /// Get the first image of the source, only this image is decoded.
        /// Priority: the image formats (see <see cref="ImageFormats"/>), then virtual files, then files.
        /// HTML images need a download, see <see cref="GetHtmlImageUrls"/>.
        /// </summary>
        /// <param name="source">IClipboardDataSource, e.g. a ClipboardSnapshot or a DataObjectReader</param>
        /// <returns>Bitmap, the caller disposes it, or null</returns>
        public static Bitmap GetFirstImage(IClipboardDataSource source)
        {
            if (source == null)
            {
                return null;
            }
            return GetImageFromFormats(source) ?? GetFirstVirtualFileImage(source) ?? GetFirstFileImage(source);
        }

        /// <summary>
        /// The image of the best image format of the source
        /// </summary>
        internal static Bitmap GetImageFromFormats(IClipboardDataSource source) => LoadFromFormats(source, LoadBitmap).FirstOrDefault();

        /// <summary>
        /// The first virtual file of the source which is an image, null when there is none (or they can't be read on this thread)
        /// </summary>
        internal static Bitmap GetFirstVirtualFileImage(IClipboardDataSource source) => LoadFromVirtualFiles(source, LoadBitmap, firstOnly: true).FirstOrDefault();

        /// <summary>
        /// The first file (CF_HDROP) of the source which is an image, null when there is none
        /// </summary>
        internal static Bitmap GetFirstFileImage(IClipboardDataSource source) => LoadFromFiles(source, LoadBitmap, firstOnly: true).FirstOrDefault();

        /// <summary>
        /// Get the drawables of the source: from the best image format, or else from all virtual files, or else from all files.
        /// </summary>
        /// <param name="source">IClipboardDataSource, e.g. a ClipboardSnapshot or a DataObjectReader</param>
        /// <returns>list with the IDrawableContainer, empty if there are none</returns>
        public static IList<IDrawableContainer> GetDrawables(IClipboardDataSource source)
        {
            if (source == null)
            {
                return new List<IDrawableContainer>();
            }

            var drawables = LoadFromFormats(source, LoadDrawables);
            if (drawables.Count == 0)
            {
                drawables = LoadFromVirtualFiles(source, LoadDrawables, firstOnly: false);
            }
            if (drawables.Count == 0)
            {
                drawables = LoadFromFiles(source, LoadDrawables, firstOnly: false);
            }
            return drawables;
        }

        /// <summary>
        /// Get the image files from the source (CF_HDROP) which Greenshot can load
        /// </summary>
        internal static IList<string> GetImageFilenames(IClipboardDataSource source)
        {
            if (source == null || !source.HasFormat(FormatDrop))
            {
                return Array.Empty<string>();
            }

            var supportedExtensions = SupportedExtensions(FileFormatHandlerActions.LoadFromStream);
            return source.GetFileNames()
                .Where(filename => !string.IsNullOrEmpty(filename) && supportedExtensions.Contains(SafeExtension(filename)))
                .ToList();
        }

        /// <summary>
        /// The urls of the images in the HTML of the source (CF_HTML, or Firefox's text/html), the caller downloads them (async) when there is no other image.
        /// Relative urls are resolved with the SourceURL of CF_HTML, when that is a web page.
        /// </summary>
        /// <param name="source">IClipboardDataSource</param>
        /// <returns>list with the urls, empty when there are none</returns>
        public static IList<string> GetHtmlImageUrls(IClipboardDataSource source)
        {
            var imageUrls = new List<string>();
            if (source == null)
            {
                return imageUrls;
            }

            if (!TryGetHtml(source, out var html, out var baseUri))
            {
                return imageUrls;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var imgNodes = doc.DocumentNode.SelectNodes("//img");
            if (imgNodes == null)
            {
                return imageUrls;
            }

            // Only resolve against web pages: a relative src must not become a file:// (UNC) url
            bool isWebPage = baseUri != null && baseUri.IsAbsoluteUri && (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps);
            foreach (var imgNode in imgNodes)
            {
                var imageUrl = WebUtility.HtmlDecode(imgNode.Attributes["src"]?.Value);
                if (string.IsNullOrEmpty(imageUrl))
                {
                    continue;
                }

                if (isWebPage && !Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute) && Uri.TryCreate(baseUri, imageUrl, out var absoluteUri))
                {
                    imageUrl = absoluteUri.AbsoluteUri;
                }
                Log.Debug(imageUrl);
                imageUrls.Add(imageUrl);
            }

            return imageUrls;
        }

        /// <summary>
        /// The HTML of the source: the fragment of CF_HTML (with its SourceURL), or Firefox's text/html
        /// </summary>
        private static bool TryGetHtml(IClipboardDataSource source, out string html, out Uri baseUri)
        {
            html = null;
            baseUri = null;
            if (source.TryGetAsHtml(out var clipboardHtml))
            {
                html = clipboardHtml.Fragment ?? clipboardHtml.FullHtml;
                baseUri = clipboardHtml.SourceUrl;
            }
            else if (source.TryGetAsUtf8String(FormatHtmlMime, out var mimeHtml))
            {
                html = mimeHtml?.TrimEnd('\0');
            }
            return !string.IsNullOrEmpty(html);
        }

        #endregion

        #region Loading images and drawables

        private static IList<string> SupportedExtensions(FileFormatHandlerActions action) => FileFormatHandlers.ExtensionsFor(action).ToList();

        private static string SafeExtension(string fileName)
        {
            try
            {
                return Path.GetExtension(fileName)?.ToLowerInvariant();
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Load a bitmap from a stream with the extension, CF_DIB / CF_DIBV5 go to the DibFileFormatHandler
        /// </summary>
        private static IEnumerable<Bitmap> LoadBitmap(Stream stream, string extension) =>
            FileFormatHandlers.TryLoadFromStream(stream, extension, out var bitmap) ? new[] { bitmap } : Array.Empty<Bitmap>();

        /// <summary>
        /// Load drawables from a stream with the extension, CF_DIB / CF_DIBV5 go to the DibFileFormatHandler
        /// </summary>
        private static IEnumerable<IDrawableContainer> LoadDrawables(Stream stream, string extension) =>
            FileFormatHandlers.LoadDrawablesFromStream(stream, extension).ToList();

        /// <summary>
        /// Load from the first image format of the source which gives a result
        /// </summary>
        private static List<T> LoadFromFormats<T>(IClipboardDataSource source, Func<Stream, string, IEnumerable<T>> load)
        {
            foreach (string format in ImageFormatOrder(source.HasFormat))
            {
                if (!source.HasFormat(format))
                {
                    continue;
                }

                Log.InfoFormat("Found {0}, trying to retrieve.", format);
                try
                {
                    if (!source.TryGetStream(format, out var stream))
                    {
                        continue;
                    }

                    using (stream)
                    {
                        if (stream.CanSeek && stream.Length == 0)
                        {
                            continue;
                        }
                        var result = load(stream, ExtensionForFormat(format)).Where(item => item != null).ToList();
                        if (result.Count > 0)
                        {
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Couldn't read {format}", ex);
                }
            }

            return new List<T>();
        }

        /// <summary>
        /// Load the virtual files with a supported extension, the content is read right away (the data object is only valid for a short time).
        /// Only the extension of the name is used, and the name is taken from SafeFileName.
        /// </summary>
        private static List<T> LoadFromVirtualFiles<T>(IClipboardDataSource source, Func<Stream, string, IEnumerable<T>> load, bool firstOnly)
        {
            var supportedExtensions = SupportedExtensions(FileFormatHandlerActions.LoadDrawableFromStream);
            return UseVirtualFiles(source, virtualFiles =>
            {
                var result = new List<T>();
                foreach (var virtualFile in virtualFiles)
                {
                    if (firstOnly && result.Count > 0)
                    {
                        break;
                    }

                    string extension = SafeExtension(virtualFile.SafeFileName);
                    if (virtualFile.IsDirectory || virtualFile.Size > MaxVirtualFileSize || extension == null || !supportedExtensions.Contains(extension))
                    {
                        continue;
                    }

                    try
                    {
                        using var content = virtualFile.OpenContent();
                        if (content != null && !(content.CanSeek && content.Length == 0))
                        {
                            result.AddRange(load(content, extension).Where(item => item != null));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Couldn't read file contents for {virtualFile.SafeFileName}.", ex);
                    }
                }
                return result;
            }, new List<T>());
        }

        /// <summary>
        /// Load the files (CF_HDROP) with a supported extension
        /// </summary>
        private static List<T> LoadFromFiles<T>(IClipboardDataSource source, Func<Stream, string, IEnumerable<T>> load, bool firstOnly)
        {
            var result = new List<T>();
            foreach (string fileName in GetImageFilenames(source))
            {
                if (firstOnly && result.Count > 0)
                {
                    break;
                }

                try
                {
                    using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    result.AddRange(load(fileStream, SafeExtension(fileName)).Where(item => item != null));
                }
                catch (Exception ex)
                {
                    Log.Error($"Couldn't read file contents of {fileName}", ex);
                }
            }
            return result;
        }

        /// <summary>
        /// Use the virtual files of the source. A DataObjectReader (drop) has them directly; for a snapshot of the clipboard
        /// Dapplo takes the OLE data object, when this is an STA thread and the clipboard didn't change since the snapshot.
        /// The files can only be read inside <paramref name="use"/>, so it must return materialized results.
        /// </summary>
        private static T UseVirtualFiles<T>(IClipboardDataSource source, Func<IReadOnlyList<VirtualFile>, T> use, T none)
        {
            switch (source)
            {
                case DataObjectReader reader:
                    reader.MaxDataSize = Math.Min(reader.MaxDataSize, MaxVirtualFileSize);
                    return use(reader.GetVirtualFiles());
                case ClipboardSnapshot snapshot when snapshot.HasVirtualFiles():
                    if (snapshot.TryUseVirtualFiles(use, out var result, MaxVirtualFileSize))
                    {
                        return result;
                    }
                    Log.Debug("The virtual files of the clipboard couldn't be read: not an STA thread, the clipboard changed, or it's in use.");
                    return none;
                default:
                    return none;
            }
        }

        #endregion
    }
}
