﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Dapplo.Windows.Common.Extensions;

namespace Greenshot.Plugin.Win10.Native
{
    /// <summary>
    /// Wraps the interop for calling the ShareUI
    /// </summary>
    public class DataTransferManagerHelper
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DataTransferManagerHelper));

        private const string DataTransferManagerId = "a5caee9b-8708-49d1-8d36-67d25a8da00c";
        private readonly IDataTransferManagerInterOp _dataTransferManagerInterOp;
        private readonly IntPtr _windowHandle;

        /// <summary>
        /// The DataTransferManager
        /// </summary>
        public DataTransferManager DataTransferManager { get; private set; }

        /// <summary>
        /// Constructor which takes a handle to initialize
        /// </summary>
        /// <param name="handle"></param>
        public DataTransferManagerHelper(IntPtr handle)
        {
            //TODO: Add a check for failure here. This will fail for versions of Windows below Windows 10
            IActivationFactory activationFactory = WindowsRuntimeMarshal.GetActivationFactory(typeof(DataTransferManager));

            // ReSharper disable once SuspiciousTypeConversion.Global
            _dataTransferManagerInterOp = (IDataTransferManagerInterOp)activationFactory;

            _windowHandle = handle;
            var riid = new Guid(DataTransferManagerId);
            var hresult = _dataTransferManagerInterOp.GetForWindow(_windowHandle, riid, out var dataTransferManager);
            if (hresult.Failed())
            {
                Log.WarnFormat("HResult for GetForWindow: {0}", hresult);
            }

            DataTransferManager = dataTransferManager;
        }

        /// <summary>
        /// Show the share UI
        /// </summary>
        public void ShowShareUi()
        {
            var hresult = _dataTransferManagerInterOp.ShowShareUIForWindow(_windowHandle);
            if (hresult.Failed())
            {
                Log.WarnFormat("HResult for ShowShareUIForWindow: {0}", hresult);
            }
            else
            {
                Log.Debug("ShowShareUIForWindow called");
            }
        }
    }
}