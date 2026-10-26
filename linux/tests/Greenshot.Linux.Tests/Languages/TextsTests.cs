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

using Greenshot.Linux.Core.Languages;

namespace Greenshot.Linux.Tests.Languages;

/// <summary>
/// Texts is static, the tests which load languages must not run in parallel with each other
/// </summary>
[Collection(nameof(TextsTests))]
public class TextsTests
{
    /// <summary>
    /// The language packs shared with Windows, and the Linux ones
    /// </summary>
    private static readonly string[] Folders =
    [
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../src/Greenshot/Languages")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Greenshot.Linux/Languages")),
    ];

    [Fact]
    public void The_shared_language_packs_are_found()
    {
        Assert.True(Directory.Exists(Folders[0]), Folders[0]);
        Texts.Initialize("en-US", Folders);
        var languages = Texts.AvailableLanguages;
        Assert.True(languages.Count >= 30, $"only {languages.Count} languages");
        Assert.Contains(languages, l => l.Ietf == "de-DE" && l.Description == "Deutsch");
    }

    [Fact]
    public void Texts_come_from_the_language_and_fall_back_to_english()
    {
        Texts.Initialize("de-DE", Folders);
        Assert.Equal("de-DE", Texts.Current);
        Assert.Equal("Bereich abfotografieren", Texts.Core("contextmenu_capturearea"));
        // Only in the Linux file, which has no German yet
        Assert.Equal("Same as the desktop", Texts.Linux("theme_system"));
        Assert.Equal("no_such_key", Texts.Core("no_such_key"));
        Texts.SetLanguage("en-US");
        Assert.Equal("Capture region", Texts.Core("contextmenu_capturearea"));
    }

    [Fact]
    public void Plugin_texts_are_found()
    {
        Texts.Initialize("de-DE", [.. Folders, Path.GetFullPath(Path.Combine(Folders[0], "../../Greenshot.Plugin.Pdf/Languages"))]);
        Assert.NotEqual("destination", Texts.Get("Pdf", "destination"));
        Texts.SetLanguage("en-US");
        Assert.Equal("Save as PDF", Texts.Get("Pdf", "destination"));
    }

    [Fact]
    public void Arguments_and_line_breaks_are_filled_in()
    {
        Texts.Initialize("en-US", Folders);
        Assert.Equal("Exported to: /tmp/a.png", Texts.Core("exported_to", "/tmp/a.png"));
        Assert.Contains("\n", Texts.Core("about_license"));
    }

    [Theory]
    [InlineData("de-AT", "de-DE")]
    [InlineData("sv", "sv-SE")]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("xx-YY", "en-US")]
    [InlineData("", "en-US")]
    public void The_closest_language_is_used(string wanted, string expected)
    {
        Assert.Equal(expected, Texts.FindClosest(wanted, ["de-DE", "de-x-franconia", "sv-SE", "pt-BR", "pt-PT", "en-US"]));
    }

    [Theory]
    [InlineData("Crop (C)", "Crop")]
    [InlineData("Undo {0}", "Undo")]
    [InlineData("&Save", "Save")]
    [InlineData("Line color (NumPad0-9, Shift+0-9)", "Line color")]
    [InlineData("Size (in pixels) of it", "Size (in pixels) of it")]
    public void Labels_lose_hints_and_placeholders(string text, string expected)
    {
        Assert.Equal(expected, Texts.Label(text));
    }

    [Fact]
    public void Ini_values_keep_equals_signs_and_unescape_line_breaks()
    {
        var texts = new Dictionary<string, Dictionary<string, string>>();
        Texts.ReadInto(texts, ["[Core]", "Some_Key = a=b\\nc", "; comment"]);
        Assert.Equal("a=b\nc", texts["core"]["some_key"]);
    }
}
