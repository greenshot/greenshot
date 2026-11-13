/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Grid = System.Windows.Controls.Grid;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Input.Mouse;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Dapplo.Ini;
using Greenshot.Editor.Destinations;
using Greenshot.Base.Threading;
using Greenshot.Base.Languages;
using Greenshot.Recipes.ViewModels;
using Greenshot.Viewer;
using Greenshot.Viewer.ViewModels;
using Greenshot.Viewer.Views;

namespace Greenshot.Recipes.Views
{

    /// <summary>
    /// The window in which the user chooses a destination: the capture as a small preview (optional), the editor recipes (when there
    /// are some), a search box and the list of the destinations. The list is always in the same order; the number in front of an
    /// entry is its place in the list as shown (the filtered list), Ctrl and the number pick it.
    /// Typing filters the list, Enter takes the first entry, Down goes into the list.
    /// </summary>
    public partial class DynamicDestinationWindow : Window, INotifyPropertyChanged
    {
        // The destinations which come first, in this order, then all others by their priority
        private static readonly string[] LeadingDestinations =
        {
            EditorDestination.DESIGNATION,
            nameof(WellKnownDestinations.Clipboard),
            nameof(WellKnownDestinations.FileDialog),
            nameof(WellKnownDestinations.FileNoDialog),
            nameof(WellKnownDestinations.Printer),
            "Windows10OCR",
            "Windows10Share"
        };

        private readonly DispatcherTimer _timer;
        private int _secondsRemaining;
        private string _searchText = "";
        private ViewerWindow _previewViewer;
        private string _customTitle;
        private EditableCapture _capture;
        private CaptureFlowHandle _runningRecipe;
        private bool _closed;
        private bool _closing;
        private ICaptureDetails _watchedDetails;
        // The mouse clicks anywhere on the screen, while the window closes with a click outside of it
        private IDisposable _clicksOutside;

        /// <summary>
        /// The width of the part with the preview, the details and the recipes when it is left of the list
        /// </summary>
        internal const double SidePanelWidth = 260;

        /// <summary>
        /// How the window looks and behaves, every value set (see DestinationWindowOptions.Resolve)
        /// </summary>
        internal DestinationWindowOptions Options { get; private set; } = new DestinationWindowOptions().Resolve(null);

        /// <summary>
        /// The preview is left of the list: only with a preview, without one the recipes are above the list
        /// </summary>
        internal bool IsPreviewLeft => Options.ShowPreview == true && Options.PreviewPosition == DestinationWindowPreviewPosition.Left;

        /// <summary>
        /// What is known of the capture, below the preview
        /// </summary>
        public CaptureMetadataViewModel Metadata { get; } = new CaptureMetadataViewModel();

        public Visibility MetadataVisibility => Options.ShowMetadata == true && _capture != null ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler PropertyChanged;

        public string WindowTitle => $"Greenshot - {HeaderTitle}";
        public string HeaderTitle => !string.IsNullOrWhiteSpace(_customTitle) ? _customTitle : Texts.Settings.Destination;
        public string ErrorMessage { get; set; }
        public Visibility ErrorBannerVisibility => !string.IsNullOrWhiteSpace(ErrorMessage) ? Visibility.Visible : Visibility.Collapsed;

        public BitmapSource PreviewImageSource { get; set; }
        public Visibility ThumbnailVisibility => PreviewImageSource != null ? Visibility.Visible : Visibility.Collapsed;
        public string DimensionsText { get; set; }

        /// <summary>
        /// All destinations in their fixed order
        /// </summary>
        public ObservableCollection<DestinationTileViewModel> AllDestinationTiles { get; } = new ObservableCollection<DestinationTileViewModel>();

        /// <summary>
        /// The destinations which match the search, in the same order; their place in this list is their number
        /// </summary>
        public ObservableCollection<DestinationTileViewModel> FilteredDestinationTiles { get; } = new ObservableCollection<DestinationTileViewModel>();

        /// <summary>
        /// The rows of the list: the matching destinations, each followed by its entries (open editors, printers) when they are shown
        /// </summary>
        public ObservableCollection<DestinationTileViewModel> DisplayedTiles { get; } = new ObservableCollection<DestinationTileViewModel>();

        /// <summary>
        /// The editor recipes which change the capture
        /// </summary>
        public ObservableCollection<RecipeChoice> AvailableRecipes { get; } = new ObservableCollection<RecipeChoice>();
        public RecipeChoice SelectedRecipe { get; set; }
        public Visibility RecipeSectionVisibility => Options.ShowRecipes == true && AvailableRecipes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    // A new search opens the destinations whose entries match again, also those the user closed
                    foreach (var tile in AllDestinationTiles)
                    {
                        tile.AutoOpenSuppressed = false;
                    }

