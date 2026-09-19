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
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.Box;
using Greenshot.Plugin.Box.Forms;
using Greenshot.Plugin.Confluence;
using Greenshot.Plugin.Confluence.Forms;
using Greenshot.Plugin.Dropbox;
using Greenshot.Plugin.Dropbox.Forms;
using Greenshot.Plugin.ExternalCommand;
using Greenshot.Plugin.ExternalCommand.Forms;
using Greenshot.Plugin.Imgur;
using Greenshot.Plugin.Imgur.Forms;
using Greenshot.Plugin.Jira;
using Greenshot.Plugin.Jira.Forms;
using Greenshot.Forms.Wpf;
using Greenshot.Plugin.Zxing;
using Greenshot.Plugin.Zxing.Views;
using Xunit;
using Greenshot.Plugin.Zxing.Controls;

namespace Greenshot.Tests.Forms
{
    public class ImageHelperAndWpfFormsTests
    {
        public ImageHelperAndWpfFormsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void ImageHelper_ToBitmapSource_NullReturnsNull()
        {
            Image image = null;
            Assert.Null(image.ToBitmapSource());
        }

        [Fact]
        public void ImageHelper_ToBitmapSource_ConvertsAndFreezes()
        {
            using var bmp32 = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            var source = bmp32.ToBitmapSource();
            Assert.NotNull(source);
            Assert.True(source.IsFrozen);
            Assert.Equal(16, source.PixelWidth);
            Assert.Equal(16, source.PixelHeight);

            using var bmp24 = new Bitmap(24, 24, PixelFormat.Format24bppRgb);
            var source24 = ((Image)bmp24).ToBitmapSource();
            Assert.NotNull(source24);
            Assert.True(source24.IsFrozen);
            Assert.Equal(24, source24.PixelWidth);
            Assert.Equal(24, source24.PixelHeight);

            using var bmp8 = new Bitmap(32, 32, PixelFormat.Format8bppIndexed);
            var source8 = ((Image)bmp8).ToBitmapSource();
            Assert.NotNull(source8);
            Assert.True(source8.IsFrozen);
            Assert.Equal(32, source8.PixelWidth);
            Assert.Equal(32, source8.PixelHeight);
        }

        [Fact]
        public void ConfluenceDestination_DisplayIcon_ReturnsValidImage()
        {
            var destination = new ConfluenceDestination();
            var icon = destination.DisplayIcon;
            Assert.NotNull(icon);
            Assert.True(icon.Width > 0);
            Assert.True(icon.Height > 0);

            var bitmapSource = icon.ToBitmapSource();
            Assert.NotNull(bitmapSource);
            Assert.True(bitmapSource.IsFrozen);
        }

        [Fact]
        public void WpfPluginWindows_CanBeInstantiatedOnStaThread()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var boxConfig = IniConfigHelper.EnsureSection<IBoxConfiguration>(() => new BoxConfigurationImpl());
                    var boxControl = new BoxConfigurationControl(boxConfig);
                    Assert.NotNull(boxControl);

                    var dropboxConfig = IniConfigHelper.EnsureSection<IDropboxConfiguration>(() => new DropboxConfigurationImpl());
                    var dropboxControl = new DropboxConfigurationControl(dropboxConfig);
                    Assert.NotNull(dropboxControl);

                    var imgurConfig = IniConfigHelper.EnsureSection<IImgurConfiguration>(() => new ImgurConfigurationImpl());
                    var imgurControl = new ImgurConfigurationControl(imgurConfig);
                    Assert.NotNull(imgurControl);

                    var jiraConfig = IniConfigHelper.EnsureSection<IJiraConfiguration>(() => new JiraConfigurationImpl());
                    var jiraControl = new JiraConfigurationControl(jiraConfig);
                    Assert.NotNull(jiraControl);

                    var zxingConfig = IniConfigHelper.EnsureSection<IZxingConfiguration>(() => new ZxingConfigurationImpl());
                    var zxingControl = new ZxingConfigurationControl(zxingConfig);
                    Assert.NotNull(zxingControl);

                    IniConfigHelper.EnsureSection<IExternalCommandConfiguration>(() => new ExternalCommandConfigurationImpl());
                    var extCmdControl = new ExternalCommandConfigurationControl();
                    Assert.NotNull(extCmdControl);

                    var confluenceConfig = IniConfigHelper.EnsureSection<IConfluenceConfiguration>(() => new ConfluenceConfigurationImpl());
                    var confluenceControl = new ConfluenceConfigurationControl(confluenceConfig);
                    Assert.NotNull(confluenceControl);

