﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Autofac;
using Dapplo.Addons;
using Greenshot.Addons.Interfaces;
using Dapplo.Windows.Common;
using Greenshot.Addons.Components;

namespace Greenshot.Addon.Win10
{
    /// <inheritdoc />
    public class Win10AddonModule : AddonModule
    {
        protected override void Load(ContainerBuilder builder)
        {
            if (WindowsVersion.IsWindows10OrLater)
            {
                builder
                    .RegisterType<Win10OcrDestination>()
                    .As<IDestination>()
                    .SingleInstance();

#if !NETCOREAPP3_0
                builder
                    .RegisterType<Win10FormEnhancer>()
                    .As<IFormEnhancer>()
                    .SingleInstance();
#endif

                builder
                    .RegisterType<Win10ShareDestination>()
                    .As<IDestination>()
                    .SingleInstance();
            }
            base.Load(builder);
        }
    }
}
