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
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Plugin.Divoom
{
    /// <summary>
    /// The texts of the [Divoom] section of greenshot.divoom.{ietf}.ini, the English text is shown per property.
    /// Add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Divoom", ModuleName = "divoom")]
    public interface IDivoomLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Capture for Divoom
        /// </summary>
        string CaptureMenu { get; }

        /// <summary>
        /// Divoom
        /// </summary>
        string Destination { get; }

        /// <summary>
        /// Shows a part of the screen on a Divoom pixel display in your network (Wi-Fi), e.g. a Pixoo 64. "Capture for Divoom" in the menu starts the capture with the size of its screen, the preview shows what the device will show; a click or Enter sends it.
        /// </summary>
        string SettingsIntro { get; }

        /// <summary>
        /// Address of the device:
        /// </summary>
        string DeviceAddress { get; }

        /// <summary>
        /// The IP address of the device (e.g. 192.168.1.20), the Divoom app shows it in the settings of the device.
        /// </summary>
        string DeviceAddressHint { get; }

        /// <summary>
        /// Find devices
        /// </summary>
        string FindDevices { get; }

        /// <summary>
        /// Asks the server of Divoom (app.divoom-gz.com) which of your devices are in this network.
        /// </summary>
        string FindDevicesHint { get; }

        /// <summary>
        /// Devices:
        /// </summary>
        string FoundDevices { get; }

        /// <summary>
        /// No device found: is it switched on and in the same network as this computer?
        /// </summary>
        string NoDevicesFound { get; }

        /// <summary>
        /// Finding the devices failed: {0}
        /// </summary>
        string FindFailed { get; }

        /// <summary>
        /// Size of the screen:
        /// </summary>
        string PanelSize { get; }

        /// <summary>
        /// {0} × {0} pixels
        /// </summary>
        string PanelSizePixels { get; }

        /// <summary>
        /// Send test picture
        /// </summary>
        string SendTest { get; }

        /// <summary>
        /// The test picture was sent, the device shows it now.
        /// </summary>
        string TestSent { get; }

        /// <summary>
        /// Enter the address of the Divoom device in the settings of the Divoom plugin first.
        /// </summary>
        string NoAddress { get; }

        /// <summary>
        /// The address "{0}" isn't the address of a device in the network.
        /// </summary>
        string InvalidAddress { get; }

        /// <summary>
        /// The Divoom device at {0} didn't take the picture: {1}
        /// </summary>
        string SendFailed { get; }
    }
}
