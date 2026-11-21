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

using System.ComponentModel;
using System.Linq;
using System.Windows;
using Greenshot.Base.Hdr;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;

namespace Greenshot.Recipes.ViewModels
{
    /// <summary>
    /// What the destination window shows of the capture below the preview: the window title, the address, the time and the size, and whether
    /// the capture has recognized text (OCR) and HDR content. Only shown, the capture isn't changed there.
    /// </summary>
    public sealed class CaptureMetadataViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The key of the window title in the meta data of a capture
        /// </summary>
        internal const string WindowTitleKey = "windowtitle";

        /// <summary>
        /// The key of the address of a web page in the meta data of a capture (browser extension, opened file)
        /// </summary>
        internal const string UrlKey = "url";

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The title of the window which was captured, null when it isn't known
        /// </summary>
        public string WindowTitle { get; private set; }

        public Visibility WindowTitleVisibility => string.IsNullOrWhiteSpace(WindowTitle) ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>
        /// The address of the captured web page, null when it isn't known
        /// </summary>
        public string Url { get; private set; }

        public Visibility UrlVisibility => string.IsNullOrWhiteSpace(Url) ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>
        /// When it was captured and its size, e.g. "13.11.2026 10:15 · 1920 × 1080 px"
        /// </summary>
        public string Details { get; private set; }

        /// <summary>
        /// The capture has recognized text (OCR lines)
        /// </summary>
        public bool HasOcr { get; private set; }

        /// <summary>
        /// The capture has HDR content
        /// </summary>
        public bool HasHdr { get; private set; }

        /// <summary>
        /// The badge says "OCR" like in the editor, the tooltip whether there is text
        /// </summary>
        public string OcrToolTip => HasOcr ? Texts.Recipe.PickerOcrAvailable : Texts.Recipe.PickerOcrMissing;

        public string HdrToolTip => HasHdr ? Texts.Recipe.PickerHdrAvailable : Texts.Recipe.PickerHdrMissing;

        /// <summary>
        /// The name of a badge for a screen reader: its letters and what they mean
        /// </summary>
        public string OcrAccessibleName => OcrBadge + ": " + OcrToolTip;

        public string HdrAccessibleName => HdrBadge + ": " + HdrToolTip;

        /// <summary>
        /// The badges of the editor, the same in every language
        /// </summary>
        public string OcrBadge => "OCR";

        public string HdrBadge => "HDR";

        /// <summary>
        /// Reads what is known of the capture
        /// </summary>
        public void Update(ISurface surface)
        {
            var details = surface?.CaptureDetails;
            string windowTitle = null;
            string url = null;
            if (details?.MetaData != null)
            {
                details.MetaData.TryGetValue(WindowTitleKey, out windowTitle);
                details.MetaData.TryGetValue(UrlKey, out url);
            }

            WindowTitle = !string.IsNullOrWhiteSpace(windowTitle) ? windowTitle : details?.Title;
            Url = url;

            var parts = new System.Collections.Generic.List<string>();
            if (details != null && details.DateTime != default)
            {
                parts.Add(details.DateTime.ToString("g"));
            }

            if (surface?.Image != null)
            {
                parts.Add($"{surface.Image.Width} × {surface.Image.Height} px");
            }

            Details = string.Join(" · ", parts);
            HasOcr = HasOcrLines(details);
            HasHdr = surface != null && surface.HdrState != HdrState.None;
            OnAllPropertiesChanged();
        }

        /// <summary>
        /// The language changed
        /// </summary>
        public void Refresh() => OnAllPropertiesChanged();

        /// <summary>
        /// The recognized text of the capture: OCR lines of its features
        /// </summary>
        internal static bool HasOcrLines(ICaptureDetails details)
        {
            if (details?.Features == null)
            {
                return false;
            }

            lock (details.Features)
            {
                return details.Features.OfType<IOcrLineFeature>().Any();
            }
        }

        private void OnAllPropertiesChanged()
        {
            // Empty: all properties
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
