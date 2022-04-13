/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// The possible actions a IFileFormatHandler might support
    /// </summary>
    public enum FileFormatHandlerActions
    {
        SaveToStream,
        LoadFromStream,
        LoadDrawableFromStream
    }

    /// <summary>
    /// This interface is for code to implement the loading and saving of certain file formats
    /// </summary>
    public interface IFileFormatHandler
    {
        /// <summary>
        /// Registry for all the extensions this IFileFormatHandler support
        /// </summary>
        IDictionary<FileFormatHandlerActions, IReadOnlyCollection<string>> SupportedExtensions { get; }

        /// <summary>
        /// Priority (from high int.MinValue, low int.MaxValue) of this IFileFormatHandler for the specified action and extension
        /// This should be used to sort the possible IFileFormatHandler
        /// </summary>
        /// <param name="fileFormatHandlerAction">FileFormatHandlerActions</param>
        /// <param name="extension">string</param>
        /// <returns>int specifying the priority for the action and extension</returns>
        public int PriorityFor(FileFormatHandlerActions fileFormatHandlerAction, string extension);

        /// <summary>
        /// Try to save the specified bitmap to the stream in the format belonging to the extension
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <param name="destination">Stream</param>
        /// <param name="extension">extension</param>
        /// <param name="surface">ISurface with the elements for those file types which can store a surface (.greenshot)</param>
        /// <param name="surfaceOutputSettings">SurfaceOutputSettings</param>
        /// <returns>bool true if it was successful</returns>
        public bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="extension"></param>
        /// <param name="bitmap"></param>
        /// <returns>bool true if it was successful</returns>
        public bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap);

        /// <summary>
        /// Try to load a drawable container from the stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="extension">string</param>
        /// <param name="parentSurface">ISurface</param>
        /// <returns>IEnumerable{IDrawableContainer}</returns>
        public IEnumerable<IDrawableContainer> LoadDrawablesFromStream(Stream stream, string extension, ISurface parentSurface = null);
    }
}
