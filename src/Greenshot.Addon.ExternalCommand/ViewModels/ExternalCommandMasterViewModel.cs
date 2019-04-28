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

using System.Linq;
using Caliburn.Micro;
using Greenshot.Addon.ExternalCommand.Configuration;
using Greenshot.Addon.ExternalCommand.Entities;

namespace Greenshot.Addon.ExternalCommand.ViewModels
{
    /// <summary>
    /// This is the master view of the external command config editor
    /// </summary>
    public class ExternalCommandMasterViewModel : Conductor<ExternalCommandDetailsViewModel>.Collection.OneActive
    {
        /// <summary>
        /// Used in the view
        /// </summary>
        public IExternalCommandConfiguration ExternalCommandConfiguration { get; }

        /// <summary>
        /// Used in the view
        /// </summary>
        public IExternalCommandLanguage ExternalCommandLanguage { get; }

        public ExternalCommandMasterViewModel(
            IExternalCommandConfiguration externalCommandConfiguration,
            IExternalCommandLanguage externalCommandLanguage)
        {
            ExternalCommandConfiguration = externalCommandConfiguration;
            ExternalCommandLanguage = externalCommandLanguage;
        }

        /// <inheritdoc />
        protected override void OnActivate()
        {
            Items.Clear();
            var items = ExternalCommandConfiguration.Commands
                .Select(ExternalCommandConfiguration.Read)
                .OrderBy(definition => definition.Name)
                .Select(definition => new ExternalCommandDetailsViewModel(definition, ExternalCommandLanguage));

            Items.AddRange(items);

            base.OnActivate();
        }

        /// <inheritdoc />
        protected override void OnDeactivate(bool close)
        {
            Items.Clear();
            base.OnDeactivate(close);
        }

        /// <summary>
        /// This adds an item
        /// </summary>
        public void Add()
        {
            Items.Add(new ExternalCommandDetailsViewModel(new ExternalCommandDefinition
            {
                Name = "New command " + Items.Count
            }, ExternalCommandLanguage));
        }

        /// <summary>
        /// This stores all items
        /// </summary>
        public void Store()
        {
            foreach (var item in Items)
            {
                // Remove before
                ExternalCommandConfiguration.Delete(item.Definition.Name);
                if (item.Definition?.IsValid != true)
                {
                    continue;
                }
                ExternalCommandConfiguration.Write(item.Definition);
            }
        }
    }
}
