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

        public RecipeApprovalWindow(CaptureRecipe recipe, string filePath, RecipeValidationResult validationResult = null)
        {
            InitializeComponent();
            DataContext = this;
            Background = WindowBackgroundBrush;

            RecipeName = recipe?.Name ?? "Unnamed Recipe";
            RecipeVersion = string.IsNullOrWhiteSpace(recipe?.Version) ? "v1.0" : $"v{recipe.Version}";
            RecipeDescription = string.IsNullOrWhiteSpace(recipe?.Description) ? "No description provided." : recipe.Description;
            RecipeId = recipe?.Id ?? "unknown";
            FilePath = filePath ?? "Unknown file path";
            FileHash = RecipeTrustStore.ComputeSha256(filePath) ?? "Unknown";

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
