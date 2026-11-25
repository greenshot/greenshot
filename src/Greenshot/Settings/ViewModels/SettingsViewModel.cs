/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Wpf;
using Greenshot.Capturing.Tools;
using Greenshot.Editor.Configuration;
using Greenshot.Helpers;
using Greenshot.Base.Threading;
using Greenshot.Base.Languages;
using Dapplo.Ini.Internationalization;
using Greenshot.Viewer;

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
        private readonly UiTheme _themeAtStart;
        private readonly string _languageAtStart;

        /// <summary>
        /// The drafts of all sections the window and the plugin pages show
        /// </summary>
        public SettingsDrafts Drafts { get; } = new SettingsDrafts();

        /// <summary>
        /// The language isn't the one of the start of the window any more (it changes at once, it is no draft)
        /// </summary>
        public bool LanguageChanged => !string.IsNullOrEmpty(_languageAtStart) && !string.Equals(Texts.Config.CurrentLanguage, _languageAtStart, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Theme and language show at once in every window (they are no draft): without OK they go back to what they were
        /// </summary>
        public void RevertPreviews()
        {
            if (ThemeManager.Instance.Theme != _themeAtStart)
            {
                ThemeManager.Instance.Theme = _themeAtStart;
            }
            if (LanguageChanged)
            {
                Texts.SetLanguage(_languageAtStart);
            }
        }

        public SettingsViewModel()
        {
            // Drafts: the window changes copies, OK applies them (SettingsDrafts.Apply), Cancel drops them
            CoreConfiguration = Drafts.Get(IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl()));
            EditorConfiguration = Drafts.Get(IniConfigHelper.EnsureSection<IEditorConfiguration>(() => new EditorConfigurationImpl()));
            ViewerConfiguration = Drafts.Get(IniConfigHelper.EnsureSection<IViewerConfiguration>(() => new ViewerConfigurationImpl()));
            _themeAtStart = ThemeManager.Instance.Theme;
            _autoStartEnabled = StartupHelper.IsAutostartEnabled();
            
            // Initialize language
            _selectedLanguage = Texts.Config.CurrentLanguage;
            _languageAtStart = _selectedLanguage;
            
            // Initialize icon size
            _iconSize = CoreConfiguration.IconSize.Width;
            
            // Initialize image formats
            InitializeImageFormats();

            InitializeFixedCaptureSizes();

            // Initialize window capture modes
            
            // Initialize destinations
            InitializeDestinations();
            
            // The options of the recipes (Greenshot Light: those of the built-in recipes)
            InitializeRecipeOptions();
            InitializeHotkeys();

#if !GREENSHOT_LIGHT
            // Plugins and AI tools: Greenshot Light has neither
            InitializePlugins();
            InitializeAiTools();
#endif

            // The memory dial follows the memory settings
            InitializeMemoryProfile();

            // Initialize clipboard formats
            InitializeClipboardFormats();
            InitializeExportMetadata();

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

        public IViewerConfiguration ViewerConfiguration { get; }
        
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

        /// <summary>
        /// A released version: only there "check for unstable updates" means something, an unstable version always offers newer unstable versions
        /// </summary>
        public bool IsReleasedVersion => !UpdateService.IsUnstableBuild;

        /// <summary>
        /// A mandatory Windows policy sets at least one setting (the settings window says so), see docs/vNext/policies.md
        /// </summary>
        public bool IsManagedByPolicies =>
#if GREENSHOT_LIGHT
            false;
#else
            Greenshot.Policies.PoliciesSectionViewModel.HasMandatoryPolicies(Dapplo.Ini.IniConfigRegistry.Get().GetSections());
#endif

        public string ThemeToggleIcon => ThemeManager.Instance.IsDarkTheme ? "☀️" : "🌙";
        public string ThemeToggleToolTip => ThemeManager.Instance.IsDarkTheme ? "Switch to Light Mode" : "Switch to Dark Mode";

        public void ToggleTheme()
        {
            ThemeManager.Instance.ToggleTheme();
            OnPropertyChanged(nameof(ThemeToggleIcon));
            OnPropertyChanged(nameof(ThemeToggleToolTip));
        }

        /// <summary>
        /// "I know what I am doing": not stored, off every time the settings open, so the expert settings are locked until it is ticked
        /// </summary>
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

        /// <summary>
        /// The largest width or height of a new image in the editor
        /// </summary>
        public const int MaximumNewImageSize = 10000;

        /// <summary>
        /// The width of a new image in the editor (the "+" after the tabs, or the editor without a capture)
        /// </summary>
        public int NewImageWidth
        {
            get => EditorConfiguration.DefaultEditorSize.Width;
            set
            {
                if (value > 0 && value <= MaximumNewImageSize && value != NewImageWidth)
                {
                    EditorConfiguration.DefaultEditorSize = new NativeSize(value, NewImageHeight);
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The height of a new image in the editor
        /// </summary>
        public int NewImageHeight
        {
            get => EditorConfiguration.DefaultEditorSize.Height;
            set
            {
                if (value > 0 && value <= MaximumNewImageSize && value != NewImageHeight)
                {
                    EditorConfiguration.DefaultEditorSize = new NativeSize(NewImageWidth, value);
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The color of a new image in the editor
        /// </summary>
        public System.Drawing.Color NewImageColor
        {
            get => EditorConfiguration.NewImageColor;
            set
            {
                if (value == NewImageColor || EditorConfiguration.IsConstant(nameof(IEditorConfiguration.NewImageColor)))
                {
                    return;
                }

                EditorConfiguration.NewImageColor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NewImageBrush));
            }
        }

        public Brush NewImageBrush => new SolidColorBrush(Color.FromArgb(NewImageColor.A, NewImageColor.R, NewImageColor.G, NewImageColor.B));

        /// <summary>
        /// The chosen color of the viewer border, choosing one means the border doesn't follow the light or dark mode
        /// </summary>
        public System.Drawing.Color ViewerBorderColor
        {
            get => ViewerConfiguration.BorderColor;
            set
            {
                if (ViewerConfiguration.IsConstant(nameof(IViewerConfiguration.BorderColor)))
                {
                    return;
                }

                ViewerConfiguration.BorderColor = value;
                if (!ViewerConfiguration.IsConstant(nameof(IViewerConfiguration.BorderColorFollowsTheme)))
                {
                    ViewerConfiguration.BorderColorFollowsTheme = false;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(ViewerBorderBrush));
            }
        }

        public Brush ViewerBorderBrush => new SolidColorBrush(Color.FromArgb(ViewerBorderColor.A, ViewerBorderColor.R, ViewerBorderColor.G, ViewerBorderColor.B));

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

        /// <summary>
        /// After a capture: ask for the destination every time. The ticked destinations stay ticked, they are only saved without the picker
        /// </summary>
        public bool PickerSelected
        {
            get => _pickerSelected;
            set
            {
                if (_pickerSelected != value)
                {
                    _pickerSelected = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SendDirectly));
                    OnPropertyChanged(nameof(IsSendToEmpty));
                }
            }
        }

        /// <summary>
        /// After a capture: send it to the ticked destinations, the other choice of <see cref="PickerSelected"/>
        /// </summary>
        public bool SendDirectly
        {
            get => !PickerSelected;
            set => PickerSelected = !value;
        }

        /// <summary>
        /// Send directly with nothing ticked: a capture would go nowhere (only loading greenshot.ini puts the editor into an empty list),
        /// so OK keeps the window open. A kept destination of a plugin which isn't loaded counts, it is saved again.
        /// </summary>
        public bool IsSendToEmpty => SendDirectly
                                     && Destinations?.Any(item => item.IsSelected && item.IsAllowed) != true
                                     && KeptOutputDestinations().Count == 0;

        /// <summary>
        /// The destinations of the settings which have no entry on the tab (e.g. of a plugin which isn't loaded), saved again as they are
        /// </summary>
        public List<string> KeptOutputDestinations() =>
            CoreConfiguration.OutputDestinations?
                .Where(designation => !string.Equals(designation, nameof(WellKnownDestinations.Picker), StringComparison.OrdinalIgnoreCase)
                                      && Destinations?.All(item => !string.Equals(item.Designation, designation, StringComparison.OrdinalIgnoreCase)) != false)
                .ToList() ?? new List<string>();

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
            // From the setting itself, whether or not the picker is among the listed destinations
            _pickerSelected = CoreConfiguration.OutputDestinations?.Any(designation => string.Equals(designation?.Trim(), nameof(WellKnownDestinations.Picker), StringComparison.OrdinalIgnoreCase)) == true;
            
            var allDestinations = GetListedDestinations();
            foreach (IDestination destination in allDestinations)
            {
                // Skip picker - it's handled separately
                if (nameof(WellKnownDestinations.Picker).Equals(destination.Designation))
                {
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
                    Designation = destination.Designation,
                    Description = description,
                    // Before IsSelected: switching it off unselects, an output destination which is off stays in the settings
                    IsAllowed = ConfigurationRestrictions.IsDestinationAllowed(CoreConfiguration, destination.Designation),
                    CanChangeAllowed = !AreDestinationExclusionsFixed && !ConfigurationRestrictions.IsDestinationBlockedByAdministrator(CoreConfiguration, destination.Designation),
                    IsSelected = CoreConfiguration.OutputDestinations?.Any(designation => string.Equals(designation?.Trim(), destination.Designation, StringComparison.OrdinalIgnoreCase)) == true
                };
                
                destItem.PropertyChanged += (_, _) => OnPropertyChanged(nameof(IsSendToEmpty));
                Destinations.Add(destItem);
            }

            // Names in the settings which aren't registered, e.g. of a plugin which isn't installed
            foreach (var designation in UnlistedDestinationNames(CoreConfiguration, Destinations.Select(item => item.Designation)))
            {
                AddUnregisteredDestination(designation);
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
                    if (destItem.Destination != null)
                    {
                        destItem.IconSource = await DestinationIcons.GetImageSourceAsync(destItem.Destination.Descriptor?.IconKey).ConfigureAwait(true);
                    }
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
