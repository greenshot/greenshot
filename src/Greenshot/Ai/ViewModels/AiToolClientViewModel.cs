/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Wpf;
using Greenshot.Editor.Configuration;
using Greenshot.Helpers;
using Greenshot.Base.Threading;

namespace Greenshot.Ai.ViewModels
{
    /// <summary>
    /// A program allowed to use Greenshot through greenshot-mcp
    /// </summary>
    public sealed class AiToolClientViewModel : INotifyPropertyChanged
    {
        public AiToolClientViewModel(string path)
        {
            Path = path;
            FileName = System.IO.Path.GetFileName(path);
            Folder = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
        }

        /// <summary>
        /// The full path, as stored in AiToolsAllowedClients
        /// </summary>
        public string Path { get; }

        public string FileName { get; }

        public string Folder { get; }

        private string _displayName;

        /// <summary>
        /// The program's name from its version information, the file name until it is known
        /// </summary>
        public string DisplayName
        {
            get => _displayName ?? FileName;
            private set
            {
                _displayName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
            }
        }

        private string _signerText = "Checking the signature...";

        public string SignerText
        {
            get => _signerText;
            private set
            {
                _signerText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SignerText)));
            }
        }

        /// <summary>
        /// Reads the name and verifies the signature (slow, not on the UI thread)
        /// </summary>
        public async Task LoadDetailsAsync()
        {
            await ThreadPoolSwitch.SwitchToThreadPoolAsync();
            if (!File.Exists(Path))
            {
                SignerText = "The program isn't there anymore";
                return;
            }
            var client = AiToolCaller.Describe(Path);
            DisplayName = client.DisplayName;
            SignerText = string.IsNullOrEmpty(client.Signer) ? "Not signed" : $"Signed by {client.Signer}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString() => Path;
    }
}
