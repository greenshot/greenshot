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
using System.IO;
using System.Linq;
using System.Reflection;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Images, icons and other binary data which are embedded as plain files (manifest resources) in an assembly.
    /// The manifest resource name of a resource which belongs to a type is the full name of the type, a dot and the resource name,
    /// e.g. "Greenshot.Editor.Forms.ImageEditorForm.btnSave.Image" (the .csproj sets this as LogicalName).
    /// These are not stored in .resx files, as non string .resx entries would need System.Resources.Extensions.
    /// </summary>
    public static class EmbeddedResources
    {
        /// <summary>
        /// The manifest resource name of the resource with the specified name, which belongs to the type
        /// </summary>
        /// <param name="owner">Type to which the resource belongs</param>
        /// <param name="name">string with the name of the resource, e.g. "btnSave.Image"</param>
        /// <returns>string</returns>
        public static string ManifestName(Type owner, string name)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            return owner.FullName + "." + name;
        }

        /// <summary>
        /// The names of all resources which belong to the type. The compiled .resx of the type (e.g. "...ImageEditorForm.resources",
        /// with the strings of the form) is not one of them.
        /// </summary>
        /// <param name="owner">Type to which the resources belong</param>
        /// <returns>IEnumerable with the names, which can be passed to the other methods</returns>
        public static IEnumerable<string> GetNames(Type owner)
        {
            string prefix = ManifestName(owner, string.Empty);
            return owner.Assembly.GetManifestResourceNames()
                .Where(manifestName => manifestName.StartsWith(prefix, StringComparison.Ordinal)
                                       && !manifestName.EndsWith(".resources", StringComparison.Ordinal))
                .Select(manifestName => manifestName.Substring(prefix.Length));
        }

        /// <summary>
        /// The content of a resource which belongs to the type
        /// </summary>
        /// <param name="owner">Type to which the resource belongs</param>
        /// <param name="name">string with the name of the resource</param>
        /// <returns>byte array, null if there is no such resource</returns>
        public static byte[] GetBytes(Type owner, string name)
        {
            return GetBytes(owner?.Assembly, ManifestName(owner, name));
        }

        /// <summary>
        /// The content of a manifest resource
        /// </summary>
        /// <param name="assembly">Assembly which contains the resource</param>
        /// <param name="manifestName">string with the full manifest resource name</param>
        /// <returns>byte array, null if there is no such resource</returns>
        public static byte[] GetBytes(Assembly assembly, string manifestName)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            using var stream = assembly.GetManifestResourceStream(manifestName);
            if (stream == null)
            {
                return null;
            }

            var bytes = new byte[stream.Length];
            int offset = 0;
            while (offset < bytes.Length)
            {
                int read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read <= 0)
                {
                    throw new EndOfStreamException($"Couldn't read the resource {manifestName}");
                }
                offset += read;
            }

            return bytes;
        }

        /// <summary>
        /// Create a new image from a resource which belongs to the type, the caller owns (disposes) it.
        /// </summary>
        /// <param name="owner">Type to which the resource belongs</param>
        /// <param name="name">string with the name of the resource, e.g. "btnSave.Image"</param>
        /// <returns>Image, null if there is no such resource</returns>
        public static Image GetImage(Type owner, string name)
        {
            var bytes = GetBytes(owner, name);
            if (bytes == null)
            {
                return null;
            }

            // GDI+ needs the stream as long as the image lives, this is what the resource reader did too
            return Image.FromStream(new MemoryStream(bytes));
        }

        /// <summary>
        /// Create a new icon from a resource which belongs to the type, the caller owns (disposes) it.
        /// </summary>
        /// <param name="owner">Type to which the resource belongs</param>
        /// <param name="name">string with the name of the resource, e.g. "Greenshot.Icon"</param>
        /// <returns>Icon, null if there is no such resource</returns>
        public static Icon GetIcon(Type owner, string name)
        {
            var bytes = GetBytes(owner, name);
            if (bytes == null)
            {
                return null;
            }

            // The icon copies the data, so the stream doesn't need to stay open
            using var stream = new MemoryStream(bytes);
            return new Icon(stream);
        }
    }
}
