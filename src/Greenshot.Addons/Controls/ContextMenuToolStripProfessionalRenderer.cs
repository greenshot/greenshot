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

using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addons.Config.Impl;
using Greenshot.Addons.Core;
using Greenshot.Gfx;

#endregion

namespace Greenshot.Addons.Controls
{
	/// <summary>
	///     ToolStripProfessionalRenderer which draws the Check correctly when the icons are larger
	/// </summary>
	public class ContextMenuToolStripProfessionalRenderer : ToolStripProfessionalRenderer
	{
	    private readonly ICoreConfiguration _coreConfiguration;

        public ContextMenuToolStripProfessionalRenderer(ICoreConfiguration coreConfiguration)
        {
            _coreConfiguration = coreConfiguration;
        }

        private Image _scaledCheckbox;
		private bool _newImage;
		protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
		{
			if (_scaledCheckbox == null || (NativeSize)_scaledCheckbox.Size != _coreConfiguration.IconSize)
			{
				if (_newImage)
				{
					_scaledCheckbox?.Dispose();
				}
				var checkbox = ((Bitmap) e.Image);
				_scaledCheckbox = checkbox.ScaleIconForDisplaying(96);
				_newImage = !Equals(checkbox, _scaledCheckbox);
			}
			var old = e.ImageRectangle;
			var clone = new ToolStripItemImageRenderEventArgs(e.Graphics, e.Item, _scaledCheckbox, new Rectangle(old.X, 0, old.Width, old.Height));
			base.OnRenderItemCheck(clone);
		}
	}
}