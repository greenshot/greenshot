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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Editor.FileFormatHandlers
{
    public class GreenshotFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private readonly List<string> _ourExtensions = new() { ".greenshot" };
        public GreenshotFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _ourExtensions;
        }

        public override bool TrySaveToStream(Bitmap bitmap, Stream stream, string extension, ISurface surface = null)
        {
            bitmap.Save(stream, ImageFormat.Png);
            using MemoryStream tmpStream = new MemoryStream();
            long bytesWritten = surface.SaveElementsToStream(tmpStream);
            using BinaryWriter writer = new BinaryWriter(tmpStream);
            writer.Write(bytesWritten);
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            byte[] marker = Encoding.ASCII.GetBytes($"Greenshot{v.Major:00}.{v.Minor:00}");
            writer.Write(marker);
            tmpStream.WriteTo(stream);
            return true;
        }

        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
            bitmap = (Bitmap)surface.GetImageForExport();
            return true;
        }
    }
}
