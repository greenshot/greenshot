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
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Greenshot.Document;
using Greenshot.Editor.Standalone.Files;
using Greenshot.Editor.Standalone.Ipc;
using Greenshot.Editor.Standalone.Settings;
using Greenshot.Editor.Standalone.Tools;
using Greenshot.Editor.Standalone.ViewModels;
using Greenshot.Editor.Standalone.Views;
using Xunit;

namespace Greenshot.Editor.Standalone.Tests.Ui
{
    public class EditorWindowTests
    {
        /// <summary>
        /// Where a point of the document is in the window
        /// </summary>
        private static Point At(EditorWindow window, double x, double y)
        {
            var canvas = EditorUi.CanvasOf(window);
            return canvas.TranslatePoint(canvas.ToScreen(new PointD(x, y)), window) ?? default;
        }

        private static async Task DragAsync(EditorWindow window, PointD from, PointD to)
        {
            window.MouseDown(At(window, from.X, from.Y), MouseButton.Left);
            window.MouseMove(At(window, (from.X + to.X) / 2, (from.Y + to.Y) / 2));
            window.MouseMove(At(window, to.X, to.Y));
            window.MouseUp(At(window, to.X, to.Y), MouseButton.Left);
            await EditorUi.DrawnAsync(window);
        }

        private static void Close(EditorWindow window)
        {
            foreach (var tab in window.ViewModel.Tabs)
            {
                tab.IsModified = false;
            }

            window.Close();
        }

        [Fact]
        public Task TheWindow_ShowsTheDocument() => EditorUi.RunAsync(async () =>
        {
            var window = await EditorUi.OpenAsync(Samples.Gallery());
            using var image = EditorUi.Capture(window, "window");
            var middle = At(window, 24, 10);
            Close(window);
            Assert.Equal(1280, image.Width);
            // The capture is drawn in the middle of the canvas: its stripes are there
            Assert.Equal(new SixLabors.ImageSharp.PixelFormats.Rgba32(40, 40, 60), image[(int)middle.X, (int)middle.Y]);
        });

        [Fact]
        public Task ARectangle_IsDrawnWithTheMouse_AndUndone() => EditorUi.RunAsync(async () =>
        {
            var window = await EditorUi.OpenAsync(Samples.Page(400, 300, Samples.White, null));
            var tab = window.ViewModel.SelectedTab;
            tab.ToolKind = ToolKind.Rectangle;
            await DragAsync(window, new PointD(50, 40), new PointD(250, 160));

            var rectangle = Assert.IsType<RectangleElement>(Assert.Single(tab.Document.Layers[1].Elements));
            Assert.Equal(new RectD(50, 40, 200, 120), rectangle.Bounds);
            Assert.Equal(rectangle.Id, Assert.Single(tab.Selection));
            Assert.True(tab.IsModified);
            using (EditorUi.Capture(window, "rectangle"))
            {
            }

            tab.Select(System.Array.Empty<System.Guid>());
            await EditorUi.DrawnAsync(window);
            using (var image = EditorUi.Capture(window, "rectangle-drawn"))
            {
                var edge = At(window, 150, 40);
                var color = image[(int)edge.X, (int)edge.Y];
                Assert.True(color.R > 150 && color.G < 100, $"The line of the rectangle is drawn: {color}");
            }

            window.ViewModel.UndoCommand.Execute(null);
            await EditorUi.DrawnAsync(window);
            Assert.Empty(tab.Document.Layers[1].Elements);
            Close(window);
        });

        [Fact]
        public Task AnElement_IsSelectedAndMovedWithTheMouse() => EditorUi.RunAsync(async () =>
        {
            var window = await EditorUi.OpenAsync(Samples.Page(400, 300, Samples.White, null,
                new RectangleElement(System.Guid.NewGuid(), new RectD(40, 40, 100, 60)) { Style = new Greenshot.Document.Style { Fill = FillStyle.Of(Samples.Green) } }));
            var tab = window.ViewModel.SelectedTab;
            await DragAsync(window, new PointD(90, 70), new PointD(190, 120));

            var moved = tab.Document.Layers[1].Elements[0];
            Assert.Equal(new RectD(140, 90, 100, 60), moved.Bounds);
            Assert.True(window.ViewModel.Properties.HasArea);
            using var image = EditorUi.Capture(window, "selected");
            Close(window);
        });

