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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Languages;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// "Details in saved images" on the Output tab: which details of ExportMetadata are written, author and copyright
    /// </summary>
    public partial class SettingsViewModel
    {
        /// <summary>
        /// One check box per detail, the private ones (they need the user's OK before they leave this computer) last
        /// </summary>
        public ObservableCollection<ExportMetadataFieldViewModel> ExportMetadataFields { get; private set; }

        private void InitializeExportMetadata()
        {
            var selected = CoreConfiguration.ExportMetadata ?? new List<ExportMetadataField>();
            var texts = Texts.Settings;
            ExportMetadataFields = new ObservableCollection<ExportMetadataFieldViewModel>(
                new[]
                {
                    (ExportMetadataField.CaptureTime, texts.ExportMetadataCapturetime),
                    (ExportMetadataField.Author, texts.ExportMetadataAuthor),
                    (ExportMetadataField.Source, texts.ExportMetadataSource),
                    (ExportMetadataField.Text, texts.ExportMetadataText),
                    (ExportMetadataField.Barcodes, texts.ExportMetadataBarcodes)
                }.Select(field => new ExportMetadataFieldViewModel(field.Item1, field.Item2, selected.Contains(field.Item1))));
        }

        /// <summary>
        /// Write the chosen details back to ExportMetadata
        /// </summary>
        public void SaveExportMetadata()
        {
            if (ExportMetadataFields != null)
            {
                CoreConfiguration.ExportMetadata = ExportMetadataFields.Where(field => field.IsSelected).Select(field => field.Field).ToList();
            }
        }
    }

    /// <summary>
    /// A check box for one detail of ExportMetadata
    /// </summary>
    public sealed class ExportMetadataFieldViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public ExportMetadataFieldViewModel(ExportMetadataField field, string name, bool isSelected)
        {
            Field = field;
            Name = name;
            _isSelected = isSelected;
        }

        public ExportMetadataField Field { get; }

        public string Name { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
