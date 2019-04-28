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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
using Autofac.Features.AttributeFilters;
using Dapplo.Addons;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Security;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Greenshot.Addons.Components;
using Greenshot.Addons.Interfaces;
using Greenshot.Components;
using Greenshot.Configuration;
using Greenshot.Configuration.Impl;
using Greenshot.Forms;
using Greenshot.Helpers;
using Greenshot.Helpers.Mapi;
using Greenshot.Processors;
using Greenshot.Ui.Configuration.ViewModels;
using Greenshot.Ui.Misc.ViewModels;
using Greenshot.Ui.Notifications.ViewModels;
using ToastNotifications.Core;

namespace Greenshot
{
    /// <inheritdoc />
    public class GreenshotAutofacModule : AddonModule
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Specify the directories for the translations manually, this is a workaround
            builder.Register(context => LanguageConfigBuilder.Create()
                    .WithSpecificDirectories(GenerateScanDirectories(
#if NET472
                    "net472",
#else
                            "netcoreapp3.0",
#endif
                            "Greenshot.Addon.LegacyEditor",
                            "Greenshot").ToArray()
                    )
                    .BuildLanguageConfig())
                .As<LanguageConfig>()
                .SingleInstance();

            builder
                .RegisterType<MetroConfigurationImpl>()
                .As<IMetroConfiguration>()
                .As<IIniSection>()
                .SingleInstance();

            builder
                .RegisterType<CaptureSupportInfo>()
                .AsSelf()
                .SingleInstance()
                .OnActivated(args =>
                {
                    // Workaround for static access in different helper classes and extensions
                    MapiMailMessage.CoreConfiguration = args.Instance.CoreConfiguration;
                });

            builder
                .RegisterType<ConfigTranslationsImpl>()
                .As<IConfigTranslations>()
                .As<ILanguage>()
                .SingleInstance();

            builder
                .RegisterType<ConfigViewModel>()
                .As<Config<IConfigScreen>>()
                .AsSelf();

            builder
                .RegisterType<AuthenticationProvider>()
                .As<IAuthenticationProvider>()
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterType<MainForm>()
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterType<PrintHelper>()
                .AsSelf();

            builder
                .RegisterType<PrintOptionsDialog>()
                .AsSelf();

            builder
                .RegisterType<LanguageDialog>()
                .AsSelf();

            builder
                .RegisterType<AboutForm>()
                .AsSelf();

            // Configuration
            builder
                .RegisterAssemblyTypes(ThisAssembly)
                .AssignableTo<IConfigScreen>()
                .As<IConfigScreen>()
                .SingleInstance();

            // Startup and Shutdown
            builder
                .RegisterAssemblyTypes(ThisAssembly)
                .AssignableTo<IService>()
                .As<IService>()
                .AsSelf()
                .AsImplementedInterfaces()
                .WithAttributeFiltering()
                .SingleInstance();
            
            builder
                .RegisterType<ErrorViewModel>()
                .AsSelf();

            builder
                .RegisterType<WindowHandle>()
                .AsSelf()
                .SingleInstance();

            // Processors
            builder
                .RegisterType<TitleFixProcessor>()
                .As<IProcessor>()
                .SingleInstance();

            // Destinations
            builder
                .RegisterAssemblyTypes(ThisAssembly)
                .AssignableTo<IDestination>()
                .As<IDestination>()
                .SingleInstance();

            builder.Register(context => new DisplayOptions { TopMost = true, Width = 400 })
                .As<DisplayOptions>()
                .SingleInstance();

            // Toasts - Not a single instance
            builder.RegisterType<UpdateNotificationViewModel>()
                .AsSelf();

            base.Load(builder);
        }


        /// <summary>
        /// Helper method to create a list of paths where the files can be located
        /// </summary>
        /// <param name="platform">string with the platform</param>
        /// <param name="addons"></param>
        /// <returns>IEnumerable with paths</returns>
        private IEnumerable<string> GenerateScanDirectories(string platform, params string[] addons)
        {
            var location = GetType().Assembly.Location;
            foreach (var addon in addons)
            {
                yield return Path.Combine(location, @"..\..\..\..\..\", addon, "bin",
#if DEBUG
                    "Debug",
#else
                    "Release",
#endif
                    platform
                );
            }
        }
    }
}
