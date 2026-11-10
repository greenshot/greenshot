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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.Divoom.Device;

namespace Greenshot.Plugin.Divoom.ViewModels;

/// <summary>
/// The settings of the plugin: the device (its address, or one found in the network) and the size of its screen
/// </summary>
public sealed class DivoomSettingsViewModel : INotifyPropertyChanged
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DivoomSettingsViewModel));
    private readonly IDivoomConfiguration _config;
    private readonly RelayCommand _findDevicesCommand;
    private readonly RelayCommand _sendTestCommand;
    private DivoomDevice _selectedDevice;
    private string _status;
    private bool _isBusy;

    public DivoomSettingsViewModel(IDivoomConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        PanelSizes = DivoomImage.PanelSizes
            .Select(size => new PanelSizeChoice(size, string.Format(Language.PanelSizePixels, size)))
            .ToList();
        _findDevicesCommand = new RelayCommand(() => AsyncCommand.Run(FindDevicesAsync, "Find the Divoom devices"), () => !IsBusy);
        _sendTestCommand = new RelayCommand(() => AsyncCommand.Run(SendTestAsync, "Send a test picture to the Divoom device"),
            () => !IsBusy && DivoomClient.GetCommandUri(DeviceAddress) != null);
    }

    private static IDivoomLanguage Language => Texts.Get<IDivoomLanguage>();

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// The address of the device, typed or taken from a found device
    /// </summary>
    public string DeviceAddress
    {
        get => _config.DeviceAddress;
        set
        {
            if (_config.DeviceAddress == value)
            {
                return;
            }
            _config.DeviceAddress = value;
            // A typed address is another device than the found one
            if (_selectedDevice != null && _selectedDevice.Address != value?.Trim())
            {
                _selectedDevice = null;
                _config.DeviceName = string.Empty;
                OnPropertyChanged(nameof(SelectedDevice));
            }
            OnPropertyChanged();
            _sendTestCommand.RaiseCanExecuteChanged();
        }
    }

    public IReadOnlyList<PanelSizeChoice> PanelSizes { get; }

    public int PanelSize
    {
        get => DivoomImage.NormalizeSize(_config.PanelSize);
        set
        {
            if (_config.PanelSize == value)
            {
                return;
            }
            _config.PanelSize = DivoomImage.NormalizeSize(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// The devices "Find devices" found
    /// </summary>
    public ObservableCollection<DivoomDevice> Devices { get; } = new ObservableCollection<DivoomDevice>();

    public bool HasDevices => Devices.Count > 0;

    /// <summary>
    /// A found device: it becomes the device of the settings
    /// </summary>
    public DivoomDevice SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (_selectedDevice == value)
            {
                return;
            }
            _selectedDevice = value;
            if (value != null)
            {
                _config.DeviceName = value.Name;
                _config.DeviceAddress = value.Address;
                OnPropertyChanged(nameof(DeviceAddress));
                _sendTestCommand.RaiseCanExecuteChanged();
            }
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// What the last action did, null for nothing
    /// </summary>
    public string Status
    {
        get => _status;
        private set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            OnPropertyChanged();
            _findDevicesCommand.RaiseCanExecuteChanged();
            _sendTestCommand.RaiseCanExecuteChanged();
        }
    }

    public ICommand FindDevicesCommand => _findDevicesCommand;

    public ICommand SendTestCommand => _sendTestCommand;

    private async Task FindDevicesAsync()
    {
        IsBusy = true;
        Status = null;
        try
        {
            var devices = await DivoomClient.FindDevicesAsync(CancellationToken.None).ConfigureAwait(true);
            Devices.Clear();
            foreach (var device in devices)
            {
                Devices.Add(device);
            }
            OnPropertyChanged(nameof(HasDevices));
            if (devices.Count == 0)
            {
                Status = Language.NoDevicesFound;
            }
            else
            {
                // The device of the settings, or the only one
                SelectedDevice = devices.FirstOrDefault(device => device.Address == _config.DeviceAddress?.Trim())
                                 ?? (devices.Count == 1 ? devices[0] : null);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            Log.Warn("Finding the Divoom devices failed", ex);
            Status = string.Format(Language.FindFailed, DivoomClient.Describe(ex));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SendTestAsync()
    {
        var commandUri = DivoomClient.GetCommandUri(DeviceAddress);
        if (commandUri == null)
        {
            return;
        }
        IsBusy = true;
        Status = null;
        try
        {
            int size = PanelSize;
            await DivoomClient.SendPictureAsync(commandUri, size, DivoomImage.CreateTestPicture(size), CancellationToken.None).ConfigureAwait(true);
            Status = Language.TestSent;
        }
        catch (Exception ex) when (ex is DivoomException or HttpRequestException or TimeoutException)
        {
            Log.Warn($"Sending the test picture to the Divoom device at {commandUri.Host} failed", ex);
            Status = string.Format(Language.SendFailed, commandUri.Host, DivoomClient.Describe(ex));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
