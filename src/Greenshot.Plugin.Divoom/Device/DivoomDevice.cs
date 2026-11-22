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

namespace Greenshot.Plugin.Divoom.Device;

/// <summary>
/// A Divoom device in the network, as the server of Divoom lists it
/// </summary>
public sealed class DivoomDevice
{
    public DivoomDevice(string name, string address)
    {
        Name = string.IsNullOrWhiteSpace(name) ? address : name.Trim();
        Address = address;
    }

    /// <summary>
    /// The name the device has in the Divoom app
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The IP address of the device in the network
    /// </summary>
    public string Address { get; }

    public string DisplayText => Name == Address ? Address : $"{Name} ({Address})";

    public override string ToString() => DisplayText;
}
