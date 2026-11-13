/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormatHandlers;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// Editor bugs from the old Jira tracker: a crop rectangle confirmed after another tool was chosen, counters with a
    /// transparent fill, opened .greenshot files and the invert effect.
    /// </summary>
    [Collection(TestCollections.InputState)]
    public class EditorOpenBugFixTests
    {
        public EditorOpenBugFixTests() => TestEnvironment.EnsureInitialized();

        private static void Sta(Action action)
        {
            Exception failure = null;
            var uiThread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            });
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            Assert.True(uiThread.Join(TimeSpan.FromSeconds(30)), "Surface interaction timed out.");
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        private static Bitmap NewBitmap(int width, int height)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            bitmap.SetResolution(96, 96);
            return bitmap;
        }

        private static void Mouse(Surface s, string name, int x, int y)
            => typeof(Control).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(s, new object[] { new MouseEventArgs(MouseButtons.Left, 1, x, y, 0) });

        /// <summary>
        /// BUG-3291: a crop rectangle stays when Esc chooses the cursor, selecting it and pressing Confirm threw a NotImplementedException
        /// </summary>
        [Fact]
        public void ACropRectangleSelectedAfterEscCanBeConfirmed() => Sta(() =>
        {
            using var surface = new Surface(NewBitmap(400, 300));
            surface.DrawingMode = DrawingModes.Crop;
            Mouse(surface, "OnMouseDown", 20, 20);
            Mouse(surface, "OnMouseMove", 120, 110);
            Mouse(surface, "OnMouseUp", 120, 110);
            var crop = Assert.IsType<CropContainer>(Assert.Single(surface.Elements));

            surface.DrawingMode = DrawingModes.None;
            surface.SelectElement(crop);
            surface.Confirm(true);

            Assert.Empty(surface.Elements);
            Assert.True(surface.Image.Width < 400 && surface.Image.Height < 300, "The image was cropped");
        });

        /// <summary>
        /// BUG-3291: Cancel removes the crop rectangle in the same situation
        /// </summary>
        [Fact]
        public void ACropRectangleSelectedAfterEscCanBeCancelled() => Sta(() =>
        {
            using var surface = new Surface(NewBitmap(400, 300));
            surface.DrawingMode = DrawingModes.Crop;
            Mouse(surface, "OnMouseDown", 20, 20);
            Mouse(surface, "OnMouseMove", 120, 110);
            Mouse(surface, "OnMouseUp", 120, 110);
            var crop = Assert.IsType<CropContainer>(Assert.Single(surface.Elements));

            surface.DrawingMode = DrawingModes.None;
            surface.SelectElement(crop);
            surface.Confirm(false);

            Assert.Empty(surface.Elements);
            Assert.Equal(new Size(400, 300), surface.Image.Size);
        });

        /// <summary>
        /// BUG-2367: a counter with a transparent fill and no outline still shows its number, so it can be clicked
        /// </summary>
        [Fact]
        public void ATransparentCounterCanBeClicked() => Sta(() =>
        {
            using var surface = new Surface(new Bitmap(200, 200));
            var counter = new StepLabelContainer(surface) { Left = 50, Top = 50, Width = 30, Height = 30 };
            counter.SetFieldValue(FieldType.FILL_COLOR, Color.Transparent);
            counter.SetFieldValue(FieldType.LINE_THICKNESS, 0);
            surface.AddElement(counter, false);

            Assert.True(counter.ClickableAt(65, 65));
            Assert.False(counter.ClickableAt(120, 120));
            Assert.Same(counter, surface.Elements.ClickableElementAt(65, 65));
        });

        /// <summary>
        /// BUG-2274 and BUG-2908: an opened .greenshot file isn't changed and has nothing selected
        /// </summary>
        [Fact]
        public void AnOpenedGreenshotFileIsUnchangedAndHasNothingSelected() => Sta(() =>
        {
            var handler = new GreenshotFileFormatHandler();
            using var stream = new MemoryStream();
            using (var surface = new Surface(new Bitmap(200, 200)))
            {
                surface.AddElement(new EllipseContainer(surface) { Left = 10, Top = 10, Width = 40, Height = 40 }, false);
                surface.AddElement(new EllipseContainer(surface) { Left = 100, Top = 100, Width = 40, Height = 40 }, false);
                Assert.True(handler.TrySaveToStream((Bitmap)surface.Image, stream, ".greenshot", surface));
            }

            stream.Position = 0;
            using var opened = (Surface)handler.LoadSurface(stream);

            Assert.Equal(2, opened.Elements.Count);
            Assert.False(opened.Modified);
            Assert.False(opened.HasSelectedElements);
        });

        /// <summary>
        /// BUG-2397: the invert effect keeps transparent pixels transparent
        /// </summary>
        [Fact]
        public void InvertKeepsTransparentPixelsTransparent()
        {
            using var source = NewBitmap(2, 1);
            source.SetPixel(0, 0, Color.FromArgb(0, 0, 0, 0));
            source.SetPixel(1, 0, Color.FromArgb(255, 255, 0, 0));

            using var inverted = ImageHelper.CreateNegative(source);

            Assert.Equal(0, inverted.GetPixel(0, 0).A);
            var red = inverted.GetPixel(1, 0);
            Assert.Equal(255, red.A);
            Assert.Equal(0, red.R);
            Assert.Equal(255, red.G);
            Assert.Equal(255, red.B);
        }
    }
}