        [Fact]
        public Task Text_IsTypedIntoANewTextBox() => EditorUi.RunAsync(async () =>
        {
            var window = await EditorUi.OpenAsync(Samples.Page(400, 300, Samples.White, null));
            var tab = window.ViewModel.SelectedTab;
            tab.ToolKind = ToolKind.Text;
            await DragAsync(window, new PointD(40, 40), new PointD(300, 120));
            Assert.NotNull(tab.EditingText);
            window.KeyTextInput("Hello");
            await EditorUi.DrawnAsync(window);
            using (EditorUi.Capture(window, "text-editing"))
            {
            }

            window.KeyPress(Key.Enter, RawInputModifiers.Control, PhysicalKey.Enter, null);
            window.KeyRelease(Key.Enter, RawInputModifiers.Control, PhysicalKey.Enter, null);
            await EditorUi.DrawnAsync(window);
            Assert.Null(tab.EditingText);
            var text = Assert.IsType<TextElement>(Assert.Single(tab.Document.Layers[1].Elements));
            Assert.Equal("Hello", text.Text);
            using (EditorUi.Capture(window, "text"))
            {
            }

            Close(window);
        });

        [Fact]
        public Task Escape_EndsTheTyping_AndKeepsTheNewSpeechBubbleWithItsText() => EditorUi.RunAsync(async () =>
        {
            var window = await EditorUi.OpenAsync(Samples.Page(400, 300, Samples.White, null));
            var tab = window.ViewModel.SelectedTab;
            tab.ToolKind = ToolKind.SpeechBubble;
            await DragAsync(window, new PointD(40, 40), new PointD(300, 120));
            Assert.NotNull(tab.EditingText);
            window.KeyTextInput("Some text");

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            await EditorUi.DrawnAsync(window);

            Assert.Null(tab.EditingText);
            Assert.Equal("Some text", Assert.IsType<SpeechBubbleElement>(Assert.Single(tab.Committed.Layers[1].Elements)).Text);
            Close(window);
        });

        [Fact]
        public Task TypedText_IsKept_WhenACommandEndsTheTyping() => EditorUi.RunAsync(async () =>
        {
            var window = await EditorUi.OpenAsync(Samples.Page(400, 300, Samples.White, null));
            var tab = window.ViewModel.SelectedTab;
            tab.ToolKind = ToolKind.Text;
            await DragAsync(window, new PointD(40, 40), new PointD(300, 120));
            window.KeyTextInput("Kept");

            // What saving, sending and copying do first
            tab.EndTextEdit(null, true);

            Assert.Null(tab.EditingText);
            Assert.Equal("Kept", Assert.IsType<TextElement>(Assert.Single(tab.Committed.Layers[1].Elements)).Text);
            Close(window);
        });

        [Fact]
        public Task ACaptureOfGreenshot_OpensAsANewDocument_AndItsFileIsRemoved() => EditorUi.RunAsync(async () =>
        {
            var window = await EditorUi.OpenAsync(null);
            Assert.False(window.ViewModel.HasTabs);
            System.IO.Directory.CreateDirectory(EditorPaths.HandoverDirectory);
            string file = System.IO.Path.Combine(EditorPaths.HandoverDirectory, System.Guid.NewGuid().ToString("N") + ".greenshot");
            var document = Samples.Page(Samples.Green);
            DocumentFiles.Save(document with { Metadata = new DocumentMetadata { Title = "Window of a program" } }, file);

            await window.ViewModel.OpenHandoverAsync(file);
            await EditorUi.DrawnAsync(window);

            var tab = Assert.Single(window.ViewModel.Tabs);
            Assert.Equal("Window of a program", tab.Title);
            Assert.Null(tab.FilePath);
            Assert.False(System.IO.File.Exists(file));
            Close(window);
        });

        [Fact]
        public Task AFileOutsideTheHandoverFolder_IsOpenedButNeverRemoved() => EditorUi.RunAsync(async () =>
        {
            var window = await EditorUi.OpenAsync(null);
            string file = System.IO.Path.Combine(EditorPaths.SettingsDirectory, "mine.greenshot");
            DocumentFiles.Save(Samples.Page(Samples.Green), file);

            await window.ViewModel.OpenHandoverAsync(file);

            Assert.Single(window.ViewModel.Tabs);
            Assert.True(System.IO.File.Exists(file));
            Close(window);
        });

