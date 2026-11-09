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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Automation;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Capturing;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using Xunit;

namespace Greenshot.Tests.Ocr
{
    /// <summary>
    /// The text read from the applications: into pixels of the capture, without what other windows cover, and the OCR only where it has none
    /// </summary>
    public class ApplicationTextTests
    {
        private static UiAutomationTextLine Line(params UiAutomationTextWord[] words) =>
            new(words.Select(word => word.Bounds).Aggregate((a, b) => a.Union(b)), words);

        /// <summary>
        /// Dapplo.Windows on a real window: a WPF window with a text and a text box shows on the screen for a moment, its text is read
        /// through UI Automation. Only with GREENSHOT_UIA_TEST set, as it shows a window.
        /// </summary>
        [Fact]
        public async Task ReadVisibleText_OfAWpfWindow_GivesItsWords()
        {
            Assert.SkipWhen(Environment.OSVersion.Platform != PlatformID.Win32NT || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GREENSHOT_UIA_TEST")),
                "Set GREENSHOT_UIA_TEST on Windows, it shows a window");
            var shown = new TaskCompletionSource<IntPtr>(TaskCreationOptions.RunContinuationsAsynchronously);
            System.Windows.Threading.Dispatcher dispatcher = null;
            var thread = new Thread(() =>
            {
                var panel = new System.Windows.Controls.StackPanel();
                panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Greenshot reads application text", FontSize = 20 });
                panel.Children.Add(new System.Windows.Controls.TextBox { Text = "Text in a text box", FontSize = 20 });
                var window = new System.Windows.Window { Content = panel, Width = 600, Height = 200, Left = 100, Top = 100, Topmost = true, ShowActivated = false };
                window.ContentRendered += (_, _) => shown.TrySetResult(new System.Windows.Interop.WindowInteropHelper(window).Handle);
                dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                window.Show();
                System.Windows.Threading.Dispatcher.Run();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
            try
            {
                var handle = await shown.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

                var snapshot = await UiAutomationText.ReadVisibleTextAsync(handle, NativeRect.Empty, null, null, TestContext.Current.CancellationToken);

                var words = snapshot.Words.Select(word => word.Text).ToList();
                Assert.Contains("application", string.Join(" ", words));
                Assert.Contains("box", string.Join(" ", words));
                Assert.All(snapshot.Words, word => Assert.False(word.Bounds.IsEmpty));
            }
            finally
            {
                dispatcher?.InvokeShutdown();
            }
        }

        [Fact]
        public void ToLines_MovesIntoTheCapture_KeepsLinkTargets()
        {
            var captureBounds = new NativeRect(100, 50, 800, 600);
            var lines = ApplicationText.ToLines(new[]
            {
                Line(new UiAutomationTextWord(new NativeRect(110, 60, 40, 16), "Visit", null),
                     new UiAutomationTextWord(new NativeRect(160, 60, 120, 16), "Greenshot", "https://getgreenshot.org/"))
            }, captureBounds, new List<NativeRect>());

            var line = Assert.Single(lines);
            Assert.Equal(TextSource.UiAutomation, line.Source);
            Assert.Equal("Visit Greenshot", line.Text);
            Assert.Equal(new NativeRect(10, 10, 170, 16), line.Bounds);
            Assert.Null(line.Words[0].LinkTarget);
            Assert.Equal("https://getgreenshot.org/", line.Words[1].LinkTarget);
            Assert.Null(line.Score);
        }

        [Fact]
        public void ToLines_WordsUnderAnotherWindow_AreLeftOut()
        {
            var captureBounds = new NativeRect(0, 0, 800, 600);
            var lines = ApplicationText.ToLines(new[]
            {
                Line(new UiAutomationTextWord(new NativeRect(10, 10, 40, 16), "Seen", null),
                     new UiAutomationTextWord(new NativeRect(300, 10, 40, 16), "Covered", null)),
                Line(new UiAutomationTextWord(new NativeRect(300, 40, 40, 16), "Hidden", null))
            }, captureBounds, new List<NativeRect> { new(250, 0, 200, 100) });

            var line = Assert.Single(lines);
            Assert.Equal("Seen", line.Text);
        }

        [Fact]
        public void Split_ElementName_SharesTheWidthByCharacters()
        {
            var words = ApplicationText.Split("ab cd", new NativeRect(0, 0, 50, 10), "https://example.com").ToList();

            Assert.Equal(new[] { "ab", "cd" }, words.Select(word => word.Text));
            Assert.Equal(0, words[0].Box.Unrotated.Left, 6);
            Assert.Equal(20, words[0].Box.Width, 6);
            Assert.Equal(30, words[1].Box.Unrotated.Left, 6);
            Assert.All(words, word => Assert.Equal("https://example.com", word.LinkTarget));
        }

        [Fact]
        public void AddRecognized_KeepsOcrLinesOnlyOutsideTheApplicationText()
        {
            var details = new CaptureDetails();
            details.Features.Add(new DetectedOcrLine(new NativeRect(0, 0, 200, 20), "exact text", new List<OcrWordInfo>()) { Source = TextSource.UiAutomation });
            var underApplicationText = new DetectedOcrLine(new NativeRect(2, 2, 190, 18), "exakt toxt", new List<OcrWordInfo>());
            var inAnImage = new DetectedOcrLine(new NativeRect(0, 300, 200, 20), "text in an image", new List<OcrWordInfo>());

            Assert.True(OcrLines.TryStartOcr(details));
            OcrLines.AddRecognized(details, new IOcrLineFeature[] { underApplicationText, inAnImage }, details.CropArea);

            Assert.Equal(new[] { "exact text", "text in an image" }, OcrLines.Find(details).Select(line => line.Text));
            // The OCR ran: it doesn't run again
            Assert.False(OcrLines.TryStartOcr(details));
        }
    }
}
