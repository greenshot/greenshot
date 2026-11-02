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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Base.Wpf;
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
    /// Modern WPF-styled interactive export dialog that presents a capture preview thumbnail,
    /// core destinations directly under the thumbnail, compact other destinations, search filtering,
    /// dynamic translation updates, the capture at full size in a viewer, and rich error recovery.
    /// </summary>
    public partial class DynamicDestinationWindow : Window, INotifyPropertyChanged
    {

        private readonly DispatcherTimer _timer;
        private int _secondsRemaining;
        private string _searchText = "";
        private ViewerWindow _previewViewer;
        private string _customTitle;
        private EditableCapture _capture;
        private CaptureFlowHandle _runningRecipe;

        public event PropertyChangedEventHandler PropertyChanged;

        public string WindowTitle => $"Greenshot - {HeaderTitle}";
        public string HeaderTitle => !string.IsNullOrWhiteSpace(_customTitle) ? _customTitle : Texts.Settings.Destination;
        public string ErrorMessage { get; set; }
        public Visibility ErrorBannerVisibility => !string.IsNullOrWhiteSpace(ErrorMessage) ? Visibility.Visible : Visibility.Collapsed;

        public BitmapSource PreviewImageSource { get; set; }
        public Visibility ThumbnailVisibility => PreviewImageSource != null ? Visibility.Visible : Visibility.Collapsed;
        public string DimensionsText { get; set; }

        // Core Destinations (under the thumbnail) & Other Destinations (in the right panel)
        public ObservableCollection<DestinationTileViewModel> CoreDestinationTiles { get; } = new ObservableCollection<DestinationTileViewModel>();
        public ObservableCollection<DestinationTileViewModel> OtherDestinationTiles { get; } = new ObservableCollection<DestinationTileViewModel>();
        public ObservableCollection<DestinationTileViewModel> FilteredOtherDestinationTiles { get; } = new ObservableCollection<DestinationTileViewModel>();

        /// <summary>
        /// The editor recipes which change the capture
        /// </summary>
        public ObservableCollection<RecipeChoice> AvailableRecipes { get; } = new ObservableCollection<RecipeChoice>();
        public RecipeChoice SelectedRecipe { get; set; }
        public Visibility RecipeSectionVisibility => AvailableRecipes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public string OtherDestinationCountText => $"({FilteredOtherDestinationTiles.Count})";

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    UpdateFilteredDestinations();
                }
            }
        }

        public string TimeoutText => _secondsRemaining > 0 
            ? string.Format(Texts.Recipe.PickerAutoClose, _secondsRemaining) 
            : "";

        public IDestination SelectedDestination { get; private set; }

        // Localized UI text strings
        public string CoreDestinationsHeaderText => Texts.Recipe.PickerCoreDestinations;
        public string OtherDestinationsHeaderText => Texts.Recipe.PickerDestinations;
        public string RecipeHeaderText => Texts.Recipe.PickerApplyRecipe;
        public string RunRecipeButtonText => "▶ " + Texts.Recipe.PickerApply;
        public string DismissButtonText => Texts.Core.Cancel;
        public string CloseButtonToolTip => Texts.Editor.Close;
        public string ClickToPreviewTooltip => Texts.Recipe.PickerPreviewTooltip;
        public string ErrorSubtitleText => Texts.Recipe.PickerErrorChooseAgain;

        public string ThemeToggleIcon => WpfThemeHelper.IsDarkMode ? "☀️" : "🌙";
        public string ThemeToggleToolTip => WpfThemeHelper.IsDarkMode 
            ? Texts.Recipe.PickerThemeLight
            : Texts.Recipe.PickerThemeDark;

        // Theme brushes bound to XAML
        public SolidColorBrush WindowBackgroundBrush => WpfThemeHelper.WindowBackground;
        public SolidColorBrush CardBackgroundBrush => WpfThemeHelper.CardBackground;
        public SolidColorBrush CardBorderBrush => WpfThemeHelper.CardBorder;
        public SolidColorBrush TextPrimaryBrush => WpfThemeHelper.TextPrimary;
        public SolidColorBrush TextSecondaryBrush => WpfThemeHelper.TextSecondary;
        public SolidColorBrush AccentBrush => WpfThemeHelper.Accent;
        public SolidColorBrush BadgeBackgroundBrush => WpfThemeHelper.BadgeBackground;
        public SolidColorBrush ErrorBackgroundBrush => WpfThemeHelper.ErrorBackground;
        public SolidColorBrush ErrorBorderBrush => WpfThemeHelper.ErrorBorder;
        public SolidColorBrush ErrorTextBrush => WpfThemeHelper.ErrorText;

        public SolidColorBrush HoverBackgroundBrush => WpfThemeHelper.IsDarkMode
            ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x24, 0x0A, 0x84, 0xFF))
            : new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x14, 0x00, 0x7A, 0xFF));

        public SolidColorBrush PressedBackgroundBrush => WpfThemeHelper.IsDarkMode
            ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x38, 0x0A, 0x84, 0xFF))
            : new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x28, 0x00, 0x7A, 0xFF));

        public DynamicDestinationWindow(
            string title,
            IEnumerable<IDestination> destinations,
            string errorMessage = null,
            int timeoutSeconds = 0)
        {
            InitializeComponent();
            DataContext = this;

            _customTitle = title;
            ErrorMessage = errorMessage;

            if (destinations != null)
            {
                // Ensure Editor destination is present among destinations for the core list
                var destList = destinations.Where(d => d != null && !DestinationHelper.IsChooser(d.Designation)).ToList();
                if (!destList.Any(d => string.Equals(d.Designation, EditorDestination.DESIGNATION, StringComparison.OrdinalIgnoreCase)))
                {
                    var editorDest = DestinationHelper.GetDestination(EditorDestination.DESIGNATION);
                    if (editorDest != null)
                    {
                        destList.Insert(0, editorDest);
                    }
                }

                foreach (var dest in destList)
                {
                    string badge = null;
                    if (!string.IsNullOrEmpty(errorMessage) && string.Equals(dest.Designation, "Clipboard", StringComparison.OrdinalIgnoreCase))
                    {
                        badge = Texts.Recipe.PickerRetry;
                    }

                    var (destTitle, destSubtitle) = FormatDestinationNames(dest);

                    var tile = new DestinationTileViewModel
                    {
                        Destination = dest,
                        Title = destTitle,
                        Subtitle = destSubtitle,
                        BadgeText = badge
                    };
                    // The icon arrives asynchronously (started on the UI thread, set there)
                    tile.LoadIconAsync().FireAndLog("Load a destination icon");

                    if (IsCoreDestination(dest))
                    {
                        CoreDestinationTiles.Add(tile);
                    }
                    else
                    {
                        OtherDestinationTiles.Add(tile);
                    }
                }
            }

            // Order core destinations in logical Greenshot order: Editor, Clipboard, Save As, Save, Print, OCR, Share
            SortCoreDestinations();

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

            WpfThemeHelper.ThemeChanged += OnThemeChanged;
            Texts.Config.LanguageChanged += OnLanguageChanged;

            SourceInitialized += (s, e) => ApplyImmersiveDarkMode();
            Closed += (s, e) =>
            {
                _timer?.Stop();
                // A recipe which still runs changes nothing anymore
                _runningRecipe?.Cancel();
                ClosePreviewViewer();
                if (_capture != null)
                {
                    _capture.Changed -= OnCaptureChanged;
                }
                WpfThemeHelper.ThemeChanged -= OnThemeChanged;
                Texts.Config.LanguageChanged -= OnLanguageChanged;
            };
            KeyDown += OnWindowKeyDown;
        }

        /// <summary>
        /// Shows the image as preview, with its size
        /// </summary>
        private void SetPreview(Image previewImage)
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
        /// The capture can be changed with the editor recipes, the window shows it as preview
        /// </summary>
        private void UseCapture(EditableCapture capture, bool showPreview)
        {
            _capture = capture;
            if (showPreview)
            {
                _capture.Changed += OnCaptureChanged;
                ShowCapture();
            }
            foreach (var trigger in EditableCapture.GetEditorRecipes())
            {
                AvailableRecipes.Add(new RecipeChoice(EditableCapture.DisplayName(trigger), trigger));
            }
            SelectedRecipe = AvailableRecipes.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedRecipe));
            OnPropertyChanged(nameof(RecipeSectionVisibility));
        }

        private void ShowCapture()
        {
            using var image = _capture.GetImageForExport();
            SetPreview(image);
        }

        private void OnCaptureChanged(object sender, EventArgs e)
        {
            // The viewer shows a copy, which isn't the capture anymore
            ClosePreviewViewer();
            ShowCapture();
        }

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

            _runningRecipe = handle;
            IsEnabled = false;
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
                IsEnabled = true;
                RunRecipeButton.Focus();
            }
        }

        private static bool IsCoreDestination(IDestination dest)
        {
            if (dest == null) return false;
            string des = dest.Designation ?? "";
            string name = dest.Descriptor?.DisplayName ?? des;

            return string.Equals(des, EditorDestination.DESIGNATION, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(des, nameof(WellKnownDestinations.Clipboard), StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(des, nameof(WellKnownDestinations.FileDialog), StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(des, nameof(WellKnownDestinations.FileNoDialog), StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(des, nameof(WellKnownDestinations.Printer), StringComparison.OrdinalIgnoreCase) ||
                   des.IndexOf("ocr", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("ocr", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   des.IndexOf("share", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("share", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SortCoreDestinations()
        {
            var ordered = CoreDestinationTiles.OrderBy(t =>
            {
                string des = t.Destination?.Designation ?? "";
                if (string.Equals(des, EditorDestination.DESIGNATION, StringComparison.OrdinalIgnoreCase)) return 1;
                if (string.Equals(des, nameof(WellKnownDestinations.Clipboard), StringComparison.OrdinalIgnoreCase)) return 2;
                if (string.Equals(des, nameof(WellKnownDestinations.FileDialog), StringComparison.OrdinalIgnoreCase)) return 3;
                if (string.Equals(des, nameof(WellKnownDestinations.FileNoDialog), StringComparison.OrdinalIgnoreCase)) return 4;
                if (string.Equals(des, nameof(WellKnownDestinations.Printer), StringComparison.OrdinalIgnoreCase)) return 5;
                if (des.IndexOf("ocr", StringComparison.OrdinalIgnoreCase) >= 0) return 6;
                if (des.IndexOf("share", StringComparison.OrdinalIgnoreCase) >= 0) return 7;
                return 10;
            }).ToList();

            CoreDestinationTiles.Clear();
            foreach (var item in ordered)
            {
                CoreDestinationTiles.Add(item);
            }
        }

        private static (string title, string subtitle) FormatDestinationNames(IDestination dest)
        {
            string des = dest.Designation ?? "";
            string rawDesc = dest.Descriptor?.DisplayName ?? des;

            switch (des.ToLowerInvariant())
            {
                case "clipboard":
                    return (Texts.Settings.DestinationClipboard, Texts.Recipe.PickerClipboardSub);
                case "editor":
                    return (Texts.Editor.Title, Texts.Recipe.PickerEditorSub);
                case "fileno_dialog":
                case "filenodialog":
                case "file":
                    return (Texts.Recipe.PickerFile, Texts.Recipe.PickerFileSub);
                case "filedialog":
                case "fileas":
                    return (Texts.Recipe.PickerFileas, Texts.Recipe.PickerFileasSub);
                case "printer":
                    return (Texts.Settings.DestinationPrinter, Texts.Recipe.PickerPrinterSub);
                case "windows10ocr":
                case "ocr":
                    return (Texts.Recipe.PickerOcr, Texts.Recipe.PickerOcrSub);
                case "windows10share":
                case "share":
                    return (Texts.Recipe.PickerShare, Texts.Recipe.PickerShareSub);
                case "mail":
                case "outlook":
                    return (Texts.Editor.Email, Texts.Recipe.PickerEmailSub);
                case "word":
                    return ("Microsoft Word", Texts.Recipe.PickerWordSub);
                case "excel":
                    return ("Microsoft Excel", Texts.Recipe.PickerExcelSub);
                case "powerpoint":
                    return ("PowerPoint", Texts.Recipe.PickerPowerpointSub);
                case "onenote":
                    return ("OneNote", Texts.Recipe.PickerOnenoteSub);
                case "zxing":
                case "zxingqrdestination":
                    return (Texts.Recipe.PickerQr, Texts.Recipe.PickerQrSub);
                case "paint":
                    return ("MS Paint", Texts.Recipe.PickerPaintSub);
                case "dropbox":
                    return ("Dropbox", Texts.Recipe.PickerUploadSub);
                case "box":
                    return ("Box", Texts.Recipe.PickerUploadSub);
                case "jira":
                    return ("Jira", Texts.Recipe.PickerJiraSub);
                case "confluence":
                    return ("Confluence", Texts.Recipe.PickerConfluenceSub);
            }

            if (rawDesc.StartsWith("Upload to ", StringComparison.OrdinalIgnoreCase))
            {
                return (rawDesc.Substring(10), Texts.Recipe.PickerCloudSub);
            }

            int parenIdx = rawDesc.IndexOf('(');
            if (parenIdx > 0 && rawDesc.EndsWith(")"))
            {
                string t = rawDesc.Substring(0, parenIdx).Trim();
                string s = rawDesc.Substring(parenIdx + 1, rawDesc.Length - parenIdx - 2).Trim();
                return (t, s);
            }

            return (rawDesc, Texts.Recipe.PickerExportSub);
        }

        private void UpdateFilteredDestinations()
        {
            FilteredOtherDestinationTiles.Clear();
            string filter = _searchText?.Trim() ?? "";

            foreach (var tile in OtherDestinationTiles)
            {
                if (string.IsNullOrEmpty(filter) ||
                    tile.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (tile.Subtitle != null && tile.Subtitle.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (tile.Destination?.Designation != null && tile.Destination.Designation.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    FilteredOtherDestinationTiles.Add(tile);
                }
            }

            OnPropertyChanged(nameof(OtherDestinationCountText));
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(CoreDestinationsHeaderText));
            OnPropertyChanged(nameof(OtherDestinationsHeaderText));
            OnPropertyChanged(nameof(RecipeHeaderText));
            OnPropertyChanged(nameof(RunRecipeButtonText));
            OnPropertyChanged(nameof(DismissButtonText));
            OnPropertyChanged(nameof(CloseButtonToolTip));
            OnPropertyChanged(nameof(ClickToPreviewTooltip));
            OnPropertyChanged(nameof(ErrorSubtitleText));
            OnPropertyChanged(nameof(ThemeToggleToolTip));
            OnPropertyChanged(nameof(TimeoutText));
        }

        private void OnThemeChanged()
        {
            ApplyImmersiveDarkMode();
            NotifyAllPropertiesChanged();
            foreach (var tile in CoreDestinationTiles)
            {
                tile.NotifyThemeChanged();
            }
            foreach (var tile in OtherDestinationTiles)
            {
                tile.NotifyThemeChanged();
            }
        }

        private void NotifyAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(WindowBackgroundBrush));
            OnPropertyChanged(nameof(CardBackgroundBrush));
            OnPropertyChanged(nameof(CardBorderBrush));
            OnPropertyChanged(nameof(TextPrimaryBrush));
            OnPropertyChanged(nameof(TextSecondaryBrush));
            OnPropertyChanged(nameof(AccentBrush));
            OnPropertyChanged(nameof(BadgeBackgroundBrush));
            OnPropertyChanged(nameof(ErrorBackgroundBrush));
            OnPropertyChanged(nameof(ErrorBorderBrush));
            OnPropertyChanged(nameof(ErrorTextBrush));
            OnPropertyChanged(nameof(HoverBackgroundBrush));
            OnPropertyChanged(nameof(PressedBackgroundBrush));
            OnPropertyChanged(nameof(ThemeToggleIcon));
            OnPropertyChanged(nameof(ThemeToggleToolTip));
        }

        private void ApplyImmersiveDarkMode()
        {
            // The title bar in the colors of the theme, it follows theme changes from now on
            WindowFrameTheme.Attach(this);
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void OnThemeToggleClicked(object sender, RoutedEventArgs e)
        {
            WpfThemeHelper.ToggleTheme();
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

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    DialogResult = false;
                    Close();
                    e.Handled = true;
                    break;
                case Key.T when Keyboard.Modifiers == ModifierKeys.None:
                    WpfThemeHelper.ToggleTheme();
                    e.Handled = true;
                    break;
            }
        }

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
        /// <param name="showPreview">Show the capture</param>
        /// <returns>The chosen destination, or null when the window was closed</returns>
        public static IDestination Choose(Window owner, EditableCapture capture, IEnumerable<IDestination> destinations, string errorMessage,
            CancellationToken cancellationToken = default, string title = null, int timeoutSeconds = 0, bool showPreview = true)
        {
            var window = new DynamicDestinationWindow(title, destinations, errorMessage, timeoutSeconds);
            window.UseCapture(capture, showPreview);
            if (owner != null)
            {
                window.Owner = owner;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

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

        private void DestinationTile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is DestinationTileViewModel vm)
            {
                SelectedDestination = vm.Destination;
                DialogResult = true;
                Close();
            }
        }

        private void RunRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedRecipe != null)
            {
                ApplyRecipeAsync(SelectedRecipe.EditorTrigger).FireAndLog("Change the capture with a recipe");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
