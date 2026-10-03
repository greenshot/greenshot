/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Plugin.Office
{
    /// <summary>
    /// The texts of the [Office] section of greenshot.office.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Office", ModuleName = "office")]
    public interface IOfficeLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Microsoft Excel
        /// </summary>
        string AppExcel { get; }

        /// <summary>
        /// Microsoft OneNote
        /// </summary>
        string AppOnenote { get; }

        /// <summary>
        /// Microsoft Outlook
        /// </summary>
        string AppOutlook { get; }

        /// <summary>
        /// Microsoft PowerPoint
        /// </summary>
        string AppPowerpoint { get; }

        /// <summary>
        /// Microsoft Word
        /// </summary>
        string AppWord { get; }

        /// <summary>
        /// Screenshots are inserted into active or newly created Excel workbooks.
        /// </summary>
        string ExcelInfo { get; }

        /// <summary>
        /// HTML
        /// </summary>
        string FormatHtml { get; }

        /// <summary>
        /// Text
        /// </summary>
        string FormatText { get; }

        /// <summary>
        /// Screenshots are exported to a new page in the default OneNote notebook.
        /// </summary>
        string OnenoteInfo { get; }

        /// <summary>
        /// Allow export in meeting items
        /// </summary>
        string OutlookAllowmeetings { get; }

        /// <summary>
        /// Default "BCC" recipient
        /// </summary>
        string OutlookEmailbcc { get; }

        /// <summary>
        /// Default "CC" recipient
        /// </summary>
        string OutlookEmailcc { get; }

        /// <summary>
        /// Email format for new emails
        /// </summary>
        string OutlookEmailFormat { get; }

        /// <summary>
        /// Default "To" recipient
        /// </summary>
        string OutlookEmailto { get; }

        /// <summary>
        /// Email subject pattern
        /// </summary>
        string OutlookSubjectPattern { get; }

        /// <summary>
        /// Lock aspect ratio of the image
        /// </summary>
        string PowerpointLockaspect { get; }

        /// <summary>
        /// Slide layout for exported captures
        /// </summary>
        string PowerpointSlideLayout { get; }

        /// <summary>
        /// Office settings
        /// </summary>
        string SettingsTitle { get; }

        /// <summary>
        /// Lock aspect ratio of the image
        /// </summary>
        string WordLockaspect { get; }
    }
}
