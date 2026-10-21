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

using System.Collections;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Greenshot.Base.Core;
using Greenshot.Editor.Drawing;
using Xunit;

namespace Greenshot.Tests.Editor
{
    [Collection(TestCollections.Clipboard)]
    public class DrawableContainerClipboardTests
    {
        public DrawableContainerClipboardTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static DrawableContainerList CreateElements(Surface surface)
        {
            var rectangle = new RectangleContainer(surface)
            {
                Left = 10,
                Top = 20,
                Width = 30,
                Height = 40
            };
            return new DrawableContainerList(surface.ID) { rectangle };
        }

        [Fact]
        public void SerializeDeserialize_RoundTrip()
        {
            using var surface = new Surface();
            var elements = CreateElements(surface);

            var result = DrawableContainerClipboard.Deserialize(DrawableContainerClipboard.Serialize(elements));

            Assert.NotNull(result);
            var rectangle = Assert.IsType<RectangleContainer>(Assert.Single(result));
            Assert.Equal(30, rectangle.Width);
            Assert.Equal(surface.ID, result.ParentID);
        }

        [Fact]
        public void Deserialize_TypeWhichIsNotAllowed_ReturnsNull()
        {
            // Another application can place anything in the format: only the types of a .greenshot file are created
            using var stream = new MemoryStream();
            new BinaryFormatter().Serialize(stream, new Hashtable { { "key", "value" } });

            Assert.Null(DrawableContainerClipboard.Deserialize(stream.ToArray()));
        }

        [Fact]
        public void Deserialize_Garbage_ReturnsNull()
        {
            Assert.Null(DrawableContainerClipboard.Deserialize(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
            Assert.Null(DrawableContainerClipboard.Deserialize(new byte[0]));
            Assert.Null(DrawableContainerClipboard.Deserialize(null));
        }

        [InteractiveDesktopFact]
        public void CopyPaste_ThroughTheClipboard()
        {
            using var surface = new Surface();
            DrawableContainerClipboard.Copy(CreateElements(surface));

            Assert.True(DrawableContainerClipboard.IsAvailable);
            // No .NET object format anymore
            Assert.False(Dapplo.Windows.Clipboard.ClipboardNative.HasFormat("Greenshot.Base.Interfaces.Drawing.IDrawableContainerList"));
            var snapshot = ClipboardHelper.ReadSnapshot(new[] { DrawableContainerClipboard.Format }, DrawableContainerClipboard.MaxSize);
            var pasted = DrawableContainerClipboard.Read(snapshot);
            Assert.NotNull(pasted);
            Assert.IsType<RectangleContainer>(pasted.Single());
        }

        [InteractiveDesktopFact]
        public void Copy_WithoutImage_PlacesOnlyTheElements()
        {
            using var surface = new Surface();
            DrawableContainerClipboard.Copy(CreateElements(surface));

            Assert.True(DrawableContainerClipboard.IsAvailable);
            Assert.NotEqual(true, ClipboardHelper.ContainsImageQuick());
        }

        [InteractiveDesktopFact]
        public void Copy_WithImage_PlacesTheElementsAndTheImage()
        {
            using var surface = new Surface();
            using var image = new Bitmap(20, 10);
            DrawableContainerClipboard.Copy(CreateElements(surface), image);

            // The editor pastes the elements...
            Assert.True(DrawableContainerClipboard.IsAvailable);
            var elements = ClipboardHelper.ReadSnapshot(new[] { DrawableContainerClipboard.Format }, DrawableContainerClipboard.MaxSize);
            Assert.IsType<RectangleContainer>(Assert.Single(DrawableContainerClipboard.Read(elements)));

            // ...other applications paste the image
            Assert.True(ClipboardHelper.ContainsImageQuick());
            var snapshot = ClipboardHelper.ReadSnapshot(ClipboardHelper.SelectImageReadFormats().ToArray());
            using var pasted = ClipboardHelper.GetFirstImage(snapshot);
            Assert.Equal(image.Size, pasted.Size);
        }

        [InteractiveDesktopFact]
        public void CopySelectedElements_PlacesTheImageWithTheElements_AndPasteTakesTheElements()
        {
            using var surface = new Surface(new Bitmap(100, 80));
            var rectangle = new RectangleContainer(surface) { Left = 10, Top = 20, Width = 30, Height = 40 };
            surface.AddElement(rectangle);
            surface.SelectElement(rectangle);

            surface.CopySelectedElements();

            // The whole capture is on the clipboard, not only the selected element
            using (var pasted = ClipboardHelper.GetFirstImage(ClipboardHelper.ReadSnapshot(ClipboardHelper.SelectImageReadFormats().ToArray())))
            {
                Assert.Equal(new Size(100, 80), pasted.Size);
            }

            // Pasting in the editor still duplicates the element, the image isn't added as a second element
            surface.PasteElementFromClipboard();
            Assert.Equal(2, surface.Elements.OfType<RectangleContainer>().Count());
            Assert.Equal(2, surface.Elements.Count);
        }

        [InteractiveDesktopFact]
        public void CutSelectedElements_PlacesOnlyTheElements()
        {
            using var surface = new Surface(new Bitmap(100, 80));
            var rectangle = new RectangleContainer(surface) { Left = 10, Top = 20, Width = 30, Height = 40 };
            surface.AddElement(rectangle);
            surface.SelectElement(rectangle);

            surface.CutSelectedElements();

            Assert.True(DrawableContainerClipboard.IsAvailable);
            Assert.NotEqual(true, ClipboardHelper.ContainsImageQuick());
        }
    }
}