                    IniConfigHelper.EnsureSection<Greenshot.Plugin.Office.IOfficeConfiguration>(() => new Greenshot.Plugin.Office.OfficeConfigurationImpl());
                    var officeControl = new Greenshot.Plugin.Office.Forms.OfficeConfigurationControl();
                    Assert.NotNull(officeControl);
                    Assert.Equal(5, officeControl.OfficeApps.Count);
                    Assert.NotNull(officeControl.SelectedApp);
                    Assert.True(officeControl.IsWordSelected);

                    var officePlugin = new Greenshot.Plugin.Office.OfficePlugin();
                    Assert.True(officePlugin.IsConfigurable);
                    Assert.NotNull(officePlugin.CreateConfigurationControl());

                    var instances = new[]
                    {
                        new Greenshot.Forms.Wpf.RunningInstanceItem
                        {
                            Index = 1,
                            ProcessId = 1234,
                            Path = @"C:\Program Files\Greenshot\Greenshot.exe"
                        }
                    };
                    var instanceRunningWindow = new Greenshot.Forms.Wpf.InstanceRunningWindow(instances);
                    Assert.NotNull(instanceRunningWindow);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public void ImageHelper_ToBitmapSource_IconConvertsAndFreezes()
        {
            var icon = GreenshotResources.GetGreenshotIcon();
            Assert.NotNull(icon);
            var source = icon.ToBitmapSource();
            Assert.NotNull(source);
            Assert.True(source.IsFrozen);
        }

        [Fact]
        public void ColorPickerWindow_CanBeInstantiatedAndSetColor()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var window = new Greenshot.Editor.Forms.ColorPickerWindow
                    {
                        SelectedColor = System.Drawing.Color.CornflowerBlue
                    };

                    Assert.Equal(System.Drawing.Color.CornflowerBlue.ToArgb(), window.SelectedColor.ToArgb());
                    Assert.Equal(143, window.PaletteCount);
                    Assert.Equal(12, window.RecentColorsCount);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public void MigratedWpfDialogs_CanBeInstantiatedOnStaThread()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    // Greenshot WPF windows
                    var languageWindow = new Greenshot.Forms.Wpf.LanguageWindow();
                    Assert.NotNull(languageWindow);

                    var printOptionsWindow = new Greenshot.Forms.Wpf.PrintOptionsWindow();
                    Assert.NotNull(printOptionsWindow);

                    // Greenshot.Editor WPF windows
                    var dropShadowWindow = new Greenshot.Editor.Forms.DropShadowSettingsWindow();
                    Assert.NotNull(dropShadowWindow);

                    var tornEdgeWindow = new Greenshot.Editor.Forms.TornEdgeSettingsWindow();
                    Assert.NotNull(tornEdgeWindow);

                    var resizeWindow = new Greenshot.Editor.Forms.ResizeSettingsWindow();
                    Assert.NotNull(resizeWindow);

                    var textObfuscationWindow = new Greenshot.Editor.Forms.TextObfuscationWindow();
                    Assert.NotNull(textObfuscationWindow);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public void ExpertSettings_IsBetaTester_ConfigAndTranslationWork()
        {
            var coreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();
            Assert.NotNull(coreConfig);

            bool original = coreConfig.IsBetaTester;
            try
            {
                coreConfig.IsBetaTester = true;
                Assert.True(coreConfig.IsBetaTester);
                coreConfig.IsBetaTester = false;
                Assert.False(coreConfig.IsBetaTester);
            }
            finally
            {
                coreConfig.IsBetaTester = original;
            }

            var textEn = Greenshot.Base.Core.Language.GetString("expertsettings_betatester");
            Assert.False(string.IsNullOrEmpty(textEn));
            Assert.Equal("Enable to enable beta-test features.", textEn);
        }

        [Fact]
        public void ComboBoxHelper_SuppressesAutoScrollOnHover()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    Greenshot.Base.Wpf.ComboBoxHelper.Initialize();

                    var cbi = new System.Windows.Controls.ComboBoxItem();
                    bool handled = false;
                    cbi.AddHandler(System.Windows.FrameworkElement.RequestBringIntoViewEvent, new System.Windows.RequestBringIntoViewEventHandler((s, e) =>
                    {
                        handled = e.Handled;
                    }), true);

