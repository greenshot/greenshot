/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
    /// Event arguments passed when an ITrigger fires.
    /// </summary>
    public class TriggerEventArgs : EventArgs
    {
        /// <summary>
        /// ID of the target recipe to execute.
        /// </summary>
        public string TargetRecipeId { get; }

        /// <summary>
        /// Optional initial parameters or contextual overrides provided by the trigger.
        /// </summary>
        public IDictionary<string, object> Parameters { get; }

        public TriggerEventArgs(string targetRecipeId, IDictionary<string, object> parameters = null)
        {
            TargetRecipeId = targetRecipeId;
            Parameters = parameters ?? new Dictionary<string, object>();
        }
    }
}