        private sealed class FakeLink : IGreenshotLink
        {
            public bool IsConnected => true;
            public System.Collections.Generic.IReadOnlyList<EditorDestination> Destinations { get; } = new[] { new EditorDestination("Clipboard", "Clipboard") };
            public (EditorDestination Destination, GreenshotDocument Document, string Title)? Sent { get; private set; }
#pragma warning disable CS0067 // Nothing changes here
            public event System.Action Changed;
#pragma warning restore CS0067

            public Task RefreshAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task ExportAsync(EditorDestination destination, GreenshotDocument document, string title, System.Threading.CancellationToken cancellationToken = default)
            {
                Sent = (destination, document, title);
                return Task.CompletedTask;
            }
        }

        [Fact]
        public Task SendTo_GivesTheDocumentToGreenshot() => EditorUi.RunAsync(async () =>
        {
            var link = new FakeLink();
            EditorSession.Current.Greenshot = link;
            try
            {
                var window = await EditorUi.OpenAsync(Samples.Page(Samples.Green));
                var tab = window.ViewModel.SelectedTab;
                Assert.True(window.ViewModel.CanSendTo);
                Assert.True(window.ViewModel.SendToCommand.CanExecute(link.Destinations[0]));

                window.ViewModel.SendToCommand.Execute(link.Destinations[0]);
                await EditorUi.DrawnAsync(window);

                Assert.NotNull(link.Sent);
                Assert.Equal("Clipboard", link.Sent.Value.Destination.Designation);
                Assert.Same(tab.Committed, link.Sent.Value.Document);
                Assert.Equal("Test", link.Sent.Value.Title);
                Assert.Contains("Clipboard", window.ViewModel.StatusText);
                Close(window);
            }
            finally
            {
                EditorSession.Current.Greenshot = null;
            }
        });

        [Fact]
        public Task AStamp_IsPlacedAsAnImage() => EditorUi.RunAsync(async () =>
        {
            var window = await EditorUi.OpenAsync(Samples.Page(400, 300, Samples.White, null));
            var library = window.ViewModel.Stamps.Library;
            var stamp = library.Add(Samples.Png(40, 20, Samples.Red).Data.ToArray(), "Red");
            window.ViewModel.Stamps.Refresh();
            var entry = window.ViewModel.Stamps.Stamps.Single(candidate => candidate.Stamp.Key == stamp.Key);
            // The thumbnails are made in the background
            var timeout = System.DateTime.UtcNow.AddSeconds(10);
            while (entry.Thumbnail == null && System.DateTime.UtcNow < timeout)
            {
                await Task.Delay(10, TestContext.Current.CancellationToken);
            }

            Assert.NotNull(entry.Thumbnail);

            window.ViewModel.PlaceStampCommand.Execute(entry);
            while (window.ViewModel.SelectedTab.Document.Layers[1].Elements.Count == 0 && System.DateTime.UtcNow < timeout)
            {
                await Task.Delay(10, TestContext.Current.CancellationToken);
            }

            await EditorUi.DrawnAsync(window);
            var image = Assert.IsType<ImageElement>(Assert.Single(window.ViewModel.SelectedTab.Document.Layers[1].Elements));
            Assert.Equal(40, image.Bounds.Width);
            Assert.Equal(20, image.Bounds.Height);
            using (EditorUi.Capture(window, "stamp"))
            {
            }

            Close(window);
        });

