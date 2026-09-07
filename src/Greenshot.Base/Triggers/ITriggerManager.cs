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

namespace Greenshot.Base.Triggers
{
    /// <summary>
    /// Registry managing all active triggers in the application.
    /// </summary>
    public interface ITriggerManager : IDisposable
    {
        /// <summary>
        /// Gets all registered triggers.
        /// </summary>
        IReadOnlyList<ITrigger> GetAllTriggers();

        /// <summary>
        /// Registers a new trigger and starts it if enabled.
        /// </summary>
        void RegisterTrigger(ITrigger trigger);

        /// <summary>
        /// Unregisters and stops a trigger by ID.
        /// </summary>
        bool UnregisterTrigger(string triggerId);

        /// <summary>
        /// Starts all enabled triggers.
        /// </summary>
        void StartAll();

        /// <summary>
        /// Stops all triggers.
        /// </summary>
        void StopAll();

        /// <summary>
        /// Event raised whenever any managed trigger fires.
        /// </summary>
        event EventHandler<TriggerEventArgs> TriggerFired;
    }
}
