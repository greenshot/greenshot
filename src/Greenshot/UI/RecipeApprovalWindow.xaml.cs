/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows;
using System.Windows.Media;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Recipes;

namespace Greenshot.UI
{
    public class TriggerBadgeModel
    {
        public string Text { get; set; }
        public Brush ForegroundBrush { get; set; }
        public Brush BackgroundBrush { get; set; }
        public Brush BorderBrush { get; set; }
    }

    public enum RecipeApprovalMode
    {
        NewRecipe,
        Modified,
        ReVerify
    }

    /// <summary>
    /// Modern WPF dialog for reviewing and approving external capture recipes with dark/light mode support.
    /// </summary>
    public partial class RecipeApprovalWindow : Window
    {
        public string RecipeName { get; set; }
        public string RecipeVersion { get; set; }
        public string RecipeDescription { get; set; }
        public string RecipeId { get; set; }
        public string FilePath { get; set; }
        public string FileHash { get; set; }

        public RecipeApprovalMode ApprovalMode { get; private set; }
        public bool IsModified => ApprovalMode == RecipeApprovalMode.Modified;
        public bool IsNewRecipe => ApprovalMode == RecipeApprovalMode.NewRecipe;

        public string WindowTitleSubtitle { get; private set; }
        public string WindowFullTitle => $"Greenshot{WindowTitleSubtitle}";
        public string HeaderIcon { get; private set; }
        public string HeaderTitle { get; private set; }
        public string HeaderDescription { get; private set; }
        public string StatusBadgeText { get; private set; }
        public SolidColorBrush StatusBadgeBackgroundBrush { get; private set; }
        public SolidColorBrush StatusBadgeBorderBrush { get; private set; }
        public SolidColorBrush StatusBadgeForegroundBrush { get; private set; }
        public string PreviousApprovalDate { get; private set; }
        public string PreviousFileHash { get; private set; }
        public string ApproveButtonText { get; private set; }
        public Visibility ModificationWarningVisibility => IsModified ? Visibility.Visible : Visibility.Collapsed;
        public Visibility WarningIconVisibility => IsModified ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShieldIconVisibility => !IsModified ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<TriggerBadgeModel> TriggerBadges { get; } = new ObservableCollection<TriggerBadgeModel>();
        public ObservableCollection<string> StepDescriptions { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ExternalCommandsList { get; } = new ObservableCollection<string>();

        public Visibility ExternalCommandWarningVisibility => HasExternalCommands ? Visibility.Visible : Visibility.Collapsed;
        public bool HasExternalCommands { get; set; }

        public bool IsApproved { get; private set; }
        public bool AllowExternalCommands { get; private set; }

        // Theme brushes for WPF binding
        public SolidColorBrush WindowBackgroundBrush => WpfThemeHelper.WindowBackground;
        public SolidColorBrush CardBackgroundBrush => WpfThemeHelper.CardBackground;
        public SolidColorBrush CardBorderBrush => WpfThemeHelper.CardBorder;
        public SolidColorBrush TextPrimaryBrush => WpfThemeHelper.TextPrimary;
        public SolidColorBrush TextSecondaryBrush => WpfThemeHelper.TextSecondary;
        public SolidColorBrush AccentBrush => WpfThemeHelper.Accent;
        public SolidColorBrush WarningBackgroundBrush => WpfThemeHelper.WarningBackground;
        public SolidColorBrush WarningBorderBrush => WpfThemeHelper.WarningBorder;
        public SolidColorBrush WarningTextBrush => WpfThemeHelper.WarningText;
        public SolidColorBrush BadgeBackgroundBrush => WpfThemeHelper.BadgeBackground;

        public RecipeApprovalWindow(CaptureRecipe recipe, string filePath, RecipeValidationResult validationResult = null, RecipeTrustRecord previousTrustRecord = null)
        {
            InitializeComponent();

            RecipeName = recipe?.Name ?? "Unnamed Recipe";
            RecipeVersion = string.IsNullOrWhiteSpace(recipe?.Version) ? "v1.0" : $"v{recipe.Version}";
            RecipeDescription = string.IsNullOrWhiteSpace(recipe?.Description) ? "No description provided." : recipe.Description;
            RecipeId = recipe?.Id ?? "unknown";
            FilePath = filePath ?? "Unknown file path";
            FileHash = RecipeTrustStore.ComputeSha256(filePath) ?? "Unknown";

            var prev = previousTrustRecord ?? RecipeTrustStore.GetTrustRecord(filePath);
            if (prev != null && !string.Equals(prev.Sha256Hash, FileHash, StringComparison.OrdinalIgnoreCase))
            {
                ApprovalMode = RecipeApprovalMode.Modified;
                WindowTitleSubtitle = " — Recipe Modification Detected";
                HeaderIcon = "⚠️";
                HeaderTitle = "Recipe File Modified on Disk";
                HeaderDescription = "This capture recipe was previously approved, but its file content has been modified on disk since it was last approved. Review the updated configuration and changes below before re-approving.";
                StatusBadgeText = "MODIFIED ON DISK";
                StatusBadgeBackgroundBrush = WarningBackgroundBrush;
                StatusBadgeBorderBrush = WarningBorderBrush;
                StatusBadgeForegroundBrush = WarningTextBrush;
                PreviousApprovalDate = prev.ApprovedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                PreviousFileHash = prev.Sha256Hash;
                ApproveButtonText = "Approve Changes";
            }
            else if (prev != null)
            {
                ApprovalMode = RecipeApprovalMode.ReVerify;
                WindowTitleSubtitle = " — Recipe Security Review";
                HeaderIcon = "🛡️";
                HeaderTitle = "Capture Recipe Review";
                HeaderDescription = "Reviewing registration, triggers, and execution permissions for this capture recipe.";
                StatusBadgeText = "ALREADY APPROVED";
                StatusBadgeBackgroundBrush = BadgeBackgroundBrush;
                StatusBadgeBorderBrush = CardBorderBrush;
                StatusBadgeForegroundBrush = TextSecondaryBrush;
                PreviousApprovalDate = prev.ApprovedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                PreviousFileHash = prev.Sha256Hash;
                ApproveButtonText = "Confirm & Enable";
            }
            else
            {
                ApprovalMode = RecipeApprovalMode.NewRecipe;
                WindowTitleSubtitle = " — Recipe Security Approval";
                HeaderIcon = "🛡️";
                HeaderTitle = "External Capture Recipe Detected";
                HeaderDescription = "A new capture recipe file is requesting to be registered into Greenshot. Review its details, triggers, and execution steps before approving.";
                StatusBadgeText = "NEW RECIPE";
                StatusBadgeBackgroundBrush = BadgeBackgroundBrush;
                StatusBadgeBorderBrush = CardBorderBrush;
                StatusBadgeForegroundBrush = AccentBrush;
                PreviousApprovalDate = null;
                PreviousFileHash = null;
                ApproveButtonText = "Approve & Enable";
            }

            DataContext = this;
            Background = WindowBackgroundBrush;
            Title = WindowFullTitle;

            // Populate Trigger badges
            if (recipe?.Triggers != null && recipe.Triggers.Count > 0)
            {
                foreach (var trigger in recipe.Triggers)
                {
                    string label;
                    if (string.Equals(trigger.TriggerType, TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase))
                    {
                        label = $"⌨ Hotkey: {trigger.GetParameter<string>("Hotkey", "None")}";
                    }
                    else if (string.Equals(trigger.TriggerType, TriggerConfig.TypeContextMenu, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(trigger.TriggerType, TriggerConfig.TypeSystray, StringComparison.OrdinalIgnoreCase))
                    {
                        label = $"📋 Systray: \"{trigger.GetParameter<string>("MenuItemText", recipe.Name)}\"";
                    }
                    else if (string.Equals(trigger.TriggerType, TriggerConfig.TypeClipboard, StringComparison.OrdinalIgnoreCase))
                    {
                        label = "📋 Clipboard Monitor";
                    }
                    else
                    {
                        label = $"{trigger.TriggerType}: {trigger.Name}";
                    }

                    TriggerBadges.Add(new TriggerBadgeModel
                    {
                        Text = label,
                        ForegroundBrush = TextPrimaryBrush,
                        BackgroundBrush = BadgeBackgroundBrush,
                        BorderBrush = CardBorderBrush
                    });
                }
            }
            else
            {
                TriggerBadges.Add(new TriggerBadgeModel
                {
                    Text = "ℹ Manual / Triggerless (Invoked via CLI / API)",
                    ForegroundBrush = TextSecondaryBrush,
                    BackgroundBrush = BadgeBackgroundBrush,
                    BorderBrush = CardBorderBrush
                });
            }

            // Populate Step descriptions
            if (recipe?.Steps != null)
            {
                for (int i = 0; i < recipe.Steps.Count; i++)
                {
                    var s = recipe.Steps[i];
                    string paramSummary = "";
                    if (string.Equals(s.StepType, WellKnownStepTypes.Border, StringComparison.OrdinalIgnoreCase))
                    {
                        paramSummary = $" ({s.GetParameter<int>("Width", 2)}px, {s.GetParameter<string>("Color", "Black")})";
                    }
                    else if (string.Equals(s.StepType, WellKnownStepTypes.Source, StringComparison.OrdinalIgnoreCase))
                    {
                        paramSummary = $" [{s.GetParameter<string>("SourceType", "Region")}]";
                    }
                    else if (string.Equals(s.StepType, WellKnownStepTypes.Destinations, StringComparison.OrdinalIgnoreCase))
                    {
                        var dests = s.GetParameter<List<string>>("DestinationDesignations");
                        if (dests != null && dests.Count > 0) paramSummary = $" -> [{string.Join(", ", dests)}]";
                    }

                    StepDescriptions.Add($"{i + 1}. {s.StepType}{paramSummary}");
                }
            }

            // External Command handling
            if (validationResult != null && validationResult.HasExternalCommands)
            {
                HasExternalCommands = true;
                foreach (var cmd in validationResult.ExternalCommands)
                {
                    ExternalCommandsList.Add(cmd);
                }
            }

            // If external commands exist, require authorization before enabling Approve
            if (HasExternalCommands)
            {
                BtnApprove.IsEnabled = false;
            }
        }

        private void OnAuthorizeChecked(object sender, RoutedEventArgs e)
        {
            if (HasExternalCommands)
            {
                BtnApprove.IsEnabled = ChkAuthorizeExternalCommands.IsChecked == true;
            }
        }

        private void OnCloseTitleBarClicked(object sender, RoutedEventArgs e)
        {
            IsApproved = false;
            DialogResult = false;
            Close();
        }

        private void OnTitleBarMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                DragMove();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyImmersiveDarkMode();

            Topmost = true;
            Activate();
            Focus();

            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero)
            {
                SetForegroundWindow(helper.Handle);
                BringWindowToTop(helper.Handle);
            }
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            CenterWindowOnScreen();
            // Switch to manual sizing after initial layout so the user can freely resize
            SizeToContent = SizeToContent.Manual;
            if (ContentScrollViewer != null)
            {
                ContentScrollViewer.ClearValue(MaxHeightProperty);
            }
        }

