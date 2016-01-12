/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GreenshotPlugin.Extensions;

namespace Greenshot.Windows
{
	/// <summary>
	/// This class helps to show the flash after taking a capture
	/// </summary>
	public partial class FlashlightWindow : Window
	{
		private FlashlightWindow(Rect area = default(Rect))
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

		/// <summary>
		/// This closes the window as soon as the storyboard is ready animating
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Storyboard_Completed(object sender, EventArgs e)
		{
			Dispatcher.Invoke(Close);
		}

		/// <summary>
		/// Show a flashing window on the specified location
		/// </summary>
		/// <param name="area"></param>
		/// <param name="token"></param>
		/// <returns>Task to wait for</returns>
		public static async Task Flash(Rect area = default(Rect), CancellationToken token = default(CancellationToken))
		{
			var flashlightWindow = new FlashlightWindow(area);
			flashlightWindow.Show();
			await flashlightWindow.WaitForClosedAsync(token);
		}
	}
}
