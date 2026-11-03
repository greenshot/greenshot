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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers
{
    public class GreenshotFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotFileFormatHandler));
        private readonly IReadOnlyCollection<string> _ourExtensions = new [] { ".greenshot" };
        public GreenshotFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToFile] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromFile] = _ourExtensions;
        }

        public override bool TrySaveToStream(Bitmap bitmap, Stream stream, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            if (surface == null)
            {
                return false;
            }

            try
            {
                bitmap.Save(stream, ImageFormat.Png);
                WriteHdrImage(stream, surface, bitmap);
                using MemoryStream tmpStream = RecyclableMemoryStreamFactory.GetStream("GreenshotFileFormatHandler.SaveToStream");
                long bytesWritten = surface.SaveElementsToStream(tmpStream);
                using BinaryWriter writer = new BinaryWriter(tmpStream);
                writer.Write(bytesWritten);
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                byte[] marker = Encoding.ASCII.GetBytes($"Greenshot{v.Major:00}.{v.Minor:00}");
                writer.Write(marker);
                tmpStream.WriteTo(stream);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't save surface as .greenshot: ", ex);
            }

            return false;
        }

        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            try
            {
                // The export is a new image, the surface with its own copy and elements isn't needed afterwards
                using var surface = LoadSurface(stream);
                bitmap = (Bitmap)surface.GetImageForExport();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't load .greenshot: ", ex);
            }
            bitmap = null;
            return false;
        }

        public override ISurface LoadSurface(Stream surfaceFileStream)
        {
            var returnSurface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
            Bitmap captureBitmap;

            // Fixed problem that the bitmap stream is disposed... by Cloning the image
            // This also ensures the bitmap is correctly created
            using (Image tmpImage = Image.FromStream(surfaceFileStream, true, true))
            {
                Log.DebugFormat("Loaded capture from .greenshot file with Size {0}x{1} and PixelFormat {2}", tmpImage.Width, tmpImage.Height, tmpImage.PixelFormat);
                captureBitmap = ImageHelper.Clone(tmpImage) as Bitmap;
            }

            HdrImage hdrImage = null;
            // Start at -14 read "GreenshotXX.YY" (XX=Major, YY=Minor)
            const int markerSize = 14;
            surfaceFileStream.Seek(-markerSize, SeekOrigin.End);
            using (var streamReader = new StreamReader(surfaceFileStream))
            {
                var greenshotMarker = streamReader.ReadToEnd();
                if (!greenshotMarker.StartsWith("Greenshot"))
                {
                    throw new ArgumentException("Stream is not a Greenshot file!");
                }

                Log.InfoFormat("Greenshot file format: {0}", greenshotMarker);
                const int fileSizeLocation = 8 + markerSize;
                surfaceFileStream.Seek(-fileSizeLocation, SeekOrigin.End);
                using BinaryReader reader = new BinaryReader(surfaceFileStream);
                long bytesWritten = reader.ReadInt64();
                long elementsStart = surfaceFileStream.Seek(-(bytesWritten + fileSizeLocation), SeekOrigin.End);
                hdrImage = ReadHdrImage(surfaceFileStream, reader, elementsStart);
                surfaceFileStream.Seek(elementsStart, SeekOrigin.Begin);
                returnSurface.LoadElementsFromStream(surfaceFileStream);
            }

            if (captureBitmap != null)
            {
                returnSurface.Image = captureBitmap;
                Log.InfoFormat("Information about .greenshot file: {0}x{1}-{2} Resolution {3}x{4}", captureBitmap.Width, captureBitmap.Height, captureBitmap.PixelFormat, captureBitmap.HorizontalResolution, captureBitmap.VerticalResolution);
                if (hdrImage != null)
                {
                    returnSurface.SetHdrImage(hdrImage);
                    hdrImage = null;
                }
            }
            hdrImage?.Dispose();

            return returnSurface;
        }

        /// <summary>
        /// Between the image and the elements: an HDR image as JPEG XR, whether HDR was asked for, its length and this tag. An older Greenshot
        /// reads the elements from the end of the file, it doesn't see it.
        /// </summary>
        private static readonly byte[] HdrTag = Encoding.ASCII.GetBytes("GreenshotHDR");

        /// <summary>
        /// The HDR image of the surface (with the edits of the editor, without the elements: they are in the file too), so the capture is
        /// HDR again when the file is opened. Nothing without HDR content or without a JPEG XR encoder.
        /// </summary>
        private static void WriteHdrImage(Stream stream, ISurface surface, Bitmap bitmap)
        {
            if (surface.HdrState != HdrState.Available)
            {
                return;
            }
            var encoder = SimpleServiceProvider.Current.GetInstance<IHdrImageEncoder>(true);
            if (encoder == null || !encoder.CanEncode(WellKnownFileFormats.Jxr))
            {
                Log.Info("No JPEG XR encoder, the .greenshot file is saved without its HDR content.");
                return;
            }
            var hdrImage = surface.HdrImage;
            if (hdrImage == null || !hdrImage.HasHdrContent)
            {
                return;
            }
            using var complete = hdrImage.Compose(bitmap);
            if (!encoder.TryEncode(complete, WellKnownFileFormats.Jxr, surface.CaptureDetails, true, out var encoded, out string error))
            {
                Log.Warn($"The .greenshot file is saved without its HDR content: {error}");
                return;
            }
            var bytes = encoded.ToArray();
            using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
            writer.Write(bytes);
            writer.Write(surface.IsHdrRequested);
            writer.Write((long)bytes.Length);
            writer.Write(HdrTag);
        }

        /// <summary>
        /// The HDR image before the elements (see <see cref="WriteHdrImage"/>), null when the file has none (also every older one)
        /// </summary>
        private static HdrImage ReadHdrImage(Stream stream, BinaryReader reader, long elementsStart)
        {
            const int trailerSize = 1 + 8;
            if (elementsStart < HdrTag.Length + trailerSize)
            {
                return null;
            }
            stream.Seek(elementsStart - HdrTag.Length, SeekOrigin.Begin);
            var tag = reader.ReadBytes(HdrTag.Length);
            if (!tag.AsSpan().SequenceEqual(HdrTag))
            {
                return null;
            }
            stream.Seek(elementsStart - HdrTag.Length - trailerSize, SeekOrigin.Begin);
            bool requested = reader.ReadBoolean();
            long length = reader.ReadInt64();
            long start = elementsStart - HdrTag.Length - trailerSize - length;
            var decoder = SimpleServiceProvider.Current.GetInstance<IHdrImageDecoder>(true);
            if (length <= 0 || start < 0 || decoder == null)
            {
                Log.Info("The HDR content of the .greenshot file can't be read here.");
                return null;
            }
            stream.Seek(start, SeekOrigin.Begin);
            using var hdrStream = new MemoryStream(reader.ReadBytes((int)length), false);
            if (!decoder.TryDecode(hdrStream, out var hdrImage))
            {
                Log.Warn("The HDR content of the .greenshot file couldn't be read.");
                return null;
            }
            hdrImage.IsRequested = requested;
            return hdrImage;
        }
    }
}
