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

namespace Greenshot.Base.Recipes.Pipeline
{
    /// <summary>
    /// What the flow runner does when a recipe is started while a flow of the same recipe is still running.
    /// </summary>
    public enum FlowConcurrency
    {
        /// <summary>
        /// Run both flows (default).
        /// </summary>
        Parallel,

        /// <summary>
        /// Ignore the new start while a flow of the recipe runs.
        /// </summary>
        Exclusive,

        /// <summary>
        /// Cancel the running flow(s) of the recipe and start the new one.
        /// </summary>
        ReplacePrevious
    }
}
