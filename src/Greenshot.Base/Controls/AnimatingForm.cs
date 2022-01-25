/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Windows.Forms;
using log4net;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// Extend this Form to have the possibility for animations on your form
    /// </summary>
    public class AnimatingForm : GreenshotForm
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AnimatingForm));
        private const int DEFAULT_VREFRESH = 60;
        private Timer _timer;

        /// <summary>
        /// This flag specifies if any animation is used
        /// </summary>
        protected bool EnableAnimation { get; set; }

        /// <summary>
        /// Vertical Refresh Rate
        /// </summary>
        protected int VRefresh => DEFAULT_VREFRESH;

        /// <summary>
        /// Check if we are in a Terminal Server session OR need to optimize for RDP / remote desktop connections
        /// </summary>
        protected bool IsTerminalServerSession => !coreConfiguration.DisableRDPOptimizing && (coreConfiguration.OptimizeForRDP || SystemInformation.TerminalServerSession);

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

            return milliseconds / VRefresh;
        }

        /// <summary>
        /// Calculate the interval for the timer to animate the frames
        /// </summary>
        /// <returns>Milliseconds for the interval</returns>
        protected int Interval() => (int)1000 / VRefresh;

        /// <summary>
        /// Initialize the animation
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
                    Interval = Interval()
                };
                _timer.Tick += Timer_Tick;
                _timer.Start();
            };

            // Un-register at close
            FormClosing += delegate { _timer?.Stop(); };
        }

        /// <summary>
        /// The tick handler initiates the animation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!Visible)
            {
                return;
            }
            try
            {
                Animate();
            }
            catch (Exception ex)
            {
                Log.Warn("An exception occurred while animating:", ex);
            }
        }

        /// <summary>
        /// This method will be called every frame, so implement your animation/redraw logic here.
        /// </summary>
        protected virtual void Animate()
        {
            throw new NotImplementedException();
        }
    }
}