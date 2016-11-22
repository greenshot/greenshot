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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Disposables;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.CaliburnMicro.Metro;
using Dapplo.Utils.Extensions;
using Greenshot.Addon.Configuration;

#endregion

namespace Greenshot.Ui.Config.ViewModels
{
	[Export(typeof(IConfigScreen))]
	public sealed class ThemeConfigViewModel : ConfigScreen
	{
		/// <summary>
		///     Here all disposables are registered, so we can clean the up
		/// </summary>
		private CompositeDisposable _disposables;

		/// <summary>
		///     The avaible theme accents
		/// </summary>
		public ObservableCollection<Tuple<ThemeAccents, string>> AvailableThemeAccents { get; set; } = new ObservableCollection<Tuple<ThemeAccents, string>>();

		/// <summary>
		///     The avaible themes
		/// </summary>
		public ObservableCollection<Tuple<Themes, string>> AvailableThemes { get; set; } = new ObservableCollection<Tuple<Themes, string>>();

		[Import]
		public ICoreConfiguration CoreConfiguration { get; set; }

		[Import]
		public IGreenshotLanguage GreenshotLanguage { get; set; }

		[Import(typeof(IWindowManager))]
		private MetroWindowManager MetroWindowManager { get; set; }

		public override void Commit()
		{
			// Manually commit
			CoreConfiguration.CommitTransaction();
			MetroWindowManager.ChangeTheme(CoreConfiguration.Theme);
			MetroWindowManager.ChangeThemeAccent(CoreConfiguration.ThemeAccent);
			base.Commit();
		}

		public override void Initialize(IConfig config)
		{
			// Prepare disposables
			_disposables?.Dispose();
			_disposables = new CompositeDisposable();

			AvailableThemeAccents.Clear();
			foreach (var themeAccent in Enum.GetValues(typeof(ThemeAccents)).Cast<ThemeAccents>())
			{
				var translation = themeAccent.EnumValueOf();
				AvailableThemeAccents.Add(new Tuple<ThemeAccents, string>(themeAccent, translation));
			}

			AvailableThemes.Clear();
			foreach (var theme in Enum.GetValues(typeof(Themes)).Cast<Themes>())
			{
				var translation = theme.EnumValueOf();
				AvailableThemes.Add(new Tuple<Themes, string>(theme, translation));
			}

			// Place this under the Ui parent
			ParentId = nameof(ConfigIds.Ui);

			// Make sure Commit/Rollback is called on the IUiConfiguration
			config.Register(CoreConfiguration);


			// automatically update the DisplayName
			var greenshotLanguageBinding = GreenshotLanguage.CreateBinding(this, nameof(IGreenshotLanguage.SettingsTitle));

			// Make sure the greenshotLanguageBinding is disposed when this is no longer active
			_disposables.Add(greenshotLanguageBinding);

			base.Initialize(config);
		}

		protected override void OnDeactivate(bool close)
		{
			_disposables.Dispose();
			base.OnDeactivate(close);
		}
	}
}