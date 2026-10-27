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

#nullable disable

using Greenshot.Linux.Core;

using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Destinations;

namespace Greenshot.Linux.Recipes.Pipeline
{
    public enum NotificationKind
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// What the steps ask the user, like IUserInteraction of Greenshot.Base. Unattended runs (command line, AI tools)
    /// have one which throws <see cref="InteractionRequiredException"/>.
    /// </summary>
    public interface IUserInteraction
    {
        bool IsInteractive { get; }

        /// <summary>
        /// The path to save to, null when canceled
        /// </summary>
        Task<string> PickSaveFileAsync(string suggestedPath, string format, CancellationToken cancellationToken);

        /// <summary>
        /// One of the destinations (the picker), null when canceled
        /// </summary>
        Task<IDestination> PickDestinationAsync(IReadOnlyList<IDestination> choices, Capture capture, CancellationToken cancellationToken);

        /// <summary>
        /// Yes (true), No (false) or canceled (null)
        /// </summary>
        Task<bool?> ConfirmAsync(string title, string message, bool isError, CancellationToken cancellationToken);

        Task NotifyAsync(NotificationKind kind, string message, string link = null);
    }

    /// <summary>
    /// For runs without the user
    /// </summary>
    public sealed class UnattendedUserInteraction : IUserInteraction
    {
        public static UnattendedUserInteraction Instance { get; } = new UnattendedUserInteraction();

        public bool IsInteractive => false;

        public Task<string> PickSaveFileAsync(string suggestedPath, string format, CancellationToken cancellationToken) =>
            throw new InteractionRequiredException("Save as");

        public Task<IDestination> PickDestinationAsync(IReadOnlyList<IDestination> choices, Capture capture, CancellationToken cancellationToken) =>
            throw new InteractionRequiredException("Destination picker");

        public Task<bool?> ConfirmAsync(string title, string message, bool isError, CancellationToken cancellationToken) =>
            throw new InteractionRequiredException(title);

        public Task NotifyAsync(NotificationKind kind, string message, string link = null)
        {
            Log.Info($"Recipe ({kind}): {message}");
            return Task.CompletedTask;
        }
    }
}
