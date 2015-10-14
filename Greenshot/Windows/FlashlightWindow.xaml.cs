/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows;

namespace Greenshot.Windows
{
	/// <summary>
	/// This shows the flash after taking a capture
	/// </summary>
	public partial class FlashlightWindow : Window
	{
		public FlashlightWindow(Rect area = default(Rect))
		{
			InitializeComponent();
			if (area.IsEmpty || area == default(Rect))
			{
				area = new Rect(
					new Point(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop),
					new Size(SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight));
			}
			Width = area.Width;
			Height = area.Height;
			Top = area.Top;
			Left = area.Left;
		}

		private void Storyboard_Completed(object sender, EventArgs e)
		{
			Dispatcher.Invoke(Close);
		}

		public static void Flash(Rect area = default(Rect))
		{
			Forms.MainForm.Instance.BeginInvoke(
			  new Action(() => {
				  new FlashlightWindow(area).Show();
			  }
			));
		}
	}
}
