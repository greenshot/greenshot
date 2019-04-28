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

using Autofac;
using Dapplo.Addons;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Greenshot.Addons.Components;
using Greenshot.Addons.Config.Impl;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Resources;
using Greenshot.Addons.ViewModels;

namespace Greenshot.Addons
{
    /// <inheritdoc />
    public class AddonsModule : AddonModule
    {
        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<CoreConfigurationImpl>()
                .As<ICoreConfiguration>()
                .As<IUiConfiguration>()
                .As<IIniSection>()
                .SingleInstance()
                .OnActivated(args =>
                {
                    // Workaround for static access in different helper classes and extensions
                    ClipboardHelper.CoreConfiguration = args.Instance;
                    FilenameHelper.CoreConfiguration = args.Instance;
                    ImageOutput.CoreConfiguration = args.Instance;
                    InteropWindowCaptureExtensions.CoreConfiguration = args.Instance;
                    WindowCapture.CoreConfiguration = args.Instance;
                    PluginUtils.CoreConfiguration = args.Instance;
                });

            builder
                .RegisterType<GreenshotLanguageImpl>()
                .As<IGreenshotLanguage>()
                .As<ILanguage>()
                .SingleInstance()
                .OnActivated(args =>
                {
                    // Workaround for static access in different helper classes and extensions
                    ImageOutput.GreenshotLanguage = args.Instance;
                });

            builder
                .RegisterType<HttpConfigurationImpl>()
                .As<IHttpConfiguration>()
                .As<IIniSection>()
                .SingleInstance();

            builder
                .RegisterType<FileConfigPartViewModel>()
                .AsSelf();

            builder
                .RegisterType<DestinationHolder>()
                .AsSelf();

            builder
                .RegisterType<PleaseWaitForm>()
                .AsSelf();

            builder.RegisterType<ExportNotification>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<ExportNotificationViewModel>()
                .AsSelf();

            builder
                .RegisterType<GreenshotResources>()
                .AsSelf()
                .SingleInstance()
                .AutoActivate();

            base.Load(builder);
        }
    }
}