                    UpdateFilteredDestinations();
                }
            }
        }

        public string TimeoutText => _secondsRemaining > 0
            ? string.Format(Texts.Recipe.PickerAutoClose, _secondsRemaining)
            : "";

        public Visibility TimeoutVisibility => _secondsRemaining > 0 ? Visibility.Visible : Visibility.Collapsed;

        public IDestination SelectedDestination { get; private set; }

        // Localized UI text strings
        public string RecipeHeaderText => Texts.Recipe.PickerApplyRecipe;
        public string RunRecipeButtonText => "▶ " + Texts.Recipe.PickerApply;
        public string ClickToPreviewTooltip => Texts.Settings.DestinationViewer;
        public string ErrorSubtitleText => Texts.Recipe.PickerErrorChooseAgain;

        public DynamicDestinationWindow(
            string title,
            IEnumerable<IDestination> destinations,
            string errorMessage = null,
            int timeoutSeconds = 0)
        {
            InitializeComponent();
            DataContext = this;
            // Not a binding: the first entry is selected at once, a binding only connects once the window is shown
            DestinationList.ItemsSource = DisplayedTiles;

            _customTitle = title;
            ErrorMessage = errorMessage;

            if (destinations != null)
            {
                // Only the destinations which were given: a step with a list of destinations shows no editor which isn't in it
                var destList = destinations.Where(d => d != null && !DestinationHelper.IsChooser(d.Designation)).ToList();

                // The same order every time, whatever the language or the order the destinations came in: the numbers rely on it
                foreach (var dest in destList.OrderBy(SortRank)
                             .ThenBy(d => d.Descriptor?.Priority ?? 10)
                             .ThenBy(d => d.Designation, StringComparer.OrdinalIgnoreCase))
                {
                    string badge = null;
                    if (!string.IsNullOrEmpty(errorMessage) && string.Equals(dest.Designation, nameof(WellKnownDestinations.Clipboard), StringComparison.OrdinalIgnoreCase))
                    {
                        badge = Texts.Recipe.PickerRetry;
                    }

                    var tile = new DestinationTileViewModel
                    {
                        Destination = dest,
                        Title = FormatDestinationTitle(dest),
                        BadgeText = badge
                    };
                    // The icon arrives asynchronously (started on the UI thread, set there)
                    tile.LoadIconAsync().FireAndLog("Load a destination icon");
                    AllDestinationTiles.Add(tile);
                }
            }

            UpdateFilteredDestinations();

            if (timeoutSeconds > 0)
            {
                _secondsRemaining = timeoutSeconds;
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _timer.Tick += (s, e) =>
                {
                    _secondsRemaining--;
                    OnPropertyChanged(nameof(TimeoutText));
                    if (_secondsRemaining <= 0)
                    {
                        _timer.Stop();
                        Close();
                    }
                };
                _timer.Start();
            }

            Texts.Config.LanguageChanged += OnLanguageChanged;

            // The search box has the focus from the start: typing filters at once
            Loaded += (s, e) =>
            {
                LimitListToWorkArea();
                // Opened from the tray, a hotkey or a recipe Windows may leave another window in the foreground: the window takes it,
                // so the keys work and a click outside of it deactivates it
                if (!IsActive)
                {
                    WindowHelper.ToForeground(new WindowInteropHelper(this).Handle);
                }
                WatchClicksOutside();
                SearchBox.Focus();
            };
            SourceInitialized += (s, e) =>
            {
                if (PlaceAtMouse)
                {
                    MoveToMouse();
                }
            };
            // A window which grows (more entries, the entries below a destination) stays on the screen
            SizeChanged += (s, e) =>
            {
                if (e.HeightChanged && IsLoaded)
                {
                    KeepInWorkArea();
                }
            };
            Closing += (s, e) => _closing = !e.Cancel;
            Closed += (s, e) =>
            {
                _closed = true;
                _timer?.Stop();
                // A recipe which still runs changes nothing anymore
                _runningRecipe?.Cancel();
                ClosePreviewViewer();
                if (_capture != null)
                {
                    _capture.Changed -= OnCaptureChanged;
                }
                if (_watchedDetails != null)
                {
                    _watchedDetails.FeaturesChanged -= OnFeaturesChanged;
                }
                _clicksOutside?.Dispose();
                _clicksOutside = null;
                Texts.Config.LanguageChanged -= OnLanguageChanged;
            };
            DataObject.AddPastingHandler(SearchBox, SearchBox_Pasting);
            PreviewKeyDown += OnWindowPreviewKeyDown;
            PreviewKeyUp += (s, e) => UpdateShortcutHint();
            // Ctrl released while another window had the focus
            Activated += (s, e) => UpdateShortcutHint();
            Deactivated += (s, e) =>
            {
                UpdateShortcutHint();
                CloseWhenClickedOutside();
            };
            PreviewMouseDown += (s, e) => StopTimeout();
            KeyDown += OnWindowKeyDown;
        }

        /// <summary>
        /// Shows the image as preview, with its size
        /// </summary>
        private void SetPreview(System.Drawing.Image previewImage)
        {
            if (previewImage == null)
            {
                return;
            }

            try
            {
                DimensionsText = $"{previewImage.Width} × {previewImage.Height} px";
                using var ms = new MemoryStream();
                previewImage.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = ms;
                bi.EndInit();
                bi.Freeze();
                PreviewImageSource = bi;
            }
            catch
            {
                // Ignore preview load error
            }

            OnPropertyChanged(nameof(DimensionsText));
            OnPropertyChanged(nameof(PreviewImageSource));
            OnPropertyChanged(nameof(ThumbnailVisibility));
        }

        /// <summary>
        /// The capture can be changed with the editor recipes, the window shows it as preview (and what is known of it) as the options say
        /// </summary>
        internal void UseCapture(EditableCapture capture, DestinationWindowOptions options)
        {
            _capture = capture;
            ApplyOptions(options);
            // The open editors, the printers: loaded in the background, the entries show when they are there
            StartLoadingSubEntries(capture.Surface?.CaptureDetails);
            if (Options.ShowPreview == true || Options.ShowMetadata == true)
            {
                _capture.Changed += OnCaptureChanged;
                ShowCapture();
            }
            // Without the recipes (option) the list and its button aren't shown
            if (Options.ShowRecipes == true)
            {
                foreach (var trigger in EditableCapture.GetEditorRecipes())
                {
                    AvailableRecipes.Add(new RecipeChoice(EditableCapture.DisplayName(trigger), trigger));
                }
            }
            SelectedRecipe = AvailableRecipes.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedRecipe));
            OnPropertyChanged(nameof(RecipeSectionVisibility));
            OnPropertyChanged(nameof(MetadataVisibility));
        }

        /// <summary>
        /// Takes the options (the ones not set come from the settings) and arranges the window for them
        /// </summary>
        internal void ApplyOptions(DestinationWindowOptions options)
        {
            Options = (options ?? new DestinationWindowOptions()).Resolve(IniConfigRegistry.GetSection<ICoreConfiguration>());
            ApplyLayout();
            // Like a context menu: no title bar (not just hidden: it would give the window the frame of Windows or a caption area)
            if (Options.CloseOnDeactivate == true)
            {
                RootGrid.Children.Remove(TitleBar);
            }

            // Grows with the list: the list has no height of its own, the work area of the screen limits it (when loaded)
            if (Options.AutoSize == true)
            {
                DestinationList.MaxHeight = double.PositiveInfinity;
            }

            OnPropertyChanged(nameof(MetadataVisibility));
        }

        /// <summary>
        /// The preview at the top: the capture (preview, details, recipes) above the list. At the left: the capture left of the list,
        /// the window is wider by it. Without a preview the recipes are always above the list.
        /// </summary>
        private void ApplyLayout()
        {
            bool left = IsPreviewLeft;
            Grid.SetRow(SidePanel, 0);
            Grid.SetColumn(SidePanel, 0);
            Grid.SetRowSpan(SidePanel, left ? 2 : 1);
            Grid.SetColumnSpan(SidePanel, left ? 1 : 2);
            Grid.SetRow(ListPanel, left ? 0 : 1);
            Grid.SetColumn(ListPanel, left ? 1 : 0);
            Grid.SetRowSpan(ListPanel, left ? 2 : 1);
            Grid.SetColumnSpan(ListPanel, left ? 1 : 2);
            SideColumn.Width = left ? new GridLength(SidePanelWidth) : GridLength.Auto;
            // Beside the list there is room for a bigger preview
            ThumbnailButton.MaxHeight = left ? 200 : 110;
            SidePanel.Margin = left ? new Thickness(0, 0, 0, 10) : new Thickness(0);
            Width = left ? 400 + SidePanelWidth : 400;
        }

        private void ShowCapture()
        {
            if (Options.ShowPreview == true)
            {
                using var image = _capture.GetImageForExport();
                SetPreview(image);
            }

            UpdateMetadata();
        }

        private void OnCaptureChanged(object sender, EventArgs e)
        {
            // The viewer shows a copy, which isn't the capture anymore
            ClosePreviewViewer();
            ShowCapture();
        }

        /// <summary>
        /// Shows what is known of the capture; the OCR of the capture may still be running, its lines come later
        /// </summary>
        private void UpdateMetadata()
        {
            if (Options.ShowMetadata != true || _capture?.Surface == null)
            {
                return;
            }

            var details = _capture.Surface.CaptureDetails;
            if (!ReferenceEquals(details, _watchedDetails))
            {
                if (_watchedDetails != null)
                {
                    _watchedDetails.FeaturesChanged -= OnFeaturesChanged;
                }

                _watchedDetails = details;
                if (details != null)
                {
                    details.FeaturesChanged += OnFeaturesChanged;
                }
            }

            Metadata.Update(_capture.Surface);
        }

        // Raised by the processors (OCR, QR) on their thread
        private void OnFeaturesChanged(object sender, EventArgs e) =>
            Dispatcher.InvokeAsync(() =>
            {
                if (!_closed)
                {
                    UpdateMetadata();
                }
            }).Task.FireAndLog("Show the details of the capture");

        /// <summary>
        /// Runs the editor recipe on the capture; the window waits for it and shows the changed capture
        /// </summary>
        private async Task ApplyRecipeAsync(IEditorTrigger trigger)
        {
            var handle = _capture.RunRecipe(trigger);
            if (handle == null)
            {
                return;
            }

            // Not IsEnabled=false: disabled controls are drawn in system colors, the window flashes light. The overlay dims the window
            // and takes the mouse, the keys are caught in OnWindowPreviewKeyDown.
            _runningRecipe = handle;
            BusyOverlay.Visibility = Visibility.Visible;
            try
            {
                var result = await handle.Completion.ConfigureAwait(true);
                if (result != null && result.State == CaptureFlowState.Failed)
                {
                    ErrorMessage = result.Reason ?? result.Error?.Message;
                    OnPropertyChanged(nameof(ErrorMessage));
                    OnPropertyChanged(nameof(ErrorBannerVisibility));
                }
            }
            finally
            {
                _runningRecipe = null;
                BusyOverlay.Visibility = Visibility.Collapsed;
                // The capture is ready, the next thing is to choose where it goes
                SearchBox.Focus();
            }
        }

        /// <summary>
        /// The place of a destination among those which come first, the others share one place after them
        /// </summary>
        private static int SortRank(IDestination destination)
        {
            int index = Array.FindIndex(LeadingDestinations, designation => string.Equals(designation, destination.Designation, StringComparison.OrdinalIgnoreCase));
            return index >= 0 ? index : LeadingDestinations.Length;
        }

        private static string FormatDestinationTitle(IDestination dest)
        {
            string des = dest.Designation ?? "";
            string rawDesc = dest.Descriptor?.DisplayName ?? des;

            switch (des.ToLowerInvariant())
            {
                case "clipboard":
                    return Texts.Settings.DestinationClipboard;
                case "editor":
                    return Texts.Editor.Title;
                case "fileno_dialog":
                case "filenodialog":
                case "file":
                    return Texts.Recipe.PickerFile;
                case "filedialog":
                case "fileas":
                    return Texts.Recipe.PickerFileas;
                case "printer":
                    return Texts.Settings.DestinationPrinter;
                case "windows10ocr":
                case "ocr":
                    return Texts.Recipe.PickerOcr;
                case "windows10share":
                case "share":
                    return Texts.Recipe.PickerShare;
                case "mail":
                case "outlook":
                    return Texts.Editor.Email;
                case "word":
                    return "Microsoft Word";
                case "excel":
                    return "Microsoft Excel";
                case "powerpoint":
                    return "PowerPoint";
                case "onenote":
                    return "OneNote";
                case "zxing":
                case "zxingqrdestination":
                    return Texts.Recipe.PickerQr;
                case "paint":
                    return "MS Paint";
                case "dropbox":
                    return "Dropbox";
                case "box":
                    return "Box";
                case "jira":
                    return "Jira";
                case "confluence":
                    return "Confluence";
            }

            if (rawDesc.StartsWith("Upload to ", StringComparison.OrdinalIgnoreCase))
            {
                return rawDesc.Substring(10);
            }

            int parenIdx = rawDesc.IndexOf('(');
            if (parenIdx > 0 && rawDesc.EndsWith(")"))
            {
                return rawDesc.Substring(0, parenIdx).Trim();
            }

            return rawDesc;
        }

        /// <summary>
        /// Shows the destinations which match the search; the first row is the one Enter takes, it shows
        /// </summary>
        private void UpdateFilteredDestinations()
        {
            RefreshRows();
            DestinationList.SelectedIndex = DisplayedTiles.Count > 0 ? 0 : -1;
        }

        /// <summary>
        /// A destination matches the search with its name or designation
        /// </summary>
        private static bool MatchesSearch(DestinationTileViewModel tile, string filter) =>
            string.IsNullOrEmpty(filter) ||
            tile.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (tile.Destination?.Designation != null && tile.Destination.Designation.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>
        /// An entry below a destination matches with its name (its designation is that of the destination)
        /// </summary>
        private static bool EntryMatchesSearch(DestinationTileViewModel entry, string filter) =>
            string.IsNullOrEmpty(filter) || entry.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// Brings the list in line with the search and the entries which are shown, and numbers the rows. A destination stays in the list
        /// when one of its entries matches, even when it doesn't match itself, and it opens to show the entries which match.
        /// </summary>
        private void RefreshRows()
        {
            string filter = _searchText?.Trim() ?? "";

            FilteredDestinationTiles.Clear();
            foreach (var tile in AllDestinationTiles)
            {
                if (MatchesSearch(tile, filter) || tile.SubEntries.Any(entry => EntryMatchesSearch(entry, filter)))
                {
                    FilteredDestinationTiles.Add(tile);
                }
            }

            SyncDisplayedTiles(filter);
            NumberRows();
        }

        /// <summary>
        /// Brings the rows of the list in line with the matching destinations and the entries which are shown, changing as little as
        /// possible: a row which stays keeps its place (and the keyboard focus). A destination shows its entries when the user opened it, or
        /// when the search found its text in one of them (unless the user closed it again): all its entries when the user opened it and
        /// it matches the search itself, else those which match.
        /// </summary>
        private void SyncDisplayedTiles(string filter)
        {
            var wanted = new List<DestinationTileViewModel>();
            foreach (var tile in AllDestinationTiles)
            {
                if (!FilteredDestinationTiles.Contains(tile))
                {
                    tile.IsShowingEntries = false;
                    continue;
                }

                wanted.Add(tile);
                bool entriesMatch = !string.IsNullOrEmpty(filter) && tile.SubEntries.Any(entry => EntryMatchesSearch(entry, filter));
                bool open = tile.HasSubEntries && (tile.IsExpanded || (entriesMatch && !tile.AutoOpenSuppressed));
                tile.IsShowingEntries = open;
                if (open)
                {
                    bool showAll = tile.IsExpanded && MatchesSearch(tile, filter);
                    wanted.AddRange(tile.SubEntries.Where(entry => showAll || EntryMatchesSearch(entry, filter)));
                }
            }

            for (int i = 0; i < wanted.Count; i++)
            {
                if (i < DisplayedTiles.Count && ReferenceEquals(DisplayedTiles[i], wanted[i]))
                {
                    continue;
                }

                int existing = DisplayedTiles.IndexOf(wanted[i]);
                if (existing > i)
                {
                    DisplayedTiles.Move(existing, i);
                }
                else
                {
                    DisplayedTiles.Insert(i, wanted[i]);
                }
            }

            while (DisplayedTiles.Count > wanted.Count)
            {
                DisplayedTiles.RemoveAt(DisplayedTiles.Count - 1);
            }
        }

        /// <summary>
        /// The number of a row is its place in the list as shown, the entries below a destination count too; it changes with every
        /// filter and when entries are shown or hidden
        /// </summary>
        private void NumberRows()
        {
            foreach (var tile in AllDestinationTiles.SelectMany(tile => new[] { tile }.Concat(tile.SubEntries)))
            {
                int index = DisplayedTiles.IndexOf(tile);
                tile.Position = index >= 0 ? index + 1 : 0;
            }
        }


        #region Entries below a destination (open editors, printers)

        private void StartLoadingSubEntries(ICaptureDetails captureDetails)
        {
            foreach (var tile in AllDestinationTiles.Where(tile => tile.Destination?.Descriptor?.HasDynamicDestinations == true).ToList())
            {
                LoadSubEntriesAsync(tile, captureDetails).FireAndLog("Load the entries of a destination");
            }
        }

        private async Task LoadSubEntriesAsync(DestinationTileViewModel tile, ICaptureDetails captureDetails)
        {
            // The same entries the destination picker (the menu) had, loaded in the background
            var subDestinations = await DestinationHelper.LoadDynamicDestinationsAsync(tile.Destination, captureDetails).ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() =>
            {
                if (!_closed)
                {
                    ApplySubEntries(tile, subDestinations);
                }
            }).Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Adds the entries below the destination. Without any the destination stays as it is. A destination which can only be used with
        /// one of its entries and has just one becomes that entry.
        /// </summary>
        internal void ApplySubEntries(DestinationTileViewModel tile, IReadOnlyList<IDestination> subDestinations)
        {
            var valid = (subDestinations ?? Array.Empty<IDestination>()).Where(d => d != null).OrderBy(d => d, DestinationComparer.Instance).ToList();
            if (valid.Count == 0)
            {
                return;
            }

            if (tile.Destination.Descriptor.UseDynamicsOnly && valid.Count == 1)
            {
                tile.Destination = valid[0];
                tile.Title = valid[0].Descriptor.DisplayName;
                tile.LoadIconAsync().FireAndLog("Load a destination icon");
                return;
            }

            foreach (var subDestination in valid)
            {
                var subTile = new DestinationTileViewModel
                {
                    Destination = subDestination,
                    Title = subDestination.Descriptor.DisplayName,
                    Parent = tile
                };
                subTile.LoadIconAsync().FireAndLog("Load a destination icon");
                tile.SubEntries.Add(subTile);
            }

            tile.NotifySubEntriesChanged();
            // The entries arrive while Ctrl may be held already
            UpdateShortcutHint();
            // A row arrives below, the selection stays where it is
            RefreshRows();
        }

        /// <summary>
        /// The user shows or hides the entries of a destination. Hiding also keeps a search from opening them again, until the search
        /// text changes.
        /// </summary>
        private static void SetShowing(DestinationTileViewModel tile, bool show)
        {
            tile.IsExpanded = show;
            tile.AutoOpenSuppressed = !show;
        }

        /// <summary>
        /// Shows or hides the entries below the destination (without a value: the other of the two)
        /// </summary>
        internal void ToggleExpanded(DestinationTileViewModel tile, bool? expand = null)
        {
            if (tile.Parent != null || !tile.HasSubEntries)
            {
                return;
            }

            var selected = DestinationList.SelectedItem as DestinationTileViewModel;
            SetShowing(tile, expand ?? !tile.IsShowingEntries);
            RefreshRows();
            // The selection stays on its row, when that row went away (an entry below) it is the destination
            DestinationList.SelectedItem = selected != null && DisplayedTiles.Contains(selected) ? selected : tile;
        }

        /// <summary>
        /// Shows or hides the entries below all destinations (Right and Left)
        /// </summary>
        /// <returns>True when something changed</returns>
        internal bool ExpandAll(bool expand)
        {
            var changing = AllDestinationTiles.Where(tile => tile.HasSubEntries && tile.IsShowingEntries != expand).ToList();
            if (changing.Count == 0)
            {
                return false;
            }

            var selected = DestinationList.SelectedItem as DestinationTileViewModel;
            foreach (var tile in changing)
            {
                SetShowing(tile, expand);
            }

            RefreshRows();
            // The selection stays on its row, when that row went away (an entry below) it is its destination
            var stays = selected != null && DisplayedTiles.Contains(selected) ? selected : selected?.Parent;
            DestinationList.SelectedItem = stays;
            if (stays != null && DestinationList.IsKeyboardFocusWithin)
            {
                SelectAndFocus(DisplayedTiles.IndexOf(stays));
            }

            return true;
        }

        /// <summary>
        /// A choice of the user: the destination is taken, except one which can't be used itself (only its entries): that one opens
        /// </summary>
        private void Activate(DestinationTileViewModel tile)
        {
            if (tile.Parent == null && !tile.CanChooseItself)
            {
                ToggleExpanded(tile);
                if (tile.IsShowingEntries)
                {
                    // The next thing is to choose one of the entries
                    SelectAndFocus(DisplayedTiles.IndexOf(tile));
                }

                return;
            }

            Choose(tile);
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: DestinationTileViewModel tile })
            {
                ToggleExpanded(tile);
                DestinationList.SelectedItem = tile;
                e.Handled = true;
            }
        }

        #endregion

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(RecipeHeaderText));
            OnPropertyChanged(nameof(RunRecipeButtonText));
            OnPropertyChanged(nameof(ClickToPreviewTooltip));
            OnPropertyChanged(nameof(ErrorSubtitleText));
            OnPropertyChanged(nameof(TimeoutText));
            Metadata.Refresh();
        }

        /// <summary>
        /// Whoever uses the window needs the time: the countdown ends at the first key or click
        /// </summary>
        private void StopTimeout()
        {
            if (_timer == null || !_timer.IsEnabled)
            {
                return;
            }

            _timer.Stop();
            _secondsRemaining = 0;
            OnPropertyChanged(nameof(TimeoutText));
            OnPropertyChanged(nameof(TimeoutVisibility));
        }

        #region Preview in the viewer

        private void Thumbnail_Click(object sender, RoutedEventArgs e) => OpenPreviewViewer();

        /// <summary>
        /// Shows a copy of the capture in the viewer, which has the zoom (1:1 when it fits), panning and a frame, also for a capture
        /// of more screens. The viewer belongs to this window: it stays in front of it, closing it (Esc) comes back here, and it
        /// closes with this window. It only shows the capture, the destination is chosen here.
        /// </summary>
        private void OpenPreviewViewer()
        {
            if (_capture?.Surface == null)
            {
                return;
            }

            if (_previewViewer != null)
            {
                _previewViewer.Activate();
                return;
            }

            var copy = _capture.Surface.Clone();
            ViewerWindow viewer;
            try
            {
                viewer = new ViewerWindow(new ViewerViewModel(copy, null, IniConfigRegistry.GetSection<IViewerConfiguration>(), canExport: false));
            }
            catch
            {
                copy.Dispose();
                throw;
            }

            viewer.Owner = this;
            // Opens over the preview, centered on it, like the preview grows (on the screen of the preview, inside its work area)
            if (ThumbnailButton.IsVisible)
            {
                var center = ThumbnailButton.PointToScreen(new Point(ThumbnailButton.ActualWidth / 2, ThumbnailButton.ActualHeight / 2));
                viewer.CenterOn = new System.Drawing.Point((int)Math.Round(center.X), (int)Math.Round(center.Y));
            }
            // A click outside of both closes this window too (when it closes like a context menu)
            viewer.Deactivated += (s, e) => CloseWhenClickedOutside();
            viewer.Closed += (s, e) =>
            {
                if (_previewViewer != viewer)
                {
                    return;
                }

                _previewViewer = null;
                if (IsVisible)
                {
                    Activate();
                    ThumbnailButton.Focus();
                }
            };
            _previewViewer = viewer;
            viewer.Show();
        }

        private void ClosePreviewViewer()
        {
            var viewer = _previewViewer;
            _previewViewer = null;
            viewer?.Close();
        }

        #endregion

        #region Keyboard and mouse

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            // Not handled before: an open list of the recipes closes with Esc, the window only when nothing else used it
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
        }

        /// <summary>
        /// A number, alone or with Ctrl, picks the entry at that place of the list, wherever the focus is. The search takes no numbers.
        /// </summary>
        private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            StopTimeout();
            UpdateShortcutHint();

            // A recipe is running: only Esc works, it closes the window and stops the recipe
            if (_runningRecipe != null)
            {
                e.Handled = e.Key != Key.Escape;
                return;
            }

            // Not with Shift or Alt (AltGr), they type characters like { or !
            var modifiers = Keyboard.Modifiers;
            if (modifiers != ModifierKeys.None && modifiers != ModifierKeys.Control)
            {
                return;
            }

            int index = IndexOfDigit(e.Key);
            if (index < 0)
            {
                return;
            }

            e.Handled = true;
            // The place in the list as shown, the entries below a destination count
            if (index < DisplayedTiles.Count)
            {
                Activate(DisplayedTiles[index]);
            }
        }

        /// <summary>
        /// The place in the list a digit stands for: 1 is the first entry, 0 the tenth; -1 for any other key
        /// </summary>
        internal static int IndexOfDigit(Key key)
        {
            int digit;
            if (key >= Key.D0 && key <= Key.D9)
            {
                digit = key - Key.D0;
            }
            else if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                digit = key - Key.NumPad0;
            }
            else
            {
                return -1;
            }

            return digit == 0 ? DestinationTileViewModel.MaxNumbered - 1 : digit - 1;
        }

        /// <summary>
        /// While Ctrl is held the numbers of the entries are underlined: they are the keys
        /// </summary>
        private void UpdateShortcutHint()
        {
            bool ctrl = IsActive && (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            foreach (var tile in AllDestinationTiles.SelectMany(tile => new[] { tile }.Concat(tile.SubEntries)))
            {
                tile.ShortcutHintVisible = ctrl;
            }
        }

        /// <summary>
        /// Numbers pick an entry, so they don't go into the search (they are caught as keys already, this is for text which doesn't
        /// come from a key, like an input method or a touch keyboard)
        /// </summary>
        private void SearchBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Any(char.IsDigit))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Pasting takes the text without its numbers
        /// </summary>
        private static void SearchBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.UnicodeText) && e.DataObject.GetData(DataFormats.UnicodeText) is string text && text.Any(char.IsDigit))
            {
                var withoutNumbers = new DataObject();
                withoutNumbers.SetData(DataFormats.UnicodeText, new string(text.Where(c => !char.IsDigit(c)).ToArray()));
                e.DataObject = withoutNumbers;
                e.FormatToApply = DataFormats.UnicodeText;
            }
        }

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter when Keyboard.Modifiers == ModifierKeys.None:
                    // The first row of the list as shown
                    if (DisplayedTiles.Count > 0)
                    {
                        Activate(DisplayedTiles[0]);
                    }
                    e.Handled = true;
                    break;
                // Right and Left show and hide the entries of all destinations, but only where the caret has nothing to move over:
                // at the end of the search text for Right, at the start for Left
                case Key.Right when Keyboard.Modifiers == ModifierKeys.None && SearchBox.SelectionLength == 0 && SearchBox.CaretIndex == SearchBox.Text.Length:
                    e.Handled = ExpandAll(true);
                    break;
                case Key.Left when Keyboard.Modifiers == ModifierKeys.None && SearchBox.SelectionLength == 0 && SearchBox.CaretIndex == 0:
                    e.Handled = ExpandAll(false);
                    break;
                case Key.Down when Keyboard.Modifiers == ModifierKeys.None:
                    FocusList();
                    e.Handled = true;
                    break;
                case Key.PageDown when Keyboard.Modifiers == ModifierKeys.None:
                    MovePage(1);
                    e.Handled = true;
                    break;
                // Nothing above the first entry: Page Up stays in the search box
                case Key.PageUp when Keyboard.Modifiers == ModifierKeys.None && DestinationList.SelectedIndex > 0:
                    MovePage(-1);
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Enter on the recipes runs the recipe (in the open list of the recipes it takes the entry, as always)
        /// </summary>
        private void RecipeBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None && !RecipeBox.IsDropDownOpen)
            {
                RunRecipe_Click(sender, e);
                e.Handled = true;
            }
        }

        private void DestinationList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.PageDown when Keyboard.Modifiers == ModifierKeys.None:
                    MovePage(1);
                    e.Handled = true;
                    break;
                case Key.PageUp when Keyboard.Modifiers == ModifierKeys.None:
                    MovePage(-1);
                    e.Handled = true;
                    break;
                case Key.Back when Keyboard.Modifiers == ModifierKeys.None:
                    // Deleting goes to the search box, like typing does: the last character of the search is removed
                    FocusSearchBox();
                    if (SearchBox.Text.Length > 0)
                    {
                        SearchBox.Text = SearchBox.Text.Substring(0, SearchBox.Text.Length - 1);
                        SearchBox.CaretIndex = SearchBox.Text.Length;
                    }
                    e.Handled = true;
                    break;
                case Key.Enter when Keyboard.Modifiers == ModifierKeys.None:
                    if (DestinationList.SelectedItem is DestinationTileViewModel selected)
                    {
                        Activate(selected);
                    }
                    e.Handled = true;
                    break;
                case Key.Up when Keyboard.Modifiers == ModifierKeys.None && DestinationList.SelectedIndex <= 0:
                    // Above the first entry is the search box
                    FocusSearchBox();
                    e.Handled = true;
                    break;
                case Key.Right when Keyboard.Modifiers == ModifierKeys.None:
                    // Shows the entries below all destinations (the open editors, the printers), wherever the selection is
                    e.Handled = ExpandAll(true);
                    break;
                case Key.Left when Keyboard.Modifiers == ModifierKeys.None:
                    // Hides them again; the selection on one of them goes to its destination
                    e.Handled = ExpandAll(false);
                    break;
            }
        }

        /// <summary>
        /// Typing in the list goes to the search box
        /// </summary>
        private void DestinationList_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text) || char.IsControl(e.Text[0]))
            {
                return;
            }

            FocusSearchBox();
            SearchBox.SelectedText = e.Text;
            SearchBox.CaretIndex = SearchBox.SelectionStart + SearchBox.SelectionLength;
            SearchBox.SelectionLength = 0;
            e.Handled = true;
        }

        private void DestinationItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: DestinationTileViewModel tile })
            {
                Activate(tile);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Moves the focus from the search box into the list, to the selected entry (the first one)
        /// </summary>
        private void FocusList()
        {
            if (DisplayedTiles.Count == 0)
            {
                return;
            }

            SelectAndFocus(Math.Max(DestinationList.SelectedIndex, 0));
        }

        /// <summary>
        /// Selects the entry, scrolls it into view and gives it the focus
        /// </summary>
        private void SelectAndFocus(int index)
        {
            DestinationList.SelectedIndex = index;
            DestinationList.ScrollIntoView(DestinationList.SelectedItem);
            DestinationList.UpdateLayout();
            if (DestinationList.ItemContainerGenerator.ContainerFromIndex(index) is FrameworkElement item)
            {
                item.Focus();
            }
        }

        /// <summary>
        /// Page Up and Page Down: a page of the list further, the focus goes into the list
        /// </summary>
        /// <param name="direction">1 for down, -1 for up</param>
        private void MovePage(int direction)
        {
            int count = DisplayedTiles.Count;
            if (count == 0)
            {
                return;
            }

            // The entries which are in view, one less so the last one stays in view as the first of the next page
            int visible = 10;
            if (DestinationList.ItemContainerGenerator.ContainerFromIndex(0) is FrameworkElement first && first.ActualHeight > 0)
            {
                visible = (int)(DestinationList.ActualHeight / first.ActualHeight);
            }

            int page = Math.Max(1, visible - 1);
            int index = Math.Max(DestinationList.SelectedIndex, 0) + direction * page;
            SelectAndFocus(Math.Min(Math.Max(index, 0), count - 1));
        }

        private void FocusSearchBox()
        {
            SearchBox.Focus();
            SearchBox.CaretIndex = SearchBox.Text.Length;
        }

        #endregion

        /// <summary>
        /// Shows the window modally with the capture: the editor recipes can change it, like in the editor, before a destination is
        /// chosen. Runs on the UI thread.
        /// </summary>
        /// <param name="owner">The window it is centered on, or null</param>
        /// <param name="capture">The capture, which the editor recipes replace</param>
        /// <param name="destinations">The destinations to choose from</param>
        /// <param name="errorMessage">The error of the last try, or null</param>
        /// <param name="cancellationToken">Closes the window</param>
        /// <param name="title">The title, null for the default</param>
        /// <param name="timeoutSeconds">Closes the window after this many seconds, 0 for never</param>
        /// <param name="options">How the window looks and behaves (e.g. of a step of a recipe), what isn't set there comes from the settings; null: all from the settings</param>
        /// <returns>The chosen destination, or null when the window was closed</returns>
        public static IDestination Choose(Window owner, EditableCapture capture, IEnumerable<IDestination> destinations, string errorMessage,
            CancellationToken cancellationToken = default, string title = null, int timeoutSeconds = 0, DestinationWindowOptions options = null)
        {
            var window = new DynamicDestinationWindow(title, destinations, errorMessage, timeoutSeconds);
            window.UseCapture(capture, options);
            window.PlaceAtMouse = window.Options.AtMouse == true;
            if (owner != null)
            {
                window.Owner = owner;
            }

            // At the mouse the window is moved when its handle exists (it has its size then), WPF must not center it
            window.WindowStartupLocation = window.PlaceAtMouse ? WindowStartupLocation.Manual
                : owner != null ? WindowStartupLocation.CenterOwner
                : WindowStartupLocation.CenterScreen;

            using (cancellationToken.Register(() => window.Dispatcher.InvokeAsync(() =>
                   {
                       if (window.IsVisible)
                       {
                           window.Close();
                       }
                   }).Task.FireAndLog("Close the destination window")))
            {
                window.ShowDialog();
            }

            cancellationToken.ThrowIfCancellationRequested();
            return window.SelectedDestination;
        }

        /// <summary>
        /// The window opens centered on the mouse pointer (on the screen with the pointer), instead of the middle of the screen
        /// </summary>
        public bool PlaceAtMouse { get; set; }

        /// <summary>
        /// Moves the window (it has a handle and a size now, it isn't shown yet) to the mouse pointer
        /// </summary>
        private void MoveToMouse()
        {
            var handle = new WindowInteropHelper(this).Handle;
            var bounds = InteropWindowFactory.CreateFor(handle).GetInfo().Bounds;
            var mouse = System.Windows.Forms.Cursor.Position;
            var workArea = System.Windows.Forms.Screen.FromPoint(mouse).WorkingArea;
            var (x, y) = LocationAtMouse(mouse.X, mouse.Y, bounds.Width, bounds.Height, workArea);
            User32Api.SetWindowPos(handle, IntPtr.Zero, x, y, 0, 0, WindowPos.SWP_NOSIZE | WindowPos.SWP_NOZORDER | WindowPos.SWP_NOACTIVATE);
        }

        /// <summary>
        /// The top left corner of a window (in screen pixels) which is centered on the mouse pointer and stays inside the work area
        /// of the screen; one which doesn't fit starts at the top left of the work area
        /// </summary>
        internal static (int X, int Y) LocationAtMouse(int mouseX, int mouseY, int width, int height, System.Drawing.Rectangle workArea) =>
            KeepInside(mouseX - width / 2, mouseY - height / 2, width, height, workArea);

        /// <summary>
        /// The top left corner of a window moved as little as possible to be inside the work area; one which doesn't fit starts at the
        /// top left of the work area
        /// </summary>
        internal static (int X, int Y) KeepInside(int x, int y, int width, int height, System.Drawing.Rectangle workArea) =>
            (Math.Max(workArea.Left, Math.Min(x, workArea.Right - width)), Math.Max(workArea.Top, Math.Min(y, workArea.Bottom - height)));

        /// <summary>
        /// Moves the window back into the work area of its screen, e.g. after it grew with the list
        /// </summary>
        private void KeepInWorkArea()
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var bounds = InteropWindowFactory.CreateFor(handle).GetInfo(forceUpdate: true).Bounds;
            var workArea = System.Windows.Forms.Screen.FromHandle(handle).WorkingArea;
            var (x, y) = KeepInside(bounds.Left, bounds.Top, bounds.Width, bounds.Height, workArea);
            if (x != bounds.Left || y != bounds.Top)
            {
                User32Api.SetWindowPos(handle, IntPtr.Zero, x, y, 0, 0, WindowPos.SWP_NOSIZE | WindowPos.SWP_NOZORDER | WindowPos.SWP_NOACTIVATE);
            }
        }

        /// <summary>
        /// A window which grows with its list (AutoSize) is at most as high as the work area of its screen: the list gets the height
        /// which is left, a longer list scrolls
        /// </summary>
        private void LimitListToWorkArea()
        {
            if (Options.AutoSize != true)
            {
                return;
            }

            var handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            double workAreaHeight = System.Windows.Forms.Screen.FromHandle(handle).WorkingArea.Height / System.Windows.Media.VisualTreeHelper.GetDpi(this).DpiScaleY;
            // Everything but the list: the title bar, the search, the preview above the list
            double others = ActualHeight - DestinationList.ActualHeight;
            DestinationList.MaxHeight = Math.Max(100, workAreaHeight - others);
        }

        /// <summary>
        /// A click outside of the window closes it also when the window never was the active one (then it isn't deactivated): the clicks
        /// of the whole screen are watched while the window is open
        /// </summary>
        private void WatchClicksOutside()
        {
            if (Options.CloseOnDeactivate != true || _clicksOutside != null)
            {
                return;
            }

            var handle = new WindowInteropHelper(this).Handle;
            _clicksOutside = MouseHook.MouseEventsNonBlocking
                .Where(click => IsButtonDown(click.WindowsMessage))
                // On the thread of the hook: which window was clicked
                .Where(click => !BelongsTo(WindowFromPoint(new NativePointStruct { X = click.Point.X, Y = click.Point.Y }), handle))
                .Subscribe(click => Dispatcher.InvokeAsync(CloseAfterClickOutside).Task.FireAndLog("Close the destination window after a click outside of it"));
        }

        private static bool IsButtonDown(WindowsMessages message) =>
            message == WindowsMessages.WM_LBUTTONDOWN || message == WindowsMessages.WM_RBUTTONDOWN || message == WindowsMessages.WM_MBUTTONDOWN
            || message == WindowsMessages.WM_XBUTTONDOWN || message == WindowsMessages.WM_NCLBUTTONDOWN || message == WindowsMessages.WM_NCRBUTTONDOWN;

        /// <summary>
        /// The clicked window is this one, or a window owned by it: the viewer of the preview, the list of the recipes, a tooltip
        /// </summary>
        private static bool BelongsTo(IntPtr clicked, IntPtr window)
        {
            // The top level window of e.g. a control, then its owners
            var current = clicked == IntPtr.Zero ? IntPtr.Zero : GetAncestor(clicked, GaRoot);
            for (int depth = 0; current != IntPtr.Zero && depth < 20; depth++)
            {
                if (current == window)
                {
                    return true;
                }

                current = GetWindow(current, GwOwner);
            }

            return false;
        }

        private void CloseAfterClickOutside()
        {
            if (_closing || _closed || _runningRecipe != null)
            {
                return;
            }

            ClosePreviewViewer();
            Close();
        }

        private const uint GaRoot = 2;
        private const uint GwOwner = 4;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePointStruct
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(NativePointStruct point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hwnd, uint command);

        /// <summary>
        /// Like a context menu: a click outside of the window (and of its viewer) closes it. Not while a recipe changes the capture, it
        /// may ask something in a window of its own.
        /// </summary>
        private void CloseWhenClickedOutside()
        {
            if (Options.CloseOnDeactivate != true || _closing || _closed)
            {
                return;
            }

            // Decided once the activation moved on: to the viewer of the preview (it belongs to this window), or somewhere else
            Dispatcher.InvokeAsync(() =>
            {
                if (_closing || _closed || _runningRecipe != null || IsActive || _previewViewer?.IsActive == true)
                {
                    return;
                }

                ClosePreviewViewer();
                Close();
            }, DispatcherPriority.Background).Task.FireAndLog("Close the destination window after a click outside of it");
        }

        private void Choose(DestinationTileViewModel tile)
        {
            SelectedDestination = tile.Destination;
            DialogResult = true;
            Close();
        }

        private void RunRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedRecipe != null)
            {
                ApplyRecipeAsync(SelectedRecipe.EditorTrigger).FireAndLog("Change the capture with a recipe");
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// An entry of the recipe list: an editor recipe which changes the capture, with the text of the editor menu
        /// </summary>
        public sealed class RecipeChoice
        {
            public RecipeChoice(string name, IEditorTrigger editorTrigger)
            {
                Name = name;
                EditorTrigger = editorTrigger;
            }

            public string Name { get; }
            public IEditorTrigger EditorTrigger { get; }
        }
    }
}