        [Fact]
        public Task AnEmoji_IsShownTurned_WhileItsTurnHandleIsDragged() => EditorUi.RunAsync(async () =>
        {
            var emoji = new EmojiElement(System.Guid.NewGuid(), new RectD(60, 60, 64, 64), "\U0001F600");
            var window = await EditorUi.OpenAsync(Samples.Page(400, 300, Samples.White, null, emoji));
            var tab = window.ViewModel.SelectedTab;
            tab.Select(new[] { emoji.Id });
            await EditorUi.DrawnAsync(window);
            // The handle which turns is 24 screen pixels above the top; the middle of the emoji is at 92, 92
            window.MouseDown(At(window, 92, 60 - 24), MouseButton.Left);
            for (int step = 1; step <= 6; step++)
            {
                double angle = step * 30 * System.Math.PI / 180;
                window.MouseMove(At(window, 92 + 60 * System.Math.Sin(angle), 92 - 60 * System.Math.Cos(angle)));
                await EditorUi.DrawnAsync(window);
                Assert.Equal(step * 30, tab.Document.Layers[1].Elements[0].Rotation, 3);
            }

            // Half a turn: the eyes (dark) are now below the middle, where the cheeks (yellow) were
            using (var image = EditorUi.Capture(window, "emoji-turned"))
            {
                var eye = At(window, 82, 102);
                var color = image[(int)eye.X, (int)eye.Y];
                Assert.True(color.R < 160, $"An eye of the turned emoji is below its middle: {color}");
            }

            window.MouseUp(At(window, 92, 92 + 60), MouseButton.Left);
            await EditorUi.DrawnAsync(window);
            Close(window);
        });

        [Fact]
        public Task ACrop_RemovesWhatIsOutside_AndGoesBackToSelecting() => EditorUi.RunAsync(async () =>
        {
            var inside = new RectangleElement(System.Guid.NewGuid(), new RectD(60, 60, 64, 64));
            var outside = new RectangleElement(System.Guid.NewGuid(), new RectD(300, 200, 80, 60));
            var partly = new RectangleElement(System.Guid.NewGuid(), new RectD(200, 100, 80, 60));
            var window = await EditorUi.OpenAsync(Samples.Page(400, 300, null, null, inside, outside, partly));
            var tab = window.ViewModel.SelectedTab;
            tab.ToolKind = ToolKind.Crop;
            await DragAsync(window, new PointD(20, 20), new PointD(220, 180));

            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
            await EditorUi.DrawnAsync(window);

            Assert.Equal(new RectD(0, 0, 200, 160), tab.Document.CanvasArea());
            Assert.Equal(new[] { inside.Id, partly.Id }, tab.Document.Layers[1].Elements.Select(element => element.Id));
            Assert.Equal(ToolKind.Select, tab.ToolKind);
            Assert.False(window.ViewModel.IsCropping);
            using (EditorUi.Capture(window, "cropped"))
            {
            }

            window.ViewModel.UndoCommand.Execute(null);
            await EditorUi.DrawnAsync(window);
            Assert.Equal(3, tab.Document.Layers[1].Elements.Count);
            Close(window);
        });

        [Fact]
        public Task StepLabels_PlacedFastWithAMovingMouse_AllHaveTheUsualSize() => EditorUi.RunAsync(async () =>
        {
            var window = await EditorUi.OpenAsync(Samples.Page(400, 300, Samples.White, null));
            var tab = window.ViewModel.SelectedTab;
            tab.ToolKind = ToolKind.StepLabel;
            for (int i = 0; i < 4; i++)
            {
                // The mouse moves some pixels between pressing and releasing
                await DragAsync(window, new PointD(60 + i * 70, 100), new PointD(60 + i * 70 + 3 + i * 3, 100 + 2 + i * 2));
            }

            var labels = tab.Document.Layers[1].Elements;
            Assert.Equal(4, labels.Count);
            Assert.All(labels, label => Assert.Equal((30d, 30d), (label.Bounds.Width, label.Bounds.Height)));
            using (EditorUi.Capture(window, "step-labels"))
            {
            }

            // A clear drag still chooses the size
            await DragAsync(window, new PointD(60, 200), new PointD(120, 250));
            Assert.Equal((60d, 60d), (tab.Document.Layers[1].Elements[4].Bounds.Width, tab.Document.Layers[1].Elements[4].Bounds.Height));
            Close(window);
        });

        [Fact]
        public Task TornEdges_AreAddedAroundTheCapture() => EditorUi.RunAsync(async () =>
        {
            var window = await EditorUi.OpenAsync(Samples.Page(480, 300, null, null));
            window.ViewModel.AddTornEdgesCommand.Execute(null);
            await EditorUi.DrawnAsync(window);
            using (EditorUi.Capture(window, "torn-edges"))
            {
            }

            var frame = Assert.Single(window.ViewModel.SelectedTab.Document.Layers.SelectMany(layer => layer.Elements).OfType<FrameElement>());
            Assert.Equal(EdgeStyle.Torn, frame.Shape.Style);
            Assert.Equal(new RectD(-8, -8, 494, 314), window.ViewModel.SelectedTab.Document.CanvasArea());
            Close(window);
        });

