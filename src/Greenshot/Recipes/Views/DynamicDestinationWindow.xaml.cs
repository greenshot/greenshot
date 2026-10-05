/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;
using Greenshot.Configuration;
using Greenshot.Editor.Destinations;
using Greenshot.Base.Threading;
using Greenshot.Base.Languages;
using Greenshot.Recipes.ViewModels;

namespace Greenshot.Recipes.Views
{

    /// <summary>
    /// Modern WPF-styled interactive export dialog that presents a capture preview thumbnail,
    /// core destinations directly under the thumbnail, compact other destinations, search filtering,
    /// dynamic translation updates, fullscreen preview zoom transition, and rich error recovery.
    /// </summary>
    public partial class DynamicDestinationWindow : Window, INotifyPropertyChanged
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private readonly DispatcherTimer _timer;
        private int _secondsRemaining;
        private string _searchText = "";
        private bool _isFullPreviewOpen;
        private string _customTitle;

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

        public ObservableCollection<CaptureRecipe> AvailableRecipes { get; } = new ObservableCollection<CaptureRecipe>();
        public CaptureRecipe SelectedRecipe { get; set; }
        public Visibility RecipeForwardingVisibility => AvailableRecipes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

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
            ? string.Format("Auto-closing in {0}s...", _secondsRemaining) 
            : "";

        public IDestination SelectedDestination { get; private set; }
        public CaptureRecipe SelectedRecipeToForward { get; private set; }
        public bool OpenInEditorRequested { get; private set; }

        // Localized UI text strings
        public string CoreDestinationsHeaderText => "Core Destinations";
        public string OtherDestinationsHeaderText => "Destinations";
        public string ForwardToRecipeHeaderText => Texts.Core.ContextmenuRecipeeditor;
        public string RunRecipeButtonText => "▶ Run Recipe";
        public string DismissButtonText => Texts.Core.Cancel;
        public string CloseButtonToolTip => Texts.Editor.Close;
        public string ClickToPreviewTooltip => "Click to view full preview (Esc to exit)";
        public string FullscreenReturnHintText => "Click anywhere or press Esc to return";
        public string ErrorSubtitleText => "Select an alternative destination below, forward to another recipe, or retry:";

