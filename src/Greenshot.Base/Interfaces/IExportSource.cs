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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Core;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// A rendered image borrowed from an <see cref="IExportSource"/> (rule R8): don't dispose or change the image, dispose the lease.
    /// </summary>
    public interface IImageLease : IDisposable
    {
        Image Image { get; }
    }

    /// <summary>
    /// What destinations export (roadmap section 5.1): one per flow (or editor export), owned by its creator, disposed when that ends.
    /// Rendering and encoding are cached per output settings, so several destinations share the work.
    /// </summary>
    public interface IExportSource : IDisposable
    {
        /// <summary>
        /// True when the capture was changed (e.g. in the editor) since it was loaded or saved: a file of the capture can't be reused.
        /// </summary>
        bool IsModified { get; }

        /// <summary>
        /// The capture rendered with the output settings (effects, color reduction). A lease: don't dispose the image.
        /// </summary>
        Task<IImageLease> RenderAsync(SurfaceOutputSettings settings, CancellationToken cancellationToken);

        /// <summary>
        /// The capture encoded in the format of the output settings.
        /// </summary>
        Task<EncodedImage> EncodeAsync(SurfaceOutputSettings settings, CancellationToken cancellationToken);

        /// <summary>
        /// Bridge until the imaging roadmap's AnnotationDocument exists: run code with the surface (a WinForms control) on the UI thread.
        /// Only for what really needs the surface: handing it to the editor, the .greenshot format, the elements.
        /// </summary>
        Task<T> UseSurfaceAsync<T>(Func<ISurface, T> use, CancellationToken cancellationToken);
    }

    /// <summary>
    /// An encoded image; <see cref="OpenRead"/> returns an independent stream per call, so parallel destinations can read it.
    /// </summary>
    public sealed class EncodedImage
    {
        private readonly byte[] _bytes;

        public EncodedImage(byte[] bytes, string format)
        {
            _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
            Format = format ?? throw new ArgumentNullException(nameof(format));
        }

        /// <summary>
        /// The id of the file format (see <see cref="IFileFormatRegistry"/>), e.g. png
        /// </summary>
        public string Format { get; }

        private FileFormatDefinition Definition =>
            SimpleServiceProvider.Current?.GetInstance<IFileFormatRegistry>(true) is { } registry && registry.TryGet(Format, out var definition) ? definition : null;

        /// <summary>
        /// MIME type from the file format registry, e.g. image/png
        /// </summary>
        public string MimeType => Definition?.MimeType ?? "application/octet-stream";

        /// <summary>
        /// File extension including the dot, e.g. ".png"
        /// </summary>
        public string FileExtension => "." + (Definition?.PreferredExtension ?? Format);

        public ReadOnlyMemory<byte> Bytes => _bytes;

        public int Length => _bytes.Length;

        /// <summary>
        /// A new read-only stream over the bytes.
        /// </summary>
        public Stream OpenRead() => new MemoryStream(_bytes, false);

        /// <summary>
        /// Copy of the bytes, for APIs which need an array.
        /// </summary>
        public byte[] ToArray() => (byte[])_bytes.Clone();
    }
}
