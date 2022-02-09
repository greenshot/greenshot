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
using System.Linq;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Base.Core.FileFormatHandlers
{
    public static class FileFormatHandlerRegistry
    {
        public static IList<IFileFormatHandler> FileFormatHandlers { get; } = new List<IFileFormatHandler>();

        public static IEnumerable<string> ExtensionsFor(FileFormatHandlerActions fileFormatHandlerAction)
        {
            return FileFormatHandlers.SelectMany(ffh => ffh.SupportedExtensions(fileFormatHandlerAction)).Distinct();
        }

        /// <summary>
        /// This wrapper method for TrySaveToStream will do:
        /// Find all the IFileFormatHandler which support the action for the supplied extension.
        /// Take the first, to call the TrySaveToStream on.
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <param name="destination">Stream</param>
        /// <param name="extension">string</param>
        /// <returns>bool</returns>
        public static bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension)
        {
            var fileFormatHandler = FileFormatHandlers
                .Where(ffh => ffh.Supports(FileFormatHandlerActions.LoadFromStream, extension))
                .OrderBy(ffh => ffh.PriorityFor(FileFormatHandlerActions.LoadFromStream, extension))
                .FirstOrDefault();

            if (fileFormatHandler == null)
            {
                return false;
            }

            return fileFormatHandler.TrySaveToStream(bitmap, destination, extension);
        }

        /// <summary>
        /// Try to load a drawable container from the stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="extension">string</param>
        /// <param name="drawableContainer">IDrawableContainer out</param>
        /// <param name="parentSurface">ISurface</param>
        /// <returns>bool true if it was successful</returns>
        public static bool TryLoadDrawableFromStream(Stream stream, string extension, out IDrawableContainer drawableContainer, ISurface parentSurface = null)
        {
            var fileFormatHandler = FileFormatHandlers
                .Where(ffh => ffh.Supports(FileFormatHandlerActions.LoadDrawableFromStream, extension))
                .OrderBy(ffh => ffh.PriorityFor(FileFormatHandlerActions.LoadDrawableFromStream, extension))
                .FirstOrDefault();

            if (fileFormatHandler == null)
            {
                drawableContainer = null;
                return false;
            }

            return fileFormatHandler.TryLoadDrawableFromStream(stream, extension, out drawableContainer, parentSurface);
        }
    }
}
