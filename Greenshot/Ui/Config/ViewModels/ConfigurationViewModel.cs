//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using Dapplo.CaliburnMicro;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Greenshot.Addon.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;

#endregion

namespace Greenshot.Ui.Config.ViewModels
{
	/// <summary>
	///     This represents the configuration window of Greenshot
	/// </summary>
	[Export]
	public class ConfigurationViewModel : Config<IConfigScreen>, IHaveIcon
	{
		/// <summary>
		///     Here all disposables are registered, so we can clean the up
		/// </summary>
		private CompositeDisposable _disposables;

		/// <summary>
		///     Get all settings controls, these are the items that are displayed.
		/// </summary>
		[ImportMany]
		public override IEnumerable<Lazy<IConfigScreen>> ConfigScreens { get; set; }

		[Import]
		private ICoreConfiguration CoreConfiguration { get; set; }

		/// <summary>
		///     Used to make it possible to show a MahApps dialog
		/// </summary>
		[Import]
		private IDialogCoordinator Dialogcoordinator { get; set; }

		/// <summary>
		///     Used to send events
		/// </summary>
		[Import]
		private IEventAggregator EventAggregator { get; set; }

		/// The following should work some day, importing translations from different packages
		//[Import]
		//public ICoreTranslations CoreTranslations { get; set; }

		//[Import]
		//public IConfigTranslations ConfigTranslations { get; set; }
		[Import]
		public IGreenshotLanguage GreenshotLanguage { get; set; }

		/// <summary>
		///     Used to show a "normal" dialog
		/// </summary>
		[Import]
		private IWindowManager WindowsManager { get; set; }

		/// <summary>
		///     Set the default config icon for the view
		/// </summary>
		public Control Icon => new PackIconMaterial
		{
			Kind = PackIconMaterialKind.Settings,
			Margin = new Thickness(10)
		};


		protected override void OnActivate()
		{
			// Prepare disposables
			_disposables?.Dispose();
			_disposables = new CompositeDisposable();

			// automatically update the DisplayName
			var greenshotLanguageBinding = GreenshotLanguage.CreateBinding(this, nameof(IGreenshotLanguage.SettingsTitle));

			// Make sure the greenshotLanguageBinding is disposed when this is no longer active
			_disposables.Add(greenshotLanguageBinding);

			base.OnActivate();
		}

		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);
			_disposables.Dispose();
		}
	}
}