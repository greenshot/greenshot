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

namespace Greenshot.Base.Triggers
{
    /// <summary>
    /// Represents an independent trigger entity (hotkey, clipboard watcher, UI menu, CLI)
    /// capable of initiating a capture recipe execution.
    /// </summary>
    public interface ITrigger : IDisposable
    {
        /// <summary>
        /// Unique identifier for this trigger.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Human-readable name or label.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The ID of the target CaptureRecipe to invoke when fired.
        /// </summary>
        string TargetRecipeId { get; set; }

        /// <summary>
        /// Whether this trigger is currently enabled and listening.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Activate the trigger (e.g. register OS hotkey, begin clipboard monitoring).
        /// </summary>
        void Start();

        /// <summary>
        /// Deactivate the trigger (e.g. unregister OS hotkey, stop monitoring).
        /// </summary>
        void Stop();

        /// <summary>
        /// Event fired when the trigger activates.
        /// </summary>
        event EventHandler<TriggerEventArgs> Triggered;
    }
}
