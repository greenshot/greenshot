/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Drawing;

namespace Greenshot.Controls {

	/*[ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ToolStrip | ToolStripItemDesignerAvailability.StatusStrip)]
	public class ToolStripColorButton : ToolStripControlHost
	{
		public event EventHandler ValueChanged;
		
		public ToolStripColorButton() :base(new ColorButton())
		{
		}
		
		public ColorButton ColorButton
		{
			get {return Control as ColorButton;}
		}
		
		public Color SelectedColor
        {
            get { return ColorButton.SelectedColor; }
            set { ColorButton.SelectedColor = value;}
        }
		
		protected override void OnSubscribeControlEvents(Control control)
		{
			base.OnSubscribeControlEvents(control);
			ColorButton.SelectedColorChanged += OnSelectedColorChanged;
		}
		protected override void OnUnsubscribeControlEvents(Control control)
		{
			base.OnUnsubscribeControlEvents(control);
			ColorButton.SelectedColorChanged -= OnSelectedColorChanged;
		}
		
		private void OnSelectedColorChanged(object sender, EventArgs e) 
		{
			if(ValueChanged != null) ValueChanged(sender, e);
		}
	}*/
}
