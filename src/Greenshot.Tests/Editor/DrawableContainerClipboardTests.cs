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
    }
}
