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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Wpf;
using Greenshot.Editor.Configuration;
using Greenshot.Helpers;

namespace Greenshot.Forms.Wpf
{
    /// <summary>
    /// ViewModel for the WPF Settings Window
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(SettingsViewModel));
        private bool _expertModeEnabled;
        private bool _autoStartEnabled;
        private bool _pickerSelected;
        private string _selectedLanguage;
        private int _iconSize;
        private PluginItem _selectedPlugin;

        public SettingsViewModel()
        {
            CoreConfiguration = IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());
            EditorConfiguration = IniConfigHelper.EnsureSection<IEditorConfiguration>(() => new EditorConfigurationImpl());
            _expertModeEnabled = !CoreConfiguration.HideExpertSettings;
            _autoStartEnabled = StartupHelper.HasRunUser() || StartupHelper.HasRunAll();
            
            // Initialize language
            _selectedLanguage = Language.CurrentLanguage;
            
            // Initialize icon size
            _iconSize = CoreConfiguration.IconSize.Width;
            
            // Initialize image formats
            InitializeImageFormats();
            
            // Initialize window capture modes
            InitializeWindowCaptureModes();
            
            // Initialize destinations
            InitializeDestinations();
            
            // Initialize plugins
            InitializePlugins();

            // Initialize clipboard formats
            InitializeClipboardFormats();

            // Initialize plugin controls collection
            PluginControls = new ObservableCollection<UIElement>();

            ThemeManager.Instance.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ThemeManager.IsDarkTheme))
                {
                    OnPropertyChanged(nameof(ThemeToggleIcon));
                    OnPropertyChanged(nameof(ThemeToggleToolTip));
                }
            };
        }

        public ICoreConfiguration CoreConfiguration { get; }
        
        public IEditorConfiguration EditorConfiguration { get; }
        
        public ObservableCollection<UIElement> PluginControls { get; }

        public ObservableCollection<PluginItem> Plugins { get; private set; }

        public PluginItem SelectedPlugin
        {
            get => _selectedPlugin;
            set
            {
                if (_selectedPlugin != value)
                {
                    _selectedPlugin = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanConfigureSelectedPlugin));
                    OnPropertyChanged(nameof(SelectedPluginControl));
                    OnPropertyChanged(nameof(HasSelectedPluginControl));
                    OnPropertyChanged(nameof(SelectedPluginControlVisibility));
                    OnPropertyChanged(nameof(NoSelectedPluginControlVisibility));
                }
            }
        }

        public UIElement SelectedPluginControl => SelectedPlugin?.GetConfigurationControl();
        public bool HasSelectedPluginControl => SelectedPluginControl != null;
        public Visibility SelectedPluginControlVisibility => HasSelectedPluginControl ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NoSelectedPluginControlVisibility => HasSelectedPluginControl ? Visibility.Collapsed : Visibility.Visible;

        public void SelectPluginByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || Plugins == null) return;
            var item = Plugins.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                SelectedPlugin = item;
            }
        }

        public bool CanConfigureSelectedPlugin => SelectedPlugin?.IsConfigurable == true;

        public void ConfigureSelectedPlugin()
        {
            if (CanConfigureSelectedPlugin)
            {
                SelectedPlugin?.Plugin.Configure();
            }
        }

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

        public ObservableCollection<ClipboardFormatItem> ClipboardFormats { get; private set; }

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
            Greenshot.UI.WpfThemeHelper.IsDarkMode = ThemeManager.Instance.IsDarkTheme;
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

        public List<WindowCaptureModeItem> WindowCaptureModes { get; private set; }

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

        private void InitializeWindowCaptureModes()
        {
            WindowCaptureModes = new List<WindowCaptureModeItem>();
            foreach (WindowCaptureMode mode in System.Enum.GetValues(typeof(WindowCaptureMode)))
            {
                WindowCaptureModes.Add(new WindowCaptureModeItem
                {
                    Value = mode,
                    Description = Language.Translate(mode)
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
                    _pickerSelected = CoreConfiguration.OutputDestinations != null && CoreConfiguration.OutputDestinations.Contains(destination.Designation);
                    continue;
                }

                ImageSource iconSource = null;
                try
                {
                    var displayIcon = destination.DisplayIcon;
                    if (displayIcon != null)
                    {
                        iconSource = displayIcon.ToBitmapSource();
                    }
                }
                catch
                {
                    // Some plugins may fail to resolve icons if their config section is not initialized
                }

                string description = destination.Designation;
                try
                {
                    description = destination.Description ?? destination.Designation;
                }
                catch
                {
                    // Fallback to designation
                }

                var destItem = new DestinationItem
                {
                    Destination = destination,
                    Description = description,
                    IconSource = iconSource,
                    IsSelected = CoreConfiguration.OutputDestinations != null && CoreConfiguration.OutputDestinations.Contains(destination.Designation)
                };
                
                Destinations.Add(destItem);
            }
        }

        private void InitializePlugins()
        {
            Plugins = new ObservableCollection<PluginItem>();
            try
            {
                var plugins = SimpleServiceProvider.Current.GetAllInstances<IGreenshotPlugin>();
                if (plugins != null)
                {
                    foreach (var plugin in plugins)
                    {
                        var assembly = plugin.GetType().Assembly;
                        var company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;
                        var version = assembly.GetName().Version?.ToString() ?? string.Empty;
                        var location = assembly.Location ?? string.Empty;

                        Plugins.Add(new PluginItem
                        {
                            Plugin = plugin,
                            Name = plugin.Name,
                            Version = version,
                            Company = company,
                            Location = location
                        });
                    }
                }

                if (Plugins.Count > 0)
                {
                    SelectedPlugin = Plugins.FirstOrDefault();
                }
            }
            catch
            {
                // In some test scenarios SimpleServiceProvider might not have plugins registered
            }
        }

        private void InitializeClipboardFormats()
        {
            ClipboardFormats = new ObservableCollection<ClipboardFormatItem>();
            var currentFormats = CoreConfiguration.ClipboardFormats ?? new List<ClipboardFormat>();
            foreach (ClipboardFormat format in System.Enum.GetValues(typeof(ClipboardFormat)))
            {
                ClipboardFormats.Add(new ClipboardFormatItem
                {
                    Format = format,
                    Name = Language.Translate(format),
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

    public class ImageFormatItem
    {
        public OutputFormat Value { get; set; }
        public string Description { get; set; }
    }

    public class WindowCaptureModeItem
    {
        public WindowCaptureMode Value { get; set; }
        public string Description { get; set; }
    }

    public class DestinationItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public IDestination Destination { get; set; }
        public string Description { get; set; }
        public ImageSource IconSource { get; set; }

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

    public class PluginItem
    {
        public IGreenshotPlugin Plugin { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Company { get; set; }
        public string Location { get; set; }
        public bool IsConfigurable => Plugin?.IsConfigurable == true;

        private UIElement _configControl;
        private bool _controlCreated;

        public UIElement GetConfigurationControl()
        {
            if (!_controlCreated)
            {
                _controlCreated = true;
                _configControl = Plugin?.CreateConfigurationControl();
            }
            return _configControl;
        }
    }

    public class ClipboardFormatItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public ClipboardFormat Format { get; set; }
        public string Name { get; set; }

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
