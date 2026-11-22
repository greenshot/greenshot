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

using System.ComponentModel;
using Dapplo.Ini.Attributes;
using Dapplo.Ini.Interfaces;

namespace Greenshot.Plugin.Divoom;

[IniSection("Divoom")]
[Description("Greenshot Divoom plugin configuration (beta)")]
public interface IDivoomConfiguration : IIniSection
{
    [Description("The address of the Divoom device in the network, its IP address (e.g. 192.168.1.20) or host name")]
    [DefaultValue("")]
    string DeviceAddress { get; set; }

    [Description("The name of the Divoom device, as the Divoom app shows it, only for the settings")]
    [DefaultValue("")]
    string DeviceName { get; set; }

    [Description("The width and height of the screen of the device in pixels: 16, 32 or 64 (Pixoo 64), the capture has this size")]
    [DefaultValue(64)]
    int PanelSize { get; set; }
}
