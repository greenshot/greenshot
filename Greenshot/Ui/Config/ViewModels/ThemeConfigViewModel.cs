using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.CaliburnMicro.Metro;
using Dapplo.Utils;
using Dapplo.Utils.Extensions;
using Greenshot.Addon.Configuration;

namespace Greenshot.Ui.Config.ViewModels
{
	[Export(typeof(IConfigScreen))]
	public sealed class ThemeConfigViewModel : ConfigScreen
	{
		private readonly Disposables _disposables = new Disposables();

		/// <summary>
		/// The avaible themes
		/// </summary>
		public ObservableCollection<Tuple<Themes, string>> AvailableThemes { get; set; } = new ObservableCollection<Tuple<Themes, string>>();

		/// <summary>
		/// The avaible theme accents
		/// </summary>
		public ObservableCollection<Tuple<ThemeAccents, string>> AvailableThemeAccents { get; set; } = new ObservableCollection<Tuple<ThemeAccents, string>>();

		[Import]
		public IGreenshotLanguage GreenshotLanguage { get; set; }

		[Import]
		public ICoreConfiguration CoreConfiguration { get; set; }

		[Import(typeof(IWindowManager))]
		private MetroWindowManager MetroWindowManager { get; set; }

		public override void Initialize(IConfig config)
		{
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
			_disposables.Add(this.BindDisplayName(GreenshotLanguage, nameof(IGreenshotLanguage.SettingsTitle)));

			base.Initialize(config);
		}

		protected override void OnDeactivate(bool close)
		{
			_disposables.Dispose();
			base.OnDeactivate(close);
		}

		public override void Commit()
		{
			// Manually commit
			CoreConfiguration.CommitTransaction();
			MetroWindowManager.ChangeTheme(CoreConfiguration.Theme);
			MetroWindowManager.ChangeThemeAccent(CoreConfiguration.ThemeAccent);
			base.Commit();
		}
	}
}
