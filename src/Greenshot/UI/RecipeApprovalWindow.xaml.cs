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
using System.IO;
using System.Windows;
using System.Windows.Media;
using Greenshot.Base.Core;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Greenshot.Recipes;

namespace Greenshot.UI
{

    /// <summary>
    /// Modern WPF dialog for reviewing and approving external capture recipes with dark/light mode support.
    /// </summary>
    public partial class RecipeApprovalWindow : Window, INotifyPropertyChanged
    {
        private readonly CaptureRecipe _recipe;
        private bool _isJsonViewerVisible;
        private string _recipeJsonContent;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string RecipeName { get; set; }
        public string RecipeVersion { get; set; }
        public string RecipeDescription { get; set; }
        public string RecipeId { get; set; }
        public string FilePath { get; set; }
        public string FileHash { get; set; }

        public bool IsJsonViewerVisible
        {
            get => _isJsonViewerVisible;
            set
            {
                if (_isJsonViewerVisible != value)
                {
                    _isJsonViewerVisible = value;
                    OnPropertyChanged(nameof(IsJsonViewerVisible));
                    OnPropertyChanged(nameof(JsonViewerVisibility));
                }
            }
        }

        public Visibility JsonViewerVisibility => _isJsonViewerVisible ? Visibility.Visible : Visibility.Collapsed;

        public string RecipeJsonContent
        {
            get => _recipeJsonContent;
            set
            {
                if (_recipeJsonContent != value)
                {
                    _recipeJsonContent = value;
                    OnPropertyChanged(nameof(RecipeJsonContent));
                }
            }
        }

        public RecipeApprovalMode ApprovalMode { get; private set; }
        public bool IsModified => ApprovalMode == RecipeApprovalMode.Modified;
        public bool IsNewRecipe => ApprovalMode == RecipeApprovalMode.NewRecipe;
        public bool IsValidationError => ApprovalMode == RecipeApprovalMode.ValidationError;

