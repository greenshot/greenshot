// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace Greenshot.Addon.ExternalCommand.Entities
{
    /// <summary>
    /// This defines an external command
    /// </summary>
    public class ExternalCommandDefinition
    {
        /// <summary>
        /// Name of the command
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The command itself
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// The arguments for the command
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// The behavior or mode of the command
        /// </summary>
        public CommandBehaviors CommandBehavior { get; set; } = CommandBehaviors.Default;

        /// <summary>
        /// Validates if this command definition is valid
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(Command);
    }
}
