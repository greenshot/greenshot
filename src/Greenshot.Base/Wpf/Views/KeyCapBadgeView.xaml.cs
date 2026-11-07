using System.Windows;
using System.Windows.Controls;

namespace Greenshot.Base.Wpf.Views
{
    public partial class KeyCapBadgeView : UserControl
    {
        public static readonly DependencyProperty KeyTextProperty =
            DependencyProperty.Register(nameof(KeyText), typeof(string), typeof(KeyCapBadgeView), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Underlines the key, e.g. to point at the keys of a shortcut while Ctrl is held
        /// </summary>
        public static readonly DependencyProperty IsUnderlinedProperty =
            DependencyProperty.Register(nameof(IsUnderlined), typeof(bool), typeof(KeyCapBadgeView),
                new PropertyMetadata(false, (d, e) => ((KeyCapBadgeView)d).KeyCapText.TextDecorations = (bool)e.NewValue ? TextDecorations.Underline : null));

        public string KeyText
        {
            get => (string)GetValue(KeyTextProperty);
            set => SetValue(KeyTextProperty, value);
        }

        public bool IsUnderlined
        {
            get => (bool)GetValue(IsUnderlinedProperty);
            set => SetValue(IsUnderlinedProperty, value);
        }

        public KeyCapBadgeView()
        {
            InitializeComponent();
        }

        public KeyCapBadgeView(string text) : this()
        {
            KeyText = text;
        }
    }
}
