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

using Caliburn.Micro;
using Greenshot.Addon.ExternalCommand.Configuration;
using Greenshot.Addon.ExternalCommand.Entities;

namespace Greenshot.Addon.ExternalCommand.ViewModels
{
    /// <summary>
    /// This is used to display the external command settings
    /// </summary>
    public class ExternalCommandDetailsViewModel : Screen
    {
        /// <summary>
        /// The definition to show the details of or edit
        /// </summary>
        public ExternalCommandDefinition Definition { get; }

        /// <summary>
        /// The translations
        /// </summary>
        public IExternalCommandLanguage ExternalCommandLanguage { get; }

        public ExternalCommandDetailsViewModel(ExternalCommandDefinition definition, IExternalCommandLanguage externalCommandLanguage)
        {
            Definition = definition;
            ExternalCommandLanguage = externalCommandLanguage;
        }
    }
}
