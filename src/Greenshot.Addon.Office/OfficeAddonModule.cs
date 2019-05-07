// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using Autofac;
using Dapplo.Addons;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Greenshot.Addon.Office.Configuration;
using Greenshot.Addon.Office.Configuration.Impl;
using Greenshot.Addon.Office.Destinations;
using Greenshot.Addon.Office.OfficeExport;
using Greenshot.Addon.Office.ViewModels;
using Greenshot.Addons.Components;
using Microsoft.Win32;

namespace Greenshot.Addon.Office
{
    /// <inheritdoc />
    public class OfficeAddonModule : AddonModule
    {
        private const string Word = "Word.Application";
        private const string Excel = "Exceldd.Application";
        private const string PowerPoint = "PowerPoint.Application";
        private const string OneNote = "OneNote.Application";
        private const string Outlook = @"Outlook.Application";

        /// <summary>
        /// Helper method to test the registry for the existence of an application
        /// </summary>
        /// <param name="applicationName">string with the application name</param>
        /// <returns></returns>
        private bool HasApplication(string applicationName)
        {
            string registryPath = $@"{applicationName}\CLSID";
            using (var registryKey = Registry.ClassesRoot.OpenSubKey(registryPath, false))
            {
                return registryKey != null && Guid.TryParse(registryKey.GetValue(null) as string, out _);
            }
        }
        
        /// <summary>
        /// Define the dependencies of this project
        /// </summary>
        /// <param name="builder">ContainerBuilder</param>
        protected override void Load(ContainerBuilder builder)
        {
            var hasDestination = false;

            if (HasApplication(Excel))
            {
                hasDestination = true;
                builder
                    .RegisterType<ExcelDestination>()
                    .As<IDestination>()
                    .SingleInstance();
            }

            if (HasApplication(Word))
            {
                hasDestination = true;
                builder
                    .RegisterType<WordDestination>()
                    .As<IDestination>()
                    .SingleInstance();
            }

            if (HasApplication(PowerPoint))
            {
                hasDestination = true;
                builder
                    .RegisterType<PowerpointDestination>()
                    .As<IDestination>()
                    .SingleInstance();
            }

            if (HasApplication(OneNote))
            {
                hasDestination = true;
                builder
                    .RegisterType<OneNoteDestination>()
                    .As<IDestination>()
                    .SingleInstance();

                builder
                    .RegisterType<OneNoteExporter>()
                    .AsSelf()
                    .SingleInstance();
            }

            if (HasApplication(Outlook))
            {
                hasDestination = true;
                builder
                    .RegisterType<OutlookDestination>()
                    .As<IDestination>()
                    .SingleInstance();
            }

            if (hasDestination)
            {
                builder
                    .RegisterType<OfficeConfigurationImpl>()
                    .As<IOfficeConfiguration>()
                    .As<IIniSection>()
                    .SingleInstance();


                builder
                    .RegisterType<OfficeLanguageImpl>()
                    .As<IOfficeLanguage>()
                    .As<ILanguage>()
                    .SingleInstance();

                builder
                    .RegisterType<OfficeConfigViewModel>()
                    .As<IConfigScreen>()
                    .SingleInstance();
            }

            base.Load(builder);
        }
    }
}
