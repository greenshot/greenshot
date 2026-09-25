using System.Windows;
using System.Windows.Input;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Plugin.RecipeEditor.ViewModels;

namespace Greenshot.Plugin.RecipeEditor.Dialogs
{
    /// <summary>
    /// Interaction logic for RecipeManagerDialog.xaml
    /// </summary>
    public partial class RecipeManagerDialog : Window
    {
        public RecipeManagerViewModel ViewModel => DataContext as RecipeManagerViewModel;

        public RecipeManagerDialog(RecipeManagerViewModel viewModel = null)
        {
            InitializeComponent();
            var vm = viewModel ?? new RecipeManagerViewModel();
            DataContext = vm;
            vm.RequestClose += Close;
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void OnMinimizeClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnFilterAllClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null) ViewModel.SelectedFilterCategory = "All";
        }

        private void OnFilterActiveClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null) ViewModel.SelectedFilterCategory = "Active";
        }

        private void OnFilterDeactivatedClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null) ViewModel.SelectedFilterCategory = "Deactivated";
        }

        private void OnFilterBuiltInClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null) ViewModel.SelectedFilterCategory = "Built-in";
        }

        private void OnFilterCustomClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null) ViewModel.SelectedFilterCategory = "Custom";
        }
    }
}
