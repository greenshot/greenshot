#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System.Diagnostics;
using System.IO;
using Autofac;
using Dapplo.Addons;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Greenshot.Addon.OCR.Configuration;
using Greenshot.Addon.OCR.Configuration.Impl;
using Greenshot.Addons.Components;

namespace Greenshot.Addon.OCR
{
    /// <inheritdoc />
    public class OcrAddonModule : AddonModule
    {
        private bool HasModi()
        {
            var ocrCommand = Path.Combine(FileTools.NormalizeDirectory(Path.GetDirectoryName(GetType().Assembly.Location)), "greenshotocrcommand.exe");
            try
            {
                using (var process = Process.Start(ocrCommand, "-c"))
                {
                    if (process != null)
                    {
                        process.WaitForExit();
                        return process.ExitCode == 0;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }

        /// <inheritdoc />
        protected override void Load(ContainerBuilder builder)
        {
            if (HasModi())
            {
                builder
                    .RegisterType<OcrConfigurationImpl>()
                    .As<IOcrConfiguration>()
                    .As<IIniSection>()
                    .SingleInstance();

                builder
                    .RegisterType<OcrLanguageImpl>()
                    .As<IOcrLanguage>()
                    .As<ILanguage>()
                    .SingleInstance();

                builder
                    .RegisterType<OcrDestination>()
                    .As<IDestination>()
                    .SingleInstance();
            }
            base.Load(builder);
        }
    }
}
