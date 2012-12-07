/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using GreenshotPlugin.Core;
using GreenshotPlugin.UnmanagedHelpers;
using Greenshot.IniFile;

namespace GreenshotPlugin.Controls {
	/// <summary>
	/// Extend this Form to have the possibility for animations on your form
	/// </summary>
	public abstract class AnimatingForm : Form {
		protected static CoreConfiguration coreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();
		private int vRefresh = 0;
		private Timer timer = null;

		/// <summary>
		/// This flag specifies if any animation is used
		/// </summary>
		protected bool EnableAnimation {
			get;
			set;
		}

		/// <summary>
		/// Vertical Refresh Rate
		/// </summary>
		protected int VRefresh {
			get {
				if (vRefresh == 0) {
					// get te hDC of the desktop to get the VREFRESH
					IntPtr hDCDesktop = User32.GetWindowDC(User32.GetDesktopWindow());
					vRefresh = GDI32.GetDeviceCaps(hDCDesktop, DeviceCaps.VREFRESH);
					User32.ReleaseDC(hDCDesktop);
				}
				return vRefresh;
			}
		}

		/// <summary>
		/// Check if we need to optimize for RDP / Terminal Server sessions
		/// </summary>
		protected bool OptimizeForTerminalServer {
			get {
				return coreConfiguration.OptimizeForRDP || SystemInformation.TerminalServerSession;
			}
		}

		/// <summary>
		/// Calculate the amount of frames that an animation takes
		/// </summary>
		/// <param name="milliseconds"></param>
		/// <returns>Number of frames, 1 if in Terminal Server Session</returns>
		protected int CalculateFrames(int milliseconds) {
			// If we are in a Terminal Server Session we return 1
			if (OptimizeForTerminalServer) {
				return 1;
			}
			return milliseconds / VRefresh;
		}

		/// <summary>
		/// Initialize the animation
		/// </summary>
		protected AnimatingForm() {
			this.Load += delegate {
				if (EnableAnimation) {
					timer = new Timer();
					timer.Interval = 1000 / VRefresh;
					timer.Tick += new EventHandler(timer_Tick);
					timer.Start();
				}
			};

			// Unregister at close
			this.FormClosing += delegate {
				if (timer != null) {
					timer.Stop();
				}
			};
		}

		/// <summary>
		/// The tick handler initiates the animation.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void timer_Tick(object sender, EventArgs e) {
			Animate();
		}

		/// <summary>
		/// This method will be called every frame, so implement your animation/redraw logic here.
		/// </summary>
		protected abstract void Animate();
	}
}
