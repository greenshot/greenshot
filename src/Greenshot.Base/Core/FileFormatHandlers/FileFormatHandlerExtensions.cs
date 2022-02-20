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
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Base.Core.FileFormatHandlers
{
    /// <summary>
    /// This is the registry where all IFileFormatHandler are registered and can be used
    /// </summary>
    public static class FileFormatHandlerExtensions
    {
        /// <summary>
        /// Make sure we handle the input extension always the same, by "normalizing" it
        /// </summary>
        /// <param name="extension">string</param>
        /// <returns>string</returns>
        public static  string NormalizeExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            extension = extension.ToLowerInvariant();
            return !extension.StartsWith(".") ? $".{extension}" : extension;
        }

        /// <summary>
        /// Return the extensions that the provided IFileFormatHandlers can accept for the specified action
        /// </summary>
        /// <param name="fileFormatHandlers">IEnumerable{IFileFormatHandler}</param>
        /// <param name="fileFormatHandlerAction"></param>
        /// <returns></returns>
        public static IEnumerable<string> ExtensionsFor(this IEnumerable<IFileFormatHandler> fileFormatHandlers, FileFormatHandlerActions fileFormatHandlerAction)
        {
            return fileFormatHandlers.Where(ffh => ffh.SupportedExtensions.ContainsKey(fileFormatHandlerAction)).SelectMany(ffh => ffh.SupportedExtensions[fileFormatHandlerAction]).Distinct().OrderBy(e => e);
        }

        /// <summary>
        /// Extension method to check if a certain IFileFormatHandler supports a certain action with a specific extension
        /// </summary>
        /// <param name="fileFormatHandler">IFileFormatHandler</param>
        /// <param name="fileFormatHandlerAction">FileFormatHandlerActions</param>
        /// <param name="extension">string</param>
        /// <returns>bool</returns>
        public static bool Supports(this IFileFormatHandler fileFormatHandler, FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            extension = NormalizeExtension(extension);
            return fileFormatHandler.SupportedExtensions.ContainsKey(fileFormatHandlerAction) && fileFormatHandler.SupportedExtensions[fileFormatHandlerAction].Contains(extension);
        }

        /// <summary>
        /// This wrapper method for TrySaveToStream will do:
        /// Find all the IFileFormatHandler which support the action for the supplied extension.
        /// Take the first, to call the TrySaveToStream on.
        /// </summary>
        /// <param name="fileFormatHandlers">IEnumerable{IFileFormatHandler}</param>
        /// <param name="bitmap">Bitmap</param>
        /// <param name="destination">Stream</param>
        /// <param name="extension">string</param>
        /// <param name="surface">ISurface</param>
        /// <returns>bool</returns>
        public static bool TrySaveToStream(this IEnumerable<IFileFormatHandler> fileFormatHandlers, Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            extension = NormalizeExtension(extension);

            var saveFileFormatHandlers = fileFormatHandlers
                .Where(ffh => ffh.Supports(FileFormatHandlerActions.LoadFromStream, extension))
                .OrderBy(ffh => ffh.PriorityFor(FileFormatHandlerActions.LoadFromStream, extension)).ToList();

            if (!saveFileFormatHandlers.Any())
            {
                return false;
            }

            foreach (var fileFormatHandler in saveFileFormatHandlers)
            {
                if (fileFormatHandler.TrySaveToStream(bitmap, destination, extension, surface))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try to load a drawable container from the stream
        /// </summary>
        /// <param name="fileFormatHandlers">IEnumerable{IFileFormatHandler}</param>
        /// <param name="stream">Stream</param>
        /// <param name="extension">string</param>
        /// <param name="parentSurface">ISurface</param>
        /// <returns>IEnumerable{IDrawableContainer}</returns>
        public static IEnumerable<IDrawableContainer> LoadDrawablesFromStream(this IEnumerable<IFileFormatHandler> fileFormatHandlers, Stream stream, string extension, ISurface parentSurface = null)
        {
            extension = NormalizeExtension(extension);

            var loadfileFormatHandler = fileFormatHandlers
                .Where(ffh => ffh.Supports(FileFormatHandlerActions.LoadDrawableFromStream, extension))
                .OrderBy(ffh => ffh.PriorityFor(FileFormatHandlerActions.LoadDrawableFromStream, extension))
                .FirstOrDefault();

            if (loadfileFormatHandler != null)
            {
                return loadfileFormatHandler.LoadDrawablesFromStream(stream, extension, parentSurface);
            }

            return Enumerable.Empty<IDrawableContainer>();
        }

        /// <summary>
        /// Try to load a Bitmap from the stream
        /// </summary>
        /// <param name="fileFormatHandlers">IEnumerable{IFileFormatHandler}</param>
        /// <param name="stream">Stream</param>
        /// <param name="extension">string</param>
        /// <param name="bitmap">Bitmap out</param>
        /// <returns>bool true if it was successful</returns>
        public static bool TryLoadFromStream(this IEnumerable<IFileFormatHandler> fileFormatHandlers, Stream stream, string extension, out Bitmap bitmap)
        {
            extension = NormalizeExtension(extension);

            var loadFileFormatHandler = fileFormatHandlers
                .Where(ffh => ffh.Supports(FileFormatHandlerActions.LoadFromStream, extension))
                .OrderBy(ffh => ffh.PriorityFor(FileFormatHandlerActions.LoadFromStream, extension))
                .FirstOrDefault();

            if (loadFileFormatHandler == null)
            {
                bitmap = null;
                return false;
            }

            return loadFileFormatHandler.TryLoadFromStream(stream, extension, out bitmap);
        }
    }
}
