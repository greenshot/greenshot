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
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Greenshot.Plugin.Win10.Internal
{
    internal class ShareInfo
    {
        public string ApplicationName { get; set; }
        public bool AreShareProvidersRequested { get; set; }
        public bool IsDeferredFileCreated { get; set; }
        public DataPackageOperation CompletedWithOperation { get; set; }
        public string AcceptedFormat { get; set; }
        public bool IsDestroyed { get; set; }
        public bool IsShareCompleted { get; set; }

        public TaskCompletionSource<bool> ShareTask { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsDataRequested { get; set; }

        public IntPtr SharingHwnd { get; set; }
    }
}