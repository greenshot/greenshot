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
using System.Text;
using Dapplo.Windows.Clipboard;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.FileFormat.V2;
using log4net;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Copy and paste of editor elements. The elements are placed as bytes in Greenshot's own registered clipboard format.
    /// The bytes are the UTF-8 JSON of the .gsa element list (<see cref="V2Helper.SerializeDrawableContainerList"/>).
    /// Any application can place data in that format, so on paste the bytes are untrusted: the size is limited, and only the
    /// element types known to the file format are created.
    /// </summary>
    public static class DrawableContainerClipboard
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DrawableContainerClipboard));

        /// <summary>
        /// The registered clipboard format for Greenshot editor elements
        /// </summary>
        public const string Format = "Greenshot.DrawableContainerList";

        /// <summary>
        /// Elements larger than this are not pasted
        /// </summary>
        public const long MaxSize = 256L * 1024 * 1024;

        /// <summary>
        /// True when the clipboard has Greenshot elements, the clipboard isn't opened for this
        /// </summary>
        public static bool IsAvailable => ClipboardNative.HasFormat(Format);

        /// <summary>
        /// Serialize the elements into bytes
        /// </summary>
        /// <param name="elements">IDrawableContainerList</param>
        /// <returns>byte[]</returns>
        public static byte[] Serialize(IDrawableContainerList elements)
        {
            if (elements == null)
            {
                throw new ArgumentNullException(nameof(elements));
            }

            var drawableContainerList = elements as DrawableContainerList ?? new DrawableContainerList(elements);
            return Encoding.UTF8.GetBytes(V2Helper.SerializeDrawableContainerList(drawableContainerList));
        }

        /// <summary>
        /// Deserialize elements from untrusted bytes, only allowed types are created
        /// </summary>
        /// <param name="data">byte[]</param>
        /// <returns>IDrawableContainerList or null when the data isn't valid</returns>
        public static IDrawableContainerList Deserialize(byte[] data)
        {
            if (data == null || data.Length == 0 || data.Length > MaxSize)
            {
                return null;
            }

            try
            {
                return V2Helper.DeserializeDrawableContainerList(Encoding.UTF8.GetString(data));
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't read the Greenshot elements from the clipboard.", ex);
                return null;
            }
        }

        /// <summary>
        /// Place the elements on the clipboard
        /// </summary>
        /// <param name="elements">IDrawableContainerList</param>
        /// <exception cref="ClipboardException">when the clipboard is in use</exception>
        public static void Copy(IDrawableContainerList elements)
        {
            var contents = new ClipboardContents().AddBytes(Serialize(elements), Format);
            ClipboardHelper.SetClipboardData(contents);
        }

        /// <summary>
        /// Read the elements from a snapshot which contains <see cref="Format"/>
        /// </summary>
        /// <param name="source">IClipboardDataSource</param>
        /// <returns>IDrawableContainerList or null</returns>
        public static IDrawableContainerList Read(IClipboardDataSource source)
        {
            if (source == null || !source.TryGetAsBytes(Format, out var data))
            {
                return null;
            }

            return Deserialize(data);
        }
    }
}
