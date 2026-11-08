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

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Greenshot.SelfService.Imgur;

namespace Greenshot.SelfService.ViewModels
{
    /// <summary>
    /// One earlier anonymous Imgur upload; the title, date and thumbnail come from Imgur after the section is shown.
    /// </summary>
    public class ImgurUploadViewModel : INotifyPropertyChanged
    {
        private string _title;
        private DateTime? _uploaded;
        private ImageSource _thumbnail;

        public ImgurUploadViewModel(string hash, string deleteHash)
        {
            Hash = hash;
            DeleteHash = deleteHash;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Hash { get; }

        public string DeleteHash { get; }

        public string Link => ImgurApi.PageUrl(Hash);

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The name a screen reader says for the row in the uploads list
        /// </summary>
        public override string ToString() => string.IsNullOrEmpty(Title) ? Link : Title;

        public DateTime? Uploaded
        {
            get => _uploaded;
            set
            {
                _uploaded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Date));
            }
        }

        public string Date => Uploaded?.ToString("g", CultureInfo.CurrentCulture);

        public ImageSource Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
