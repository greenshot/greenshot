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
using System.IO;
using System.Linq;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.Document.Legacy;
using Greenshot.Document.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.WpfEditor.Files
{
    /// <summary>
    /// Opens and saves documents: .greenshot packages (zip with document.json), .greenshot files of Greenshot 1.2 - 1.4
    /// (through the legacy mapper) and images. Saving an image format draws the document with the document renderer.
    /// </summary>
    public static class DocumentFiles
    {
        public const string DocumentExtension = ".greenshot";

        /// <summary>
        /// The image formats the editor saves, by extension
        /// </summary>
        private static readonly Dictionary<string, Func<IImageEncoder>> Encoders = new(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = () => new PngEncoder(),
            [".jpg"] = () => new JpegEncoder { Quality = 90 },
            [".jpeg"] = () => new JpegEncoder { Quality = 90 },
            [".bmp"] = () => new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel32, SupportTransparency = true },
            [".gif"] = () => new GifEncoder(),
            [".tif"] = () => new TiffEncoder(),
            [".tiff"] = () => new TiffEncoder(),
            [".webp"] = () => new WebpEncoder()
        };

        private static readonly Dictionary<string, string> MediaTypeExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/png"] = "png",
            ["image/jpeg"] = "jpg",
            ["image/gif"] = "gif",
            ["image/bmp"] = "bmp",
            ["image/tiff"] = "tif",
            ["image/webp"] = "webp",
            ["image/svg+xml"] = "svg",
            ["image/x-icon"] = "ico",
            ["image/vnd.microsoft.icon"] = "ico",
            ["image/x-emf"] = "emf",
            ["image/x-wmf"] = "wmf",
            ["image/jxl"] = "jxl",
            ["image/avif"] = "avif"
        };

        public static IReadOnlyCollection<string> ImageExtensions => Encoders.Keys;

        public static bool IsDocument(string path) => string.Equals(Path.GetExtension(path), DocumentExtension, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Reads a document, an old .greenshot file or an image
        /// </summary>
        public static GreenshotDocument Open(string path)
        {
            var bytes = File.ReadAllBytes(path);
            using var stream = new MemoryStream(bytes, false);
            if (bytes.Length > 4 && bytes[0] == 'P' && bytes[1] == 'K')
            {
                return GreenshotPackage.Read(stream);
            }

            if (IsDocument(path))
            {
                // Greenshot 1.2 - 1.4: a PNG followed by the serialized elements
                return LegacyToDocument.ReadGreenshotFile(stream);
            }

            return FromImageBytes(bytes);
        }

        /// <summary>
        /// A new document showing the image (PNG, JPEG, ... whatever ImageSharp reads)
        /// </summary>
        public static GreenshotDocument FromImageBytes(byte[] bytes)
        {
            var resource = ImageResource(bytes, out int width, out int height);
            return DocumentEdits.FromImage(resource, width, height);
        }

        /// <summary>
        /// A resource with the image, its media type and size read from the image
        /// </summary>
        public static Resource ImageResource(byte[] bytes, out int width, out int height)
        {
            var info = Image.Identify(bytes, out IImageFormat format) ?? throw new InvalidDataException("The file is not an image ImageSharp can read");
            width = info.Width;
            height = info.Height;
            return new Resource(Uuid7.CreateVersion7(), format?.DefaultMimeType ?? "image/png", bytes) { Width = width, Height = height };
        }

        /// <summary>
        /// Saves the document as .greenshot, or drawn as image in the format of the extension
        /// </summary>
        public static void Save(GreenshotDocument document, string path)
        {
            // Written next to the file first, so a failed save doesn't destroy the old file
            string temporary = path + ".saving";
            try
            {
                using (var stream = File.Create(temporary))
                {
                    if (IsDocument(path))
                    {
                        GreenshotPackage.Write(document, stream, ExtensionFor);
                    }
                    else
                    {
                        string extension = Path.GetExtension(path);
                        var encoder = Encoders.TryGetValue(extension, out var create) ? create() : new PngEncoder();
                        using var image = DocumentRenderer.Render(document);
                        if (encoder is JpegEncoder or BmpEncoder)
                        {
                            // No transparency: on white, like the old editor
                            image.Mutate(context => context.BackgroundColor(Color.White));
                        }

                        image.Save(stream, encoder);
                    }
                }

                if (File.Exists(path))
                {
                    File.Replace(temporary, path, null);
                }
                else
                {
                    File.Move(temporary, path);
                }
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        /// <summary>
        /// The extension a resource of the media type gets in a package
        /// </summary>
        public static string ExtensionFor(string mediaType) => mediaType != null && MediaTypeExtensions.TryGetValue(mediaType, out var extension) ? extension : "bin";

        /// <summary>
        /// The document drawn as PNG, e.g. for the clipboard
        /// </summary>
        public static byte[] ToPng(GreenshotDocument document)
        {
            using var image = DocumentRenderer.Render(document);
            using var stream = new MemoryStream();
            image.Save(stream, new PngEncoder());
            return stream.ToArray();
        }

        /// <summary>
        /// The document drawn, as BGRA pixels (Bgra32 of WPF, not premultiplied) with its size
        /// </summary>
        public static (byte[] Pixels, int Width, int Height) ToBgra(GreenshotDocument document)
        {
            using var image = DocumentRenderer.Render(document);
            var pixels = new byte[image.Width * image.Height * 4];
            image.ProcessPixelRows(rows =>
            {
                for (int y = 0; y < rows.Height; y++)
                {
                    var row = rows.GetRowSpan(y);
                    int offset = y * row.Length * 4;
                    for (int x = 0; x < row.Length; x++, offset += 4)
                    {
                        var pixel = row[x];
                        pixels[offset] = pixel.B;
                        pixels[offset + 1] = pixel.G;
                        pixels[offset + 2] = pixel.R;
                        pixels[offset + 3] = pixel.A;
                    }
                }
            });
            return (pixels, image.Width, image.Height);
        }

        /// <summary>
        /// The file name for a document of the title, without characters Windows doesn't allow
        /// </summary>
        public static string FileNameOf(string title)
        {
            var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
            var name = new string((title ?? string.Empty).Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
            return name.Length == 0 ? "capture" : name;
        }
    }
}
