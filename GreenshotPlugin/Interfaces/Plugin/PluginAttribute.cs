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

using System;

#endregion

namespace GreenshotPlugin.Interfaces.Plugin
{
	[Serializable]
	[AttributeUsage(AttributeTargets.Assembly)]
	public sealed class PluginAttribute : Attribute, IComparable
	{
		public PluginAttribute(string entryType, bool configurable)
		{
			EntryType = entryType;
			Configurable = configurable;
		}

		public string Name { get; set; }

		public string CreatedBy { get; set; }

		public string Version { get; set; }

		public string EntryType { get; private set; }

		public bool Configurable { get; private set; }

		public string DllFile { get; set; }

		public int CompareTo(object obj)
		{
			var other = obj as PluginAttribute;
			if (other != null)
			{
				return Name.CompareTo(other.Name);
			}
			throw new ArgumentException("object is not a PluginAttribute");
		}
	}

	// Delegates for hooking up events.
}