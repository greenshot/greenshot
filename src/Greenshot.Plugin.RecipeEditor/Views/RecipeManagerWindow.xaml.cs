using System.Windows;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Plugin.RecipeEditor.ViewModels;

namespace Greenshot.Plugin.RecipeEditor.Views
{
    /// <summary>
    /// Interaction logic for RecipeManagerWindow.xaml
    /// </summary>
    public partial class RecipeManagerWindow : Window
    {
        public RecipeManagerViewModel ViewModel => DataContext as RecipeManagerViewModel;

        public RecipeManagerWindow(RecipeManagerViewModel viewModel = null)
        {
            InitializeComponent();
            var vm = viewModel ?? new RecipeManagerViewModel();
            DataContext = vm;
            vm.RequestClose += Close;
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

        private void OnFilterAiClicked(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null) ViewModel.SelectedFilterCategory = "AI";
        }
    }
}
