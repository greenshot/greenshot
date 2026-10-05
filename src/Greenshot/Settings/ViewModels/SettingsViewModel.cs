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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Wpf;
using Greenshot.Editor.Configuration;
using Greenshot.Helpers;
using Greenshot.Base.Threading;
using Greenshot.Base.Languages;
using Dapplo.Ini.Internationalization;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// ViewModel for the WPF Settings Window
    /// </summary>
    public partial class SettingsViewModel : INotifyPropertyChanged
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(SettingsViewModel));
        private bool _expertModeEnabled;
        private bool _autoStartEnabled;
        private bool _pickerSelected;
        private string _selectedLanguage;
        private int _iconSize;

        public SettingsViewModel()
        {
            CoreConfiguration = IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());
            EditorConfiguration = IniConfigHelper.EnsureSection<IEditorConfiguration>(() => new EditorConfigurationImpl());
            _expertModeEnabled = !CoreConfiguration.HideExpertSettings;
            _autoStartEnabled = StartupHelper.HasRunUser() || StartupHelper.HasRunAll();
            
            // Initialize language
            _selectedLanguage = Texts.Config.CurrentLanguage;
            
            // Initialize icon size
            _iconSize = CoreConfiguration.IconSize.Width;
            
            // Initialize image formats
            InitializeImageFormats();
            
            // Initialize window capture modes
            
            // Initialize destinations
            InitializeDestinations();
            
            // The options of the recipes (Greenshot Light: those of the built-in recipes)
            InitializeRecipeOptions();

#if !GREENSHOT_LIGHT
            // Plugins and AI tools: Greenshot Light has neither
            InitializePlugins();
            InitializeAiTools();
