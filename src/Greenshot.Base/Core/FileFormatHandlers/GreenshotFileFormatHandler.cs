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
using System.Drawing;
using System.IO;
using System.Linq;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Base.Core.FileFormatHandlers
{
    public class GreenshotFileFormatHandler : IFileFormatHandler
    {
        private static readonly string[] SupportedExtensions = { "greenshot" };

        public bool CanDoActionForExtension(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            if (fileFormatHandlerAction == FileFormatHandlerActions.LoadDrawableFromStream)
            {
                return false;
            }
            return SupportedExtensions.Contains(extension);
        }

        public void SaveToStream(Bitmap bitmap, Stream destination, string extension)
        {
            throw new NotImplementedException();
        }

        public Bitmap Load(Stream stream, string extension)
        {
            var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
            return (Bitmap)surface.GetImageForExport();
        }

        public IDrawableContainer LoadDrawableFromStream(Stream stream, string extension, ISurface parent)
        {
            throw new NotImplementedException();
        }
    }
}
