using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Greenshot.Base.Wpf;

namespace Greenshot.Plugin.RecipeEditor.ViewModels
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
            OnPropertyChanged(nameof(WindowBackgroundBrush));
            OnPropertyChanged(nameof(CardBackgroundBrush));
            OnPropertyChanged(nameof(CardBorderBrush));
            OnPropertyChanged(nameof(TextPrimaryBrush));
            OnPropertyChanged(nameof(TextSecondaryBrush));
            OnPropertyChanged(nameof(AccentBrush));
            OnPropertyChanged(nameof(BadgeBackgroundBrush));
            OnPropertyChanged(nameof(IsDarkMode));
            OnPropertyChanged(nameof(ThemeToggleIcon));
            OnPropertyChanged(nameof(ThemeToggleText));
            OnPropertyChanged(nameof(ThemeToggleToolTip));
            OnPropertyChanged(nameof(NodeCardBackgroundBrush));
            OnPropertyChanged(nameof(NodeCardBorderBrush));
        }

        public bool IsDarkMode => WpfThemeHelper.IsDarkMode;
        public string ThemeToggleIcon => WpfThemeHelper.IsDarkMode ? "☀️" : "🌙";
        public string ThemeToggleText => WpfThemeHelper.IsDarkMode ? "Light" : "Dark";
        public string ThemeToggleToolTip => WpfThemeHelper.IsDarkMode ? "Switch to Light Mode" : "Switch to Dark Mode";

        // Theme brushes for WPF binding with automatic Dark/Light mode support
        public System.Windows.Media.SolidColorBrush WindowBackgroundBrush => WpfThemeHelper.WindowBackground;
        public System.Windows.Media.SolidColorBrush CardBackgroundBrush => WpfThemeHelper.CardBackground;
        public System.Windows.Media.SolidColorBrush CardBorderBrush => WpfThemeHelper.CardBorder;
        public System.Windows.Media.SolidColorBrush TextPrimaryBrush => WpfThemeHelper.TextPrimary;
        public System.Windows.Media.SolidColorBrush TextSecondaryBrush => WpfThemeHelper.TextSecondary;
        public System.Windows.Media.SolidColorBrush AccentBrush => WpfThemeHelper.Accent;
        public System.Windows.Media.SolidColorBrush BadgeBackgroundBrush => WpfThemeHelper.BadgeBackground;

        // Elevated node card styling
        public System.Windows.Media.SolidColorBrush NodeCardBackgroundBrush => WpfThemeHelper.IsDarkMode
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x28, 0x28, 0x2A))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));

        public System.Windows.Media.SolidColorBrush NodeCardBorderBrush => WpfThemeHelper.IsDarkMode
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x48, 0x48, 0x4C))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD0, 0xD0, 0xD6));

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
