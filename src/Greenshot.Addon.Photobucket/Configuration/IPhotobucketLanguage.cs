//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.ComponentModel;
using Dapplo.Config.Language;

namespace Greenshot.Addon.Photobucket.Configuration
{
	/// <summary>
	/// This contains the translations for the photo bucket add-on
	/// </summary>
	[Language("Photobucket")]
	public interface IPhotobucketLanguage : ILanguage, INotifyPropertyChanged
	{
#pragma warning disable 1591
		string CommunicationWait { get; }

		string Configure { get; }

		string LabelClear { get; }

		string LabelUploadFormat { get; }

		string SettingsTitle { get; }

		string UploadFailure { get; }

		string UploadMenuItem { get; }

		string UploadSuccess { get; }

		string UsePageLink { get; }
	}
}