/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Recipes.Views;

namespace Greenshot.Recipes.Steps
{
    /// <summary>
    /// Waits with the countdown window (it never takes the focus, so an open menu or popup stays open), used by the Delay step
    /// and by the Source step for a wait (CaptureWait) of a second or more
    /// </summary>
    internal static class CaptureCountdown
    {
        /// <summary>
        /// A wait this long or longer shows the countdown, a shorter one only lets a menu close
        /// </summary>
        public static readonly TimeSpan ShownFrom = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The countdown closes this long before the end, so it is gone from the screen before the next step captures it
        /// </summary>
        private static readonly TimeSpan GoneBeforeEnd = TimeSpan.FromMilliseconds(250);

        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Show the countdown and wait, it shows the whole seconds left and closes shortly before the end
        /// </summary>
        /// <returns>False when the user cancelled</returns>
        public static async Task<bool> WaitAsync(CaptureFlowContext context, TimeSpan wait, CancellationToken cancellationToken)
        {
            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            DelayCountdownWindow window = null;
            await context.Ui.InvokeAsync(() =>
            {
                window = new DelayCountdownWindow(SecondsLeft(wait), () => cancelled.TrySetResult(true));
                window.ShowWithoutFocus();
            }, cancellationToken).ConfigureAwait(false);

            try
            {
                var left = wait;
                while (left > GoneBeforeEnd)
                {
                    // Up to the next whole second, the last step ends when the countdown has to be gone
                    var step = TimeSpan.FromTicks(left.Ticks % OneSecond.Ticks);
                    if (step == TimeSpan.Zero) step = OneSecond;
                    if (left - step < GoneBeforeEnd) step = left - GoneBeforeEnd;
                    var tick = Task.Delay(step, cancellationToken);
                    if (await Task.WhenAny(tick, cancelled.Task).ConfigureAwait(false) == cancelled.Task)
                    {
                        return false;
                    }
                    // Throws when the flow was cancelled
                    await tick.ConfigureAwait(false);
                    left -= step;
                    int secondsLeft = SecondsLeft(left);
                    await context.Ui.InvokeAsync(() => window.SecondsLeft = secondsLeft, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                await context.Ui.InvokeAsync(() => window?.Close(), CancellationToken.None).ConfigureAwait(false);
            }

            var rest = wait < GoneBeforeEnd ? wait : GoneBeforeEnd;
            await Task.Delay(rest, cancellationToken).ConfigureAwait(false);
            return true;
        }

        /// <summary>
        /// The whole seconds shown for the time left, at least 1
        /// </summary>
        internal static int SecondsLeft(TimeSpan left) => Math.Max(1, (int)Math.Ceiling(left.TotalSeconds));
    }
}
