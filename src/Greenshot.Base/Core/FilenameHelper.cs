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

using System.IO;

namespace Greenshot.Base.Core
{
    public static class FilenameHelper
    {
        // File names and folders from templates: see Greenshot.Base.Expressions.OutputTemplates
        private const string UnsafeReplacement = "_";

        /// <summary>
        /// Remove invalid characters from the fully qualified filename
        /// </summary>
        /// <param name="fullPath">string with the full path to a file</param>
        /// <returns>string with the full path to a file, without invalid characters</returns>
        public static string MakeFqFilenameSafe(string fullPath)
        {
            string path = MakePathSafe(Path.GetDirectoryName(fullPath));
            string filename = MakeFilenameSafe(Path.GetFileName(fullPath));
            // Make the fullpath again and return
            return Path.Combine(path, filename);
        }

        /// <summary>
        /// Remove invalid characters from the filename
        /// </summary>
        /// <param name="filename">string with the full path to a file</param>
        /// <returns>string with the full path to a file, without invalid characters</returns>
        public static string MakeFilenameSafe(string filename)
        {
            // Make the filename save!
            if (filename != null)
            {
                foreach (char disallowed in Path.GetInvalidFileNameChars())
                {
                    filename = filename.Replace(disallowed.ToString(), UnsafeReplacement);
                }
            }

            return filename;
        }

        /// <summary>
        /// Remove invalid characters from the path
        /// </summary>
        /// <param name="path">string with the full path to a file</param>
        /// <returns>string with the full path to a file, without invalid characters</returns>
        public static string MakePathSafe(string path)
        {
            // Make the path save!
            if (path != null)
            {
                foreach (char disallowed in Path.GetInvalidPathChars())
                {
                    path = path.Replace(disallowed.ToString(), UnsafeReplacement);
                }
            }

            return path;
        }

        /// <summary>
        /// Checks whether a directory name is valid in the current file system
        /// </summary>
        /// <param name="directoryName">directory name (not path!)</param>
        /// <returns>true if directory name is valid</returns>
        public static bool IsDirectoryNameValid(string directoryName)
        {
            var forbiddenChars = Path.GetInvalidPathChars();
            foreach (var forbiddenChar in forbiddenChars)
            {
                if (directoryName == null || directoryName.Contains(forbiddenChar.ToString()))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether a filename is valid in the current file system
        /// </summary>
        /// <param name="filename">name of the file</param>
        /// <returns>true if filename is valid</returns>
        public static bool IsFilenameValid(string filename)
        {
            var forbiddenChars = Path.GetInvalidFileNameChars();
            foreach (var forbiddenChar in forbiddenChars)
            {
                if (filename == null || filename.Contains(forbiddenChar.ToString()))
                {
                    return false;
                }
            }

            return true;
        }
    }
}