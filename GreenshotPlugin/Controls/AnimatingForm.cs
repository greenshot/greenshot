/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Threading.Tasks;
using System.Threading;
using System.Timers;
using Dapplo.Windows.Enums;
using Dapplo.Windows.SafeHandles;
using Dapplo.Windows.Native;

namespace GreenshotPlugin.Controls
{
	/// <summary>
	/// Extend this Form to have the possibility for animations on your form
	/// </summary>
	public class AnimatingForm : GreenshotForm
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(AnimatingForm));
		private const int DefaultVrefresh = 60;
		private int _vRefresh;
		private System.Timers.Timer _timer;

		/// <summary>
		/// This flag specifies if any animation is used
		/// </summary>
		protected bool EnableAnimation
		{
			get;
			set;
		}

		/// <summary>
		/// Vertical Refresh Rate
		/// </summary>
		protected int VRefresh
		{
			get
			{
				if (_vRefresh == 0)
				{
					// get te hDC of the desktop to get the VREFRESH
					using (var desktopHandle = SafeWindowDCHandle.fromDesktop())
					{
						_vRefresh = Gdi32.GetDeviceCaps(desktopHandle, DeviceCaps.VREFRESH);
					}
				}
				// A vertical refresh rate value of 0 or 1 represents the display hardware's default refresh rate.
				// As there is currently no know way to get the default, we guess it.
				if (_vRefresh <= 1)
				{
					_vRefresh = DefaultVrefresh;
				}
				return _vRefresh;
			}
		}

		/// <summary>
		/// Check if we are in a Terminal Server session OR need to optimize for RDP / remote desktop connections
		/// </summary>
		protected bool IsTerminalServerSession
		{
			get
			{
				return coreConfiguration.OptimizeForRdp || System.Windows.Forms.SystemInformation.TerminalServerSession;
			}
		}

		/// <summary>
		/// Calculate the amount of frames that an animation takes
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
			return milliseconds/VRefresh;
		}

		/// <summary>
		/// Initialize the animation
		/// </summary>
		protected AnimatingForm()
		{
			Load += delegate
			{
				if (EnableAnimation)
				{
					_timer = new System.Timers.Timer
					{
						Interval = 1000d/VRefresh,
						SynchronizingObject = this
					};
					_timer.Elapsed += timer_Tick;
					_timer.Start();
				}
			};

			// Unregister at close
			FormClosing += delegate
			{
				if (_timer != null)
				{
					_timer.Stop();
					_timer.Dispose();
				}
			};
		}

		/// <summary>
		/// The tick handler initiates the animation.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void timer_Tick(object sender, ElapsedEventArgs e)
		{
			try
			{
				await Animate();
			}
			catch (Exception ex)
			{
				Log.Warning("An exception occured while animating:", ex);
			}
		}

		/// <summary>
		/// This method will be called every frame, so implement your animation/redraw logic here.
		/// </summary>
		protected virtual Task Animate(CancellationToken token = default(CancellationToken))
		{
			throw new NotImplementedException();
		}
	}
}