using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;

namespace Greenshot.Base.Wpf.Views
{
    public partial class HotkeyDisplayView : UserControl
    {
        public static readonly DependencyProperty HotkeyStringProperty =
            DependencyProperty.Register(
                nameof(HotkeyString),
                typeof(string),
                typeof(HotkeyDisplayView),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHotkeyStringChanged));

        public static readonly DependencyProperty HeaderTextProperty =
            DependencyProperty.Register(
                nameof(HeaderText),
                typeof(string),
                typeof(HotkeyDisplayView),
                new PropertyMetadata(string.Empty, OnHeaderTextChanged));

        public static readonly DependencyProperty ShowEditButtonProperty =
            DependencyProperty.Register(
                nameof(ShowEditButton),
                typeof(bool),
                typeof(HotkeyDisplayView),
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

        /// <summary>
        /// The hotkey as one line of text for a screen reader, e.g. "Ctrl + Shift + A then B"
        /// </summary>
        public string AccessibleText { get; private set; } = string.Empty;

        public event EventHandler EditRequested;

        public HotkeyDisplayView()
        {
            InitializeComponent();
            if (EditButton != null)
            {
                EditButton.Visibility = ShowEditButton ? Visibility.Visible : Visibility.Collapsed;
            }
            UpdateBadges();
            // "None", "then" and the name of the button are built in code, follow a language switch
            Loaded += (_, _) =>
            {
                // Loaded can come again without Unloaded in between, subscribe only once
                Texts.Settings.PropertyChanged -= OnLanguageChanged;
                Texts.Settings.PropertyChanged += OnLanguageChanged;
            };
            Unloaded += (_, _) => Texts.Settings.PropertyChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(object sender, PropertyChangedEventArgs e)
        {
            // A language switch notifies every changed text of the section, only these few are used here
            switch (e.PropertyName)
            {
                case null:
                case "":
                case nameof(ISettingsLanguage.HotkeyNone):
                case nameof(ISettingsLanguage.HotkeyThen):
                case nameof(ISettingsLanguage.HotkeyEdit):
                case nameof(ISettingsLanguage.HotkeyEditAccessible):
                    UpdateBadges();
                    break;
            }
        }

        private static void OnHeaderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HotkeyDisplayView control)
            {
                control.UpdateAccessibleNames();
            }
        }

        /// <summary>
        /// The badges are only drawn, a screen reader gets the hotkey as the name of the panel and as the help text of the
        /// edit button, whose name says which hotkey it edits ("Edit: Capture region")
        /// </summary>
        private void UpdateAccessibleNames()
        {
            AutomationProperties.SetName(BadgesPanel, AccessibleText);
            if (EditButton == null)
            {
                return;
            }
            var editFormat = Texts.Settings.HotkeyEditAccessible;
            AutomationProperties.SetName(EditButton, string.IsNullOrEmpty(HeaderText) || string.IsNullOrEmpty(editFormat)
                ? Texts.Settings.HotkeyEdit
                : string.Format(editFormat, HeaderText));
            AutomationProperties.SetHelpText(EditButton, AccessibleText);
        }

        private TextBlock CreateNoneText()
        {
            return new TextBlock
            {
                Text = Texts.Settings.HotkeyNone,
                FontStyle = FontStyles.Italic,
                Foreground = ThemeManager.Instance.MutedBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };
        }

        private static void OnHotkeyStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HotkeyDisplayView control)
            {
                control.UpdateBadges();
            }
        }

        private static void OnShowEditButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HotkeyDisplayView control && control.EditButton != null)
            {
                control.EditButton.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void UpdateBadges()
        {
            BadgesPanel.Items.Clear();

            string hotkey = HotkeyString;
            var sequence = string.IsNullOrWhiteSpace(hotkey) || string.Equals(hotkey.Trim(), "None", StringComparison.OrdinalIgnoreCase)
                ? null
                : HotkeySequence.Parse(hotkey);
            if (sequence == null || sequence.IsEmpty)
            {
                BadgesPanel.Items.Add(CreateNoneText());
                AccessibleText = Texts.Settings.HotkeyNone;
                UpdateAccessibleNames();
                return;
            }

            string thenWord = Texts.Settings.HotkeyThen;
            var chordTexts = new List<string>();
            for (int i = 0; i < sequence.Chords.Count; i++)
            {
                if (i > 0)
                {
                    var thenText = new TextBlock
                    {
                        Text = thenWord,
                        FontWeight = FontWeights.Normal,
                        FontStyle = FontStyles.Italic,
                        Foreground = ThemeManager.Instance.MutedBrush,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(6, 0, 6, 0)
                    };
                    thenText.SetBinding(TextBlock.FontSizeProperty, FontSizeExtension.CreateBinding(11));
                    BadgesPanel.Items.Add(thenText);
                }

                var chord = sequence.Chords[i];
                var badges = chord.GetBadges();
                chordTexts.Add(string.Join(" + ", badges));

                for (int j = 0; j < badges.Count; j++)
                {
                    if (j > 0)
                    {
                        var plusText = new TextBlock
                        {
                            Text = "+",
                            FontWeight = FontWeights.Bold,
                            Foreground = ThemeManager.Instance.MutedBrush,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(2, 0, 2, 0)
                        };
                        plusText.SetBinding(TextBlock.FontSizeProperty, FontSizeExtension.CreateBinding(11));
                        BadgesPanel.Items.Add(plusText);
                    }

                    BadgesPanel.Items.Add(new KeyCapBadgeView(badges[j]));
                }
            }
            AccessibleText = string.Join($" {thenWord} ", chordTexts);
            UpdateAccessibleNames();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
