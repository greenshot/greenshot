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
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Encapsulates the visual and text payload of a capture (raw bitmap, surface, extracted OCR text, metadata).
    /// Kept separate from pipeline flow control (CaptureFlowContext) to allow future data structure refactorings.
    /// </summary>
    public interface ICapturePayload : IDisposable
    {
        /// <summary>
        /// Raw capture object holding bitmap, cursor, screen bounds and native details.
        /// </summary>
        ICapture RawCapture { get; set; }

        /// <summary>
        /// Surface holding drawable elements, layers, and annotations.
        /// </summary>
        ISurface Surface { get; set; }

        /// <summary>
        /// Text content extracted via OCR or text selection, if applicable.
        /// </summary>
        string ExtractedText { get; set; }

        /// <summary>
        /// Pre-rendered bitmap cache shared across destinations to avoid redundant composite rendering passes.
        /// </summary>
        Image SharedRenderedBitmap { get; set; }

        /// <summary>
        /// Whether the surface should be kept alive for the editor instead of being disposed when pipeline finishes.
        /// </summary>
        bool RetainSurfaceForEditor { get; set; }

        /// <summary>
        /// Arbitrary payload metadata (e.g. source window details, DPI, timestamp).
        /// </summary>
        IDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Ensures a Surface is instantiated from the RawCapture if not already present.
        /// </summary>
        ISurface EnsureSurface();
    }
}
