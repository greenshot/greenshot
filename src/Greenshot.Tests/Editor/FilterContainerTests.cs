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

using System.Drawing;
using System.IO;
using System.Linq;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.FileFormat;
using Xunit;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// Loading a .greenshot file must keep the settings of the filters, not take the last used ones
    /// </summary>
    public class FilterContainerTests
    {
        public FilterContainerTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static T SaveAndLoad<T>(Surface surface) where T : DrawableContainer
        {
            using var stream = new MemoryStream();
            GreenshotFileVersionHandler.SaveToStreamInCurrentVersion(surface, stream);
            stream.Position = 0;
            using var loadedSurface = GreenshotFileVersionHandler.CreateSurfaceFromStream(stream);
            return Assert.Single(loadedSurface.Elements.OfType<T>());
        }

        [Fact]
        public void Obfuscate_KeepsTheBlurRadiusOnLoad()
        {
            using var surface = new Surface(new Bitmap(100, 100));
            DrawableContainer obfuscate = new ObfuscateContainer(surface);
            obfuscate.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, PreparedFilter.BLUR);
            obfuscate.SetFieldValue(FieldType.BLUR_RADIUS, 12);
            surface.AddElement(obfuscate, false, false);

            DrawableContainer loaded = SaveAndLoad<ObfuscateContainer>(surface);

            Assert.Equal(12, loaded.GetField(FieldType.BLUR_RADIUS).Value);
            Assert.Single(loaded.Filters);
        }

        [Fact]
        public void Highlight_KeepsTheColorOnLoad()
        {
            using var surface = new Surface(new Bitmap(100, 100));
            DrawableContainer highlight = new HighlightContainer(surface);
            highlight.SetFieldValue(FieldType.PREPARED_FILTER_HIGHLIGHT, PreparedFilter.TEXT_HIGHTLIGHT);
            highlight.SetFieldValue(FieldType.FILL_COLOR, Color.Red);
            surface.AddElement(highlight, false, false);

            DrawableContainer loaded = SaveAndLoad<HighlightContainer>(surface);

            Assert.Equal(Color.Red.ToArgb(), ((Color)loaded.GetField(FieldType.FILL_COLOR).Value).ToArgb());
            Assert.Single(loaded.Filters);
        }

        [Fact]
        public void Obfuscate_LoadedContainer_StillChangesItsFilter()
        {
            using var surface = new Surface(new Bitmap(100, 100));
            DrawableContainer obfuscate = new ObfuscateContainer(surface);
            obfuscate.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, PreparedFilter.BLUR);
            surface.AddElement(obfuscate, false, false);
            DrawableContainer loaded = SaveAndLoad<ObfuscateContainer>(surface);

            loaded.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, PreparedFilter.PIXELIZE);

            Assert.True(loaded.HasField(FieldType.PIXEL_SIZE));
            Assert.False(loaded.HasField(FieldType.BLUR_RADIUS));
        }
    }
}
