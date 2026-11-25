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
using System.Collections.Generic;
using System.Linq;
using Dapplo.Ini;
using Greenshot.Base.Core;

namespace Greenshot.Base.Recipes.Pipeline
{
    /// <summary>
    /// A step which hands the capture to destinations of its own, e.g. the Box upload or an external command. The flow
    /// doesn't run the step when one of them is switched off (Settings > Destinations) or blocked by the administrator,
    /// like the Destinations step leaves out such a destination.
    /// </summary>
    public interface IUsesDestinations
    {
        /// <summary>
        /// The designations of the destinations the step uses, e.g. "Box" or "External MS Paint"; none for a step which
        /// doesn't use one with these parameters (an external command which is given in the recipe itself)
        /// </summary>
        IEnumerable<string> UsedDestinations { get; }
    }

    /// <summary>
    /// The check of <see cref="IUsesDestinations"/>
    /// </summary>
    public static class UsedDestinations
    {
        /// <summary>
        /// The first destination of the step which may not be used with the configuration of Greenshot, null when the step may run
        /// </summary>
        public static string FirstNotAllowed(object step) =>
            step is IUsesDestinations ? FirstNotAllowed(step, CoreConfiguration()) : null;

        /// <summary>
        /// The first destination of the step which may not be used, null when the step may run
        /// </summary>
        public static string FirstNotAllowed(object step, ICoreConfiguration configuration) =>
            (step as IUsesDestinations)?.UsedDestinations?
            .FirstOrDefault(designation => !string.IsNullOrEmpty(designation) && !ConfigurationRestrictions.IsDestinationAllowed(configuration, designation));

        private static ICoreConfiguration CoreConfiguration()
        {
            try
            {
                return IniConfigRegistry.GetSection<ICoreConfiguration>();
            }
            catch (Exception)
            {
                // Not loaded (tests): nothing is switched off
                return null;
            }
        }
    }
}
