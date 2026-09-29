/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// <see cref="IUserInteraction"/> for unattended runs (recipes started from the command line, tests): nothing is shown,
    /// settings keep their current values, anything which needs a decision of the user throws <see cref="InteractionRequiredException"/>.
    /// </summary>
    public sealed class HeadlessUserInteraction : IUserInteraction
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HeadlessUserInteraction));

        public static HeadlessUserInteraction Instance { get; } = new HeadlessUserInteraction();

        public bool IsInteractive => false;

        public Task<string> PickSaveFileAsync(SaveFileRequest request, CancellationToken cancellationToken)
        {
            throw new InteractionRequiredException("Save file dialog");
        }

        public Task<SurfaceOutputSettings> PromptOutputSettingsAsync(SurfaceOutputSettings current, CancellationToken cancellationToken)
        {
            // Keep the current settings
            return Task.FromResult(current);
        }

        public Task<IDestination> PickDestinationAsync(IReadOnlyList<IDestination> choices, CancellationToken cancellationToken)
        {
            throw new InteractionRequiredException("Destination picker");
        }

        public Task<TResult> ShowDialogAsync<TResult>(IDialogViewModel<TResult> viewModel, CancellationToken cancellationToken)
        {
            throw new InteractionRequiredException(viewModel?.GetType().Name ?? "Dialog");
        }

        public Task<T> RunWithProgressAsync<T>(string title, Func<IProgress<ProgressInfo>, CancellationToken, Task<T>> work, CancellationToken cancellationToken)
        {
            if (work == null) throw new ArgumentNullException(nameof(work));
            return work(null, cancellationToken);
        }

        public Task NotifyAsync(Notification notification)
        {
            if (notification != null)
            {
                Log.InfoFormat("Notification ({0}): {1}", notification.Kind, notification.Message);
            }

            return Task.CompletedTask;
        }

        public Task<bool?> ConfirmAsync(string title, string message, bool isError, CancellationToken cancellationToken)
        {
            Log.WarnFormat("{0}: {1}", title, message);
            return Task.FromResult<bool?>(null);
        }
    }
}
