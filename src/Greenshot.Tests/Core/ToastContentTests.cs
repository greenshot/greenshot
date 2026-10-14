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


using System.Linq;
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
        public void ToXml_WithImage_WritesTheToastGenericTemplate()
        {
            var toast = XElement.Parse(new ToastContent("1")
                .AddText("Saved")
                .AddImage(@"C:\Temp\a b.png")
                .ToXml()
                .GetXml());

            Assert.Equal("1", (string)toast.Attribute("launch"));
            var binding = toast.Element("visual")?.Element("binding");
            Assert.Equal("ToastGeneric", (string)binding?.Attribute("template"));
            Assert.Equal("Saved", (string)binding?.Element("text"));
            Assert.Equal("file:///C:/Temp/a%20b.png", (string)binding?.Element("image")?.Attribute("src"));
        }

        [Fact]
        public void ToXml_WithRows_WritesAGroupPerRow_WithTheIconBesideTheText()
        {
            var binding = XElement.Parse(new ToastContent("1")
                .AddText("Window title")
                .AddRow(@"C:\Icons\clipboard.png", "Exported to: Clipboard")
                .AddRow(null, "Exported to: File")
                .ToXml()
                .GetXml()).Element("visual")?.Element("binding");

            var rows = binding?.Elements("group").ToList();
            Assert.NotNull(rows);
            Assert.Equal(2, rows.Count);

            var withIcon = rows[0].Elements("subgroup").ToList();
            Assert.Equal(2, withIcon.Count);
            Assert.Equal("file:///C:/Icons/clipboard.png", (string)withIcon[0].Element("image")?.Attribute("src"));
            Assert.Equal("true", (string)withIcon[0].Element("image")?.Attribute("hint-removeMargin"));
            Assert.Equal("Exported to: Clipboard", (string)withIcon[1].Element("text"));
            Assert.Equal("true", (string)withIcon[1].Element("text")?.Attribute("hint-wrap"));

            // Without an icon only the text
            var withoutIcon = rows[1].Elements("subgroup").ToList();
            Assert.Single(withoutIcon);
            Assert.Equal("Exported to: File", (string)withoutIcon[0].Element("text"));

            // The title stays the first text of the toast
            Assert.Equal("Window title", (string)binding.Element("text"));
        }

        [Fact]
        public void ToXml_WithAppLogo_WritesOneAppLogoOverride()
        {
            var binding = XElement.Parse(new ToastContent("1")
                .AddText("Window title")
                .SetAppLogo(@"C:\Icons\clipboard.png")
                .SetAppLogo(@"C:\Icons\word.png")
                .AddImage(@"C:\Temp\thumbnail.png")
                .ToXml()
                .GetXml()).Element("visual")?.Element("binding");

            var logos = binding?.Elements("image").Where(i => (string)i.Attribute("placement") == "appLogoOverride").ToList();
            Assert.NotNull(logos);
            Assert.Single(logos);
            Assert.Equal("file:///C:/Icons/word.png", (string)logos[0].Attribute("src"));
            // The thumbnail stays an inline image
            Assert.Contains(binding.Elements("image"), i => i.Attribute("placement") == null && (string)i.Attribute("src") == "file:///C:/Temp/thumbnail.png");
        }

        [Fact]
        public void NotificationIcons_GetFileName_IsAStableFileNameForEveryIconKey()
        {
            var resource = Greenshot.Notifications.NotificationIcons.GetFileName("resource:Clipboard.Image");
            var exe = Greenshot.Notifications.NotificationIcons.GetFileName(@"exe:0:C:\Program Files\Microsoft Office\root\Office16\WINWORD.EXE");

            Assert.Equal(resource, Greenshot.Notifications.NotificationIcons.GetFileName("resource:Clipboard.Image"));
            Assert.NotEqual(resource, exe);
            Assert.EndsWith(".png", exe);
            Assert.Equal(-1, exe.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()));
        }
    }
}
