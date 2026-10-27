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

#nullable disable

using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Editor;
using SkiaSharp;

namespace Greenshot.Linux.Recipes.Pipeline
{
    /// <summary>
    /// What flows through a recipe: the capture, the editor surface once a step draws on it, the recognized text and
    /// metadata the steps share. The same as Greenshot.Base, with the Linux capture and editor surface.
    /// </summary>
    public interface ICapturePayload : IDisposable
    {
        /// <summary>
        /// The capture as it was acquired (cropped by the selection)
        /// </summary>
        Capture RawCapture { get; set; }

        /// <summary>
        /// The editor surface, made by <see cref="EnsureSurface"/> when a step adds annotations
        /// </summary>
        EditorSurface Surface { get; set; }

        string ExtractedText { get; set; }

        /// <summary>
        /// The image the destinations get: the surface with its annotations, or the capture; owned by the payload
        /// </summary>
        Task<Capture> GetExportSourceAsync(IUiDispatcher ui, CancellationToken cancellationToken = default);

        /// <summary>
        /// The image changed, the next export renders it again
        /// </summary>
        void InvalidateExportSource();

        /// <summary>
        /// The editor took the surface, disposing the payload must not dispose it
        /// </summary>
        bool RetainSurfaceForEditor { get; set; }

        IDictionary<string, object> Metadata { get; }

        EditorSurface EnsureSurface();

        /// <summary>
        /// The current image: of the surface when there is one, else of the capture
        /// </summary>
        SKBitmap Image { get; }

        CaptureDetails Details { get; }

        /// <summary>
        /// A copy for a branch of the flow, which can change it without changing this one
        /// </summary>
        ICapturePayload Clone();
    }
}
