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
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Greenshot.Document;
using Greenshot.Editor.Standalone.Controls;
using Greenshot.Editor.Standalone.Settings;
using Greenshot.Editor.Standalone.ViewModels;
using Greenshot.Editor.Standalone.Views;

namespace Greenshot.Editor.Standalone.Tests.Ui
{
    /// <summary>
    /// Runs the editor without a screen (Avalonia.Headless, drawn by Skia like on a real screen) for the tests of its window
    /// </summary>
    internal static class EditorUi
    {
        private static readonly Lazy<HeadlessUnitTestSession> Session = new(() => HeadlessUnitTestSession.StartNew(typeof(EditorUi)));

        /// <summary>
        /// The tests never read or write the settings of the user
        /// </summary>
        [ModuleInitializer]
        internal static void UseOwnSettings()
        {
            EditorPaths.SettingsDirectory = Path.Combine(Path.GetTempPath(), "greenshot-editor-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(EditorPaths.SettingsDirectory);
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                try
                {
                    Directory.Delete(EditorPaths.SettingsDirectory, true);
                }
                catch (IOException)
                {
                    // Left for the cleanup of the temporary folder
                }
            };
        }

        // Found by HeadlessUnitTestSession
        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });

        /// <summary>
        /// Runs the test on the UI thread of the editor
        /// </summary>
        public static Task RunAsync(Func<Task> test) => Session.Value.Dispatch(async () =>
        {
            await test();
            return true;
        }, Xunit.TestContext.Current.CancellationToken);

        /// <summary>
        /// An editor window which shows the document, drawn
        /// </summary>
        public static async Task<EditorWindow> OpenAsync(GreenshotDocument document, int width = 1280, int height = 800)
        {
            var window = new EditorWindow(new EditorWindowViewModel()) { Width = width, Height = height };
            window.Show();
            if (document != null)
            {
                window.ViewModel.Add(document, "Test");
            }

            await DrawnAsync(window);
            return window;
        }

        public static DocumentCanvas CanvasOf(EditorWindow window) => window.FindControl<DocumentCanvas>("Canvas");

        /// <summary>
        /// Waits until the window shows the document as it is now: the document is drawn on another thread
        /// </summary>
        public static async Task DrawnAsync(EditorWindow window)
        {
            var canvas = CanvasOf(window);
            var timeout = DateTime.UtcNow.AddSeconds(30);
            do
            {
                Dispatcher.UIThread.RunJobs();
                await Task.Delay(10, Xunit.TestContext.Current.CancellationToken);
                if (DateTime.UtcNow > timeout)
                {
                    throw new TimeoutException("The document was not drawn");
                }
            } while (!canvas.IsDrawn);

            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>
        /// The window as it is drawn, saved as PNG when GREENSHOT_TEST_OUTPUT names a folder
        /// </summary>
        public static SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32> Capture(EditorWindow window, string name)
        {
            using var frame = window.CaptureRenderedFrame();
            using var stream = new MemoryStream();
            frame.Save(stream, PngBitmapEncoderOptions.Default);
            var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(stream.ToArray());
            Samples.Keep(image, name);
            return image;
        }
    }
}
