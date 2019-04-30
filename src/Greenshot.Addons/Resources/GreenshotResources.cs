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
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using Dapplo.Addons;
using Dapplo.Windows.Icons;
using Greenshot.Gfx;

namespace Greenshot.Addons.Resources
{
	/// <summary>
	///     Centralized storage of the icons and bitmaps
	/// </summary>
	public class GreenshotResources
	{
	    private readonly IResourceProvider _resourceProvider;
        // TODO: Remove all usages
	    public static GreenshotResources Instance;

        public GreenshotResources(IResourceProvider resourceProvider)
        {
            _resourceProvider = resourceProvider;
            Instance = this;
        }

        /// <summary>
        /// Get the Greenshot icon as BitmapSource
        /// </summary>
        /// <returns>BitmapSource</returns>
	    public BitmapSource GreenshotIconAsBitmapSource()
	    {
            using (var icon = GetGreenshotIcon())
            {
                return icon.ToBitmapSource();
            }
        }

        /// <summary>
        /// Get the Greenshot logo as an Icon
        /// </summary>
        /// <returns>Icon</returns>
	    public Icon GetGreenshotIcon()
	    {
	        return GetIcon("Greenshot", GetType());
	    }

        /// <summary>
        /// Get an embedded icon
        /// </summary>
        /// <param name="name">string</param>
        /// <param name="type">Type</param>
        /// <returns>Icon</returns>
	    public Icon GetIcon(string name, Type type = null)
	    {
	        using (var iconStream = _resourceProvider.ResourceAsStream((type ?? GetType()).Assembly, "Resources", $"{name}.Icon.ico"))
	        {
	            return new Icon(iconStream);
	        }
	    }

        /// <summary>
        /// Get the Greenshot logo as a Bitmap
        /// </summary>
        /// <returns>IBitmapWithNativeSupport</returns>
        public IBitmapWithNativeSupport GetGreenshotImage()
		{
		    return GetBitmap("Greenshot.Image.png", GetType());
		}

        /// <summary>
        /// Get a bitmap from an embedded resource
        /// </summary>
        /// <param name="name">string</param>
        /// <param name="type">Type</param>
        /// <returns>IBitmapWithNativeSupport</returns>
	    public IBitmapWithNativeSupport GetBitmap(string name, Type type = null)
	    {
	        if (name.EndsWith(".Image"))
	        {
	            name += ".png";

	        }
	        using (var imageStream = _resourceProvider.ResourceAsStream((type ?? GetType()).Assembly, "Resources",  name))
	        {
	            return BitmapHelper.FromStream(imageStream);
	        }
	    }

        /// <summary>
        /// Get a byte[] from an embedded resource
        /// </summary>
        /// <param name="name">string</param>
        /// <param name="type">Type</param>
        /// <returns>bate[]</returns>
	    public byte[] GetBytes(string name, Type type = null)
        {
            using (var stream = _resourceProvider.ResourceAsStream((type ?? GetType()).Assembly, "Resources", name))
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}