        [Fact]
        public Task TornEdges_CanBeChanged_AfterClickingThem() => EditorUi.RunAsync(async () =>
        {
            var window = await EditorUi.OpenAsync(Samples.Page(480, 300, null, null));
            var tab = window.ViewModel.SelectedTab;
            var properties = window.ViewModel.Properties;
            window.ViewModel.AddTornEdgesCommand.Execute(null);
            await EditorUi.DrawnAsync(window);
            Assert.True(properties.ShowsFrame, "Nothing is selected: the settings of the edges are shown with the canvas");

            // A click on the capture with its torn edges: something is selected now, the settings stay
            await DragAsync(window, new PointD(240, 150), new PointD(240, 150));
            await EditorUi.DrawnAsync(window);
            Assert.NotEmpty(tab.SelectedElements);
            Assert.True(properties.ShowsFrame, "The settings of the torn edges are shown when they are clicked");
            Assert.True(properties.FrameHasTeeth);

            properties.FrameToothHeight = 24;
            properties.FrameToothRange = 40;
            properties.FrameBottom = false;
            await EditorUi.DrawnAsync(window);
            using (EditorUi.Capture(window, "torn-edges-changed"))
            {
            }

            var frame = Assert.Single(tab.Document.Layers.SelectMany(layer => layer.Elements).OfType<FrameElement>());
            Assert.Equal((24, 40), (frame.Shape.Height, frame.Shape.Range));
            Assert.Equal(EdgeStyle.None, frame.Sides.Bottom.Style);
            Assert.Null(frame.Sides.Top);
            Assert.False(properties.FrameBottom);
            properties.FrameBottom = true;
            Assert.Null(Assert.Single(tab.Document.Layers.SelectMany(layer => layer.Elements).OfType<FrameElement>()).Sides);
            Close(window);
        });

        [Fact]
        public Task Grayscale_ChangesTheCapture_NotWhatIsDrawnOnIt_AndUndoBringsItBack() => EditorUi.RunAsync(async () =>
        {
            var box = new RectangleElement(System.Guid.NewGuid(), new RectD(40, 40, 100, 60)) { Style = Samples.Plain with { Fill = FillStyle.Of(Samples.Red) } };
            var window = await EditorUi.OpenAsync(Samples.Page(200, 150, Samples.Green, null, box));
            var tab = window.ViewModel.SelectedTab;

            window.ViewModel.GrayscaleCommand.Execute(null);
            await EditorUi.DrawnAsync(window);

            using (var image = EditorUi.Capture(window, "grayscale"))
            {
                var capture = At(window, 10, 10);
                var gray = image[(int)capture.X, (int)capture.Y];
                Assert.True(gray.R == gray.G && gray.G == gray.B, $"The capture is gray: {gray}");
                var drawn = At(window, 90, 70);
                Assert.Equal(new SixLabors.ImageSharp.PixelFormats.Rgba32(255, 0, 0), image[(int)drawn.X, (int)drawn.Y]);
            }

            Assert.Single(tab.Document.Resources);
            window.ViewModel.InvertCommand.Execute(null);
            window.ViewModel.UndoCommand.Execute(null);
            window.ViewModel.UndoCommand.Execute(null);
            await EditorUi.DrawnAsync(window);
            using (var image = EditorUi.Capture(window, "grayscale-undone"))
            {
                var capture = At(window, 10, 10);
                Assert.Equal(new SixLabors.ImageSharp.PixelFormats.Rgba32(0, 128, 0), image[(int)capture.X, (int)capture.Y]);
            }

            Close(window);
        });

        [Fact]
        public Task TheDarkTheme_ShowsEveryPanel() => EditorUi.RunAsync(async () =>
        {
            Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            try
            {
                var document = Samples.Gallery();
                var window = await EditorUi.OpenAsync(document);
                var tab = window.ViewModel.SelectedTab;
                tab.Select(new[] { document.Layers[1].Elements.OfType<TextElement>().First().Id });
                await EditorUi.DrawnAsync(window);
                using var image = EditorUi.Capture(window, "dark");
                Close(window);
                Assert.True(image[5, 400].R < 80, "The window is dark");
            }
            finally
            {
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            }
        });
    }
}
