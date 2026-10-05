using System.Windows;
using System.Windows.Controls;

namespace Greenshot.Base.Wpf.Views
{
    public partial class KeyCapBadgeView : UserControl
    {
        public static readonly DependencyProperty KeyTextProperty =
            DependencyProperty.Register(nameof(KeyText), typeof(string), typeof(KeyCapBadgeView), new PropertyMetadata(string.Empty));

        public string KeyText
        {
            get => (string)GetValue(KeyTextProperty);
            set => SetValue(KeyTextProperty, value);
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
