/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Drawing;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// Marker interface for destinations that can accept a pre-rendered bitmap,
    /// allowing them to skip their own surface render pass when a shared rendered
    /// image is already available from another destination in the same capture pipeline.
    ///
    /// Destinations implementing this interface will be called via
    /// <see cref="ExportCaptureWithRenderedImage"/> instead of the standard
    /// <see cref="IDestination.ExportCapture"/> when a shared bitmap is available,
    /// avoiding redundant GDI+ composite passes per destination.
    ///
    /// Future candidates: EmailDestination, PrinterDestination.
    /// </summary>
    public interface IAcceptsPreRenderedImage
    {
        /// <summary>
        /// Export the capture using a pre-rendered bitmap. The destination must not
        /// dispose <paramref name="preRenderedImage"/> — lifetime is managed by the caller.
        /// </summary>
        ExportInformation ExportCaptureWithRenderedImage(Image preRenderedImage, ISurface surface, ICaptureDetails captureDetails);
    }
}
