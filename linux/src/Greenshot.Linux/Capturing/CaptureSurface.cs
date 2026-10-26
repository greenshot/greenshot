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

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Capturing.Interactive;
using SkiaSharp;

namespace Greenshot.Linux.Capturing;

/// <summary>
/// Draws the frozen screen of one monitor with the selection, the crosshair, the zoomer and the panels.
/// The control shows the image pixels <see cref="ScreenPixels"/> of the snapshot, stretched over its size.
/// </summary>
internal sealed class CaptureSurface : Control
{
    private const double ZoomerSize = 160;
    private const int ZoomerPixels = 21;
    private const double ZoomerOffset = 24;

    private static readonly IBrush DimBrush = new SolidColorBrush(Color.FromArgb(0x70, 0, 0, 0));
    private static readonly IBrush WindowDimBrush = new SolidColorBrush(Color.FromArgb(0x50, 0, 0, 0));
    private static readonly IBrush PanelBrush = new SolidColorBrush(Color.FromArgb(0xE0, 0x20, 0x20, 0x20));
    private static readonly IPen SelectionPen = new Pen(new SolidColorBrush(Color.FromRgb(0x8B, 0xC3, 0x4A)), 2);
    private static readonly IPen WindowPen = new Pen(new SolidColorBrush(Color.FromRgb(0x8B, 0xC3, 0x4A)), 2, new DashStyle([4, 3], 0));
    private static readonly IPen CrosshairPen = new Pen(new SolidColorBrush(Color.FromArgb(0xA0, 0xFF, 0xFF, 0xFF)), 1, new DashStyle([2, 2], 0));
    private static readonly IPen ZoomerFramePen = new Pen(Brushes.White, 2);
    private static readonly IPen ZoomerPixelPen = new Pen(Brushes.Red, 1);
    private static readonly Typeface PanelTypeface = new(FontFamily.Default);

    private readonly Bitmap _image;
    private readonly SelectionModel _model;

    public CaptureSurface(Bitmap image, SKRectI screenPixels, SelectionModel model)
    {
        _image = image;
        ScreenPixels = screenPixels;
        _model = model;
        ClipToBounds = true;
        Focusable = true;
    }

    /// <summary>
    /// The pixels of the snapshot this monitor shows
    /// </summary>
    public SKRectI ScreenPixels { get; }

    private double ScaleX => Bounds.Width > 0 ? ScreenPixels.Width / Bounds.Width : 1;

    private double ScaleY => Bounds.Height > 0 ? ScreenPixels.Height / Bounds.Height : 1;

    /// <summary>
    /// The snapshot pixel under a point of this control; points outside give pixels of the neighbour monitors
    /// </summary>
    public SKPointI ToImage(Point point) => new(
        ScreenPixels.Left + (int)Math.Floor(point.X * ScaleX),
        ScreenPixels.Top + (int)Math.Floor(point.Y * ScaleY));

    private Point ToControl(SKPointI pixel) => new((pixel.X - ScreenPixels.Left) / ScaleX, (pixel.Y - ScreenPixels.Top) / ScaleY);

    private Rect ToControl(SKRectI pixels) => new(
        ToControl(new SKPointI(pixels.Left, pixels.Top)),
        ToControl(new SKPointI(pixels.Right, pixels.Bottom)));

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var source = new Rect(ScreenPixels.Left, ScreenPixels.Top, ScreenPixels.Width, ScreenPixels.Height);
        context.DrawImage(_image, source, bounds);

        var highlight = _model.Highlight;
        var hasHighlight = !highlight.IsEmpty && SKRectI.Intersect(highlight, ScreenPixels) is { IsEmpty: false };
        var highlightRect = ToControl(highlight);
        DrawDimming(context, bounds, hasHighlight ? highlightRect : default, _model.IsSelecting ? DimBrush : WindowDimBrush);

        if (hasHighlight)
        {
            context.DrawRectangle(null, _model.IsSelecting || _model.Mode == CaptureMode.Window ? SelectionPen : WindowPen, highlightRect);
        }

        var cursorOnThisScreen = _model.IsCursorKnown && ScreenPixels.Contains(_model.Cursor.X, _model.Cursor.Y);
        var cursor = ToControl(_model.Cursor);
        if (_model.Mode == CaptureMode.Region && cursorOnThisScreen)
        {
            context.DrawLine(CrosshairPen, new Point(0, cursor.Y + 0.5), new Point(bounds.Width, cursor.Y + 0.5));
            context.DrawLine(CrosshairPen, new Point(cursor.X + 0.5, 0), new Point(cursor.X + 0.5, bounds.Height));
        }

        if (hasHighlight)
        {
            DrawSizeLabel(context, bounds, highlightRect, highlight);
        }

        if (cursorOnThisScreen && _model.ShowZoomer && _model.Mode == CaptureMode.Region)
        {
            DrawZoomer(context, bounds, cursor);
        }

