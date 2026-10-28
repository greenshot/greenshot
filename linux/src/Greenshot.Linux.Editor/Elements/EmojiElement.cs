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

using System.Text.Json.Serialization;
using Greenshot.Linux.Editor.Emoji;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Rendering;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Elements;

/// <summary>
/// An emoji, drawn with the Twemoji font; it stays square. The EmojiContainer of the Windows editor.
/// </summary>
public sealed class EmojiElement : DrawableElement
{
    public const string DefaultEmoji = "🙂";

    private const float CreateSize = 64;

    private SKPoint _createStart;

    public string Emoji { get; set; } = DefaultEmoji;

    public override string WindowsTypeName => "EmojiContainer";

    [JsonIgnore]
    public override IReadOnlyList<FieldType> Fields => [];

    [JsonIgnore]
    public override SKSize? DefaultSize => new SKSize(CreateSize, CreateSize);

    public override ElementStyle CreateDefaultStyle() => new();

    public override void Draw(RenderContext context) => EmojiRenderer.Draw(context.Canvas, Emoji, NormalizedBounds);

    public override void BeginCreate(SKPoint point)
    {
        _createStart = point;
        base.BeginCreate(point);
    }

    /// <summary>
    /// Always square, like the scale options of Windows say
    /// </summary>
    public override void UpdateCreate(SKPoint point, bool constrain) => base.UpdateCreate(point, constrain: true);

    /// <summary>
    /// A click puts the emoji in its default size with its middle at the click, like on Windows
    /// </summary>
    public override bool EndCreate()
    {
        var r = NormalizedBounds;
        if (r.Width < MinimumSize && r.Height < MinimumSize)
        {
            Bounds = SKRect.Create(_createStart.X - CreateSize / 2, _createStart.Y - CreateSize / 2, CreateSize, CreateSize);
            return true;
        }

        return base.EndCreate();
    }

    public override void DragHandle(HandlePosition handle, SKPoint point, bool keepAspect) => base.DragHandle(handle, point, keepAspect: true);
}
