using System;
using System.IO;
using System.Linq;
using System.Threading;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Wpf;
using Greenshot.Forms.Wpf;
using Xunit;
using Xunit.Abstractions;

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
        public void WindowsAppHelper_NegativeLookup_IsCached()
        {
            string nonExistentApp = "Definitely_Not_A_Real_App_987654.exe";
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result1 = WindowsAppHelper.GetAppLogo(nonExistentApp);
            long firstLookupMs = sw.ElapsedMilliseconds;
            Assert.Null(result1);

            sw.Restart();
            var result2 = WindowsAppHelper.GetAppLogo(nonExistentApp);
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
            Assert.NotNull(viewModel.WindowCaptureModes);
            Assert.NotNull(viewModel.Destinations);
            Assert.NotNull(viewModel.Plugins);
            Assert.NotNull(viewModel.ClipboardFormats);
            Assert.NotEmpty(viewModel.ClipboardFormats);
        }

        [Fact]
        public void PluginTranslations_AreLoadedCorrectly()
        {
            Assert.Equal("Upload to Box", Language.GetString("box", "upload_menu_item"));
            Assert.Equal("Image format", Language.GetString("box.label_upload_format"));
            Assert.Equal("Link to clipboard", Language.GetString("box.label_AfterUploadLinkToClipBoard"));
            Assert.Equal("Upload to Dropbox", Language.GetString("dropbox", "upload_menu_item"));
            Assert.Equal("Upload to Jira", Language.GetString("jira", "upload_menu_item"));
            Assert.Equal("Office settings", Language.GetString("office", "settings_title"));
            Assert.Equal("Lock aspect ratio of the image", Language.GetString("office", "word_lockaspect"));
            Assert.Equal("Slide layout for exported captures", Language.GetString("office", "powerpoint_slide_layout"));
            Assert.Equal("Email format for new emails", Language.GetString("office", "outlook_email_format"));
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
