using System;
using System.IO;
using System.Linq;
using System.Threading;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Wpf;
using Greenshot.Forms.Wpf;
using Xunit;

namespace Greenshot.Tests.Forms
{
    public class SettingsWindowTests
    {
        public SettingsWindowTests()
        {
            TestEnvironment.EnsureInitialized();
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
