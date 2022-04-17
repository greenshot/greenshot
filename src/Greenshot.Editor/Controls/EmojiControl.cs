using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Controls
{
    internal class EmojiControl : Image
    {
        public static readonly DependencyProperty EmojiProperty = DependencyProperty.Register("Emoji", typeof(string), typeof(EmojiControl), new PropertyMetadata(default(string), OnEmojiPropertyChanged));

        private static void OnEmojiPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EmojiControl)d).Source = null;
        }

        public string Emoji
        {
            get { return (string)GetValue(EmojiProperty); }
            set { SetValue(EmojiProperty, value); }
        }

        private static void OnUseSystemFontPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EmojiControl)d).Source = null;
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (Source == null && !string.IsNullOrEmpty(Emoji))
            {
                Source = EmojiRenderer.GetBitmapSource(Emoji, iconSize: 48, useSystemFont: false);
            }

            base.OnRender(dc);
        }
    }
}
