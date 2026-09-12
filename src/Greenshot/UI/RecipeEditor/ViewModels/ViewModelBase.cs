using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Greenshot.UI.RecipeEditor.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Theme brushes for WPF binding with automatic Dark/Light mode support
        public System.Windows.Media.SolidColorBrush WindowBackgroundBrush => WpfThemeHelper.WindowBackground;
        public System.Windows.Media.SolidColorBrush CardBackgroundBrush => WpfThemeHelper.CardBackground;
        public System.Windows.Media.SolidColorBrush CardBorderBrush => WpfThemeHelper.CardBorder;
        public System.Windows.Media.SolidColorBrush TextPrimaryBrush => WpfThemeHelper.TextPrimary;
        public System.Windows.Media.SolidColorBrush TextSecondaryBrush => WpfThemeHelper.TextSecondary;
        public System.Windows.Media.SolidColorBrush AccentBrush => WpfThemeHelper.Accent;
        public System.Windows.Media.SolidColorBrush BadgeBackgroundBrush => WpfThemeHelper.BadgeBackground;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
            : this(_ => execute(), canExecute == null ? (Predicate<object>)null : _ => canExecute())
        {
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
