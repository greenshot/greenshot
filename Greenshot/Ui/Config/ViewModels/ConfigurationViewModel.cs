/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using Dapplo.CaliburnMicro;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.Utils;
using Greenshot.Addon.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;

namespace Greenshot.Ui.Config.ViewModels
{
	/// <summary>
	/// This represents the configuration window of Greenshot
	/// </summary>
	[Export]
	public class ConfigurationViewModel : Config<IConfigScreen>, IHaveIcon
	{
		private readonly Disposables _disposables = new Disposables();

		//[Import]
		//public ICoreTranslations CoreTranslations { get; set; }

		//[Import]
		//public IConfigTranslations ConfigTranslations { get; set; }

		[Import]
		public IGreenshotLanguage GreenshotLanguage { get; set; }

		[Import]
		private ICoreConfiguration CoreConfiguration { get; set; }

		/// <summary>
		///     Used to send events
		/// </summary>
		[Import]
		private IEventAggregator EventAggregator { get; set; }

		/// <summary>
		///     Get all settings controls, these are the items that are displayed.
		/// </summary>
		[ImportMany]
		public override IEnumerable<IConfigScreen> ConfigScreens { get; set; }

		/// <summary>
		///     Used to show a "normal" dialog
		/// </summary>
		[Import]
		private IWindowManager WindowsManager { get; set; }

		/// <summary>
		///     Used to make it possible to show a MahApps dialog
		/// </summary>
		[Import]
		private IDialogCoordinator Dialogcoordinator { get; set; }

		/// <summary>
		/// Set the default config icon for the view
		/// </summary>
		public Control Icon => new PackIconMaterial
		{
			Kind = PackIconMaterialKind.Settings,
			Margin = new Thickness(10)
		};


		protected override void OnActivate()
		{
			// automatically update the DisplayName
			_disposables.Add(this.BindDisplayName(GreenshotLanguage, nameof(IGreenshotLanguage.SettingsTitle)));

			base.OnActivate();
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);
			_disposables.Dispose();
		}
	}
}