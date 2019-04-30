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

using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Configuration;

namespace Greenshot.Ui.Configuration.ViewModels
{
    /// <summary>
    /// The ViewModel for the DestinationPicke4 configuration
    /// </summary>
    public sealed class DestinationPickerConfigViewModel : SimpleConfigScreen
    {
        private readonly DestinationHolder _destinationHolder;

        /// <summary>
        ///     Here all disposables are registered, so we can clean the up
        /// </summary>
        private CompositeDisposable _disposables;

        /// <summary>
        /// Provides the ICoreConfiguration for the view
        /// </summary>
        public ICoreConfiguration CoreConfiguration { get; }

        /// <summary>
        /// Provides the IConfigTranslations for the view
        /// </summary>
        public IConfigTranslations ConfigTranslations { get; }

        /// <summary>
        /// Provides the IGreenshotLanguage for the view
        /// </summary>
        public IGreenshotLanguage GreenshotLanguage { get; }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="configTranslations">IConfigTranslations</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="destinationHolder">DestinationHolder</param>
        public DestinationPickerConfigViewModel(
            ICoreConfiguration coreConfiguration,
            IConfigTranslations configTranslations,
            IGreenshotLanguage greenshotLanguage,
            DestinationHolder destinationHolder
            )
        {
            _destinationHolder = destinationHolder;
            ConfigTranslations = configTranslations;
            GreenshotLanguage = greenshotLanguage;
            CoreConfiguration = coreConfiguration;
        }

        /// <inheritdoc />
        public override void Initialize(IConfig config)
        {
            // Prepare disposables
            _disposables?.Dispose();

            // Place this under the Ui parent
            ParentId = nameof(ConfigIds.InternalDestinations);

            // Make sure Commit/Rollback is called on the IUiConfiguration
            config.Register(CoreConfiguration);

            // automatically update the DisplayName
            _disposables = new CompositeDisposable
            {
                GreenshotLanguage.CreateDisplayNameBinding(this, nameof(IGreenshotLanguage.SettingsDestinationPicker))
            };

            UsedDestinations.Clear();
            if (CoreConfiguration.PickerDestinations.Any())
            {
                foreach (var outputDestination in CoreConfiguration.PickerDestinations)
                {
                    var pickerDestination = _destinationHolder.SortedActiveDestinations
                        .Where(destination => !"Picker".Equals(destination.Designation))
                        .FirstOrDefault(destination => outputDestination == destination.Designation);

                    if (pickerDestination != null)
                    {
                        UsedDestinations.Add(pickerDestination);
                    }
                }
            }
            else
            {
                foreach (var pickerDestination in _destinationHolder.SortedActiveDestinations
                    .Where(destination => !"Picker".Equals(destination.Designation)))
                {
                    UsedDestinations.Add(pickerDestination);
                }
            }
            AvailableDestinations.Clear();
            foreach (var destination in _destinationHolder.SortedActiveDestinations
                .Where(destination => !"Picker".Equals(destination.Designation))
                .Where(destination => UsedDestinations.All(pickerDestination => pickerDestination.Designation != destination.Designation)))
            {
                AvailableDestinations.Add(destination);
            }
            base.Initialize(config);
        }

        /// <inheritdoc />
        public override void Commit()
        {
            CoreConfiguration.PickerDestinations = UsedDestinations.Select(d => d.Designation).ToList();
            base.Commit();
        }

        /// <inheritdoc />
        protected override void OnDeactivate(bool close)
        {
            _disposables.Dispose();
            base.OnDeactivate(close);
        }

        /// <summary>
        /// This is the list of IDestination which are available
        /// </summary>
        public ObservableCollection<IDestination> AvailableDestinations { get; } = new ObservableCollection<IDestination>();

        /// <summary>
        /// This is the list of IDestination which are currently used
        /// </summary>
        public ObservableCollection<IDestination> UsedDestinations { get; } = new ObservableCollection<IDestination>();
        
    }
}
