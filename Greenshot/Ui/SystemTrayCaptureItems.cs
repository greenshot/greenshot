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
using System;
using Dapplo.CaliburnMicro.Extensions;
using Dapplo.CaliburnMicro.Menu;
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
	public class SystemTrayCaptureItems
	{
		private const string IdPrefix = "B_";

		[Import]
		private ICoreConfiguration CoreConfiguration { get; set; }

		[Import]
		private IGreenshotLanguage GreenshotLanguage { get; set; }


		//contextmenu_capturearea.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.RegionHotkey);
		//contextmenu_capturelastregion.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.LastregionHotkey);
		//contextmenu_capturewindow.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.WindowHotkey);
		//contextmenu_capturefullscreen.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.FullscreenHotkey);
		//contextmenu_captureie.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(coreConfiguration.IEHotkey);

		/// <summary>
		/// Region capture menu-item
		/// </summary>
		[Export("systray", typeof(IMenuItem))]
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

				// Update HotKeyHint when the language or hotkey changes
				CoreConfiguration.OnPropertyChanged($"({nameof(CoreConfiguration.Language)}|{nameof(CoreConfiguration.RegionHotkey)}").Subscribe(s => menuItem.HotKeyHint = HotkeyControl.GetLocalizedHotkeyStringFromString(CoreConfiguration.RegionHotkey));
				// Update the title of this menu item when the translation changes
				GreenshotLanguage.CreateBinding(menuItem, nameof(IGreenshotLanguage.ContextmenuCaptureArea));
				return menuItem;

			}
		}

		/// <summary>
		/// Window capture menu-item
		/// </summary>
		[Export("systray", typeof(IMenuItem))]
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

				// Update HotKeyHint when the language or hotkey changes
				CoreConfiguration.OnPropertyChanged($"({nameof(CoreConfiguration.Language)}|{nameof(CoreConfiguration.WindowHotkey)}").Subscribe(s => menuItem.HotKeyHint = HotkeyControl.GetLocalizedHotkeyStringFromString(CoreConfiguration.WindowHotkey));
				// Update the title of this menu item when the translation changes
				GreenshotLanguage.CreateBinding(menuItem, nameof(IGreenshotLanguage.ContextmenuCaptureWindow));

				return menuItem;

			}
		}

		/// <summary>
		/// Screen capture menu-item
		/// </summary>
		[Export("systray", typeof(IMenuItem))]
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

				// Update HotKeyHint when the language or hotkey changes
				CoreConfiguration.OnPropertyChanged($"({nameof(CoreConfiguration.Language)}|{nameof(CoreConfiguration.FullscreenHotkey)}").Subscribe(s => menuItem.HotKeyHint = HotkeyControl.GetLocalizedHotkeyStringFromString(CoreConfiguration.FullscreenHotkey));
				// Update the title of this menu item when the translation changes
				GreenshotLanguage.CreateBinding(menuItem, nameof(IGreenshotLanguage.ContextmenuCaptureFullScreenAll));
				return menuItem;

			}
		}
	}
}
