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

namespace Greenshot.Plugin.Jira
{
    /// <summary>
    /// The texts of the [Jira] section of greenshot.jira.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Jira", ModuleName = "jira")]
    public interface IJiraLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Cancel
        /// </summary>
        string Cancel { get; }

        /// <summary>
        /// Assignee
        /// </summary>
        string ColumnAssignee { get; }

        /// <summary>
        /// Created
        /// </summary>
        string ColumnCreated { get; }

        /// <summary>
        /// ID
        /// </summary>
        string ColumnId { get; }

        /// <summary>
        /// Type
        /// </summary>
        string ColumnIssueType { get; }

        /// <summary>
        /// Reporter
        /// </summary>
        string ColumnReporter { get; }

        /// <summary>
        /// Summary
        /// </summary>
        string ColumnSummary { get; }

        /// <summary>
        /// Transferring data to JIRA, please wait...
        /// </summary>
        string CommunicationWait { get; }

        /// <summary>
        /// Comment
        /// </summary>
        string LabelComment { get; }

        /// <summary>
        /// Filename
        /// </summary>
        string LabelFilename { get; }

        /// <summary>
        /// JIRA
        /// </summary>
        string LabelJira { get; }

        /// <summary>
        /// JIRA Filter
        /// </summary>
        string LabelJirafilter { get; }

        /// <summary>
        /// Image format
        /// </summary>
        string LabelUploadFormat { get; }

        /// <summary>
        /// Url
        /// </summary>
        string LabelUrl { get; }

        /// <summary>
        /// There was a problem during the login: {0}
        /// </summary>
        string LoginError { get; }

        /// <summary>
        /// Please enter your Jira login data
        /// </summary>
        string LoginTitle { get; }

        /// <summary>
        /// OK
        /// </summary>
        string Ok { get; }

        /// <summary>
        /// Jira settings
        /// </summary>
        string SettingsTitle { get; }

        /// <summary>
        /// Upload
        /// </summary>
        string Upload { get; }

        /// <summary>
        /// An error occurred while uploading to Jira:
        /// </summary>
        string UploadFailure { get; }

        /// <summary>
        /// Upload to Jira
        /// </summary>
        string UploadMenuItem { get; }

        /// <summary>
        /// Successfully uploaded image to Jira!
        /// </summary>
        string UploadSuccess { get; }
    }
}
