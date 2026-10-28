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

using Greenshot.Linux.Editor.Emoji;
using SkiaSharp;

namespace Greenshot.Linux.Tests.Editor;

public class EmojiTests
{
    [Fact]
    public void Emoji_are_drawn_in_color_with_the_twemoji_font()
    {
        Assert.True(File.Exists(EmojiRenderer.FontPath));
        var bitmap = EmojiRenderer.GetBitmap("😀", 64);
        var center = bitmap.GetPixel(32, 20);
        // The yellow face, not a black outline glyph
        Assert.True(center.Red > 200 && center.Green > 150 && center.Blue < 100, center.ToString());
    }

    [Fact]
    public void Skin_tones_are_combined_with_their_emoji()
    {
        var yellow = EmojiRenderer.GetBitmap("👋", 64);
        var medium = EmojiRenderer.GetBitmap("👋🏽", 64);
        // Not the yellow hand followed by a brown square: one hand in the skin tone
        var hand = FirstOpaque(yellow);
        var toned = medium.GetPixel(hand.X, hand.Y);
        Assert.True(toned.Alpha > 200 && toned.Red < yellow.GetPixel(hand.X, hand.Y).Red - 30, $"{toned} {yellow.GetPixel(hand.X, hand.Y)}");
        Assert.Equal(0, medium.GetPixel(63, 32).Alpha);
    }

    private static SKPointI FirstOpaque(SKBitmap bitmap)
    {
        for (int y = 24; y < 40; y++)
        {
            for (int x = 20; x < 44; x++)
            {
                var color = bitmap.GetPixel(x, y);
                if (color.Alpha == 255 && color.Red > 200 && color.Green > 150)
                {
                    return new SKPointI(x, y);
                }
            }
        }

        throw new InvalidOperationException("No yellow in the middle");
    }

    [Fact]
    public void The_picker_has_the_groups_with_the_skin_tones_as_variations()
    {
        var groups = EmojiData.Groups;
        Assert.Equal("Smileys & Emotion", groups[0].Name);
        Assert.Equal("😀", groups[0].Icon);
        Assert.DoesNotContain(groups, g => g.Name == "Component");
        var wave = groups.SelectMany(g => g.Emojis).First(e => e.Name == "waving hand");
        Assert.Equal(5, wave.Variations.Count);
        Assert.True(groups.Sum(g => g.Emojis.Count) > 1500);
    }
}
