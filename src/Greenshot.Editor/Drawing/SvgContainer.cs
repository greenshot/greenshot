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
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Svg;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// This provides a resizable SVG container, redrawing the SVG in the size the container takes.
    /// </summary>
    [Serializable]
    public class SvgContainer : VectorGraphicsContainer
    {
        private MemoryStream _svgContent;

        [NonSerialized]
        private SvgDocument _svgDocument;

        public SvgContainer(Stream stream, ISurface parent) : base(parent)
        {
            _svgContent = new MemoryStream();
            stream.CopyTo(_svgContent);
            Init();
            Size = new Size((int)_svgDocument.Width, (int)_svgDocument.Height);
        }

        protected override void Init()
        {
            base.Init();
            // Do nothing when there is no content
            if (_svgContent == null)
            {
                return;
            }
            _svgContent.Position = 0;

            _svgDocument = SvgDocument.Open<SvgDocument>(_svgContent);
        }

        protected override Image ComputeBitmap()
        {
            //var image = ImageHelper.CreateEmpty(Width, Height, PixelFormat.Format32bppArgb, Color.Transparent);

            var image = _svgDocument.Draw(Width, Height);
            
            if (RotationAngle == 0) return image;

            var newImage = image.Rotate(RotationAngle);
            image.Dispose();
            return newImage;
        }

        public override bool HasDefaultSize => true;

        public override NativeSize DefaultSize => new NativeSize((int)_svgDocument.Width, (int)_svgDocument.Height);
    }
}