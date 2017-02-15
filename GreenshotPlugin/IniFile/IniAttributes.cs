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

namespace GreenshotPlugin.IniFile
{
	/// <summary>
	///     Attribute for telling that this class is linked to a section in the ini-configuration
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class IniSectionAttribute : Attribute
	{
		public string Description;

		public IniSectionAttribute(string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}

	/// <summary>
	///     Attribute for telling that a field is linked to a property in the ini-configuration selection
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class IniPropertyAttribute : Attribute
	{
		public IniPropertyAttribute()
		{
			Separator = ",";
		}

		public IniPropertyAttribute(string name) : this()
		{
			Name = name;
		}

		public string Description { get; set; }

		public string Separator { get; set; }

		public string DefaultValue { get; set; }

		public string LanguageKey { get; set; }

		// If Encrypted is set to true, the value will be decrypted on load and encrypted on save
		public bool Encrypted { get; set; }

		public bool FixedValue { get; set; }

		public bool Expert { get; set; }

		public bool ExcludeIfNull { get; set; }

		public string Name { get; set; }
	}
}