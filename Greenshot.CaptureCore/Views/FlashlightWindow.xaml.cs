//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Threading.Tasks;
using System.Windows;
using Dapplo.Utils;
using Greenshot.Core.Extensions;

#endregion

namespace Greenshot.CaptureCore.Views
{
	/// <summary>
	///     This class helps to show the flash after taking a capture
	/// </summary>
	public partial class FlashlightWindow : Window
	{
		private FlashlightWindow(Rect area = default(Rect))
		{
			InitializeComponent();
			if (area.IsEmpty || (area == default(Rect)))
			{
				area = new Rect(
					new Point(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop),
					new Size(SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight));
			}
			else
			{
				// Scale device coordinates (from the capture form) to WPF
				area = Rect.Transform(area, this.TransformFromDevice());
			}
			Width = area.Width;
			Height = area.Height;
			Top = area.Top;
			Left = area.Left;
		}

		/// <summary>
		///     Show a flashing window on the specified location
		/// </summary>
		/// <param name="area"></param>
		/// <returns>Task to wait for</returns>
		public static Task Flash(Rect area = default(Rect))
		{
			return UiContext.RunOn(async () =>
			{
				var flashlightWindow = new FlashlightWindow(area);
				flashlightWindow.Show();
				await flashlightWindow.WaitForClosedAsync();
			});
		}

		/// <summary>
		///     This closes the window as soon as the storyboard is ready animating
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Storyboard_Completed(object sender, EventArgs e)
		{
			Dispatcher.Invoke(Close);
		}
	}
}