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
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.CaliburnMicro.Menu;
using Dapplo.Utils;
using Dapplo.Utils.Extensions;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Controls;
using Greenshot.CaptureCore;
using MahApps.Metro.IconPacks;

namespace Greenshot.Ui
{
	/// <summary>
	/// This class contains all the "capture" menu items for the context-menu in the system tray
	/// 
	/// </summary>
	[Export("systray", typeof(IMenuItemProvider))]
	public class SystemTrayCaptureItems : IMenuItemProvider
	{
		private const string IdPrefix = "B_";
		private readonly Disposables _disposables = new Disposables();

		[Import]
		private ICoreConfiguration CoreConfiguration { get; set; }

		[Import]
		private IGreenshotLanguage GreenshotLanguage { get; set; }

		/// <summary>
		/// Provide the menu items for the context menu
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IMenuItem> ProvideMenuItems()
		{
			yield return CaptureArea;
			yield return CaptureWindow;
			yield return CaptureScreen;
		}

		//contextmenu_capturearea.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.RegionHotkey);
		//contextmenu_capturelastregion.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.LastregionHotkey);
		//contextmenu_capturewindow.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.WindowHotkey);
		//contextmenu_capturefullscreen.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.FullscreenHotkey);
		//contextmenu_captureie.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.IEHotkey);

		/// <summary>
		/// Region capture menu-item
		/// </summary>
		private MenuItem CaptureArea
		{
			get
			{
				var menuItem = new MenuItem
				{
					Id = $"{IdPrefix}CaptureArea",
					Icon = new PackIconMaterial
					{
						Kind = PackIconMaterialKind.Selection,
					},
					ClickAction = async item =>
					{
						await CaptureHelper.CaptureRegionAsync(false).ConfigureAwait(false);
					}
				};
				// change the HotKeyHint when either the hotkey changes, or the language
				_disposables.Add(CoreConfiguration.OnPropertyChanged(s => menuItem.HotKeyHint = HotkeyControl.GetLocalizedHotkeyStringFromString(CoreConfiguration.RegionHotkey), $"({nameof(CoreConfiguration.Language)}|{nameof(CoreConfiguration.RegionHotkey)}"));
				_disposables.Add(menuItem.BindDisplayName(GreenshotLanguage, nameof(IGreenshotLanguage.ContextmenuCaptureArea)));
				return menuItem;

			}
		}

		/// <summary>
		/// Window capture menu-item
		/// </summary>
		private MenuItem CaptureWindow
		{
			get
			{
				var menuItem = new MenuItem
				{
					Id = $"{IdPrefix}CaptureWindow",
					Icon = new PackIconMaterial
					{
						Kind = PackIconMaterialKind.Windows,
					},
					ClickAction = async item =>
					{
						await CaptureHelper.CaptureWindowInteractiveAsync(false).ConfigureAwait(false);
					}
				};
				// change the HotKeyHint when either the hotkey changes, or the language
				_disposables.Add(CoreConfiguration.OnPropertyChanged(s => menuItem.HotKeyHint = HotkeyControl.GetLocalizedHotkeyStringFromString(CoreConfiguration.WindowHotkey), $"({nameof(CoreConfiguration.Language)}|{nameof(CoreConfiguration.WindowHotkey)}"));
				_disposables.Add(menuItem.BindDisplayName(GreenshotLanguage, nameof(IGreenshotLanguage.ContextmenuCaptureWindow)));
				return menuItem;

			}
		}

		/// <summary>
		/// Screen capture menu-item
		/// </summary>
		private MenuItem CaptureScreen
		{
			get
			{
				// See: MainForm.MultiScreenDropDownOpening
				var menuItem = new MenuItem
				{
					Id = $"{IdPrefix}CaptureScreen",
					Icon = new PackIconEntypo
					{
						Kind = PackIconEntypoKind.ResizeFullScreen,
					},
					ClickAction = async item =>
					{
						await CaptureHelper.CaptureFullscreenAsync(false, ScreenCaptureMode.FullScreen).ConfigureAwait(false);
					}
				};
				// change the HotKeyHint when either the hotkey changes, or the language
				_disposables.Add(CoreConfiguration.OnPropertyChanged(s => menuItem.HotKeyHint = HotkeyControl.GetLocalizedHotkeyStringFromString(CoreConfiguration.FullscreenHotkey), $"({nameof(CoreConfiguration.Language)}|{nameof(CoreConfiguration.FullscreenHotkey)}"));
				_disposables.Add(menuItem.BindDisplayName(GreenshotLanguage, nameof(IGreenshotLanguage.ContextmenuCaptureFullScreenAll)));
				return menuItem;

			}
		}
	}
}
