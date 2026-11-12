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
using System.Linq;
using System.Windows.Media;
using Greenshot.Document;
using Greenshot.Document.Editing;
using Greenshot.WpfEditor.Editing;
using Greenshot.WpfEditor.Files;
using Greenshot.WpfEditor.Tools;
using Greenshot.WpfEditor.ViewModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Xunit;
using static Greenshot.Tests.WpfEditor.WpfEditorTestHelper;

namespace Greenshot.Tests.WpfEditor
{
    [Collection(TestCollections.WpfEditorSession)]
    public class EditorPanelAndFilesTests
    {
        private static byte[] Png(int width, int height, Action<IImageProcessingContext> draw = null)
        {
            using var image = new Image<Rgba32>(width, height, new Rgba32(255, 255, 255, 255));
            if (draw != null)
            {
                image.Mutate(draw);
            }

            using var stream = new MemoryStream();
            image.SaveAsPng(stream);
            return stream.ToArray();
        }

        [Fact]
        public void Properties_ChangeTheSelectedElement_AndTheDefaultOfItsTool()
        {
            RunOnSta(() =>
            {
                var before = EditorSession.Current.StyleFor(ToolKind.Ellipse);
                try
                {
                    var ellipse = new EllipseElement(Guid.NewGuid(), new RectD(10, 10, 40, 40));
                    using var tab = new DocumentTabViewModel(DocumentEdits.Add(NewDocument(), new[] { ellipse }), "test");
                    var properties = new PropertiesViewModel { Tab = tab };
                    Assert.False(properties.ShowsSelection);

                    tab.Select(new[] { ellipse.Id });
                    Assert.True(properties.ShowsSelection);
                    Assert.True(properties.HasStroke);
                    properties.LineWidth = 7;
                    properties.LineColor = Colors.Blue;

                    var style = tab.Committed.StyleOf(DocumentEdits.Find(tab.Committed, ellipse.Id));
                    Assert.Equal(7, style.Stroke.Width);
                    Assert.Equal(new Rgba(0, 0, 255, 255), style.Stroke.Color);
                    Assert.Equal(7, EditorSession.Current.StyleFor(ToolKind.Ellipse).Stroke.Width);
                }
                finally
                {
                    EditorSession.Current.SetStyle(ToolKind.Ellipse, before);
                }
            });
        }

        [Fact]
        public void CaptureFrames_ABorderGrowsTheCanvas_RemovingItShrinksIt()
        {
            var document = NewDocument();

            var framed = CaptureFrames.WithFrame(document, frame => frame with { Band = new FrameBand(5, new Rgba(0, 0, 0, 255)) });

            Assert.NotNull(CaptureFrames.FrameOf(framed));
            Assert.Equal(CanvasFit.Content, framed.Canvas.Fit);
            Assert.Equal(410, framed.CanvasArea().Width);

            var plain = CaptureFrames.WithoutFrame(framed);
            Assert.Null(CaptureFrames.FrameOf(plain));
            Assert.Equal(new RectD(0, 0, 400, 300), plain.CanvasArea());
        }

        [Fact]
        public void AutoCropping_FindsTheContentInsideAPlainBorder()
        {
            var document = DocumentFiles.FromImageBytes(Png(100, 80, context => context.Fill(SixLabors.ImageSharp.Color.Red, new RectangleF(20, 10, 30, 40))));

            Assert.Equal(new RectD(20, 10, 30, 40), AutoCropping.ContentArea(document));
        }

        [Fact]
        public void DocumentFiles_SaveAndOpenAGreenshotPackage()
        {
            string path = Path.Combine(Path.GetTempPath(), $"wpfeditor-{Guid.NewGuid():N}.greenshot");
            try
            {
                var arrow = new ArrowElement(Guid.NewGuid(), new PointD(10, 10), new PointD(60, 40));
                var document = DocumentEdits.Add(DocumentFiles.FromImageBytes(Png(100, 80)), new[] { arrow });

                DocumentFiles.Save(document, path);
                var opened = DocumentFiles.Open(path);

                Assert.Equal(new RectD(0, 0, 100, 80), opened.CanvasArea());
                Assert.Equal(arrow.End, ((ArrowElement)DocumentEdits.Find(opened, arrow.Id)).End);
                Assert.False(File.Exists(path + ".saving"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void DocumentFiles_SaveAsPng_DrawsTheElements()
        {
            string path = Path.Combine(Path.GetTempPath(), $"wpfeditor-{Guid.NewGuid():N}.png");
            try
            {
                var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(10, 10, 30, 30)) { Style = new Style { Fill = FillStyle.Of(new Rgba(0, 0, 255, 255)), Shadow = new ShadowStyle { Enabled = false } } };
                var document = DocumentEdits.Add(DocumentFiles.FromImageBytes(Png(100, 80)), new[] { rectangle });

                DocumentFiles.Save(document, path);

                using var saved = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
                Assert.Equal(100, saved.Width);
                Assert.Equal(new Rgba32(0, 0, 255, 255), saved[25, 25]);
                Assert.Equal(new Rgba32(255, 255, 255, 255), saved[80, 70]);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void DocumentFiles_AnImageOpensAsNewDocument()
        {
            string path = Path.Combine(Path.GetTempPath(), $"wpfeditor-{Guid.NewGuid():N}.png");
            try
            {
                File.WriteAllBytes(path, Png(64, 32));

                var document = DocumentFiles.Open(path);

                Assert.Equal(new RectD(0, 0, 64, 32), document.CanvasArea());
                Assert.Equal("image/png", document.Resources.Values.Single().MediaType);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void ElementText_LabelsAndTexts()
        {
            var rectangle = new RectangleElement(Guid.NewGuid(), new RectD(0, 0, 10, 10));

            var labelled = (RectangleElement)ElementText.WithText(rectangle, "Label");
            Assert.Equal("Label", ElementText.TextOf(labelled));
            Assert.Null(((RectangleElement)ElementText.WithText(labelled, string.Empty)).Label);
            Assert.Equal("x", ElementText.TextOf(ElementText.WithText(new TextElement(Guid.NewGuid(), default, string.Empty), "x")));
        }
    }
}
