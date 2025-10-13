/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2025 Thomas Braun, Jens Klingen, Robin Krom
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

namespace Greenshot.Base.Interfaces.Plugin
{
    /// <summary>
    /// Attribute to specify a custom plugin identifier at assembly level
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class AssemblyPluginIdentifierAttribute : Attribute
    {
        /// <summary>
        /// The identifier used for the plugin in configuration
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Constructor for the plugin identifier attribute
        /// </summary>
        /// <param name="identifier">The identifier for the plugin in configuration</param>
        public AssemblyPluginIdentifierAttribute(string identifier)
        {
            Identifier = identifier;
        }
    }
}