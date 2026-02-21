/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using Greenshot.Base.Core;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Provides data binding support for translations that update when language changes
    /// </summary>
    public class TranslationData : INotifyPropertyChanged
    {
        private readonly string _key;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationData"/> class.
        /// </summary>
        /// <param name="key">The language key.</param>
        public TranslationData(string key)
        {
            _key = key;
            Language.LanguageChanged += OnLanguageChanged;
        }

        /// <summary>
        /// Cleanup event handler when object is disposed
        /// </summary>
        ~TranslationData()
        {
            Language.LanguageChanged -= OnLanguageChanged;
        }

        /// <summary>
        /// Gets the translated value for the key
        /// </summary>
        public object Value => Language.GetString(_key);

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
