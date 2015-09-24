/*
 * A Picasa Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
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

using Dapplo.Config.Language;
using System.ComponentModel;

namespace GreenshotPicasaPlugin {
	[Language("Picasa")]
	public interface IPicasaLanguage : ILanguage, INotifyPropertyChanged
	{
		string UploadMenuItem {
			get;
		}
		string SettingsTitle
		{
			get;
		}
		string LabelUploadFormat
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
		string Configure
		{
			get;
		}
		string LabelAfterUpload
		{
			get;
		}
		string LabelAfterUploadLinkToClipBoard
		{
			get;
		}
	}
}