        public Visibility ValidationErrorVisibility => IsValidationError ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ApprovalActionsVisibility => !IsValidationError ? Visibility.Visible : Visibility.Collapsed;

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
        public Visibility ShieldIconVisibility => (!IsModified && !IsValidationError) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ErrorIconVisibility => IsValidationError ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<RecipeValidationErrorItem> ValidationErrors { get; } = new ObservableCollection<RecipeValidationErrorItem>();
        public ObservableCollection<TriggerBadgeModel> TriggerBadges { get; } = new ObservableCollection<TriggerBadgeModel>();
        public ObservableCollection<string> StepDescriptions { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> ExternalCommandsList { get; } = new ObservableCollection<string>();

        public Visibility TriggersVisibility => TriggerBadges.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PipelineVisibility => StepDescriptions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ExternalCommandWarningVisibility => (HasExternalCommands && !IsValidationError) ? Visibility.Visible : Visibility.Collapsed;
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
        public SolidColorBrush ErrorBackgroundBrush => WpfThemeHelper.ErrorBackground;
        public SolidColorBrush ErrorBorderBrush => WpfThemeHelper.ErrorBorder;
        public SolidColorBrush ErrorTextBrush => WpfThemeHelper.ErrorText;
        public SolidColorBrush BadgeBackgroundBrush => WpfThemeHelper.BadgeBackground;

        public RecipeApprovalWindow(CaptureRecipe recipe, string filePath, RecipeValidationResult validationResult = null, RecipeTrustRecord previousTrustRecord = null)
        {
            _recipe = recipe;
            InitializeComponent();

            RecipeName = recipe?.Name ?? (File.Exists(filePath) ? Path.GetFileName(filePath) : "Unnamed Recipe");
            RecipeVersion = string.IsNullOrWhiteSpace(recipe?.Version) ? "" : $"v{recipe.Version}";
            RecipeDescription = string.IsNullOrWhiteSpace(recipe?.Description) ? (validationResult != null && !validationResult.IsValid ? "Recipe configuration failed validation." : "No description provided.") : recipe.Description;
            RecipeId = recipe?.Id ?? "unknown";
            FilePath = filePath ?? "Unknown file path";
            FileHash = RecipeTrustStore.ComputeSha256(filePath) ?? "Unknown";

            if (validationResult != null && !validationResult.IsValid)
            {
                ApprovalMode = RecipeApprovalMode.ValidationError;
                WindowTitleSubtitle = " — Recipe Validation Failed";
                HeaderIcon = "❌";
                HeaderTitle = "Recipe Validation Failed";
                HeaderDescription = "Greenshot could not load or register this capture recipe because it contains configuration errors. Review the diagnostic details below:";
                StatusBadgeText = "VALIDATION ERROR";
                StatusBadgeBackgroundBrush = ErrorBackgroundBrush;
                StatusBadgeBorderBrush = ErrorBorderBrush;
                StatusBadgeForegroundBrush = ErrorTextBrush;
                ApproveButtonText = "Close";

                foreach (var err in validationResult.Errors)
                {
                    ValidationErrors.Add(RecipeValidationErrorItem.Create(err));
                }
            }
            else
            {
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
            }

            DataContext = this;
            Background = WindowBackgroundBrush;
            Title = WindowFullTitle;

            PopulateTriggers(recipe);
            PopulateSteps(recipe);

            // Gated Actions / External Command handling
            if (!IsValidationError && validationResult != null && validationResult.HasGatedActions)
            {
                HasExternalCommands = true;
                foreach (var action in validationResult.GatedActions)
                {
                    ExternalCommandsList.Add(FormatGatedAction(action));
                }
            }

            // If gated actions exist, require authorization before enabling Approve
            if (HasExternalCommands && BtnApprove != null)
            {
                BtnApprove.IsEnabled = false;
            }
        }

        public RecipeApprovalWindow(string filePath, RecipeValidationResult validationResult = null, CaptureRecipe recipe = null, string rawErrorMessage = null)
        {
            _recipe = recipe;
            ApprovalMode = RecipeApprovalMode.ValidationError;
            InitializeComponent();

            RecipeName = recipe?.Name ?? (File.Exists(filePath) ? Path.GetFileName(filePath) : "Unknown Recipe");
            RecipeVersion = string.IsNullOrWhiteSpace(recipe?.Version) ? "" : $"v{recipe.Version}";
            RecipeDescription = string.IsNullOrWhiteSpace(recipe?.Description) ? "Recipe configuration failed validation." : recipe.Description;
            RecipeId = recipe?.Id ?? "unknown";
            FilePath = filePath ?? "Unknown file path";
            FileHash = RecipeTrustStore.ComputeSha256(filePath) ?? "Unknown";

            WindowTitleSubtitle = " — Recipe Validation Failed";
            HeaderIcon = "❌";
            HeaderTitle = "Recipe Validation Failed";
            HeaderDescription = "Greenshot could not load or register this capture recipe because it contains configuration errors. Review the diagnostic details below:";
            StatusBadgeText = "VALIDATION ERROR";
            StatusBadgeBackgroundBrush = ErrorBackgroundBrush;
            StatusBadgeBorderBrush = ErrorBorderBrush;
            StatusBadgeForegroundBrush = ErrorTextBrush;
            ApproveButtonText = "Close";

            DataContext = this;
            Background = WindowBackgroundBrush;
            Title = WindowFullTitle;

            if (validationResult != null && validationResult.Errors.Count > 0)
            {
                foreach (var err in validationResult.Errors)
                {
                    ValidationErrors.Add(RecipeValidationErrorItem.Create(err));
                }
            }
            else if (!string.IsNullOrWhiteSpace(rawErrorMessage))
            {
                ValidationErrors.Add(RecipeValidationErrorItem.Create(rawErrorMessage));
            }
            else
            {
                ValidationErrors.Add(RecipeValidationErrorItem.Create("An unknown validation error occurred while loading the recipe."));
            }

            PopulateTriggers(recipe);
            PopulateSteps(recipe);
        }

        public static void ShowValidationError(string filePath, RecipeValidationResult validationResult = null, CaptureRecipe recipe = null, string rawErrorMessage = null)
        {
            void Show()
            {
                var window = new RecipeApprovalWindow(filePath, validationResult, recipe, rawErrorMessage)
                {
                    Topmost = true,
                    ShowActivated = true,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                var mainForm = SimpleServiceProvider.Current.GetInstance<System.Windows.Forms.Form>(isOptional: true);
                if (mainForm != null && mainForm.IsHandleCreated && mainForm.Visible)
                {
                    new System.Windows.Interop.WindowInteropHelper(window).Owner = mainForm.Handle;
                }

                window.ShowDialog();
            }

            if (System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA)
            {
                Show();
            }
            else
            {
                var staThread = new System.Threading.Thread(() => Show());
                staThread.SetApartmentState(System.Threading.ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            }
        }

        private void PopulateTriggers(CaptureRecipe recipe)
        {
            TriggerBadges.Clear();
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
            else if (!IsValidationError)
            {
                TriggerBadges.Add(new TriggerBadgeModel
                {
                    Text = "ℹ Manual / Triggerless (Invoked via CLI / API)",
                    ForegroundBrush = TextSecondaryBrush,
                    BackgroundBrush = BadgeBackgroundBrush,
                    BorderBrush = CardBorderBrush
                });
            }
        }

        private void PopulateSteps(CaptureRecipe recipe)
        {
            StepDescriptions.Clear();
            if (recipe?.Nodes != null)
            {
                for (int i = 0; i < recipe.Nodes.Count; i++)
                {
                    var s = recipe.Nodes[i];
                    string paramSummary = "";
                    if (string.Equals(s.StepType, WellKnownStepTypes.Border, StringComparison.OrdinalIgnoreCase))
                    {
                        paramSummary = $" ({s.GetParameter<int>("Width", 2)}px, {s.GetParameter<string>("Color", "Black")})";
                    }
                    else if (string.Equals(s.StepType, WellKnownStepTypes.Source, StringComparison.OrdinalIgnoreCase))
                    {
                        paramSummary = $" [{s.GetParameter<string>("SourceType", "Region")}]";
                    }
                    else if (string.Equals(s.StepType, WellKnownStepTypes.Annotation, StringComparison.OrdinalIgnoreCase))
                    {
                        paramSummary = $" [{s.GetParameter<string>("AnnotationType", s.GetParameter<string>("Type", "Element"))}]";
                    }
                    else if (string.Equals(s.StepType, WellKnownStepTypes.SetVariable, StringComparison.OrdinalIgnoreCase))
                    {
                        paramSummary = $" [{s.GetParameter<string>("Variable", "var")}]";
                    }
                    else if (string.Equals(s.StepType, WellKnownStepTypes.Destinations, StringComparison.OrdinalIgnoreCase))
                    {
                        var dests = s.GetParameter<List<string>>("DestinationDesignations");
                        if (dests != null && dests.Count > 0) paramSummary = $" -> [{string.Join(", ", dests)}]";
                    }

                    StepDescriptions.Add($"[{s.Id}] {s.StepType}{paramSummary}");
                }
            }
        }

        private static string FormatGatedAction(RecipeGatedAction action)
        {
            if (action == null) return string.Empty;

            string typeName = !string.IsNullOrEmpty(action.DescriptionKey)
                ? Greenshot.Base.Core.Language.GetString(action.DescriptionKey)
                : null;

            if (string.IsNullOrEmpty(typeName))
            {
                typeName = action.GateType switch
                {
                    RecipeGateType.ExternalCommand => Greenshot.Base.Core.Language.GetString("recipe_gate_external_command") ?? "External Command",
                    RecipeGateType.NetworkAccess => Greenshot.Base.Core.Language.GetString("recipe_gate_network_access") ?? "Network Access",
                    RecipeGateType.FileSystemAccess => Greenshot.Base.Core.Language.GetString("recipe_gate_file_system_access") ?? "File System Access",
                    _ => Greenshot.Base.Core.Language.GetString("recipe_gate_custom") ?? "Custom Action"
                };
            }

            return !string.IsNullOrWhiteSpace(action.Target)
                ? $"{typeName}: {action.Target}"
                : typeName;
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
            try
            {
                if (!string.IsNullOrWhiteSpace(FilePath))
                {
                    ExplorerHelper.OpenInExplorer(FilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open Explorer for '{FilePath}': {ex.Message}");
            }
        }

        private void OnViewFileClicked(object sender, RoutedEventArgs e)
        {
            ToggleJsonViewer();
        }

        private void OnCloseJsonViewerClicked(object sender, RoutedEventArgs e)
        {
            IsJsonViewerVisible = false;
        }

        private void OnCopyJsonClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(RecipeJsonContent))
                {
                    System.Windows.Clipboard.SetText(RecipeJsonContent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy JSON to clipboard: {ex.Message}");
            }
        }

        public void ToggleJsonViewer()
        {
            IsJsonViewerVisible = !IsJsonViewerVisible;
            if (IsJsonViewerVisible && string.IsNullOrEmpty(RecipeJsonContent))
            {
                LoadJsonContent();
            }
        }

        private void LoadJsonContent()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
                {
                    var fi = new FileInfo(FilePath);
                    if (fi.Length > 1024 * 1024)
                    {
                        RecipeJsonContent = "// Recipe file is too large to preview (> 1MB).";
                    }
                    else
                    {
                        RecipeJsonContent = File.ReadAllText(FilePath);
                    }
                }
                else if (_recipe != null)
                {
                    RecipeJsonContent = RecipeSerializer.Serialize(_recipe);
                }
                else
                {
                    RecipeJsonContent = "// Recipe content unavailable.";
                }
            }
            catch (Exception ex)
            {
                RecipeJsonContent = $"// Error reading recipe: {ex.Message}";
            }
        }

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape && IsJsonViewerVisible)
            {
                IsJsonViewerVisible = false;
                e.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(e);
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

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            IsApproved = false;
            DialogResult = false;
            Close();
        }

        private void OnEditFileClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(FilePath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open recipe file in editor: {ex.Message}");
            }
        }

        private void OnApproveClicked(object sender, RoutedEventArgs e)
        {
            IsApproved = true;
            AllowExternalCommands = ChkAuthorizeExternalCommands?.IsChecked == true;
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
