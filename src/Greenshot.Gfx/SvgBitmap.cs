// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Svg;
using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;


namespace Greenshot.Gfx
{
	/// <summary>
	///     Create an image look like of the SVG
	/// </summary>
	public class SvgBitmap : IBitmapWithNativeSupport
	{
		private readonly SvgDocument _svgDocument;

		private Bitmap _imageClone;

        /// <inheritdoc />
        /// <param name="stream">Stream</param>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        public SvgBitmap(Stream stream, int? width = null, int? height = null) : this(SvgDocument.Open<SvgDocument>(stream), width, height)
		{
        }
        
        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <param name="svgDocument">SvgDocument</param>
        /// <param name="width">int</param>
        /// <param name="height">int</param>
        public SvgBitmap(SvgDocument svgDocument, int? width = null, int? height = null)
        {
            _svgDocument = svgDocument;
            if ((int) _svgDocument.ViewBox.Height == 0)
            {
                Height = (int)_svgDocument.Height;
            }
            else
            {
                Height = (int)_svgDocument.ViewBox.Height;
            }
            if ((int)_svgDocument.ViewBox.Width == 0)
            {
                Width = (int)_svgDocument.Width;
            }
            else
            {
                Width = (int)_svgDocument.ViewBox.Width;
            }

            if (width.HasValue)
            {
                Width = width.Value;
            }
            if (height.HasValue)
            {
                Height = height.Value;
            }

            // Generate the native bitmap
            GenerateNativeBitmap();
        }

		/// <summary>
		///     Height of the image, can be set to change
		/// </summary>
		public int Height { get; set; }

		/// <summary>
		///     Width of the image, can be set to change.
		/// </summary>
		public int Width { get; set; }

		/// <summary>
		///     PixelFormat of the underlying image
		/// </summary>
		public PixelFormat PixelFormat => _imageClone.PixelFormat;

		/// <summary>
		///     Horizontal resolution of the underlying image
		/// </summary>
		public float HorizontalResolution => _imageClone.HorizontalResolution;

		/// <summary>
		///     Vertical resolution of the underlying image
		/// </summary>
		public float VerticalResolution => _imageClone.VerticalResolution;

        /// <summary>
        ///     Underlying image, or an on demand rendered version with different attributes as the original
        /// </summary>
        public Bitmap NativeBitmap => GenerateNativeBitmap();

        /// <inheritdoc />
        public BitmapSource NativeBitmapSource
        {
            get
            {
                GenerateNativeBitmap();
                var bitmapData = _imageClone.LockBits(new Rectangle(0, 0, _imageClone.Width, _imageClone.Height), ImageLockMode.ReadOnly, _imageClone.PixelFormat);
                try
                {
                    return BitmapSource.Create(bitmapData.Width, bitmapData.Height, _imageClone.HorizontalResolution, _imageClone.VerticalResolution, PixelFormats.Bgr24, null, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
                }
                finally
                {
                    _imageClone.UnlockBits(bitmapData);
                }
            }
        }
        
        private Bitmap GenerateNativeBitmap()
        {
            if (_imageClone?.Height == Height && _imageClone?.Width == Width)
            {
                return _imageClone;
            }

            // Calculate new image clone
            _imageClone?.Dispose();
            var emptyBitmap = BitmapFactory.CreateEmpty(Width, Height, PixelFormat.Format32bppArgb, Color.Transparent, 96, 96);
            _imageClone = emptyBitmap.NativeBitmap;
            _svgDocument.Draw(_imageClone);
            return _imageClone;
        }

        /// <inheritdoc/>
        public Size Size => new Size(Width, Height);

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			_imageClone?.Dispose();
		}

        /// <summary>
        ///     Factory to create via a stream
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <param name="width">int optional</param>
        /// <param name="height">int optional</param>
        /// <returns>IBitmap</returns>
        public static IBitmapWithNativeSupport FromStream(Stream stream, int? width = null, int? height = null)
		{
			return new SvgBitmap(stream, width, height);
		}
	}
}