        // The panels are always shown on the monitor with the cursor, or on the first one while the cursor is unknown
        if (cursorOnThisScreen || (!_model.IsCursorKnown && ScreenPixels.Contains(_model.Cursor.X, _model.Cursor.Y)))
        {
            DrawPanels(context, bounds, cursor);
        }
    }

    /// <summary>
    /// Darken everything except the area which would be captured
    /// </summary>
    private static void DrawDimming(DrawingContext context, Rect bounds, Rect keep, IBrush brush)
    {
        if (keep.Width <= 0 || keep.Height <= 0)
        {
            context.FillRectangle(brush, bounds);
            return;
        }

        var geometry = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(bounds), new RectangleGeometry(keep));
        context.DrawGeometry(brush, null, geometry);
    }

    private void DrawSizeLabel(DrawingContext context, Rect bounds, Rect area, SKRectI pixels)
    {
        var text = Text(string.Create(CultureInfo.InvariantCulture, $"{pixels.Width} × {pixels.Height}"), 13);
        var origin = new Point(area.Left, area.Top - text.Height - 6);
        if (origin.Y < 0)
        {
            origin = new Point(area.Left + 4, area.Top + 4);
        }

        origin = new Point(Math.Clamp(origin.X, 0, Math.Max(0, bounds.Width - text.Width - 8)), origin.Y);
        context.FillRectangle(PanelBrush, new Rect(origin.X, origin.Y, text.Width + 8, text.Height + 4), 3);
        context.DrawText(text, new Point(origin.X + 4, origin.Y + 2));
    }

    /// <summary>
    /// The magnifier next to the cursor, it moves to the other side near the edges of the screen
    /// </summary>
    private void DrawZoomer(DrawingContext context, Rect bounds, Point cursor)
    {
        double x = cursor.X + ZoomerOffset;
        double y = cursor.Y + ZoomerOffset;
        if (x + ZoomerSize > bounds.Width)
        {
            x = cursor.X - ZoomerOffset - ZoomerSize;
        }

        if (y + ZoomerSize > bounds.Height)
        {
            y = cursor.Y - ZoomerOffset - ZoomerSize;
        }

        var target = new Rect(x, y, ZoomerSize, ZoomerSize);
        int half = ZoomerPixels / 2;
        var source = new Rect(_model.Cursor.X - half, _model.Cursor.Y - half, ZoomerPixels, ZoomerPixels);

        context.FillRectangle(Brushes.Black, target);
        using (context.PushClip(target))
        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
        {
            context.DrawImage(_image, source, target);
        }

        double pixelSize = ZoomerSize / ZoomerPixels;
        context.DrawRectangle(null, ZoomerPixelPen, new Rect(x + half * pixelSize, y + half * pixelSize, pixelSize, pixelSize));
        context.DrawRectangle(null, ZoomerFramePen, target);

        var position = Text(string.Create(CultureInfo.InvariantCulture, $"{_model.Cursor.X}, {_model.Cursor.Y}"), 11);
        var labelY = y + ZoomerSize + 2;
        context.FillRectangle(PanelBrush, new Rect(x, labelY, ZoomerSize, position.Height + 4));
        context.DrawText(position, new Point(x + (ZoomerSize - position.Width) / 2, labelY + 2));
    }

    private void DrawPanels(DrawingContext context, Rect bounds, Point cursor)
    {
        if (_model.ShowHelp)
        {
            var help = Text(HelpText(), 13);
            DrawPanel(context, bounds, cursor, help, alignRight: false);
        }

        if (_model.ShowInfo)
        {
            var info = Text(InfoText(), 13);
            DrawPanel(context, bounds, cursor, info, alignRight: true);
        }

        if (!_model.ShowHelp)
        {
            var hint = Text(_model.Mode == CaptureMode.Window ? "Window – F1 keys" : "Region – F1 keys", 12);
            DrawPanel(context, bounds, cursor, hint, alignRight: false);
        }
    }

    /// <summary>
    /// A panel in a top corner; it moves down when the cursor comes near, so it never hides what is selected
    /// </summary>
    private static void DrawPanel(DrawingContext context, Rect bounds, Point cursor, FormattedText text, bool alignRight)
    {
        const double margin = 16;
        var size = new Size(text.Width + 24, text.Height + 16);
        var x = alignRight ? bounds.Width - size.Width - margin : margin;
        var y = margin;
        var panel = new Rect(new Point(x, y), size);
        if (panel.Inflate(40).Contains(cursor))
        {
            panel = new Rect(new Point(x, bounds.Height - size.Height - margin), size);
        }

        context.FillRectangle(PanelBrush, panel, 6);
        context.DrawText(text, new Point(panel.X + 12, panel.Y + 8));
    }

    private string HelpText()
    {
        var lines = new List<string> { "Esc\tCancel" };
        if (_model.Mode == CaptureMode.Window)
        {
            lines.Add("Enter / click\tCapture the window");
            lines.Add("Tab / Shift+Tab\tNext / previous window");
        }
        else
        {
            lines.Add("Drag\tSelect a region");
            lines.Add("Enter\tStart or finish the selection");
            lines.Add("↑↓←→\tMove one pixel (Ctrl: ten)");
            lines.Add("Z\tShow or hide the zoomer");
            if (_model.CanSelectWindows)
            {
                lines.Add("Click\tCapture the window under the cursor");
            }
        }

        if (_model.CanSelectWindows)
        {
            lines.Add("Space\tSwitch between region and window");
        }

        lines.Add("I\tShow or hide the info");
        lines.Add("F1\tShow or hide the keys");
        return string.Join('\n', lines);
    }

    private string InfoText()
    {
        var lines = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture, $"Screen\t{ScreenPixels.Width} × {ScreenPixels.Height}"),
            string.Create(CultureInfo.InvariantCulture, $"Cursor\t{_model.Cursor.X}, {_model.Cursor.Y}"),
        };
        var highlight = _model.Highlight;
        if (!highlight.IsEmpty)
        {
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"Selection\t{highlight.Width} × {highlight.Height}"));
        }

        if (_model.HoveredWindow is { } window && !string.IsNullOrEmpty(window.Title))
        {
            lines.Add($"Window\t{(window.Title.Length > 40 ? window.Title[..40] + "…" : window.Title)}");
        }

        return string.Join('\n', lines);
    }

    private static FormattedText Text(string text, double size) =>
        new(text.Replace("\t", "    "), CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, PanelTypeface, size, Brushes.White);
}
