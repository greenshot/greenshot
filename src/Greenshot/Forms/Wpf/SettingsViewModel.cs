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

            // Programs allowed to use Greenshot through greenshot-mcp
            AiToolsAllowedClients = new ObservableCollection<AiToolClientItem>((CoreConfiguration.AiToolsAllowedClients ?? new List<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path)).Select(path => new AiToolClientItem(path.Trim())));
            AiToolsAllowedClients.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasNoAiToolClients));
            _aiToolsExcludedProcessesText = string.Join(", ", CoreConfiguration.AiToolsExcludedProcesses ?? new List<string>());
            RefreshApprovedRecipes();

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

        /// <summary>
        /// Programs (full paths) the user allowed to use Greenshot through greenshot-mcp, written back on save
        /// </summary>
        public ObservableCollection<AiToolClientItem> AiToolsAllowedClients { get; }

        /// <summary>
        /// AI tools may use Greenshot at all (opt-in); everything else on the AI tools tab only matters when this is on
        /// </summary>
        public bool AiToolsEnabled
        {
            get => CoreConfiguration.AiToolsEnabled;
            set
            {
                if (CoreConfiguration.AiToolsEnabled != value)
                {
                    CoreConfiguration.AiToolsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _aiToolsExcludedProcessesText;

        /// <summary>
        /// The excluded processes, separated by commas, written back on save
        /// </summary>
        public string AiToolsExcludedProcessesText
        {
            get => _aiToolsExcludedProcessesText;
            set
            {
                if (_aiToolsExcludedProcessesText != value)
                {
                    _aiToolsExcludedProcessesText = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The process names from <see cref="AiToolsExcludedProcessesText"/>
        /// </summary>
        public List<string> GetAiToolsExcludedProcesses()
        {
            return (_aiToolsExcludedProcessesText ?? string.Empty)
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim())
                .Where(name => name.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private AiToolClientItem _selectedAiToolClient;

        public AiToolClientItem SelectedAiToolClient
        {
            get => _selectedAiToolClient;
            set
            {
                if (_selectedAiToolClient != value)
                {
                    _selectedAiToolClient = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedAiToolClient));
                }
            }
        }

        public bool HasSelectedAiToolClient => _selectedAiToolClient != null;

        public bool HasNoAiToolClients => AiToolsAllowedClients.Count == 0;

        /// <summary>
        /// The recipes from files, with their approval: shown in core, so approvals can be seen and revoked without the recipe editor
        /// </summary>
        public ObservableCollection<ApprovedRecipeItem> ApprovedRecipes { get; } = new ObservableCollection<ApprovedRecipeItem>();

        private ApprovedRecipeItem _selectedApprovedRecipe;

        public ApprovedRecipeItem SelectedApprovedRecipe
        {
            get => _selectedApprovedRecipe;
            set
            {
                if (_selectedApprovedRecipe != value)
                {
                    _selectedApprovedRecipe = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedApprovedRecipe));
                }
            }
        }

        public bool HasSelectedApprovedRecipe => _selectedApprovedRecipe != null;

        public void RefreshApprovedRecipes()
        {
            string selectedId = _selectedApprovedRecipe?.RecipeId;
            ApprovedRecipes.Clear();
            var manager = Greenshot.Recipes.RecipeManager.Instance;
            foreach (var recipe in manager.GetAllRecipes().Where(r => !string.IsNullOrEmpty(r.FilePath)).OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                var details = manager.GetRecipeDetails(recipe.Id);
                ApprovedRecipes.Add(new ApprovedRecipeItem(recipe, details));
            }
            SelectedApprovedRecipe = ApprovedRecipes.FirstOrDefault(r => string.Equals(r.RecipeId, selectedId, StringComparison.OrdinalIgnoreCase));
        }
        
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
                SimpleServiceProvider.Current.GetInstance<IGreenshotMainForm>(isOptional: true)?.ShowSetting(SelectedPlugin?.Name);
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
                    InitializeImageFormats();
                    OnPropertyChanged(nameof(ImageFormats));
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
            var registry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
            if (registry == null)
            {
                return;
            }

            foreach (var format in registry.GetSaveableFileFormats())
            {
                ImageFormats.Add(new ImageFormatItem
                {
                    Value = format.Id,
                    Description = format.GetDisplayNameWithPreferredExtension(),
                    DisplayNameWithPreferredExtension = format.GetDisplayNameWithPreferredExtension()
                });
            }

            // Ensure the current output file format is included in the list, even if it's not registered
            if (!ImageFormats.Any(item => string.Equals(item.Value, CoreConfiguration.OutputFileFormat, StringComparison.OrdinalIgnoreCase)))
            {
                ImageFormats.Add(new ImageFormatItem
                {
                    Value = CoreConfiguration.OutputFileFormat,
                    Description = CoreConfiguration.OutputFileFormat,
                    DisplayNameWithPreferredExtension = CoreConfiguration.OutputFileFormat
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

                var destItem = new DestinationItem
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
        public string Value { get; set; }
        public string Description { get; set; }
        public string DisplayNameWithPreferredExtension { get; set; }
    }

    public class WindowCaptureModeItem
    {
        public WindowCaptureMode Value { get; set; }
        public string Description { get; set; }
    }

    public class DestinationItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private ImageSource _iconSource;

        public IDestination Destination { get; set; }
        public string Description { get; set; }
        public ImageSource IconSource
        {
            get => _iconSource;
            set
            {
                if (_iconSource != value)
                {
                    _iconSource = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconSource)));
                }
            }
        }

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
        public bool IsConfigurable => Plugin is IConfigurablePlugin;

        private UIElement _configControl;
        private bool _controlCreated;

        public UIElement GetConfigurationControl()
        {
            if (!_controlCreated)
            {
                _controlCreated = true;
                _configControl = Plugin == null ? null : PluginHelper.Instance.CreateSettingsView(Plugin) as UIElement;
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

    /// <summary>
    /// A recipe from a file in the settings, with its approval in a line
    /// </summary>
    public sealed class ApprovedRecipeItem
    {
        public ApprovedRecipeItem(Greenshot.Base.Recipes.CaptureRecipe recipe, Greenshot.Base.Recipes.RecipeDetails details)
        {
            Recipe = recipe;
            Details = details;
            string by = string.IsNullOrEmpty(details?.ProposedBy) ? "" : $" · written by {details.ProposedBy}";
            string state = details?.ApprovedAt == null ? "not approved"
                : details.IsApprovalCurrent ? $"approved {details.ApprovedAt:yyyy-MM-dd}"
                : "changed since its approval";
            int off = details?.Triggers?.Count(t => t.EndsWith("(off, not approved)", StringComparison.Ordinal)) ?? 0;
            string offText = off == 0 ? "" : off == 1 ? " · 1 trigger off" : $" · {off} triggers off";
            Title = recipe.Name;
            Subtitle = $"{state}{by}{offText}";
            NeedsAttention = details?.ApprovedAt == null || !details.IsApprovalCurrent || off > 0;
            DisplayText = $"{recipe.Name}{by} · {state}{offText}";
        }

        public string Title { get; }

        /// <summary>
        /// The approval state, who wrote it and the triggers left off
        /// </summary>
        public string Subtitle { get; }

        /// <summary>
        /// Not approved, changed since the approval, or triggers left off
        /// </summary>
        public bool NeedsAttention { get; }

        public Greenshot.Base.Recipes.CaptureRecipe Recipe { get; }

        public Greenshot.Base.Recipes.RecipeDetails Details { get; }

        public string RecipeId => Recipe.Id;

        public string DisplayText { get; }

        public override string ToString() => DisplayText;
    }

    /// <summary>
    /// A program allowed to use Greenshot through greenshot-mcp
    /// </summary>
    public sealed class AiToolClientItem
    {
        public AiToolClientItem(string path)
        {
            Path = path;
            FileName = System.IO.Path.GetFileName(path);
            Folder = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
        }

        /// <summary>
        /// The full path, as stored in AiToolsAllowedClients
        /// </summary>
        public string Path { get; }

        public string FileName { get; }

        public string Folder { get; }

        public override string ToString() => Path;
    }
}
