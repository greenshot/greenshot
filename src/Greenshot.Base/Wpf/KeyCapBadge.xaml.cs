using System.Windows;
using System.Windows.Controls;

namespace Greenshot.Base.Wpf
{
    public partial class KeyCapBadge : UserControl
    {
        public static readonly DependencyProperty KeyTextProperty =
            DependencyProperty.Register(nameof(KeyText), typeof(string), typeof(KeyCapBadge), new PropertyMetadata(string.Empty));

        public string KeyText
        {
            get => (string)GetValue(KeyTextProperty);
            set => SetValue(KeyTextProperty, value);
        }

        public KeyCapBadge()
        {
            InitializeComponent();
        }

        public KeyCapBadge(string text) : this()
        {
            KeyText = text;
        }
    }
}
