#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Windows.Forms;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using Dapplo.Windows.SafeHandles;
using log4net;

#endregion

namespace GreenshotPlugin.Controls
{
	/// <summary>
	///     Extend this Form to have the possibility for animations on your form
	/// </summary>
	public class AnimatingForm : GreenshotForm
	{
		private const int DEFAULT_VREFRESH = 60;
		private static readonly ILog Log = LogManager.GetLogger(typeof(AnimatingForm));
		private Timer _timer;
		private int _vRefresh;

		/// <summary>
		///     Initialize the animation
		/// </summary>
		protected AnimatingForm()
		{
			Load += delegate
			{
				if (!EnableAnimation)
				{
					return;
				}
				_timer = new Timer
				{
					Interval = 1000 / VRefresh
				};
				_timer.Tick += timer_Tick;
				_timer.Start();
			};

			// Unregister at close
			FormClosing += delegate { _timer?.Stop(); };
		}

		/// <summary>
		///     This flag specifies if any animation is used
		/// </summary>
		protected bool EnableAnimation { get; set; }

		/// <summary>
		///     Vertical Refresh Rate
		/// </summary>
		protected int VRefresh
		{
			get
			{
				if (_vRefresh == 0)
				{
					// get te hDC of the desktop to get the VREFRESH
					using (var desktopHandle = SafeWindowDcHandle.FromDesktop())
					{
						_vRefresh = Gdi32.GetDeviceCaps(desktopHandle, DeviceCaps.VREFRESH);
					}
				}
				// A vertical refresh rate value of 0 or 1 represents the display hardware's default refresh rate.
				// As there is currently no know way to get the default, we guess it.
				if (_vRefresh <= 1)
				{
					_vRefresh = DEFAULT_VREFRESH;
				}
				return _vRefresh;
			}
		}

		/// <summary>
		///     Check if we are in a Terminal Server session OR need to optimize for RDP / remote desktop connections
		/// </summary>
		protected bool IsTerminalServerSession => !coreConfiguration.DisableRDPOptimizing && (coreConfiguration.OptimizeForRDP || SystemInformation.TerminalServerSession);

		/// <summary>
		///     Calculate the amount of frames that an animation takes
		/// </summary>
		/// <param name="milliseconds"></param>
		/// <returns>Number of frames, 1 if in Terminal Server Session</returns>
		protected int FramesForMillis(int milliseconds)
		{
			// If we are in a Terminal Server Session we return 1
			if (IsTerminalServerSession)
			{
				return 1;
			}
			return milliseconds / VRefresh;
		}

		/// <summary>
		///     The tick handler initiates the animation.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void timer_Tick(object sender, EventArgs e)
		{
			try
			{
				Animate();
			}
			catch (Exception ex)
			{
				Log.Warn("An exception occured while animating:", ex);
			}
		}

		/// <summary>
		///     This method will be called every frame, so implement your animation/redraw logic here.
		/// </summary>
		protected virtual void Animate()
		{
			throw new NotImplementedException();
		}
	}
}