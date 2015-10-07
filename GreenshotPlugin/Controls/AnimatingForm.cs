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
using GreenshotPlugin.UnmanagedHelpers;
using log4net;
using System.Threading.Tasks;
using System.Threading;
using System.Timers;

namespace GreenshotPlugin.Controls
{
	/// <summary>
	/// Extend this Form to have the possibility for animations on your form
	/// </summary>
	public class AnimatingForm : GreenshotForm
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (AnimatingForm));
		private const int DEFAULT_VREFRESH = 60;
		private int vRefresh = 0;
		private System.Timers.Timer timer = null;

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
				if (vRefresh == 0)
				{
					// get te hDC of the desktop to get the VREFRESH
					using (var desktopHandle = SafeWindowDCHandle.fromDesktop())
					{
						vRefresh = GDI32.GetDeviceCaps(desktopHandle, DeviceCaps.VREFRESH);
					}
				}
				// A vertical refresh rate value of 0 or 1 represents the display hardware's default refresh rate.
				// As there is currently no know way to get the default, we guess it.
				if (vRefresh <= 1)
				{
					vRefresh = DEFAULT_VREFRESH;
				}
				return vRefresh;
			}
		}

		/// <summary>
		/// Check if we are in a Terminal Server session OR need to optimize for RDP / remote desktop connections
		/// </summary>
		protected bool isTerminalServerSession
		{
			get
			{
				return coreConfiguration.OptimizeForRDP || System.Windows.Forms.SystemInformation.TerminalServerSession;
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
			if (isTerminalServerSession)
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
					timer = new System.Timers.Timer();
					timer.Interval = 1000/VRefresh;
					timer.Elapsed += timer_Tick;
					timer.SynchronizingObject = this;
					timer.Start();
				}
			};

			// Unregister at close
			FormClosing += delegate
			{
				if (timer != null)
				{
					timer.Stop();
					timer.Dispose();
				}
			};
		}

		/// <summary>
		/// The tick handler initiates the animation.
		/// </summary>
		/// <param name="sender"></param>
		private async void timer_Tick(object sender, ElapsedEventArgs e)
		{
			try
			{
				await Animate();
			}
			catch (Exception ex)
			{
				LOG.Warn("An exception occured while animating:", ex);
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