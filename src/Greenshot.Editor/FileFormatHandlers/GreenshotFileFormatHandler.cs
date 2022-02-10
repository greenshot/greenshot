﻿/*
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
using System.IO;
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Editor.FileFormatHandlers
{
    public class GreenshotFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        protected override string[] OurExtensions { get; } = { ".greenshot" };

        /// <inheritdoc />
        public override IEnumerable<string> SupportedExtensions(FileFormatHandlerActions fileFormatHandlerAction)
        {
            if (fileFormatHandlerAction == FileFormatHandlerActions.LoadDrawableFromStream)
            {
                return Enumerable.Empty<string>();
            }

            return OurExtensions;
        }

        /// <inheritdoc />
        public override bool Supports(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            if (fileFormatHandlerAction == FileFormatHandlerActions.SaveToStream)
            {
                return false;
            }

            return OurExtensions.Contains(NormalizeExtension(extension));
        }

        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension)
        {
            throw new NotImplementedException();
        }

        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            var surface = SimpleServiceProvider.Current.GetInstance<Func<ISurface>>().Invoke();
            bitmap = (Bitmap)surface.GetImageForExport();
            return true;
        }

    }
}
