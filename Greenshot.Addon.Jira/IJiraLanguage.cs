/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using Dapplo.Config.Language;

namespace Greenshot.Addon.Jira
{
	[Language("Jira")]
	public interface IJiraLanguage : ILanguage, INotifyPropertyChanged
	{
		string UploadMenuItem
		{
			get;
		}

		string ColumnAssignee
		{
			get;
		}

		string ColumnCreated
		{
			get;
		}

		string ColumnId
		{
			get;
		}

		string ColumnReporter
		{
			get;
		}

		string ColumnSummary
		{
			get;
		}

		string LabelComment
		{
			get;
		}

		string LabelFilename
		{
			get;
		}

		string LabelJira
		{
			get;
		}

		string LabelJirafilter
		{
			get;
		}

		string LoginError
		{
			get;
		}

		string LoginTitle
		{
			get;
		}

		string SettingsTitle
		{
			get;
		}

		string LabelUrl
		{
			get;
		}

		string LabelUploadFormat
		{
			get;
		}

		string Ok
		{
			get;
		}

		string Cancel
		{
			get;
		}

		string UploadSuccess
		{
			get;
		}

		string UploadFailure
		{
			get;
		}

		string CommunicationWait
		{
			get;
		}
	}
}