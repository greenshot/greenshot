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
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Greenshot.Document;
using log4net;

namespace Greenshot.WpfEditor.Files
{
    /// <summary>
    /// The image on the clipboard of Windows: copied as PNG (keeps transparency) and as bitmap (for every other program),
    /// pasted from PNG, a bitmap or an image file
    /// </summary>
    public static class EditorClipboard
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(EditorClipboard));
        private const string PngFormat = "PNG";

        public static void CopyImage(GreenshotDocument document)
        {
            var png = DocumentFiles.ToPng(document);
            var data = new DataObject();
            data.SetData(PngFormat, new MemoryStream(png), false);
            data.SetImage(ToBitmapSource(document));
            Clipboard.SetDataObject(data, true);
        }

        public static BitmapSource ToBitmapSource(GreenshotDocument document)
        {
            var (pixels, width, height) = DocumentFiles.ToBgra(document);
            var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
            bitmap.Freeze();
            return bitmap;
        }

        public static bool ContainsImage()
        {
            try
            {
                return Clipboard.ContainsData(PngFormat) || Clipboard.ContainsImage() || ImageFileOnClipboard() != null;
            }
            catch (Exception ex)
            {
                // Another program holds the clipboard
                Log.Debug("The clipboard can't be read", ex);
                return false;
            }
        }

        /// <summary>
        /// The bytes of the image on the clipboard (PNG, or the image file), null when there is none
        /// </summary>
        public static byte[] PasteImage()
        {
            try
            {
                if (Clipboard.GetData(PngFormat) is MemoryStream png)
                {
                    return png.ToArray();
                }

                if (ImageFileOnClipboard() is string file)
                {
                    return File.ReadAllBytes(file);
                }

                if (Clipboard.ContainsImage() && Clipboard.GetImage() is BitmapSource bitmap)
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = new MemoryStream();
                    encoder.Save(stream);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Pasting the image of the clipboard failed", ex);
            }

            return null;
        }

        private static string ImageFileOnClipboard()
        {
            if (!Clipboard.ContainsFileDropList())
            {
                return null;
            }

            return Clipboard.GetFileDropList().Cast<string>().FirstOrDefault(IsImageFile);
        }

        public static bool IsImageFile(string path)
        {
            string extension = Path.GetExtension(path);
            return DocumentFiles.ImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) && File.Exists(path);
        }
    }
}
