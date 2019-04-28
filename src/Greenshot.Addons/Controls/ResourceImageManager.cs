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

using System;
using System.Collections.Generic;
using System.Linq;
using Greenshot.Addons.Resources;
using Greenshot.Gfx;

namespace Greenshot.Addons.Controls
{
	/// <summary>
	/// Wrap a ComponentResourceManager for images
	/// </summary>
	public class ResourceImageManager : IDisposable
	{
        private readonly Type _resourceType;
		private readonly List<IBitmapWithNativeSupport> _images = new List<IBitmapWithNativeSupport>();

        /// <summary>
        /// A constructor where one specifies the type which contains the resources
        /// </summary>
        /// <param name="resourceType">Type</param>
		public ResourceImageManager(Type resourceType)
		{
            _resourceType = resourceType;
        }

		/// <summary>
		/// Get icons for displaying
		/// </summary>
		/// <param name="imageName">string with the name</param>
		/// <returns>Bitmap</returns>
		public IBitmapWithNativeSupport GetIcon(string imageName)
		{
			var bitmap = GreenshotResources.Instance.GetBitmap(imageName, _resourceType);
			var result = bitmap.ScaleIconForDisplaying(96);
			if (Equals(bitmap, result))
			{
				return bitmap;
			}
			_images.Add(result);
			return result;
		}

		private void ReleaseUnmanagedResources()
		{
			foreach (var bitmap in _images.ToList())
			{
				_images.Remove(bitmap);
				bitmap.Dispose();
			}
		}

        /// <inheritdoc />
		public void Dispose()
		{
			ReleaseUnmanagedResources();
		}

        /// <summary>
        /// Destructor
        /// </summary>
		~ResourceImageManager()
		{
			ReleaseUnmanagedResources();
		}
	}
}
