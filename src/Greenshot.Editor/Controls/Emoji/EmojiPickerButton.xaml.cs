/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */


using System.Windows;
using System.Windows.Controls;
using Dapplo.Ini;
using Greenshot.Base.Wpf;
using Greenshot.Editor.Configuration;

namespace Greenshot.Editor.Controls.Emoji
{
    /// <summary>
    /// A button with the emoji, a click opens the emoji picker in a popup. For places without a panel, e.g. the recipe editor.
    /// </summary>
    public partial class EmojiPickerButton : UserControl
    {
        public static readonly DependencyProperty SelectionProperty = DependencyProperty.Register(
            nameof(Selection), typeof(string), typeof(EmojiPickerButton),
            new FrameworkPropertyMetadata("☺", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        private EmojiPickerViewModel _picker;

        public EmojiPickerButton()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The picked emoji
        /// </summary>
        public string Selection
        {
            get => (string)GetValue(SelectionProperty);
            set => SetValue(SelectionProperty, value);
        }

        /// <summary>
        /// The picker is built the first time it opens
        /// </summary>
        private void OnPopupOpened(object sender, System.EventArgs e)
        {
            if (_picker == null)
            {
                _picker = new EmojiPickerViewModel(IniConfigRegistry.GetSection<IEditorConfiguration>());
                _picker.Picked += (_, args) =>
                {
                    Selection = args.Emoji;
                    PickerPopup.IsOpen = false;
                    OpenButton.Focus();
                };
                Picker.DataContext = _picker;
            }

            // The colors of Greenshot's theme, the picker uses the keys of the editor panels
            EditorPanelResources.Apply(Resources, ThemeManager.Instance.CurrentPalette);
            _picker.CurrentEmoji = Selection;
            Picker.Focus();
        }
    }
}
