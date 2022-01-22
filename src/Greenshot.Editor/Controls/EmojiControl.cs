using System;
using System.Windows;
using System.Windows.Controls;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Controls
{
    internal class EmojiControl : Image
    {
        public static readonly DependencyProperty EmojiProperty = DependencyProperty.Register("Emoji", typeof(string), typeof(EmojiControl), new PropertyMetadata(default(string), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EmojiControl)d).Source = EmojiRenderer.GetBitmapSource((string)e.NewValue, 48);
        }

        public string Emoji
        {
            get { return (string)GetValue(EmojiProperty); }
            set { SetValue(EmojiProperty, value); }
        }
    }
}
