#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

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