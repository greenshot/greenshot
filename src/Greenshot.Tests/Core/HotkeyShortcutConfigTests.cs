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

using System.Collections.Generic;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class HotkeyShortcutConfigTests
    {
        [Fact]
        public void ParseList_NullOrEmpty_ReturnsEmptyList()
        {
            Assert.Empty(HotkeyShortcutConfig.ParseList(null));
            Assert.Empty(HotkeyShortcutConfig.ParseList(string.Empty));
            Assert.Empty(HotkeyShortcutConfig.ParseList("   "));
        }

        [Fact]
        public void ParseList_InvalidJson_ReturnsEmptyListInsteadOfThrowing()
        {
            var result = HotkeyShortcutConfig.ParseList("{not-valid-json");
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ToJson_Then_ParseList_RoundTripsAllFields()
        {
            var original = new List<HotkeyShortcutConfig>
            {
                HotkeyShortcutConfig.Create("Ctrl + PrintScreen", HotkeyCaptureFunctions.Fullscreen, new[] { "Clipboard" }),
                HotkeyShortcutConfig.Create("Alt + PrintScreen", HotkeyCaptureFunctions.Window, new[] { "Editor" })
            };

            string json = HotkeyShortcutConfig.ToJson(original);
            var roundTripped = HotkeyShortcutConfig.ParseList(json);

            Assert.Equal(2, roundTripped.Count);

            Assert.Equal(original[0].Id, roundTripped[0].Id);
            Assert.Equal("Ctrl + PrintScreen", roundTripped[0].Hotkey);
            Assert.Equal(HotkeyCaptureFunctions.Fullscreen, roundTripped[0].CaptureFunctionId);
            Assert.Equal(new List<string> { "Clipboard" }, roundTripped[0].Destinations);

            Assert.Equal("Alt + PrintScreen", roundTripped[1].Hotkey);
            Assert.Equal(HotkeyCaptureFunctions.Window, roundTripped[1].CaptureFunctionId);
            Assert.Equal(new List<string> { "Editor" }, roundTripped[1].Destinations);
        }

        [Fact]
        public void ToJson_NullList_SerializesAsEmptyArray()
        {
            string json = HotkeyShortcutConfig.ToJson(null);
            var parsed = HotkeyShortcutConfig.ParseList(json);
            Assert.Empty(parsed);
        }

        [Fact]
        public void Create_GeneratesUniqueStableIds()
        {
            var a = HotkeyShortcutConfig.Create("Ctrl + Alt + R", HotkeyCaptureFunctions.Region, new[] { "Picker" });
            var b = HotkeyShortcutConfig.Create("Ctrl + Alt + R", HotkeyCaptureFunctions.Region, new[] { "Picker" });

            Assert.False(string.IsNullOrEmpty(a.Id));
            Assert.False(string.IsNullOrEmpty(b.Id));
            Assert.NotEqual(a.Id, b.Id);
        }
    }
}
