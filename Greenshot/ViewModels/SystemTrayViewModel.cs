using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Caliburn.Micro;
using Dapplo.CaliburnMicro.Menu;
using Dapplo.CaliburnMicro.NotifyIconWpf;
using Dapplo.CaliburnMicro.NotifyIconWpf.ViewModels;
using Dapplo.Log.Facade;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Interfaces;
using Greenshot.Views;
using MahApps.Metro.IconPacks;
using System.Windows;

namespace Greenshot.ViewModels
{
	[Export(typeof(ITrayIconViewModel))]
	public class SystemTrayViewModel : TrayIconViewModel, IHandle<INotification>, IPartImportsSatisfiedNotification
	{
		private static readonly LogSource Log = new LogSource();

		[ImportMany("systray", typeof(IMenuItem))]
		private IEnumerable<IMenuItem> ContextMenuItems { get; set; }

		[Import]
		private IEventAggregator EventAggregator { get; set; }

		[Import]
		private ICoreConfiguration CoreConfiguration { get; set; }

		public void OnImportsSatisfied()
		{
			EventAggregator.Subscribe(this);

		}

		protected override void OnActivate()
		{
			var items = ContextMenuItems.ToList();
			items.Add(new MenuItem
			{
				IsSeparator = true,
				Id = "Y_Separator"
			});
			items.Add(new MenuItem
			{
				Id = "Y_Exit",
				Icon = new PackIconMaterial
				{
					Kind = PackIconMaterialKind.ExitToApp
				},
				ClickAction = item => Application.Current.Shutdown()
			});
			ConfigureMenuItems(items);

			// TODO: This doesn't work??
			var logo = new GreenshotLogo();
			SetIcon(logo);

			base.OnActivate();

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

		public void Handle(INotification notification)
		{
			// Check if the user wants to see notifications
			if (!CoreConfiguration.ShowTrayNotification)
			{
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