                    cbi.BringIntoView();
                    Assert.True(handled, "RequestBringIntoView should be handled/suppressed when no navigation keys are pressed.");
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public void PluginConfigurations_QuicklinkEnabled_DefaultsToFalse()
        {
            var extCmd = IniConfigHelper.EnsureSection<IExternalCommandConfiguration>(() => new ExternalCommandConfigurationImpl());
            Assert.False(extCmd.QuicklinkEnabled);

            var imgur = IniConfigHelper.EnsureSection<IImgurConfiguration>(() => new ImgurConfigurationImpl());
            Assert.False(imgur.QuicklinkEnabled);

            var dropbox = IniConfigHelper.EnsureSection<IDropboxConfiguration>(() => new DropboxConfigurationImpl());
            Assert.False(dropbox.QuicklinkEnabled);

            var box = IniConfigHelper.EnsureSection<IBoxConfiguration>(() => new BoxConfigurationImpl());
            Assert.False(box.QuicklinkEnabled);

            var jira = IniConfigHelper.EnsureSection<IJiraConfiguration>(() => new JiraConfigurationImpl());
            Assert.False(jira.QuicklinkEnabled);

            var confluence = IniConfigHelper.EnsureSection<IConfluenceConfiguration>(() => new ConfluenceConfigurationImpl());
            Assert.False(confluence.QuicklinkEnabled);

            var office = IniConfigHelper.EnsureSection<Greenshot.Plugin.Office.IOfficeConfiguration>(() => new Greenshot.Plugin.Office.OfficeConfigurationImpl());
            Assert.False(office.QuicklinkEnabled);

            var zxing = IniConfigHelper.EnsureSection<IZxingConfiguration>(() => new ZxingConfigurationImpl());
            Assert.False(zxing.QuicklinkEnabled);
        }

        [Fact]
        public void ExternalCommandConfigurationControl_PropertyBindingsAndDependentPropertiesWork()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var config = IniConfigHelper.EnsureSection<IExternalCommandConfiguration>(() => new ExternalCommandConfigurationImpl());
                    var control = new ExternalCommandConfigurationControl();

                    // Quicklink
                    bool origQuicklink = control.QuicklinkEnabled;
                    control.QuicklinkEnabled = !origQuicklink;
                    Assert.Equal(!origQuicklink, control.QuicklinkEnabled);
                    Assert.Equal(!origQuicklink, config.QuicklinkEnabled);
                    control.QuicklinkEnabled = origQuicklink;

                    // Per-command settings on SelectedCommand
                    Assert.NotNull(control.SelectedCommand);
                    var cmd = control.SelectedCommand;

                    cmd.RedirectStandardOutput = true;
                    cmd.ParseOutputForUri = true;
                    Assert.True(cmd.CanConfigureOutputOptions);
                    Assert.True(cmd.CanConfigureUriToClipboard);

                    cmd.ParseOutputForUri = false;
                    Assert.True(cmd.CanConfigureOutputOptions);
                    Assert.False(cmd.CanConfigureUriToClipboard);

                    cmd.RedirectStandardOutput = false;
                    Assert.False(cmd.CanConfigureOutputOptions);
                    Assert.False(cmd.CanConfigureUriToClipboard);

                    cmd.RedirectStandardOutput = true;
                    cmd.ParseOutputForUri = true;

                    // Boolean toggling on SelectedCommand
                    cmd.RedirectStandardError = false;
                    Assert.False(cmd.RedirectStandardError);
                    Assert.False(config.RedirectStandardErrorCommand[cmd.Name]);
                    cmd.RedirectStandardError = true;
                    Assert.True(config.RedirectStandardErrorCommand[cmd.Name]);

                    cmd.ShowStandardOutputInLog = true;
                    Assert.True(cmd.ShowStandardOutputInLog);
                    Assert.True(config.ShowStandardOutputInLogCommand[cmd.Name]);
                    cmd.ShowStandardOutputInLog = false;
                    Assert.False(config.ShowStandardOutputInLogCommand[cmd.Name]);

                    cmd.OutputToClipboard = true;
                    Assert.True(cmd.OutputToClipboard);
                    Assert.True(config.OutputToClipboardCommand[cmd.Name]);
                    cmd.OutputToClipboard = false;
                    Assert.False(config.OutputToClipboardCommand[cmd.Name]);

