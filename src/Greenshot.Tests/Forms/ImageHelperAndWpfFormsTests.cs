using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Media.Imaging;
using Dapplo.Ini;
using Greenshot.Base.Core;
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
using Greenshot.Plugin.Zxing;
using Greenshot.Plugin.Zxing.Forms;
using Xunit;

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
        public void ColorDialog_Facade_PropertiesAndGetInstanceWork()
        {
            using var cd = new Greenshot.Editor.Forms.ColorDialog
            {
                Color = System.Drawing.Color.MediumSeaGreen
            };

            Assert.Equal(System.Drawing.Color.MediumSeaGreen, cd.Color);
            Assert.Same(cd, Greenshot.Editor.Forms.ColorDialog.GetInstance());
        }
    }
}
