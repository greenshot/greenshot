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
using System.Threading;
using Greenshot.Ai.Views;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using Greenshot.Plugins.Views;
using Greenshot.Settings.ViewModels;
using Greenshot.Settings.Views;
using Xunit;
using Greenshot.Base.Languages;
using Greenshot.Plugin.Box;
using Greenshot.Plugin.Dropbox;
using Greenshot.Plugin.Jira;
using Greenshot.Plugin.Office;

namespace Greenshot.Tests.Forms
{
    public class SettingsWindowTests
    {
        private readonly ITestOutputHelper _output;

        public SettingsWindowTests(ITestOutputHelper output)
        {
            _output = output;
            TestEnvironment.EnsureInitialized();
        }


        [Fact]
        public async System.Threading.Tasks.Task WindowsAppHelper_NegativeLookup_IsCached()
        {
            string nonExistentApp = "Definitely_Not_A_Real_App_987654.exe";
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result1 = await WindowsAppHelper.GetAppLogoAsync(nonExistentApp);
            long firstLookupMs = sw.ElapsedMilliseconds;
            Assert.Null(result1);

            sw.Restart();
            var result2 = await WindowsAppHelper.GetAppLogoAsync(nonExistentApp);
            long secondLookupMs = sw.ElapsedMilliseconds;
            Assert.Null(result2);

            // Second lookup must be cached and essentially instantaneous (< 10ms)
            Assert.True(secondLookupMs < 15, $"Second negative lookup should be cached (took {secondLookupMs}ms vs {firstLookupMs}ms)");
        }

        [Fact]
        public void SettingsViewModel_CanBeInstantiated()
        {
            var viewModel = new SettingsViewModel();
            Assert.NotNull(viewModel.CoreConfiguration);
            Assert.NotNull(viewModel.EditorConfiguration);
            Assert.NotNull(viewModel.ImageFormats);
            Assert.NotNull(viewModel.Destinations);
            Assert.NotNull(viewModel.Plugins);
            Assert.NotNull(viewModel.ClipboardFormats);
            Assert.NotEmpty(viewModel.ClipboardFormats);
        }

        [Fact]
        public void PluginTranslations_AreLoadedCorrectly()
        {
            Assert.Equal("Upload to Box", Texts.Get<IBoxLanguage>().UploadMenuItem);
            Assert.Equal("Image format", Texts.Get<IBoxLanguage>().LabelUploadFormat);
            Assert.Equal("Link to clipboard", Texts.Get<IBoxLanguage>().LabelAfterUploadLinkToClipBoard);
            Assert.Equal("Upload to Dropbox", Texts.Get<IDropboxLanguage>().UploadMenuItem);
            Assert.Equal("Upload to Jira", Texts.Get<IJiraLanguage>().UploadMenuItem);
            Assert.Equal("Office settings", Texts.Get<IOfficeLanguage>().SettingsTitle);
            Assert.Equal("Lock aspect ratio of the image", Texts.Get<IOfficeLanguage>().WordLockaspect);
            Assert.Equal("Slide layout for exported captures", Texts.Get<IOfficeLanguage>().PowerpointSlideLayout);
            Assert.Equal("Email format for new emails", Texts.Get<IOfficeLanguage>().OutlookEmailFormat);
        }

        [Fact]
        public void SettingsViewModel_PrintColorModes_SynchronizeCorrectly()
        {
            var viewModel = new SettingsViewModel();

            // Set to Grayscale
            viewModel.PrintGrayscale = true;
            Assert.True(viewModel.PrintGrayscale);
            Assert.False(viewModel.PrintColor);
            Assert.False(viewModel.PrintMonochrome);
            Assert.True(viewModel.CoreConfiguration.OutputPrintGrayscale);
            Assert.False(viewModel.CoreConfiguration.OutputPrintMonochrome);

            // Set to Monochrome
            viewModel.PrintMonochrome = true;
            Assert.True(viewModel.PrintMonochrome);
            Assert.False(viewModel.PrintColor);
            Assert.False(viewModel.PrintGrayscale);
            Assert.False(viewModel.CoreConfiguration.OutputPrintGrayscale);
            Assert.True(viewModel.CoreConfiguration.OutputPrintMonochrome);

            // Set to Color
            viewModel.PrintColor = true;
            Assert.True(viewModel.PrintColor);
            Assert.False(viewModel.PrintGrayscale);
            Assert.False(viewModel.PrintMonochrome);
            Assert.False(viewModel.CoreConfiguration.OutputPrintGrayscale);
            Assert.False(viewModel.CoreConfiguration.OutputPrintMonochrome);
        }

