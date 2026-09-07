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

namespace Greenshot.Base.Interfaces.Forms
{
    /// <summary>
    /// The IImageEditor is the Interface that the Greenshot ImageEditor has to implement
    /// </summary>
    public interface IImageEditor
    {
        /// <summary>
        /// Get the current Image from the Editor for Exporting (save/upload etc)
        /// This is actually a wrapper which calls Surface.GetImageForExport().
        /// Don't forget to call image.Dispose() when finished!!!
        /// </summary>
        /// <returns>Bitmap</returns>
        Image GetImageForExport();

        /// <summary>
        /// Make the ICaptureDetails from the current Surface in the EditorForm available.
        /// </summary>
        ICaptureDetails CaptureDetails { get; }

        /// <summary>
        /// Gets or sets the surface used for image editing operations.
        /// </summary>
        /// <remarks>The surface represents the editable area where drawing, annotation, and manipulation
        /// of image content occur. Assigning a new surface replaces the current editing context.</remarks>
        ISurface Surface { get; set; }

        /// <summary>
        /// Attempts to save the current state to the specified file path.
        /// </summary>
        /// <param name="filePath">The full path of the file where the state will be saved. Cannot be null or empty.</param>
        /// <returns>true if the state was successfully saved; otherwise, false.</returns>
        bool TrySaveState(string filePath);
    }
}