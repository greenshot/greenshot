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
using System.Windows;
using System.Windows.Controls;
using Greenshot.Ai.ViewModels;
using Greenshot.Base.Wpf;
using Greenshot.Base.Languages;
using Greenshot.Settings.ViewModels;

namespace Greenshot.Ai.Views
{
    /// <summary>
    /// The AI tools tab of the settings (greenshot-mcp), on the <see cref="SettingsViewModel"/> of the settings window.
    /// Not in Greenshot Light.
    /// </summary>
    public partial class AiToolsSettingsPage : UserControl
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(AiToolsSettingsPage));

        public AiToolsSettingsPage()
        {
            InitializeComponent();
        }

        private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

        private void RemoveAiToolClient_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is AiToolClientViewModel client)
            {
                ViewModel.AiToolsAllowedClients.Remove(client);
            }
        }

        private void AllowDeniedClient_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AllowDeniedClient((sender as FrameworkElement)?.DataContext as AiToolClientViewModel);
        }

        private void AskAgainDeniedClient_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AskAgain((sender as FrameworkElement)?.DataContext as AiToolClientViewModel);
        }

        private void RemoveExcludedProcess_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is string processName)
            {
                ViewModel.AiToolsExcludedProcesses.Remove(processName);
            }
        }

        private void AddExcludedProcess_Click(object sender, RoutedEventArgs e)
        {
            AddExcludedProcess();
        }

        private void NewExcludedProcess_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddExcludedProcess();
                e.Handled = true;
            }
        }

        private void AddExcludedProcess()
        {
            // Several at once are fine too: "KeePass, Bitwarden"
            foreach (string name in (ViewModel.NewExcludedProcess ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                ViewModel.AddExcludedProcess(name);
            }
            ViewModel.NewExcludedProcess = string.Empty;
        }

        private void McpDownload_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Warn("Could not open the download page of greenshot-mcp", ex);
            }
            e.Handled = true;
        }

        private void ShowRecipeDetails_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as ApprovedRecipeViewModel;
            if (item == null) return;
            Greenshot.Recipes.RecipeManager.Instance.ShowRecipeDetails(item.RecipeId);
        }

        private void ReviewRecipeApproval_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as ApprovedRecipeViewModel;
            if (item == null) return;
            // Takes effect right away, like every approval
            var result = Greenshot.Recipes.RecipeManager.Instance.ReviewApproval(item.RecipeId);
            if (result != null && !result.IsValid)
            {
                ThemedMessageBox.Show(Window.GetWindow(this), string.Join("\n", result.Errors), Texts.Settings.Recipeapprovals, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            ViewModel.RefreshApprovedRecipes();
        }

        private void RevokeRecipeApproval_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as ApprovedRecipeViewModel;
            if (item == null) return;
            int choice = ThemedMessageBox.ShowChoice(Window.GetWindow(this), "Revoke Approval",
                $"Revoke the approval of \"{item.Recipe.Name}\"? It stops running right away and isn't loaded again. Its file stays where it is: " +
                "open it again to review and approve it.", MessageBoxImage.Warning, new[] { "Revoke", "Keep" }, defaultIndex: 1, cancelIndex: 1);
            if (choice != 0) return;
            Greenshot.Recipes.RecipeManager.Instance.RevokeApproval(item.RecipeId);
            ViewModel.RefreshApprovedRecipes();
        }
    }
}
