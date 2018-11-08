#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System.ComponentModel;
using Dapplo.Config.Language;

namespace Greenshot.Addon.Lutim
{
    [Language("Lutim")]
    public interface ILutimLanguage : ILanguage, INotifyPropertyChanged
    {
        string Cancel { get; }

        string ClearQuestion { get; }

        string CommunicationWait { get; }

        string Configure { get; }

        string DeleteQuestion { get; }

        string DeleteTitle { get; }

        string History { get; }

        string LabelClear { get; }

        string LabelUploadFormat { get; }

        string LabelUrl { get; }

        string Ok { get; }

        string SettingsTitle { get; }

        string UploadFailure { get; }

        string UploadMenuItem { get; }

        string UploadSuccess { get; }

        string UsePageLink { get; }

        string AnonymousAccess { get; }
    }
}
