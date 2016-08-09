using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MenuItem = Dapplo.CaliburnMicro.Menu.MenuItem;

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

		private RenderTargetBitmap Render(Visual v)
		{
			// new a drawing visual and get its context
			DrawingVisual dv = new DrawingVisual();
			using (var dc = dv.RenderOpen())
			{
				// generate a visual brush by input, and paint
				VisualBrush vb = new VisualBrush(v);
				dc.DrawRectangle(vb, null, new Rect(0, 0, 16, 16));
			}
			var rtb = new RenderTargetBitmap(100,100,96d, 96d, PixelFormats.Pbgra32);
			rtb.Render(dv);
			return rtb;
		}

		public WriteableBitmap SaveAsWriteableBitmap(FrameworkElement frameworkElement)
		{
			if (frameworkElement == null) return null;

			// Save current canvas transform
			Transform transform = frameworkElement.LayoutTransform;
			// reset current transform (in case it is scaled or rotated)
			frameworkElement.LayoutTransform = null;

			frameworkElement.Width = 500;
			frameworkElement.Height = 500;
			// Get the size of canvas
			Size size = new Size(100, 100);
			// Measure and arrange the surface
			// VERY IMPORTANT
			frameworkElement.Measure(size);
			frameworkElement.Arrange(new Rect(new Point(), size));
			frameworkElement.UpdateLayout();

			// Create a render bitmap and push the surface to it
			RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
			  (int)size.Width,
			  (int)size.Height,
			  96d,
			  96d,
			  PixelFormats.Pbgra32);
			renderBitmap.Render(frameworkElement);

			//Restore previously saved layout
			frameworkElement.LayoutTransform = transform;

			//create and return a new WriteableBitmap using the RenderTargetBitmap
			return new WriteableBitmap(renderBitmap);

		}

		protected override void OnActivate()
		{
			// TODO: This doesn't work??
			var logo = new GreenshotLogo();

			var dv = Render(logo);
			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(dv));
			using (var filestream = new FileStream(@"c:\LocalData\test.png", FileMode.Create))
			{
				encoder.Save(filestream);
			}

			//SetIcon(logo);

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
					IsSeparator = true,
					Id = "Y_Separator"
				});
			}
			items.Add(new MenuItem
			{
				Id = "Y_Exit",
				Icon = new PackIconModern
				{
					Kind = PackIconModernKind.AxisXLetter,
					Foreground = Brushes.DarkRed
				},
				ClickAction = item => Application.Current.Shutdown()
			});
			ConfigureMenuItems(items);
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
