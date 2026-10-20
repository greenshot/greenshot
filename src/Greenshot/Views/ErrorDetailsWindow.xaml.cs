/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Views
{
    /// <summary>
    /// The view of an <see cref="ErrorDetailsRequest"/>: the message, a note, and the details folded away with a button to copy them
    /// </summary>
    public partial class ErrorDetailsWindow : Window
    {
        private readonly string _details;

        private ErrorDetailsWindow(ErrorDetailsRequest request)
        {
            InitializeComponent();
            Title = request.Title;
            MessageText.Text = request.Message;
            NoteText.Text = request.Note;
            NoteText.Visibility = string.IsNullOrEmpty(request.Note) ? Visibility.Collapsed : Visibility.Visible;
            _details = request.Details;
            DetailsText.Text = _details;
            bool hasDetails = !string.IsNullOrEmpty(_details);
            DetailsExpander.Visibility = hasDetails ? Visibility.Visible : Visibility.Collapsed;
            CopyButton.Visibility = DetailsExpander.Visibility;
        }

        /// <summary>
        /// Registered for ShowDialogAsync, on the UI thread
        /// </summary>
        public static bool Show(ErrorDetailsRequest request)
        {
            new ErrorDetailsWindow(request).ShowDialog();
            return true;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            ClipboardHelper.SetClipboardData(_details);
        }
    }
}
