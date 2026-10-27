using System.ComponentModel;
#nullable disable

using Greenshot.Linux.Core.Languages;
using System.Windows.Input;
using System.Runtime.CompilerServices;

namespace Greenshot.Linux.RecipeEditor.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ViewModelBase()
        {
            WpfThemeHelper.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged()
        {
            OnPropertyChanged(nameof(IsDarkMode));
            OnPropertyChanged(nameof(ThemeToggleIcon));
            OnPropertyChanged(nameof(ThemeToggleText));
            OnPropertyChanged(nameof(ThemeToggleToolTip));
        }

        public bool IsDarkMode => WpfThemeHelper.IsDarkMode;
        public string ThemeToggleIcon => WpfThemeHelper.IsDarkMode ? "☀️" : "🌙";
        public string ThemeToggleText => WpfThemeHelper.IsDarkMode ? Texts.Settings("theme_light") : Texts.Settings("theme_dark");
        public string ThemeToggleToolTip => WpfThemeHelper.IsDarkMode ? Texts.Get("RecipeEditor", "tooltip_switch_light") : Texts.Get("RecipeEditor", "tooltip_switch_dark");

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
}
