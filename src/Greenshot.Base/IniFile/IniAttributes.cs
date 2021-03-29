/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using System;

namespace Greenshot.Base.IniFile
{
    /// <summary>
    /// Attribute for telling that this class is linked to a section in the ini-configuration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class IniSectionAttribute : Attribute
    {
        public IniSectionAttribute(string name)
        {
            Name = name;
        }

        public string Description;
        public string Name { get; set; }
    }

    /// <summary>
    /// Attribute for telling that a field is linked to a property in the ini-configuration selection
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