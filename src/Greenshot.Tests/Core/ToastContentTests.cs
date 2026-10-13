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


using System.Xml.Linq;
using Greenshot.Base.Native.Toasts;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The toast XML which Windows shows
    /// </summary>
    public class ToastContentTests
    {
        [Fact]
        public void ToXml_WithImageAndButton_WritesTheToastGenericTemplate()
        {
            var toast = XElement.Parse(new ToastContent("open:1")
                .AddText("Saved")
                .AddImage(@"C:\Temp\a b.png")
                .AddButton("Edit", "edit:1")
                .ToXml()
                .GetXml());

            Assert.Equal("open:1", (string)toast.Attribute("launch"));
            var binding = toast.Element("visual")?.Element("binding");
            Assert.Equal("ToastGeneric", (string)binding?.Attribute("template"));
            Assert.Equal("Saved", (string)binding?.Element("text"));
            Assert.Equal("file:///C:/Temp/a%20b.png", (string)binding?.Element("image")?.Attribute("src"));
            var action = toast.Element("actions")?.Element("action");
            Assert.Equal("Edit", (string)action?.Attribute("content"));
            Assert.Equal("edit:1", (string)action?.Attribute("arguments"));
        }

        [Fact]
        public void ToXml_WithoutButtons_HasNoActions()
        {
            var toast = XElement.Parse(new ToastContent("open:1").AddText("Saved").ToXml().GetXml());

            Assert.Null(toast.Element("actions"));
        }
    }
}
