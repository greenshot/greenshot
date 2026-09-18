using System;
using System.Windows;
using System.Windows.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;

namespace Greenshot.UI.Controls
{
    public partial class HotkeyDisplayControl : UserControl
    {
        public static readonly DependencyProperty HotkeyStringProperty =
            DependencyProperty.Register(
                nameof(HotkeyString),
                typeof(string),
                typeof(HotkeyDisplayControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHotkeyStringChanged));

        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register(
                nameof(HeaderText),
                typeof(string),
                typeof(HotkeyDisplayControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ShowEditButtonProperty =
            DependencyProperty.Register(
                nameof(ShowEditButton),
                typeof(bool),
                typeof(HotkeyDisplayControl),
                new PropertyMetadata(true, OnShowEditButtonChanged));

        public string HotkeyString
        {
            get => (string)GetValue(HotkeyStringProperty);
            set => SetValue(HotkeyStringProperty, value);
        }

        public string HeaderText
        {
            get => (string)GetValue(HeaderTextProperty);
            set => SetValue(HeaderTextProperty, value);
        }

        public bool ShowEditButton
        {
            get => (bool)GetValue(ShowEditButtonProperty);
            set => SetValue(ShowEditButtonProperty, value);
        }

        public ItemsControl BadgesContainer => BadgesPanel;
        public Button EditButtonControl => EditButton;

        public event EventHandler EditRequested;

        public HotkeyDisplayControl()
        {
            InitializeComponent();
            if (EditButton != null)
            {
                EditButton.Visibility = ShowEditButton ? Visibility.Visible : Visibility.Collapsed;
            }
            UpdateBadges();
        }

        private static void OnHotkeyStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HotkeyDisplayControl control)
            {
                control.UpdateBadges();
            }
        }

        private static void OnShowEditButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HotkeyDisplayControl control && control.EditButton != null)
            {
                control.EditButton.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void UpdateBadges()
        {
            BadgesPanel.Items.Clear();

            string hotkey = HotkeyString;
            if (string.IsNullOrWhiteSpace(hotkey) || string.Equals(hotkey.Trim(), "None", StringComparison.OrdinalIgnoreCase))
            {
                var noneText = new TextBlock
                {
                    Text = "None",
                    FontStyle = FontStyles.Italic,
                    Foreground = ThemeManager.Instance.MutedBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 4, 0)
                };
                BadgesPanel.Items.Add(noneText);
                return;
            }

            var sequence = HotkeySequence.Parse(hotkey);
            if (sequence.IsEmpty)
            {
                var noneText = new TextBlock
                {
                    Text = "None",
                    FontStyle = FontStyles.Italic,
                    Foreground = ThemeManager.Instance.MutedBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 4, 0)
                };
                BadgesPanel.Items.Add(noneText);
                return;
            }

            for (int i = 0; i < sequence.Chords.Count; i++)
            {
                if (i > 0)
                {
                    var thenText = new TextBlock
                    {
                        Text = "then",
                        FontWeight = FontWeights.Normal,
                        FontStyle = FontStyles.Italic,
                        FontSize = 11,
                        Foreground = ThemeManager.Instance.MutedBrush,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(6, 0, 6, 0)
                    };
                    BadgesPanel.Items.Add(thenText);
                }

                var chord = sequence.Chords[i];
                var badges = chord.GetBadges();

                for (int j = 0; j < badges.Count; j++)
                {
                    if (j > 0)
                    {
                        var plusText = new TextBlock
                        {
                            Text = "+",
                            FontWeight = FontWeights.Bold,
                            FontSize = 11,
                            Foreground = ThemeManager.Instance.MutedBrush,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(2, 0, 2, 0)
                        };
                        BadgesPanel.Items.Add(plusText);
                    }

                    BadgesPanel.Items.Add(new KeyCapBadge(badges[j]));
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
