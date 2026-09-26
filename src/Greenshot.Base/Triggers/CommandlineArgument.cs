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

namespace Greenshot.Base.Triggers
{
    /// <summary>
    /// Explicit argument declaration for a CommandlineTrigger.
    /// Documents expected input parameters, their mapping to flow context variables,
    /// whether they are mandatory, and default fallback values.
    /// </summary>
    public class CommandlineArgument
    {
        /// <summary>
        /// The CLI argument name passed by the user (e.g. "file", "format", "lang").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The flow context property name where this argument's value is stored (e.g. "Filename", "Format").
        /// If omitted or empty, defaults to Name.
        /// </summary>
        public string Variable { get; set; }

        /// <summary>
        /// Human-readable explanation of what this argument does and accepts.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether this argument is mandatory. When true, execution fails if not provided and no default exists.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Default value assigned if the user does not provide this argument.
        /// </summary>
        public string DefaultValue { get; set; }
    }
}
