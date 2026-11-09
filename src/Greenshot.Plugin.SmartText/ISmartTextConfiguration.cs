/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

using System.Collections.Generic;
using System.ComponentModel;
using Dapplo.Ini.Attributes;
using Dapplo.Ini.Interfaces;

namespace Greenshot.Plugin.SmartText;

[IniSection("SmartText")]
[Description("Greenshot Smart text plugin configuration (beta)")]
public interface ISmartTextConfiguration : IIniSection
{
    [Description("On (True) or off (False) per Smart text detector: the id of a built-in one (e.g. Phone, IpAddress) or the name of an own pattern; one which isn't listed has its default (built-in ones see docs/vNext/smart-text.md, own patterns are on)")]
    Dictionary<string, bool> Detectors { get; set; }
}