        [Fact]
        public void FixedToEnabledConverter_EvaluatesConstantAndBooleanFlags()
        {
            var converter = new FixedToEnabledConverter();
            var config = new CoreConfigurationImpl();

            // Normal property is not constant by default -> enabled (true)
            var result = converter.Convert(config, typeof(bool), nameof(ICoreConfiguration.OutputFilePath), null);
            Assert.Equal(true, result);

            // MultiBinding with true flag -> enabled
            var multiResultEnabled = converter.Convert(new object[] { true, config }, typeof(bool), nameof(ICoreConfiguration.OutputFilePath), null);
            Assert.Equal(true, multiResultEnabled);

            // MultiBinding with false flag (e.g. ExpertMode disabled) -> disabled (false)
            var multiResultDisabled = converter.Convert(new object[] { false, config }, typeof(bool), nameof(ICoreConfiguration.OutputFilePath), null);
            Assert.Equal(false, multiResultDisabled);
        }

        [Fact]
        public void SettingsWindow_CanBeInstantiatedOnStaThread()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var window = new SettingsWindow();
                    Assert.NotNull(window);
                    Assert.NotNull(window.DataContext);

                    var windowWithPlugin = new SettingsWindow("Imgur");
                    Assert.NotNull(windowWithPlugin);
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
        public void SettingsWindow_HasTheAiToolsAndPluginsPages_AndSelectsTabsByName()
        {
            Exception threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var window = new SettingsWindow();
                    // The full version: the pages are their own controls, Greenshot Light doesn't have them
                    Assert.IsType<AiToolsSettingsPage>(window.AiToolsTabItem.Content);
                    Assert.IsType<PluginsSettingsPage>(window.PluginsTabItem.Content);

                    window.SelectTab("plugins");
                    Assert.Same(window.PluginsTabItem, window.SettingsTabControl.SelectedItem);
                    // By name, not by index: the expert tab comes after the AI tools and plugins tabs
                    if (((SettingsViewModel)window.DataContext).IsExpertTabVisible)
                    {
                        window.SelectTab("expert");
                        Assert.Same(window.ExpertTabItem, window.SettingsTabControl.SelectedItem);
                    }
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
        public void SettingsViewModel_PluginSelection_ControlsAndPropertiesWork()
        {
            var viewModel = new SettingsViewModel();
            Assert.NotNull(viewModel.Plugins);

            // Test SelectPluginByName with invalid/empty
            viewModel.SelectPluginByName(null);
            viewModel.SelectPluginByName("");

            if (viewModel.Plugins.Count > 0)
            {
                var first = viewModel.Plugins[0];
                viewModel.SelectPluginByName(first.Name);
                Assert.Equal(first, viewModel.SelectedPlugin);

                if (viewModel.HasSelectedPluginControl)
                {
                    Assert.NotNull(viewModel.SelectedPluginControl);
                    Assert.Equal(System.Windows.Visibility.Visible, viewModel.SelectedPluginControlVisibility);
                    Assert.Equal(System.Windows.Visibility.Collapsed, viewModel.NoSelectedPluginControlVisibility);
                }
                else
                {
                    Assert.Null(viewModel.SelectedPluginControl);
                    Assert.Equal(System.Windows.Visibility.Collapsed, viewModel.SelectedPluginControlVisibility);
                    Assert.Equal(System.Windows.Visibility.Visible, viewModel.NoSelectedPluginControlVisibility);
                }
            }
        }
    }
}
