/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Win10.Destinations;
using Greenshot.Plugin.Win10.Processors;

namespace Greenshot.Plugin.Win10
{
    /// <summary>
    /// This is the Win10Plugin
    /// </summary>
    public sealed class Win10Plugin : IGreenshotPlugin
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Win10Plugin));

        public void Dispose()
        {
            // Nothing to dispose
        }

        public void Configure()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string Name => "Win10";

        /// <summary>
        /// Specifies if the plugin can be configured
        /// </summary>
        public bool IsConfigurable => false;

        /// <summary>
        /// Implementation of the IGreenshotPlugin.Initialize
        /// </summary>
        /// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
        public bool Initialize()
        {
            // Here we check if the build version of Windows is actually what we support
            if (!WindowsVersion.IsWindows10BuildOrLater(17763))
            {
                Log.WarnFormat("No support for Windows build {0}", WindowsVersion.BuildVersion);
                return false;
            }

            SimpleServiceProvider.Current.AddService<INotificationService>(ToastNotificationService.Create());
            // Set this as IOcrProvider
            SimpleServiceProvider.Current.AddService<IOcrProvider>(new Win10OcrProvider());
            // Add the processor
            SimpleServiceProvider.Current.AddService<IProcessor>(new Win10OcrProcessor());

            // Add the destinations
            SimpleServiceProvider.Current.AddService<IDestination>(new Win10OcrDestination());
            SimpleServiceProvider.Current.AddService<IDestination>(new Win10ShareDestination());
            return true;
        }

        public void Shutdown()
        {
            // Nothing to shutdown
        }
    }
}