/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Configuration;
using Greenshot.Destinations;

namespace Greenshot.Forms.Wpf
{
    /// <summary>
    /// ViewModel for the WPF Settings Window
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private bool _expertModeEnabled;
        private bool _autoStartEnabled;
        private bool _pickerSelected;
        private string _selectedLanguage;

        public SettingsViewModel()
        {
            CoreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();
            _expertModeEnabled = !CoreConfiguration.HideExpertSettings;
            // AutoStart is handled separately from INI config
            _autoStartEnabled = false;
            
            // Initialize language
            _selectedLanguage = Language.CurrentLanguage;
            
            // Initialize image formats
            InitializeImageFormats();
            
            // Initialize destinations
            InitializeDestinations();
        }

        public CoreConfiguration CoreConfiguration { get; }

        public bool ExpertModeEnabled
        {
            get => _expertModeEnabled;
            set
            {
                if (_expertModeEnabled != value)
                {
                    _expertModeEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AutoStartEnabled
        {
            get => _autoStartEnabled;
            set
            {
                if (_autoStartEnabled != value)
                {
                    _autoStartEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public IList<LanguageFile> SupportedLanguages => Language.SupportedLanguages;

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    Language.CurrentLanguage = value;
                    CoreConfiguration.Language = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<ImageFormatItem> ImageFormats { get; private set; }

        public ObservableCollection<DestinationItem> Destinations { get; private set; }

        public bool PickerSelected
        {
            get => _pickerSelected;
            set
            {
                if (_pickerSelected != value)
                {
                    _pickerSelected = value;
                    OnPropertyChanged();
                    // When picker is selected, deselect all destinations
                    if (value)
                    {
                        foreach (var dest in Destinations)
                        {
                            dest.IsSelected = false;
                        }
                    }
                }
            }
        }

        private void InitializeImageFormats()
        {
            ImageFormats = new List<ImageFormatItem>();
            foreach (OutputFormat format in System.Enum.GetValues(typeof(OutputFormat)))
            {
                ImageFormats.Add(new ImageFormatItem
                {
                    Value = format,
                    Description = Language.Translate(format)
                });
            }
        }

        private void InitializeDestinations()
        {
            Destinations = new ObservableCollection<DestinationItem>();
            
            foreach (IDestination destination in DestinationHelper.GetAllDestinations())
            {
                // Skip picker - it's handled separately
                if (nameof(WellKnownDestinations.Picker).Equals(destination.Designation))
                {
                    _pickerSelected = CoreConfiguration.OutputDestinations.Contains(destination.Designation);
                    continue;
                }

                var destItem = new DestinationItem
                {
                    Destination = destination,
                    Description = destination.Description,
                    IsSelected = CoreConfiguration.OutputDestinations.Contains(destination.Designation)
                };
                
                Destinations.Add(destItem);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ImageFormatItem
    {
        public OutputFormat Value { get; set; }
        public string Description { get; set; }
    }

    public class DestinationItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public IDestination Destination { get; set; }
        public string Description { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
