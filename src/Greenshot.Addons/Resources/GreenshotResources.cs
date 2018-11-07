#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System.ComponentModel;
using System.Drawing;
using System.Windows.Media.Imaging;
using Dapplo.Addons;
using Dapplo.Windows.Icons;
using Greenshot.Gfx;

#endregion

namespace Greenshot.Addons.Resources
{
	/// <summary>
	///     Centralized storage of the icons & bitmaps
	/// </summary>
	public class GreenshotResources
	{
		private static readonly ComponentResourceManager greenshotResources = new ComponentResourceManager(typeof(GreenshotResources));
	    private readonly IResourceProvider _resourceProvider;
        // TODO: Remove all usages
	    public static GreenshotResources Instance;

        public GreenshotResources(IResourceProvider resourceProvider)
        {
            _resourceProvider = resourceProvider;
            Instance = this;
        }

	    public BitmapSource GreenshotIconAsBitmapSource()
	    {
            using (var icon = GetGreenshotIcon())
            {
                return icon.ToBitmapSource();
            }
        }

	    public Icon GetGreenshotIcon()
	    {
	        return GetIcon("Greenshot");
	    }

	    public Icon GetIcon(string name)
	    {
	        using (var iconStream = _resourceProvider.ResourceAsStream(GetType().Assembly, "Resources", $"{name}.Icon.ico"))
	        {
	            return new Icon(iconStream);
	        }
	    }

        public Bitmap GetGreenshotImage()
		{
		    return GetBitmap("Greenshot.Image.png");
		}

	    public Bitmap GetBitmap(string name)
	    {
	        if (name.EndsWith(".Image"))
	        {
	            name = name + ".png";

	        }
	        using (var pngStream = _resourceProvider.ResourceAsStream(GetType().Assembly, "Resources",  name))
	        {
	            return BitmapHelper.FromStream(pngStream);
	        }
	    }
    }
}