        public string ThemeToggleIcon => WpfThemeHelper.IsDarkMode ? "☀️" : "🌙";
        public string ThemeToggleToolTip => WpfThemeHelper.IsDarkMode 
            ? "Switch to Light Mode (T)" 
            : "Switch to Dark Mode (T)";

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
            Image previewImage,
            IEnumerable<IDestination> destinations,
            IEnumerable<CaptureRecipe> recipes = null,
            string errorMessage = null,
            int timeoutSeconds = 0)
        {
            InitializeComponent();
            DataContext = this;

            _customTitle = title;
            ErrorMessage = errorMessage;

            if (previewImage != null)
            {
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
            }

            if (destinations != null)
            {
                // Ensure Editor destination is present among destinations for the core list
                var destList = destinations.Where(d => d != null && !string.Equals(d.Designation, "Picker", StringComparison.OrdinalIgnoreCase)).ToList();
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
                        badge = "Retry";
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

            if (recipes != null)
            {
                foreach (var r in recipes)
                {
                    if (r != null && r.IsEnabled)
                    {
                        AvailableRecipes.Add(r);
                    }
                }
                SelectedRecipe = AvailableRecipes.FirstOrDefault();
            }

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
                WpfThemeHelper.ThemeChanged -= OnThemeChanged;
                Texts.Config.LanguageChanged -= OnLanguageChanged;
            };
            KeyDown += OnWindowKeyDown;
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
                    return (Texts.Settings.DestinationClipboard, "Copy image to clipboard");
                case "editor":
                    return (Texts.Editor.Title, "Annotate and export");
                case "fileno_dialog":
                case "filenodialog":
                case "file":
                    return ("Save Directly", "Save to folder");
                case "filedialog":
                case "fileas":
                    return ("Save As...", "Choose folder & format");
                case "printer":
                    return (Texts.Settings.DestinationPrinter, "Send to printer");
                case "windows10ocr":
                case "ocr":
                    return ("OCR Text", "Extract text from image");
                case "windows10share":
                case "share":
                    return ("Windows Share", "Share with apps & contacts");
                case "mail":
                case "outlook":
                    return (Texts.Editor.Email, "New email attachment");
                case "word":
                    return ("Microsoft Word", "Insert into document");
                case "excel":
                    return ("Microsoft Excel", "Insert into worksheet");
                case "powerpoint":
                    return ("PowerPoint", "Insert into presentation");
                case "onenote":
                    return ("OneNote", "Send to notebook");
                case "zxing":
                case "zxingqrdestination":
                    return ("QR Code Actions", "Detect & decode QR codes");
                case "paint":
                    return ("MS Paint", "Open in Paint");
                case "dropbox":
                    return ("Dropbox", "Upload & copy link");
                case "box":
                    return ("Box", "Upload & copy link");
                case "imgur":
                    return ("Imgur", "Upload & copy link");
                case "jira":
                    return ("Jira", "Attach to issue");
                case "confluence":
                    return ("Confluence", "Attach to page");
            }

            if (rawDesc.StartsWith("Upload to ", StringComparison.OrdinalIgnoreCase))
            {
                return (rawDesc.Substring(10), "Cloud upload");
            }

            int parenIdx = rawDesc.IndexOf('(');
            if (parenIdx > 0 && rawDesc.EndsWith(")"))
            {
                string t = rawDesc.Substring(0, parenIdx).Trim();
                string s = rawDesc.Substring(parenIdx + 1, rawDesc.Length - parenIdx - 2).Trim();
                return (t, s);
            }

            return (rawDesc, "Export destination");
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
            OnPropertyChanged(nameof(ForwardToRecipeHeaderText));
            OnPropertyChanged(nameof(RunRecipeButtonText));
            OnPropertyChanged(nameof(DismissButtonText));
            OnPropertyChanged(nameof(CloseButtonToolTip));
            OnPropertyChanged(nameof(ClickToPreviewTooltip));
            OnPropertyChanged(nameof(FullscreenReturnHintText));
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
            try
            {
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    int useImmersiveDarkMode = WpfThemeHelper.IsDarkMode ? 1 : 0;
                    int hr = DwmSetWindowAttribute(helper.Handle, 20, ref useImmersiveDarkMode, sizeof(int));
                    if (hr != 0)
                    {
                        DwmSetWindowAttribute(helper.Handle, 19, ref useImmersiveDarkMode, sizeof(int));
                    }
                }
            }
            catch
            {
                // Silently ignore if DWM call is unsupported on older OS
            }
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

        #region Fullscreen Preview Zoom Transition

        private void Thumbnail_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && PreviewImageSource != null)
            {
                OpenFullscreenPreview();
            }
        }

        private void FullPreviewOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                CloseFullscreenPreview();
            }
        }

        private void FullPreviewClose_Click(object sender, RoutedEventArgs e)
        {
            CloseFullscreenPreview();
        }

        private void OpenFullscreenPreview()
        {
            _isFullPreviewOpen = true;
            FullPreviewOverlay.Visibility = Visibility.Visible;
            FullPreviewOverlay.Opacity = 0.0;
            PreviewScale.ScaleX = 0.88;
            PreviewScale.ScaleY = 0.88;

            var ease = new CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };

            var fadeAnim = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease };
            var scaleXAnim = new DoubleAnimation(0.88, 1.0, TimeSpan.FromMilliseconds(240)) { EasingFunction = ease };
            var scaleYAnim = new DoubleAnimation(0.88, 1.0, TimeSpan.FromMilliseconds(240)) { EasingFunction = ease };

            FullPreviewOverlay.BeginAnimation(OpacityProperty, fadeAnim);
            PreviewScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            PreviewScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
        }

        private void CloseFullscreenPreview()
        {
            if (!_isFullPreviewOpen) return;

            var ease = new CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn };

            var fadeAnim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease };
            var scaleXAnim = new DoubleAnimation(1.0, 0.88, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease };
            var scaleYAnim = new DoubleAnimation(1.0, 0.88, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease };

            fadeAnim.Completed += (s, e) =>
            {
                FullPreviewOverlay.Visibility = Visibility.Collapsed;
                _isFullPreviewOpen = false;
            };

            FullPreviewOverlay.BeginAnimation(OpacityProperty, fadeAnim);
            PreviewScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);
            PreviewScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
        }

        #endregion

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    if (_isFullPreviewOpen)
                    {
                        CloseFullscreenPreview();
                    }
                    else
                    {
                        DialogResult = false;
                        Close();
                    }
                    e.Handled = true;
                    break;
                case Key.T when Keyboard.Modifiers == ModifierKeys.None:
                    WpfThemeHelper.ToggleTheme();
                    e.Handled = true;
                    break;
            }
        }

        private void DestinationTile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is DestinationTileViewModel vm)
            {
                SelectedDestination = vm.Destination;
                if (string.Equals(vm.Destination?.Designation, EditorDestination.DESIGNATION, StringComparison.OrdinalIgnoreCase))
                {
                    OpenInEditorRequested = true;
                }
                DialogResult = true;
                Close();
            }
        }

        private void RunRecipe_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedRecipe != null)
            {
                SelectedRecipeToForward = SelectedRecipe;
                DialogResult = true;
                Close();
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
    }
}
