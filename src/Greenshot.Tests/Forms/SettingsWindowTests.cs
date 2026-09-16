using System;
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
    }
}