        private void CenterWindowOnScreen()
        {
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    var screen = System.Windows.Forms.Screen.FromHandle(helper.Handle);
                    var bounds = screen.WorkingArea;
                    Left = bounds.Left + (bounds.Width - ActualWidth) / 2;
                    Top = bounds.Top + (bounds.Height - ActualHeight) / 2;
                    return;
                }
            }
            catch
            {
                // Fallback
            }

            try
            {
                var workArea = SystemParameters.WorkArea;
                Left = workArea.Left + (workArea.Width - ActualWidth) / 2;
                Top = workArea.Top + (workArea.Height - ActualHeight) / 2;
            }
            catch
            {
                // Silently ignore
            }
        }

        private void OnOpenInExplorerClicked(object sender, RoutedEventArgs e)
        {
            OpenInExplorer(FilePath);
        }

        private void OnViewFileClicked(object sender, RoutedEventArgs e)
        {
            OpenRecipeFile(FilePath);
        }

        public static void OpenInExplorer(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath)) return;

                string fullPath = System.IO.Path.GetFullPath(filePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
                }
                else
                {
                    string dir = System.IO.Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"\"{dir}\"");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open Explorer for '{filePath}': {ex.Message}");
            }
        }

        public static void OpenRecipeFile(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath)) return;

                string fullPath = System.IO.Path.GetFullPath(filePath);
                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath)
                        {
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        System.Diagnostics.Process.Start("notepad.exe", $"\"{fullPath}\"");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open recipe file '{filePath}': {ex.Message}");
            }
        }

        private void ApplyImmersiveDarkMode()
        {
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero && WpfThemeHelper.IsDarkMode)
                {
                    int useImmersiveDarkMode = 1;
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

        [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        private void OnApproveClicked(object sender, RoutedEventArgs e)
        {
            IsApproved = true;
            AllowExternalCommands = ChkAuthorizeExternalCommands.IsChecked == true;
            DialogResult = true;
            Close();
        }

        private void OnRejectClicked(object sender, RoutedEventArgs e)
        {
            IsApproved = false;
            DialogResult = false;
            Close();
        }
    }
}
