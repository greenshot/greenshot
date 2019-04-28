// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Windows.Forms;
using Dapplo.Config.Language;
using Dapplo.Log;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.SafeHandles;
using Greenshot.Addons.Core;

namespace Greenshot.Addons.Controls
{
	/// <summary>
	///     Extend this Form to have the possibility for animations on your form
	/// </summary>
	public class AnimatingForm : GreenshotForm
	{
        /// <summary>
        /// The ICoreConfiguration which can be used in all derived forms
        /// </summary>
	    protected readonly ICoreConfiguration _coreConfiguration;
	    private const int DefaultVerticalRefresh = 60;
		private static readonly LogSource Log = new LogSource();
		private Timer _timer;
		private int _vRefresh;

        /// <summary>
        ///     Initialize the animation
        /// </summary>
        protected AnimatingForm(ICoreConfiguration coreConfiguration, ILanguage language) : base(language)
		{
		    _coreConfiguration = coreConfiguration;
		    Load += (sender, args) =>
            {
                DoubleBuffered = true;
                if (!EnableAnimation)
                {
                    return;
                }

                _timer = new Timer
                {
                    Interval = 1000 / VRefresh
                };
                _timer.Tick += TimerTick;
                _timer.Start();
            };

			// Unregister at close
			FormClosing += (sender, args) => _timer?.Stop();
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
						_vRefresh = Gdi32Api.GetDeviceCaps(desktopHandle, DeviceCaps.VREFRESH);
					}
				}
				// A vertical refresh rate value of 0 or 1 represents the display hardware's default refresh rate.
				// As there is currently no know way to get the default, we guess it.
				if (_vRefresh <= 1)
				{
					_vRefresh = DefaultVerticalRefresh;
				}
				return _vRefresh;
			}
		}

		/// <summary>
		///     Check if we are in a Terminal Server session OR need to optimize for RDP / remote desktop connections
		/// </summary>
		protected bool IsTerminalServerSession => !_coreConfiguration.DisableRDPOptimizing && (_coreConfiguration.OptimizeForRDP || SystemInformation.TerminalServerSession);

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
		private void TimerTick(object sender, EventArgs e)
		{
			try
			{
				Animate();
			}
			catch (Exception ex)
			{
				Log.Warn().WriteLine(ex, "An exception occured while animating:");
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