                    cmd.UriToClipboard = false;
                    Assert.False(cmd.UriToClipboard);
                    Assert.False(config.UriToClipboardCommand[cmd.Name]);
                    cmd.UriToClipboard = true;
                    Assert.True(config.UriToClipboardCommand[cmd.Name]);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public void PluginUtils_QuicklinkAndSeparatorVisibilityTests()
        {
            // 1. Unified quicklink text
            string text = PluginUtils.GetQuicklinkText("Dropbox");
            Assert.Equal("Configure Dropbox", text);

            string textImgur = PluginUtils.GetQuicklinkText("Imgur");
            Assert.Equal("Configure Imgur", textImgur);

            // 2. Separator visibility when no plugin items are visible
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            var topSeparator = new System.Windows.Forms.ToolStripSeparator { Tag = "PluginsAreAddedAfter" };
            var pluginItem1 = new System.Windows.Forms.ToolStripMenuItem("Item 1") { Visible = false };
            var pluginItem2 = new System.Windows.Forms.ToolStripMenuItem("Item 2") { Visible = false };
            var bottomSeparator = new System.Windows.Forms.ToolStripSeparator { Tag = "PluginsAreAddedBefore" };

            contextMenu.Items.Add(topSeparator);
            contextMenu.Items.Add(pluginItem1);
            contextMenu.Items.Add(pluginItem2);
            contextMenu.Items.Add(bottomSeparator);

            PluginUtils.UpdatePluginSeparatorsVisibility(contextMenu);
            Assert.False(topSeparator.Available, "Top separator should be hidden when all plugin items are invisible");

            // Make one item visible
            pluginItem1.Available = true;
            PluginUtils.UpdatePluginSeparatorsVisibility(contextMenu);
            Assert.True(topSeparator.Available, "Top separator should be visible when at least one plugin item is visible");

            // Hide it again
            pluginItem1.Available = false;
            PluginUtils.UpdatePluginSeparatorsVisibility(contextMenu);
            Assert.False(topSeparator.Available, "Top separator should be hidden again when all items become invisible");
        }

        [Fact]
        public void ZxingEditorWindow_InstantiatesAndPopulatesModelCorrectly()
        {
            Assert.Equal(ZXing.BarcodeFormat.QR_CODE, ZxingEditorWindow.MapFormatIndex(0));
            Assert.Equal(ZXing.BarcodeFormat.CODE_128, ZxingEditorWindow.MapFormatIndex(4));
            Assert.Equal(0, ZxingEditorWindow.MapFormatToIndex(ZXing.BarcodeFormat.QR_CODE));
            Assert.Equal(4, ZxingEditorWindow.MapFormatToIndex(ZXing.BarcodeFormat.CODE_128));

            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var model = new ZxingModel
                    {
                        FormatIndex = 0,
                        QrCategoryIndex = 1,
                        WifiSsid = "TestWifi",
                        WifiPassword = "SecretPassword",
                        WifiEncryptionIndex = 0,
                        Margin = 2,
                        RoundedDots = true
                    };

                    var window = new ZxingEditorWindow(model);
                    Assert.NotNull(window);
                    Assert.Equal("Edit QR / Barcode", window.Title);

                    // Verify payload generation for WiFi
                    string payload = window.GetPayloadString();
                    Assert.Contains("WIFI:S:TestWifi;T:WPA;P:SecretPassword;;", payload);

                    // Test model populate
                    var updatedModel = new ZxingModel();
                    window.PopulateModel(updatedModel);
                    Assert.Equal("TestWifi", updatedModel.WifiSsid);
                    Assert.Equal("SecretPassword", updatedModel.WifiPassword);
                    Assert.True(updatedModel.RoundedDots);
                    Assert.Equal(2, updatedModel.Margin);

                    // Test theme toggle
                    bool initialTheme = WpfThemeHelper.IsDarkMode;
                    WpfThemeHelper.ToggleTheme();
                    Assert.NotEqual(initialTheme, WpfThemeHelper.IsDarkMode);
                    WpfThemeHelper.ToggleTheme(); // Toggle back
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public void ThemeManager_HasControlBorderBrushAndNotifies()
        {
            var tm = ThemeManager.Instance;
            Assert.NotNull(tm.ControlBorderBrush);

            var dict = tm.GetThemeResources();
            Assert.True(dict.Contains("ThemeControlBorderBrush"));

            bool notified = false;
            System.ComponentModel.PropertyChangedEventHandler handler = (s, e) =>
            {
                if (e.PropertyName == nameof(ThemeManager.ControlBorderBrush))
                {
                    notified = true;
                }
            };

            tm.PropertyChanged += handler;
            try
            {
                tm.ToggleTheme();
                Assert.True(notified);
            }
            finally
            {
                tm.PropertyChanged -= handler;
                tm.ToggleTheme(); // Restore
            }
        }

        [Fact]
        public void HotkeyEditorModal_HasInitialViewModelDataContext_ToPreventInheritedBindingErrors()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var modal = new HotkeyEditorModal();
                    Assert.NotNull(modal.DataContext);
                    Assert.IsType<HotkeyEditorViewModel>(modal.DataContext);
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.Null(threadEx);
        }

        [Fact]
        public void WindowsAppHelper_TrimExcessiveTransparentBorders_ReturnsDetachedBitmap()
        {
            using var bmp = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(Brushes.Red, 10, 10, 20, 20);
            }

            var trimmed = WindowsAppHelper.TrimExcessiveTransparentBorders(bmp);
            Assert.NotNull(trimmed);
            Assert.IsType<Bitmap>(trimmed);
            var bitmapSource = trimmed.ToBitmapSource();
            Assert.NotNull(bitmapSource);
            trimmed.Dispose();
        }
    }
}
