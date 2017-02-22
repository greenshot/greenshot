#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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

#region Usings

using GreenshotPlugin.Interop;

#endregion

namespace GreenshotOfficePlugin.OfficeInterop
{
	/// <summary>
	///     Wrapper for Outlook.Application, see: http://msdn.microsoft.com/en-us/library/aa210897%28v=office.11%29.aspx
	///     This is the initial COM-Object which is created/retrieved
	/// </summary>
	[ComProgId("Outlook.Application")]
	public interface IOutlookApplication : IComCommon
	{
		string Name { get; }

		string Version { get; }

		IInspectors Inspectors { get; }

		IExplorers Explorers { get; }

		IItem CreateItem(OlItemType itemType);
		object CreateItemFromTemplate(string templatePath, object inFolder);
		object CreateObject(string objectName);
		IInspector ActiveInspector();
		INameSpace GetNameSpace(string type);
		IExplorer ActiveExplorer();
	}
}