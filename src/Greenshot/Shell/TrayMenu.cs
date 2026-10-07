/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using Dapplo.Windows.User32;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
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

        /// <summary>
        /// At most this many recipe options in the "Automatic steps" block of the quick settings, the others are in Settings > Recipes ("More…")
        /// </summary>
        private const int MaxRecipeQuickSettings = 6;

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

            // Captures
            var captureArea = AddItem(menu, Texts.Core.ContextmenuCapturearea, "contextmenu_capturearea.Image", () => CaptureHelper.CaptureRegion(false));
            captureArea.InputGestureText = HotkeyText(conf.RegionHotkey);

            var lastRegion = AddItem(menu, Texts.Core.ContextmenuCapturelastregion, "contextmenu_capturelastregion.Image", () => CaptureHelper.CaptureLastRegion(false));
            lastRegion.InputGestureText = HotkeyText(conf.LastregionHotkey);
            lastRegion.IsEnabled = conf.LastCapturedRegion != NativeRect.Empty;

            var captureWindow = AddItem(menu, Texts.Core.ContextmenuCapturewindow, "contextmenu_capturewindow.Image", () => CaptureHelper.CaptureWindowInteractive(false));
            captureWindow.InputGestureText = HotkeyText(conf.WindowHotkey);

            menu.Items.Add(CreateFullscreenItem(conf));
            menu.Items.Add(ThemedMenu.CreateSeparator());

            menu.Items.Add(CreateWindowListItem(conf));
            menu.Items.Add(ThemedMenu.CreateSeparator());

            var recipesItem = CreateRecipesItem();
            if (recipesItem != null)
            {
                menu.Items.Add(recipesItem);
            }

            // Other sources
            var clipboardItem = AddItem(menu, Texts.Core.ContextmenuCaptureclipboard, "contextmenu_captureclipboard.Image", () => CaptureHelper.CaptureClipboard());
            string clipboardHotkey = HotkeyText(conf.ClipboardHotkey);
            if (!string.IsNullOrEmpty(clipboardHotkey) && !"None".Equals(clipboardHotkey))
            {
                clipboardItem.InputGestureText = clipboardHotkey;
            }
            // The quick check only looks at the formats; when a file list, virtual files or HTML could contain an image, the item is enabled when the background check finds one
            bool? clipboardImage = ClipboardHelper.ContainsImageQuick();
            clipboardItem.IsEnabled = clipboardImage == true;
            if (clipboardImage == null)
            {
                EnableWhenClipboardHasImageAsync(clipboardItem).FireAndLog("Check the clipboard for an image", Log);
            }

            AddItem(menu, Texts.Core.ContextmenuOpenfile, "contextmenu_openfile.Image", () => TrayActions.CaptureFile());
            menu.Items.Add(ThemedMenu.CreateSeparator());

            AddItem(menu, Texts.Core.ContextmenuOpenrecentcapture, null, TrayActions.OpenLastCaptureLocation);
            menu.Items.Add(ThemedMenu.CreateSeparator());

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
                var quickSettings = CreateQuickSettingsItem(conf);
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
        /// "Capture full screen": with more than one display a sub menu with "all" and each display
        /// </summary>
        private static MenuItem CreateFullscreenItem(ICoreConfiguration conf)
        {
            var displays = DisplayInfo.AllDisplayInfos;
            if (displays.Length <= 1)
            {
                var single = ThemedMenu.CreateItem(Texts.Core.ContextmenuCapturefullscreen, GetIcon("contextmenu_capturefullscreen.Image"),
                    () => RunLater(() => CaptureHelper.CaptureFullscreen(false, conf.ScreenCaptureMode)));
                single.InputGestureText = HotkeyText(conf.FullscreenHotkey);
                return single;
            }

            var fullscreen = ThemedMenu.CreateItem(Texts.Core.ContextmenuCapturefullscreen, GetIcon("contextmenu_capturefullscreen.Image"));
            fullscreen.InputGestureText = HotkeyText(conf.FullscreenHotkey);
            AddItem(fullscreen, Texts.Core.ContextmenuCapturefullscreenAll, null, () => CaptureHelper.CaptureFullscreen(false, ScreenCaptureMode.FullScreen));

            var allScreensBounds = DisplayInfo.ScreenBounds;
            foreach (var displayInfo in displays)
            {
                var displayToCapture = displayInfo;
                string deviceAlignment = displayToCapture.DeviceName;

                if (displayInfo.Bounds.Top == allScreensBounds.Top && displayInfo.Bounds.Bottom != allScreensBounds.Bottom)
                {
                    deviceAlignment += " " + Texts.Core.ContextmenuCapturefullscreenTop;
                }
                else if (displayInfo.Bounds.Top != allScreensBounds.Top && displayInfo.Bounds.Bottom == allScreensBounds.Bottom)
                {
                    deviceAlignment += " " + Texts.Core.ContextmenuCapturefullscreenBottom;
                }

                if (displayInfo.Bounds.Left == allScreensBounds.Left && displayInfo.Bounds.Right != allScreensBounds.Right)
                {
                    deviceAlignment += " " + Texts.Core.ContextmenuCapturefullscreenLeft;
                }
                else if (displayInfo.Bounds.Left != allScreensBounds.Left && displayInfo.Bounds.Right == allScreensBounds.Right)
                {
                    deviceAlignment += " " + Texts.Core.ContextmenuCapturefullscreenRight;
                }

                AddItem(fullscreen, deviceAlignment, null, () => CaptureHelper.CaptureRegion(false, displayToCapture.Bounds));
            }

            return fullscreen;
        }

        /// <summary>
        /// "Capture window from list": the windows are listed when the sub menu opens, with a live thumbnail when hovering one
        /// </summary>
        private MenuItem CreateWindowListItem(ICoreConfiguration conf)
        {
            var windowList = ThemedMenu.CreateItem(Texts.Core.ContextmenuCapturewindowfromlist);
            // A placeholder, so the item is a sub menu; replaced by the windows when it opens
            windowList.Items.Add(ThemedMenu.CreateItem("…"));
            windowList.SubmenuOpened += (sender, args) =>
            {
                // SubmenuOpened bubbles up from the sub menus
                if (!ReferenceEquals(args.OriginalSource, windowList))
                {
                    return;
                }

                FillWindowList(windowList, conf);
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

        private void FillWindowList(MenuItem windowList, ICoreConfiguration conf)
        {
            windowList.Items.Clear();
            // Only show the preview when enabled and DWM is there
            bool thumbnailPreview = conf.ThumnailPreview;

            foreach (var window in WindowDetails.GetTopLevelWindows())
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(window.ToString());
                }

                string title = window.Text;
                if (string.IsNullOrEmpty(title))
                {
                    continue;
                }

                if (title.Length > conf.MaxMenuItemLength)
                {
                    title = title.Substring(0, Math.Min(title.Length, conf.MaxMenuItemLength));
                }

                ImageSource icon;
                using (var displayIcon = window.DisplayIcon)
                {
                    icon = ThemedMenu.ToImageSource(displayIcon);
                }

                var windowToCapture = window;
                var item = AddItem(windowList, title, null, () => CaptureHelper.CaptureWindow(windowToCapture));
                if (icon != null)
                {
                    item.Icon = ThemedMenu.CreateIcon(icon);
                }

                if (thumbnailPreview)
                {
                    item.MouseEnter += (sender, args) => ShowThumbnail(item, windowToCapture);
                    item.MouseLeave += (sender, args) => _thumbnailWindow?.Hide();
                }
            }
        }

        /// <summary>
        /// Show the thumbnail of the window above (or under) the sub menu with the windows
        /// </summary>
        private void ShowThumbnail(MenuItem item, WindowDetails window)
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
        /// The recipes with a context menu trigger, and the recipe commands; null when the recipe feature is off
        /// </summary>
        private static MenuItem CreateRecipesItem()
        {
            if (!RecipeConfigHelper.IsRecipeFeatureEnabled())
            {
                return null;
            }

            var recipesItem = ThemedMenu.CreateItem(Texts.Core.ContextmenuRecipes ?? "Recipes");

            var triggerManager = SimpleServiceProvider.Current.GetInstance<ITriggerManager>(isOptional: true) as TriggerManager ?? TriggerManager.Instance;
            var recipeManager = SimpleServiceProvider.Current.GetInstance<IRecipeManager>(isOptional: true) ?? RecipeManager.Instance;

            int recipeItemCount = 0;
            foreach (var trigger in triggerManager.GetContextMenuTriggers().OrderBy(t => t.Order))
            {
                var recipe = recipeManager.GetRecipeById(trigger.TargetRecipeId);
                if (recipe == null || !recipe.ShowInContextMenu || !recipe.IsEnabled)
                {
                    continue;
                }

                var menuTrigger = trigger;
                var item = AddItem(recipesItem, trigger.MenuItemText ?? recipe.Name, null, () => menuTrigger.Fire());
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
        /// The quick settings: the settings which are changed often, stored right away
        /// </summary>
        private MenuItem CreateQuickSettingsItem(ICoreConfiguration conf)
        {
            var quickSettings = ThemedMenu.CreateItem(Texts.Core.ContextmenuQuicksettings);
            var coreSection = IniConfigRegistry.GetSection<ICoreConfiguration>();

            // Only add if the value is not fixed
            if (coreSection == null || !coreSection.IsConstant("CaptureMousepointer"))
            {
                quickSettings.Items.Add(ThemedMenu.CreateCheckItem(Texts.Settings.CaptureMousepointer, conf.CaptureMousepointer, value => conf.CaptureMousepointer = value));
            }

            if (coreSection == null || !coreSection.IsConstant("Destinations"))
            {
                var destinations = ThemedMenu.CreateItem(Texts.Settings.Destination);
                foreach (var destination in DestinationHelper.GetAllDestinations())
                {
                    var selectedDestination = destination;
                    destinations.Items.Add(ThemedMenu.CreateCheckItem(destination.Descriptor?.DisplayName ?? destination.Designation,
                        conf.OutputDestinations.Contains(destination.Designation),
                        isChecked => QuickSettingDestinationChanged(conf, selectedDestination, isChecked)));
                }

                if (destinations.Items.Count > 0)
                {
                    quickSettings.Items.Add(destinations);
                }
            }

            // print options
            var printOptions = ThemedMenu.CreateItem(Texts.Settings.Printoptions);
            AddBoolMenuItem(printOptions, coreSection, "OutputPrintPromptOptions", Texts.Settings.Alwaysshowprintoptionsdialog, v => conf.OutputPrintPromptOptions = v, conf.OutputPrintPromptOptions);
            AddBoolMenuItem(printOptions, coreSection, "OutputPrintAllowRotate", Texts.Core.PrintoptionsAllowrotate, v => conf.OutputPrintAllowRotate = v, conf.OutputPrintAllowRotate);
            AddBoolMenuItem(printOptions, coreSection, "OutputPrintAllowEnlarge", Texts.Core.PrintoptionsAllowenlarge, v => conf.OutputPrintAllowEnlarge = v, conf.OutputPrintAllowEnlarge);
            AddBoolMenuItem(printOptions, coreSection, "OutputPrintAllowShrink", Texts.Core.PrintoptionsAllowshrink, v => conf.OutputPrintAllowShrink = v, conf.OutputPrintAllowShrink);
            AddBoolMenuItem(printOptions, coreSection, "OutputPrintCenter", Texts.Core.PrintoptionsAllowcenter, v => conf.OutputPrintCenter = v, conf.OutputPrintCenter);
            AddBoolMenuItem(printOptions, coreSection, "OutputPrintInverted", Texts.Core.PrintoptionsInverted, v => conf.OutputPrintInverted = v, conf.OutputPrintInverted);
            AddBoolMenuItem(printOptions, coreSection, "OutputPrintGrayscale", Texts.Core.PrintoptionsPrintgrayscale, v => conf.OutputPrintGrayscale = v, conf.OutputPrintGrayscale);
            AddBoolMenuItem(printOptions, coreSection, "OutputPrintMonochrome", Texts.Core.PrintoptionsPrintmonochrome, v => conf.OutputPrintMonochrome = v, conf.OutputPrintMonochrome);
            AddBoolMenuItem(printOptions, coreSection, "OutputPrintFooter", Texts.Core.PrintoptionsTimestamp, v => conf.OutputPrintFooter = v, conf.OutputPrintFooter);
            if (printOptions.Items.Count > 0)
            {
                quickSettings.Items.Add(printOptions);
            }

            // effects
            var effects = ThemedMenu.CreateItem(Texts.Settings.Visualization);
            AddBoolMenuItem(effects, coreSection, "PlayCameraSound", Texts.Settings.Playsound, v => conf.PlayCameraSound = v, conf.PlayCameraSound);
            AddBoolMenuItem(effects, coreSection, "ShowTrayNotification", Texts.Settings.Shownotify, v => conf.ShowTrayNotification = v, conf.ShowTrayNotification);
            if (effects.Items.Count > 0)
            {
                quickSettings.Items.Add(effects);
            }

            AddRecipeQuickSettings(quickSettings);
            return quickSettings;
        }

        /// <summary>
        /// Adds a checked item for a config property, skipping it when the property is marked as constant (admin-enforced).
        /// </summary>
        private static void AddBoolMenuItem(MenuItem parent, IIniSection section, string propertyName, string text, Action<bool> setter, bool currentValue)
        {
            if (section != null && section.IsConstant(propertyName))
            {
                return;
            }

            parent.Items.Add(ThemedMenu.CreateCheckItem(text, currentValue, setter));
        }

        private static void QuickSettingDestinationChanged(ICoreConfiguration conf, IDestination selectedDestination, bool isChecked)
        {
            if (isChecked)
            {
                if (selectedDestination.Designation.Equals(nameof(WellKnownDestinations.Picker)))
                {
                    // If the item is the destination picker, remove all others
                    conf.OutputDestinations.Clear();
                }
                else
                {
                    // If the item is not the destination picker, remove the picker
                    conf.OutputDestinations.Remove(nameof(WellKnownDestinations.Picker));
                }

                // Checked an item, add if the destination is not yet selected
                if (!conf.OutputDestinations.Contains(selectedDestination.Designation))
                {
                    conf.OutputDestinations.Add(selectedDestination.Designation);
                }
            }
            else if (conf.OutputDestinations.Contains(selectedDestination.Designation))
            {
                // deselected a destination, only remove if it was selected
                conf.OutputDestinations.Remove(selectedDestination.Designation);
            }

            // Check if something was selected, if not make the picker the default
            if (conf.OutputDestinations.Count == 0)
            {
                conf.OutputDestinations.Add(nameof(WellKnownDestinations.Picker));
            }
        }

        /// <summary>
        /// The options recipes offer in the quick settings ("quickSettings": true), in one "Automatic steps" block: a switch
        /// is a checked item, a choice a submenu with one item per value, at most <see cref="MaxRecipeQuickSettings"/> of them,
        /// and "More…" opens Settings > Recipes. The value is stored right away.
        /// </summary>
        private void AddRecipeQuickSettings(MenuItem quickSettings)
        {
            List<(FlowDefinition Recipe, RecipeOption Option)> options;
            try
            {
                // The extensions (border, drop shadow, caption, ...) first, then the recipes
                var manager = RecipeManager.Instance;
                options = manager.GetAllExtensions()
                    .OrderBy(e => e.Extends?.Order ?? 0).ThenBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                    .Cast<FlowDefinition>()
                    .Concat(manager.GetAllRecipes().OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase))
                    .Where(r => r?.Options != null)
                    .SelectMany(r => r.Options
                        .Where(o => o != null && o.QuickSettings && (o.Type == ContractDataType.Boolean || (o.Type == ContractDataType.Enum && o.Choices != null)))
                        .Select(o => (Recipe: r, Option: o)))
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't add the options of the recipes to the quick settings.", ex);
                return;
            }

            if (options.Count == 0)
            {
                return;
            }

            if (quickSettings.Items.Count > 0)
            {
                quickSettings.Items.Add(ThemedMenu.CreateSeparator());
            }

            var header = ThemedMenu.CreateItem(Texts.Core.QuicksettingsAutomaticsteps);
            header.IsEnabled = false;
            quickSettings.Items.Add(header);

            // The same label of two recipes gets the recipe name in front
            var duplicateLabels = new HashSet<string>(options.GroupBy(o => o.Option.DisplayLabel, StringComparer.CurrentCultureIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key), StringComparer.CurrentCultureIgnoreCase);
            if (options.Count > MaxRecipeQuickSettings)
            {
                Log.DebugFormat("{0} recipe options are marked for the quick settings, showing {1}.", options.Count, MaxRecipeQuickSettings);
            }

            foreach (var (recipe, option) in options.Take(MaxRecipeQuickSettings))
            {
                string label = duplicateLabels.Contains(option.DisplayLabel) ? $"{RecipeText.Translate(recipe.Name ?? recipe.Id)}: {option.DisplayLabel}" : option.DisplayLabel;
                var value = RecipeOptionStore.GetValue(recipe, option);
                if (option.Type == ContractDataType.Boolean)
                {
                    var switchItem = ThemedMenu.CreateCheckItem(label, value is true, isChecked => RecipeOptionStore.SetValue(recipe.Id, option, isChecked));
                    if (!string.IsNullOrEmpty(option.DisplayDescription))
                    {
                        switchItem.ToolTip = option.DisplayDescription;
                    }
                    quickSettings.Items.Add(switchItem);
                    continue;
                }

                var choiceList = ThemedMenu.CreateItem(label);
                foreach (var choice in option.Choices.Where(c => c != null))
                {
                    var selectedChoice = choice;
                    bool isCurrent = string.Equals(choice.Value, value as string, StringComparison.OrdinalIgnoreCase);
                    // Like a radio button: clicking the current value keeps it
                    choiceList.Items.Add(ThemedMenu.CreateCheckItem(choice.DisplayLabel, isCurrent, _ => RecipeOptionStore.SetValue(recipe.Id, option, selectedChoice.Value)));
                }
                quickSettings.Items.Add(choiceList);
            }

            quickSettings.Items.Add(ThemedMenu.CreateItem(Texts.Core.QuicksettingsAutomaticstepsMore, null, () => RunLater(() => _shell.ShowSetting(null, "recipes"))));
        }
    }
}