#endif

            // The memory dial follows the memory settings
            InitializeMemoryProfile();

            // Initialize clipboard formats
            InitializeClipboardFormats();

            ThemeManager.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ThemeManager.IsDarkTheme))
                {
                    OnPropertyChanged(nameof(ThemeToggleIcon));
                    OnPropertyChanged(nameof(ThemeToggleToolTip));
                }
                else if (e.PropertyName == nameof(ThemeManager.Theme))
                {
                    OnPropertyChanged(nameof(SelectedTheme));
                }
            };
        }

        public ICoreConfiguration CoreConfiguration { get; }

        public IEditorConfiguration EditorConfiguration { get; }
        
        public bool PrintColor
        {
            get => !CoreConfiguration.OutputPrintGrayscale && !CoreConfiguration.OutputPrintMonochrome;
            set
            {
                if (value)
                {
                    CoreConfiguration.OutputPrintGrayscale = false;
                    CoreConfiguration.OutputPrintMonochrome = false;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PrintGrayscale));
                    OnPropertyChanged(nameof(PrintMonochrome));
                }
            }
        }

        public bool PrintGrayscale
        {
            get => CoreConfiguration.OutputPrintGrayscale;
            set
            {
                if (value)
                {
                    CoreConfiguration.OutputPrintGrayscale = true;
                    CoreConfiguration.OutputPrintMonochrome = false;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PrintColor));
                    OnPropertyChanged(nameof(PrintMonochrome));
                }
            }
        }

        public bool PrintMonochrome
        {
            get => CoreConfiguration.OutputPrintMonochrome;
            set
            {
                if (value)
                {
                    CoreConfiguration.OutputPrintGrayscale = false;
                    CoreConfiguration.OutputPrintMonochrome = true;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PrintColor));
                    OnPropertyChanged(nameof(PrintGrayscale));
                }
            }
        }

        public ObservableCollection<ClipboardFormatViewModel> ClipboardFormats { get; private set; }

        private static ImageSource _greenshotIconSource;

        public static ImageSource GetGreenshotIconSource()
        {
            if (_greenshotIconSource != null)
            {
                return _greenshotIconSource;
            }

            try
            {
                using (var icon = GreenshotResources.GetGreenshotIcon())
                {
                    if (icon != null)
                    {
                        _greenshotIconSource = Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        _greenshotIconSource.Freeze();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to load Greenshot icon source", ex);
            }

            return _greenshotIconSource;
        }

        public ImageSource WindowIcon => GetGreenshotIconSource();

        public bool IsExpertTabVisible => !CoreConfiguration.HideExpertSettings;

        public string ThemeToggleIcon => ThemeManager.Instance.IsDarkTheme ? "☀️" : "🌙";
        public string ThemeToggleToolTip => ThemeManager.Instance.IsDarkTheme ? "Switch to Light Mode" : "Switch to Dark Mode";

        public void ToggleTheme()
        {
            ThemeManager.Instance.ToggleTheme();
            OnPropertyChanged(nameof(ThemeToggleIcon));
            OnPropertyChanged(nameof(ThemeToggleToolTip));
        }

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

        /// <summary>
        /// The languages with a language pack, for the language picker
        /// </summary>
        public IList<LanguageInfo> SupportedLanguages => Texts.Config.GetLanguages().Where(l => l.HasBaseFile).ToList();

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    // The closest available language is used and stored in the configuration
                    _selectedLanguage = Texts.SetLanguage(value);
                    InitializeImageFormats();
                    OnPropertyChanged(nameof(ImageFormats));
                    OnPropertyChanged();
                }
            }
        }

        public List<ImageFormatViewModel> ImageFormats { get; private set; }

        public int IconSize
        {
            get => _iconSize;
            set
            {
                if (_iconSize != value && value >= 16 && value <= 256)
                {
                    _iconSize = value;
                    CoreConfiguration.IconSize = new NativeSize(value, value);
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<DestinationViewModel> Destinations { get; private set; }

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
            ImageFormats = new List<ImageFormatViewModel>();
            var registry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
            if (registry == null)
            {
                return;
            }

            foreach (var format in registry.GetSaveableFileFormats())
            {
                ImageFormats.Add(new ImageFormatViewModel
                {
                    Value = format.Id,
                    Description = format.GetDisplayNameWithPreferredExtension(),
                    DisplayNameWithPreferredExtension = format.GetDisplayNameWithPreferredExtension()
                });
            }

            // Ensure the current output file format is included in the list, even if it's not registered
            if (!ImageFormats.Any(item => string.Equals(item.Value, CoreConfiguration.OutputFileFormat, StringComparison.OrdinalIgnoreCase)))
            {
                ImageFormats.Add(new ImageFormatViewModel
                {
                    Value = CoreConfiguration.OutputFileFormat,
                    Description = CoreConfiguration.OutputFileFormat,
                    DisplayNameWithPreferredExtension = CoreConfiguration.OutputFileFormat
                });
            }
        }

        private void InitializeDestinations()
        {
            Destinations = new ObservableCollection<DestinationViewModel>();
            
            var allDestinations = DestinationHelper.GetAllDestinations().ToList();
            foreach (IDestination destination in allDestinations)
            {
                // Skip picker - it's handled separately
                if (nameof(WellKnownDestinations.Picker).Equals(destination.Designation))
                {
                    _pickerSelected = CoreConfiguration.OutputDestinations != null && CoreConfiguration.OutputDestinations.Contains(destination.Designation);
                    continue;
                }

                string description = destination.Designation;
                try
                {
                    description = destination.Descriptor?.DisplayName ?? destination.Designation;
                }
                catch
                {
                    // Fallback to designation
                }

                var destItem = new DestinationViewModel
                {
                    Destination = destination,
                    Description = description,
                    IsSelected = CoreConfiguration.OutputDestinations != null && CoreConfiguration.OutputDestinations.Contains(destination.Designation)
                };
                
                Destinations.Add(destItem);
            }

            // Resolve the destination icons asynchronously, the window opens right away
            LoadDestinationIconsAsync().FireAndLog("Load the destination icons");
        }

        /// <summary>
        /// Started on the UI thread, the icons are set there (continuations return to the UI thread)
        /// </summary>
        private async Task LoadDestinationIconsAsync()
        {
            foreach (var destItem in Destinations.ToList())
            {
                try
                {
                    destItem.IconSource = await DestinationIcons.GetImageSourceAsync(destItem.Destination?.Descriptor?.IconKey).ConfigureAwait(true);
                }
                catch (Exception)
                {
                    // Some plugins may fail to resolve icons if their config section is not initialized
                }
            }
        }

        private void InitializeClipboardFormats()
        {
            ClipboardFormats = new ObservableCollection<ClipboardFormatViewModel>();
            var currentFormats = CoreConfiguration.ClipboardFormats ?? new List<ClipboardFormat>();
            foreach (ClipboardFormat format in System.Enum.GetValues(typeof(ClipboardFormat)))
            {
                ClipboardFormats.Add(new ClipboardFormatViewModel
                {
                    Format = format,
                    Name = Texts.Translate(format),
                    IsSelected = currentFormats.Contains(format)
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
