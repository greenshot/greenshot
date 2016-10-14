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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Menu;
using Dapplo.CaliburnMicro.NotifyIconWpf;
using Dapplo.CaliburnMicro.NotifyIconWpf.ViewModels;
using Dapplo.Log.Facade;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Interfaces;
using MahApps.Metro.IconPacks;
using MenuItem = Dapplo.CaliburnMicro.Menu.MenuItem;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.Utils;
using Greenshot.Addon.Ui;
using Greenshot.Ui.Config.ViewModels;

namespace Greenshot.Ui.ViewModels
{
	/// <summary>
	/// This contains all the logic for the system-tray, which is the icon, context-menu and notification messages.
	/// </summary>
	[Export(typeof(ITrayIconViewModel))]
	public class SystemTrayViewModel : TrayIconViewModel, IHandle<INotification>, IPartImportsSatisfiedNotification
	{
		private static readonly LogSource Log = new LogSource();
		private readonly Disposables _disposables = new Disposables();

		[ImportMany("systray", typeof(IMenuItem))]
		private IEnumerable<IMenuItem> ContextMenuItems { get; set; }

		[Import]
		private IEventAggregator EventAggregator { get; set; }

		[Import]
		private ICoreConfiguration CoreConfiguration { get; set; }

		[Import]
		private IGreenshotLanguage GreenshotLanguage { get; set; }

		[Import]
		public ConfigurationViewModel ConfigurationViewModel { get; set; }


		[Import]
		public IWindowManager WindowManager { get; set; }

		/// <summary>Called when a part's imports have been satisfied and it is safe to use.</summary>
		public void OnImportsSatisfied()
		{
			// Subscribe "this" as handler for INotification events
			EventAggregator.Subscribe(this);
		}

		/// <summary>Called when deactivating.</summary>
		/// <param name="close">Inidicates whether this instance will be closed.</param>
		protected override void OnDeactivate(bool close)
		{
			base.OnDeactivate(close);
			_disposables.Dispose();
		}

		/// <summary>Called when activating.</summary>
		protected override void OnActivate()
		{
			var brushConverter = new BrushConverter();
			var logo = new PackIconGreenshot
			{
				Kind = PackIconKindGreenshot.Greenshot,
				Foreground = brushConverter.ConvertFromString("#FF9AFF00") as SolidColorBrush,
				Background = brushConverter.ConvertFromString("#FF3D3D3D") as SolidColorBrush
			};

			SetIcon(logo);

			base.OnActivate();

			BuildSystrayContextMenu();
			// Check if the user wan't to see the tray icon
			if (!CoreConfiguration.HideTrayicon)
			{
				Show();
			}
			else
			{
				Log.Info().WriteLine("System tray is disabled.");
			}
		}

		/// <summary>
		/// builds the context menu for the system tray, this should be called from the UI thread!
		/// </summary>
		private void BuildSystrayContextMenu()
		{
			var items = ContextMenuItems.ToList();
			if (items.Count > 0)
			{
				items.Add(new MenuItem
				{
					Style = MenuItemStyles.Separator,
					Id = "Y_Separator"
				});
				items.Add(new MenuItem
				{
					Style = MenuItemStyles.Separator,
					Id = "W_Separator"
				});
			}
			items.Add(new MenuItem
			{
				Style = MenuItemStyles.Title,
				Id = "A_Title",
				DisplayName = "Greenshot"
			});
			var exitMenuItem = new MenuItem
			{
				Id = "Z_Exit",
				Icon = new PackIconModern
				{
					Kind = PackIconModernKind.Close,
					Foreground = Brushes.DarkRed
				},
				ClickAction = item => Application.Current.Shutdown()
			};

			_disposables.Add(exitMenuItem.BindDisplayName(GreenshotLanguage, nameof(IGreenshotLanguage.ContextmenuExit)));
			items.Add(exitMenuItem);

			var configurationMenuItem = new MenuItem
			{
				Id = "X_Configure",
				Icon = new PackIconModern
				{
					Kind = PackIconModernKind.Settings
				},
				ClickAction = item =>
				{
					if (!ConfigurationViewModel.IsActive)
					{
						WindowManager.ShowDialog(ConfigurationViewModel);
					}
				}
			};

			_disposables.Add(configurationMenuItem.BindDisplayName(GreenshotLanguage, nameof(IGreenshotLanguage.SettingsTitle)));
			items.Add(configurationMenuItem);

			ConfigureMenuItems(items);
			items.ApplyIconMargin(new Thickness(2));
		}

		/// <summary>
		/// Implementation for the left-click on the icon
		/// </summary>
		public override void Click()
		{
			Log.Info().WriteLine("System tray was clicked.");

			switch (CoreConfiguration.LeftClickAction)
			{
				case ClickActions.CaptureLastRegion:
				case ClickActions.CaptureRegion:
				case ClickActions.CaptureWindow:
				case ClickActions.CaptureScreen:
				case ClickActions.DoNothing:
				case ClickActions.OpenLastInEditor:
				case ClickActions.OpenLastInExplorer:
					break;
			}
		}

		/// <summary>
		/// Handle INotification messages
		/// </summary>
		/// <param name="notification"></param>
		public void Handle(INotification notification)
		{
			// Check if the user wants to see notifications
			if (!CoreConfiguration.ShowTrayNotification)
			{
				// Ignore, user doesn't want to be disturbed
				return;
			}

			switch (notification.NotificationType)
			{
				case NotificationTypes.Fail:
					TrayIcon.ShowErrorBalloonTip(notification.Text, notification.ErrorText);
					break;
				case NotificationTypes.Success:
					TrayIcon.ShowInfoBalloonTip(notification.Text, notification.ErrorText);
					break;
			}
		}
	}
}
