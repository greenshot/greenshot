/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dapplo.Ini;
using Dapplo.Ini.Interfaces;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Help;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Helpers;
using Greenshot.Recipes;
using Greenshot.Recipes.Triggers;
using Greenshot.Settings;
using log4net;

namespace Greenshot.Shell
{
    /// <summary>
    /// The tray menu: a themed WPF menu (light or dark), which WPF sizes for the DPI of the monitor it opens on.
    /// It is built every time it opens, so it always shows the current configuration, recipes, plugin entries and texts.
    /// </summary>
    public sealed class TrayMenu
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TrayMenu));

        private static readonly ConcurrentDictionary<string, ImageSource> Icons = new ConcurrentDictionary<string, ImageSource>();

        private readonly IGreenshotShell _shell;
        private ContextMenu _openMenu;
        private ThumbnailWindow _thumbnailWindow;

        private static ICoreConfiguration CoreConfig => IniConfigRegistry.GetSection<ICoreConfiguration>();

        internal TrayMenu(IGreenshotShell shell)
        {
            _shell = shell;
        }

        /// <summary>
        /// True while the menu is open
        /// </summary>
        public bool IsOpen => _openMenu?.IsOpen == true;

        /// <summary>
        /// Build the menu and open it at the cursor, on the UI thread
        /// </summary>
        public void Show()
        {
            if (IsOpen)
            {
                _openMenu.IsOpen = false;
            }

            var menu = Build();
            menu.Closed += (sender, args) =>
            {
                CleanupThumbnail();
                if (ReferenceEquals(_openMenu, menu))
                {
                    _openMenu = null;
                }
            };
            _openMenu = menu;
            ThemedMenu.ShowAtCursor(menu);
        }

        /// <summary>
        /// Close the menu when it is open
        /// </summary>
        public void Close()
        {
            if (_openMenu != null)
            {
                _openMenu.IsOpen = false;
            }
        }

        /// <summary>
        /// The icon of the tray menu from the embedded files in Resources\Tray, null when there is none
        /// </summary>
        /// <param name="name">e.g. "contextmenu_capturearea.Image"</param>
        public static ImageSource GetIcon(string name)
        {
            return Icons.GetOrAdd(name, LoadIcon);
        }

        private static ImageSource LoadIcon(string name)
        {
            try
            {
                var bytes = EmbeddedResources.GetBytes(typeof(TrayMenu), name);
                if (bytes == null)
                {
                    return null;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = new MemoryStream(bytes);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                Log.Warn($"Couldn't load the tray menu icon {name}", ex);
                return null;
            }
        }

        private ContextMenu Build()
        {
            var conf = CoreConfig;
            var menu = ThemedMenu.CreateContextMenu(followTaskbar: true);

            // The captures, the Recipes menu and the other items of recipes
            AddRecipeItems(menu, conf);

            // The entries of the plugins, with their own separator
            var pluginEntries = PluginUtils.GetVisibleContextMenuEntries();
            if (pluginEntries.Count > 0)
            {
                foreach (var entry in pluginEntries)
                {
                    var pluginEntry = entry;
                    menu.Items.Add(ThemedMenu.CreateItem(pluginEntry.Text, ThemedMenu.ToImageSource(pluginEntry.Image), () => RunLater(pluginEntry.PerformClick)));
                }

                menu.Items.Add(ThemedMenu.CreateSeparator());
            }

            bool hasSettingsBlock = false;
            if (!conf.DisableQuickSettings)
            {
                var quickSettings = CreateQuickSettingsItem();
                if (quickSettings.Items.Count > 0)
                {
                    menu.Items.Add(quickSettings);
                    hasSettingsBlock = true;
                }
            }

            // Disable access to the settings, for feature #3521446
            if (!conf.DisableSettings)
            {
                AddItem(menu, Texts.Core.ContextmenuSettings, "contextmenu_settings.Image", () => _shell.ShowSetting());
                hasSettingsBlock = true;
            }

            if (hasSettingsBlock)
            {
                menu.Items.Add(ThemedMenu.CreateSeparator());
            }

            menu.Items.Add(ThemedMenu.CreateItem(Texts.Core.ContextmenuHelp, GetIcon("contextmenu_help.Image"), () => AsyncCommand.Run(HelpFileLoader.LoadHelpAsync, "Load the help")));

            var now = DateTime.Now;
            bool showPresent = (now.Month == 12 && now.Day > 19 && now.Day < 27) || // christmas
                               (now.Month == 3 && now.Day > 13 && now.Day < 21); // birthday
            AddItem(menu, Texts.Core.ContextmenuDonate, showPresent ? "contextmenu_present.Image" : "contextmenu_donate.Image", TrayActions.OpenDonatePage);
            AddItem(menu, Texts.Core.ContextmenuAbout, null, () => _shell.ShowAbout());
            menu.Items.Add(ThemedMenu.CreateSeparator());

            AddItem(menu, Texts.Core.ContextmenuExit, "contextmenu_exit.Image", () => _shell.Exit());
            return menu;
        }

        /// <summary>
        /// Add an item which runs the action after the menu closed, exceptions are logged
        /// </summary>
        private static MenuItem AddItem(ItemsControl parent, string text, string iconName, Action action)
        {
            var item = ThemedMenu.CreateItem(text, iconName == null ? null : GetIcon(iconName), () => RunLater(action));
            parent.Items.Add(item);
            return item;
        }

        /// <summary>
        /// Run the action after the current UI event (e.g. when the menu closed), exceptions are logged
        /// </summary>
        private static void RunLater(Action action)
        {
            UiDispatcher.Current.InvokeAsync(action).FireAndLog("Menu action", Log);
        }

        private static string HotkeyText(string hotkey)
        {
            return HotkeyManager.GetLocalizedHotkeyStringFromString(hotkey) ?? string.Empty;
        }

        private static async Task EnableWhenClipboardHasImageAsync(MenuItem clipboardItem)
        {
            // Continues on the UI thread
            if (await ClipboardHelper.ContainsImageAsync().ConfigureAwait(true))
            {
                clipboardItem.IsEnabled = true;
            }
        }

        /// <summary>
        /// The order of the Recipes menu between the items of the recipes, see <see cref="TriggerConfig.CreateMainMenu"/>
        /// </summary>
        internal const int RecipesMenuOrder = 200;

        /// <summary>
        /// The items of the recipes whose ContextMenu trigger is in the group "Main" (the captures of the built-in recipes,
        /// "Open last capture location", ...) with the Recipes menu, by their order: a separator between items whose order is
        /// in another hundred, and after the last one
        /// </summary>
        private void AddRecipeItems(ContextMenu menu, ICoreConfiguration conf)
        {
            var triggerManager = SimpleServiceProvider.Current.GetInstance<ITriggerManager>(isOptional: true) as TriggerManager ?? TriggerManager.Instance;
            var recipeManager = SimpleServiceProvider.Current.GetInstance<IRecipeManager>(isOptional: true) ?? RecipeManager.Instance;

            var entries = triggerManager.GetContextMenuTriggers()
                .Where(trigger => trigger.IsInMainMenu)
                .Select(trigger => (Trigger: trigger, Recipe: recipeManager.GetRecipeById(trigger.TargetRecipeId)))
                .Where(entry => entry.Recipe != null && entry.Recipe.IsEnabled && entry.Recipe.ShowInContextMenu)
                .Select(entry => (entry.Trigger.Order, Text: entry.Trigger.MenuItemText ?? entry.Recipe.DisplayName,
                    Create: (Func<MenuItem>)(() => CreateRecipeItem(entry.Trigger, entry.Recipe, triggerManager, conf))))
                .ToList();

            var recipesItem = CreateRecipesItem(triggerManager, recipeManager);
            if (recipesItem != null)
            {
                entries.Add((RecipesMenuOrder, Texts.Core.ContextmenuRecipes, () => recipesItem));
            }

            int? lastBlock = null;
            foreach (var entry in entries.OrderBy(e => e.Order).ThenBy(e => e.Text, StringComparer.CurrentCultureIgnoreCase))
            {
                int block = (int)Math.Floor(entry.Order / 100.0);
                if (lastBlock != null && block != lastBlock)
                {
                    menu.Items.Add(ThemedMenu.CreateSeparator());
                }

                menu.Items.Add(entry.Create());
                lastBlock = block;
            }

            if (lastBlock != null)
            {
                menu.Items.Add(ThemedMenu.CreateSeparator());
            }
        }

        /// <summary>
        /// The item of a recipe in the tray menu: with its icon and hotkey, a sub menu for the displays or windows, and
        /// disabled when its source has nothing to capture (no last region, no image on the clipboard)
        /// </summary>
        private MenuItem CreateRecipeItem(ContextMenuTrigger trigger, CaptureRecipe recipe, TriggerManager triggerManager, ICoreConfiguration conf)
        {
            string text = trigger.MenuItemText ?? recipe.DisplayName;
            var icon = string.IsNullOrWhiteSpace(trigger.Icon) ? null : GetIcon(trigger.Icon.Trim());

            MenuItem item;
            if (string.Equals(trigger.Expand, TriggerConfig.ExpandWindows, StringComparison.OrdinalIgnoreCase))
            {
                // The hotkey of the recipe captures the active window, not one of the list
                item = CreateWindowListItem(text, icon, trigger, conf);
                return item;
            }

            item = string.Equals(trigger.Expand, TriggerConfig.ExpandDisplays, StringComparison.OrdinalIgnoreCase) && DisplayInfo.AllDisplayInfos.Length > 1
                ? CreateDisplaysItem(text, icon, trigger)
                : ThemedMenu.CreateItem(text, icon, () => RunLater(() => trigger.Fire()));

            var hotkeyTrigger = triggerManager.FindHotkeyTriggerForRecipe(recipe.Id);
            if (!string.IsNullOrWhiteSpace(hotkeyTrigger?.HotkeyString))
            {
                item.InputGestureText = HotkeyText(hotkeyTrigger.HotkeyString);
            }

            switch (recipe.FindFirstNodeByType(WellKnownStepTypes.Source)?.GetParameter("SourceType", CaptureSourceType.Region))
            {
                case CaptureSourceType.LastRegion:
                    item.IsEnabled = conf.LastCapturedRegion != NativeRect.Empty;
                    break;
                case CaptureSourceType.Clipboard:
                    // The quick check only looks at the formats; when a file list, virtual files or HTML could contain an image,
                    // the item is enabled when the background check finds one
                    bool? clipboardImage = ClipboardHelper.ContainsImageQuick();
                    item.IsEnabled = clipboardImage == true;
                    if (clipboardImage == null)
                    {
                        EnableWhenClipboardHasImageAsync(item).FireAndLog("Check the clipboard for an image", Log);
                    }
                    break;
            }

            return item;
        }

        /// <summary>
        /// A sub menu with "All" and each display: "All" captures all displays, a display is passed as PreSuppliedRegion
        /// </summary>
        private static MenuItem CreateDisplaysItem(string text, ImageSource icon, ContextMenuTrigger trigger)
        {
            var fullscreen = ThemedMenu.CreateItem(text, icon);
            AddItem(fullscreen, Texts.Core.ContextmenuCapturefullscreenAll, null,
                () => trigger.Fire(new Dictionary<string, object> { ["ScreenCaptureMode"] = ScreenCaptureMode.FullScreen }));

            var allScreensBounds = DisplayInfo.ScreenBounds;
            foreach (var displayInfo in DisplayInfo.AllDisplayInfos)
            {
                var bounds = displayInfo.Bounds;
                string deviceAlignment = displayInfo.DeviceName;

                if (bounds.Top == allScreensBounds.Top && bounds.Bottom != allScreensBounds.Bottom)
                {
                    deviceAlignment += " " + Texts.Core.ContextmenuCapturefullscreenTop;
                }
                else if (bounds.Top != allScreensBounds.Top && bounds.Bottom == allScreensBounds.Bottom)
                {
                    deviceAlignment += " " + Texts.Core.ContextmenuCapturefullscreenBottom;
                }

                if (bounds.Left == allScreensBounds.Left && bounds.Right != allScreensBounds.Right)
                {
                    deviceAlignment += " " + Texts.Core.ContextmenuCapturefullscreenLeft;
                }
                else if (bounds.Left != allScreensBounds.Left && bounds.Right == allScreensBounds.Right)
                {
                    deviceAlignment += " " + Texts.Core.ContextmenuCapturefullscreenRight;
                }

                AddItem(fullscreen, deviceAlignment, null, () => trigger.Fire(new Dictionary<string, object> { ["PreSuppliedRegion"] = bounds }));
            }

            return fullscreen;
        }

        /// <summary>
        /// A sub menu with the windows, listed when it opens, with a live thumbnail when hovering one; the chosen window is
        /// passed as TargetWindow
        /// </summary>
        private MenuItem CreateWindowListItem(string text, ImageSource icon, ContextMenuTrigger trigger, ICoreConfiguration conf)
        {
            var windowList = ThemedMenu.CreateItem(text, icon);
            // A placeholder, so the item is a sub menu; replaced by the windows when it opens
            windowList.Items.Add(ThemedMenu.CreateItem("…"));
            windowList.SubmenuOpened += (sender, args) =>
            {
                // SubmenuOpened bubbles up from the sub menus
                if (!ReferenceEquals(args.OriginalSource, windowList))
                {
                    return;
                }

                FillWindowList(windowList, trigger, conf);
            };
            windowList.SubmenuClosed += (sender, args) =>
            {
                if (ReferenceEquals(args.OriginalSource, windowList))
                {
                    CleanupThumbnail();
                }
            };
            return windowList;
        }

        private void FillWindowList(MenuItem windowList, ContextMenuTrigger trigger, ICoreConfiguration conf)
        {
            windowList.Items.Clear();
            // Only show the preview when enabled and DWM is there
            bool thumbnailPreview = conf.ThumnailPreview;

            foreach (var window in WindowHelper.GetTopLevelWindows())
            {
                string title = window.GetCaption();
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Window {window.Handle} '{title}' ({window.GetClassname()})");
                }
                if (string.IsNullOrEmpty(title))
                {
                    continue;
                }

                if (title.Length > conf.MaxMenuItemLength)
                {
                    title = title.Substring(0, Math.Min(title.Length, conf.MaxMenuItemLength));
                }

                ImageSource icon;
                using (var displayIcon = window.GetDisplayIcon())
                {
                    icon = ThemedMenu.ToImageSource(displayIcon);
                }

                var windowToCapture = window;
                var item = AddItem(windowList, title, null, () => trigger.Fire(new Dictionary<string, object> { ["TargetWindow"] = windowToCapture }));
                if (icon != null)
                {
                    item.Icon = ThemedMenu.CreateIcon(icon);
                }

                if (thumbnailPreview)
                {
                    item.MouseEnter += (sender, args) => ShowThumbnail(item, windowToCapture);
                    item.MouseLeave += (sender, args) => _thumbnailWindow?.Hide();
                    // Also for the item the arrow keys are on
                    item.GotKeyboardFocus += (sender, args) => ShowThumbnail(item, windowToCapture);
                    item.LostKeyboardFocus += (sender, args) => _thumbnailWindow?.Hide();
                }
            }
        }

        /// <summary>
        /// Show the thumbnail of the window above (or under) the sub menu with the windows
        /// </summary>
        private void ShowThumbnail(MenuItem item, IInteropWindow window)
        {
            try
            {
                if (PresentationSource.FromVisual(item) is not HwndSource source || source.RootVisual is not FrameworkElement popupRoot)
                {
                    return;
                }

                // PointToScreen returns pixels, as the thumbnail window uses them
                var topLeft = popupRoot.PointToScreen(new Point(0, 0));
                var bottomRight = popupRoot.PointToScreen(new Point(popupRoot.ActualWidth, popupRoot.ActualHeight));
                var bounds = new NativeRect((int)topLeft.X, (int)topLeft.Y, (int)(bottomRight.X - topLeft.X), (int)(bottomRight.Y - topLeft.Y));

                _thumbnailWindow ??= new ThumbnailWindow();
                _thumbnailWindow.ShowThumbnail(window, bounds, source.Handle);
            }
            catch (Exception ex)
            {
                Log.Debug("Couldn't show the thumbnail", ex);
            }
        }

        private void CleanupThumbnail()
        {
            if (_thumbnailWindow == null)
            {
                return;
            }

            _thumbnailWindow.Close();
            _thumbnailWindow = null;
        }

        /// <summary>
        /// The recipes with a context menu trigger which isn't in the group "Main", and the recipe commands; null when the recipe feature is off
        /// </summary>
        private static MenuItem CreateRecipesItem(TriggerManager triggerManager, IRecipeManager recipeManager)
        {
            if (!RecipeConfigHelper.IsRecipeFeatureEnabled())
            {
                return null;
            }

            var recipesItem = ThemedMenu.CreateItem(Texts.Core.ContextmenuRecipes ?? "Recipes");

            int recipeItemCount = 0;
            var menuGroups = new Dictionary<string, MenuItem>(StringComparer.OrdinalIgnoreCase);
            var menuEntries = triggerManager.GetContextMenuTriggers()
                .Where(trigger => !trigger.IsInMainMenu)
                .Select(trigger => new
                {
                    Trigger = trigger,
                    Recipe = recipeManager.GetRecipeById(trigger.TargetRecipeId),
                    Group = string.IsNullOrWhiteSpace(trigger.Group) ? "Recipes" : trigger.Group.Trim()
                })
                .Where(entry => entry.Recipe != null && entry.Recipe.ShowInContextMenu && entry.Recipe.IsEnabled)
                .OrderBy(entry => string.Equals(entry.Group, "Recipes", StringComparison.OrdinalIgnoreCase) ? 0 : 10)
                .ThenBy(entry => entry.Group, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(entry => entry.Trigger.Order)
                .ThenBy(entry => entry.Recipe.DisplayName, StringComparer.CurrentCultureIgnoreCase);
            foreach (var entry in menuEntries)
            {
                var trigger = entry.Trigger;
                var recipe = entry.Recipe;

                var menuTrigger = trigger;
                string group = entry.Group;
                ItemsControl parent = recipesItem;
                if (!string.Equals(group, "Recipes", StringComparison.OrdinalIgnoreCase))
                {
                    if (!menuGroups.TryGetValue(group, out var groupItem))
                    {
                        groupItem = ThemedMenu.CreateItem(group);
                        menuGroups.Add(group, groupItem);
                        recipesItem.Items.Add(groupItem);
                    }
                    parent = groupItem;
                }

                var item = AddItem(parent, trigger.MenuItemText ?? recipe.Name, null, () => menuTrigger.Fire());
                var hotkeyTrigger = triggerManager.FindHotkeyTriggerForRecipe(recipe.Id);
                if (hotkeyTrigger != null && !string.IsNullOrWhiteSpace(hotkeyTrigger.HotkeyString))
                {
                    item.InputGestureText = hotkeyTrigger.HotkeyString;
                }

                recipeItemCount++;
            }

            if (recipeItemCount > 0)
            {
                recipesItem.Items.Add(ThemedMenu.CreateSeparator());
            }

            AddItem(recipesItem, Texts.Core.ContextmenuImportrecipe ?? "Import Recipe...", null, TrayActions.ImportRecipe);
            AddItem(recipesItem, Texts.Core.ContextmenuReloadrecipes ?? "Reload Recipes", null, () => recipeManager.ReloadRecipes());

            var editorService = SimpleServiceProvider.Current.GetInstance<IRecipeEditorService>(isOptional: true);
            if (editorService != null)
            {
                AddItem(recipesItem, Texts.Core.ContextmenuManagerecipes ?? "Recipe Manager...", null, editorService.OpenRecipeManager);
                AddItem(recipesItem, Texts.Core.ContextmenuRecipeeditor ?? "Recipe Editor...", null, TrayActions.OpenRecipeEditor);
            }

            return recipesItem;
        }

        /// <summary>
        /// The quick settings (<see cref="QuickSettings"/>) as a sub menu: a group is a sub menu (an inline group a header with its
        /// items), a switch a checked item, a choice a sub menu with one item per value
        /// </summary>
        private MenuItem CreateQuickSettingsItem()
        {
            var quickSettings = ThemedMenu.CreateItem(Texts.Core.ContextmenuQuicksettings);
            foreach (var group in QuickSettings.Create())
            {
                if (group.Title == null)
                {
                    AddQuickSettings(quickSettings, group.Settings);
                    continue;
                }

                if (!group.IsInline)
                {
                    var groupItem = ThemedMenu.CreateItem(group.Title);
                    AddQuickSettings(groupItem, group.Settings);
                    quickSettings.Items.Add(groupItem);
                    continue;
                }

                if (quickSettings.Items.Count > 0)
                {
                    quickSettings.Items.Add(ThemedMenu.CreateSeparator());
                }
                var header = ThemedMenu.CreateItem(group.Title);
                header.IsEnabled = false;
                quickSettings.Items.Add(header);
                AddQuickSettings(quickSettings, group.Settings);
                if (group.MoreSettingsTab != null)
                {
                    string tab = group.MoreSettingsTab;
                    quickSettings.Items.Add(ThemedMenu.CreateItem(Texts.Core.QuicksettingsAutomaticstepsMore, null, () => RunLater(() => _shell.ShowSetting(null, tab))));
                }
            }
            return quickSettings;
        }

        private static void AddQuickSettings(MenuItem parent, IEnumerable<QuickSetting> settings)
        {
            foreach (var setting in settings)
            {
                if (!setting.IsChoice)
                {
                    var switchItem = ThemedMenu.CreateCheckItem(setting.Label, setting.IsOn, setting.SetOn);
                    if (setting.Description != null)
                    {
                        switchItem.ToolTip = setting.Description;
                    }
                    parent.Items.Add(switchItem);
                    continue;
                }

                var choiceList = ThemedMenu.CreateItem(setting.Label);
                foreach (var choice in setting.Choices)
                {
                    // Like a radio button: clicking the current value keeps it
                    choiceList.Items.Add(ThemedMenu.CreateCheckItem(choice.Label, choice.IsSelected, _ => choice.Select()));
                }
                parent.Items.Add(choiceList);
            }
        }
    }
}
