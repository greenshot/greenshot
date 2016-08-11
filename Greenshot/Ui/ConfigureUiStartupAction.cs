/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Dapplo.Addons;
using Dapplo.CaliburnMicro;
using Dapplo.CaliburnMicro.Metro;

namespace Greenshot.Ui
{
	[StartupAction(StartupOrder = (int)CaliburnStartOrder.Bootstrapper + 1)]
	public class ConfigureUiStartupAction : IStartupAction
	{
		[Import(typeof(IWindowManager))]
		private MetroWindowManager MetroWindowManager { get; set; }

		public Task StartAsync(CancellationToken token = new CancellationToken())
		{
			var demoUri = new Uri("pack://application:,,,/Greenshot;component/Ui/GreenshotResourceDirectory.xaml", UriKind.RelativeOrAbsolute);
			MetroWindowManager.AddResourceDictionary(demoUri);

			// TODO: add theme support later
			// MetroWindowManager.ChangeTheme(CoreConfiguration.Theme);
			// MetroWindowManager.ChangeThemeAccent(CoreConfiguration.ThemeAccent);
			return Task.FromResult(true);
		}
	}
}
