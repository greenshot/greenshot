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

using Greenshot.Linux.Editor.Effects;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Rendering;
using SkiaSharp;

namespace Greenshot.Linux.Editor;

/// <summary>
/// Crop, crop out and torn edges: the image changes size and the elements follow, the cut edges are elements
/// (ApplyCrop, ApplyCutOut, AddTornEdges and ApplyElementToImage of the Windows Surface)
/// </summary>
public sealed partial class EditorSurface
{
    /// <summary>
    /// Where an element is, compared to a strip which is cut out
    /// </summary>
    public enum CutOutPlacement
    {
        /// <summary>
        /// Above or left of the cut, it stays
        /// </summary>
        Before,

        /// <summary>
        /// Completely inside the cut, it is removed
        /// </summary>
        Inside,

        /// <summary>
        /// Below or right of the cut, it moves with the image
        /// </summary>
        After,

        /// <summary>
        /// Over an edge of the cut, it stays
        /// </summary>
        Across,
    }

    public static CutOutPlacement GetCutOutPlacement(SKRect bounds, int cutStart, int cutSize, bool horizontal)
    {
        bounds = bounds.Standardized;
        float start = horizontal ? bounds.Top : bounds.Left;
        float end = horizontal ? bounds.Bottom : bounds.Right;
        int cutEnd = cutStart + cutSize;
        if (end <= cutStart)
        {
            return CutOutPlacement.Before;
        }

        if (start >= cutEnd)
        {
            return CutOutPlacement.After;
        }

        return start >= cutStart && end <= cutEnd ? CutOutPlacement.Inside : CutOutPlacement.Across;
    }

    /// <summary>
    /// Crop: keep the area. Elements completely outside are removed, the others move with the image.
    /// With a cut edge style the sides which were cut off get edges in that style, as torn edges element.
    /// </summary>
    /// <param name="settings">The tooth and shadow settings for the edges</param>
    /// <returns>false when the area is empty</returns>
    public bool ApplyCrop(SKRectI area, CutMarkStyle cutMarkStyle = CutMarkStyle.None, TornEdgeSettings? settings = null)
    {
        area.Intersect(new SKRectI(0, 0, Width, Height));
        if (area.Width < 1 || area.Height < 1)
        {
            return false;
        }

        // top, right, bottom, left
        bool[] cutSides = [area.Top > 0, area.Right < Width, area.Bottom < Height, area.Left > 0];
        bool withEdges = cutMarkStyle != CutMarkStyle.None && cutSides.Any(cut => cut);
        var edgeSettings = (settings ?? new TornEdgeSettings()).Clone();
        edgeSettings.Edges = cutSides;
        edgeSettings.Seed = 0;
        int margin = withEdges ? CutElement.GetShadowMargin(edgeSettings) : 0;
        int[] margins = cutSides.Select(cut => withEdges && cut ? margin : 0).ToArray();

        SKBitmap newImage = ImageEffects.Crop(Image, area).Image;
        if (margins.Any(m => m > 0))
        {
            using var cropped = newImage;
            newImage = CutOut.Expand(cropped, edgeSettings.BackgroundColor, margins);
        }

        Checkpoint();
        ClearSelection();
        var removed = new List<DrawableElement>();
        foreach (var element in _elements)
        {
            if (element is TornEdgeElement tornEdges)
            {
                // The sides which were cut off lose their edges, the room for the shadow there belongs to the new edges
                var edges = (bool[])tornEdges.Edges.Clone();
                var tornMargins = (int[])tornEdges.Margins.Clone();
                for (int side = 0; side < 4; side++)
                {
                    if (cutSides[side])
                    {
                        edges[side] = false;
                        tornMargins[side] = margins[side];
                    }
                }

                tornEdges.Edges = edges;
                tornEdges.Margins = tornMargins;
                if (!edges.Any(edge => edge))
                {
                    removed.Add(element);
                }
            }
            else if (IsOutside(element.NormalizedBounds, area))
            {
                removed.Add(element);
            }
        }

        Remove(removed);
        ReplaceImage(newImage, SKMatrix.CreateTranslation(margins[3] - area.Left, margins[0] - area.Top));
        if (withEdges)
        {
            var cropEdges = new TornEdgeElement { Style = new TornEdgeElement().CreateDefaultStyle(), Margins = margins };
            cropEdges.ApplySettings(edgeSettings);
            cropEdges.Style.CutMarkStyle = cutMarkStyle;
            Add(cropEdges, select: false);
        }

        return true;
    }

    /// <summary>
    /// Is an element completely outside of the crop? Lines have no width or height, so the bounds are at least one pixel.
    /// </summary>
    private static bool IsOutside(SKRect bounds, SKRectI area)
    {
        float right = Math.Max(bounds.Right, bounds.Left + 1);
        float bottom = Math.Max(bounds.Bottom, bounds.Top + 1);
        return right <= area.Left || bounds.Left >= area.Right || bottom <= area.Top || bounds.Top >= area.Bottom;
    }

    /// <summary>
    /// Crop out: cut out the rows (horizontal) or columns of the area and join the other parts. Elements after the cut move
    /// with the image, elements inside it are removed, the others stay. With a cut edge style a few rows / columns stay for
    /// the gap of a cut mark, which is placed over the joint.
    /// </summary>
    /// <param name="settings">The tooth and shadow settings for the cut mark</param>
    /// <returns>false when nothing or everything would be cut out</returns>
    public bool ApplyCutOut(SKRectI area, bool horizontal, CutMarkStyle cutMarkStyle = CutMarkStyle.None, TornEdgeSettings? settings = null)
    {
        area.Intersect(new SKRectI(0, 0, Width, Height));
        int cutStart = horizontal ? area.Top : area.Left;
        int cutSize = horizontal ? area.Height : area.Width;
        int imageLength = horizontal ? Height : Width;
        if (area.Width < 1 || area.Height < 1 || cutSize >= imageLength)
        {
            return false;
        }

        // Only a cut in the middle has a joint to mark, the gap of the mark keeps a part of the strip
        CutMarkElement? cutMark = null;
        if (cutMarkStyle != CutMarkStyle.None && cutStart > 0 && cutStart + cutSize < imageLength)
        {
            var markSettings = (settings ?? new TornEdgeSettings()).Clone();
            markSettings.Seed = 0;
            cutMark = new CutMarkElement { Horizontal = horizontal };
            cutMark.Style = cutMark.CreateDefaultStyle();
            cutMark.ApplySettings(markSettings);
            cutMark.Style.CutMarkStyle = cutMarkStyle;
        }

        int gap = cutMark != null ? Math.Min(cutSize, Math.Max(2, cutMark.ToothHeight)) : 0;
        var newImage = CutOut.CutStrip(Image, cutStart + gap, cutSize - gap, horizontal);

        Checkpoint();
        ClearSelection();
        int moveBy = cutSize - gap;
        var move = horizontal ? SKMatrix.CreateTranslation(0, -moveBy) : SKMatrix.CreateTranslation(-moveBy, 0);
        var removed = new List<DrawableElement>();
        foreach (var element in _elements)
        {
            switch (GetCutOutPlacement(element.NormalizedBounds, cutStart, cutSize, horizontal))
            {
                case CutOutPlacement.After:
                    element.Transform(move);
                    break;
                case CutOutPlacement.Inside:
                    removed.Add(element);
                    break;
            }
        }

        Remove(removed);
        ReplaceImage(newImage, SKMatrix.Identity);
        if (cutMark != null)
        {
            int bandSize = CutMarkElement.GetBandSize(cutMark.ToothHeight, gap);
            int bandStart = cutStart + gap / 2 - bandSize / 2;
            cutMark.Bounds = horizontal ? SKRect.Create(0, bandStart, Width, bandSize) : SKRect.Create(bandStart, 0, bandSize, Height);
            Add(cutMark, select: false);
        }

        return true;
    }

    /// <summary>
    /// Tear the edges of the image, as an element which can be changed later; the canvas grows for the shadow.
    /// When there are torn edges already, these are selected.
    /// </summary>
    public TornEdgeElement AddTornEdges(TornEdgeSettings settings)
    {
        if (_elements.OfType<TornEdgeElement>().FirstOrDefault() is { } existing)
        {
            Select(existing);
            return existing;
        }

        var edgeSettings = settings.Clone();
        edgeSettings.Seed = 0;
        int margin = CutElement.GetShadowMargin(edgeSettings);
        Checkpoint();
        if (margin > 0)
        {
            // Room for the shadow like the torn edge effect makes, it is part of the torn off area
            ReplaceImage(CutOut.Expand(Image, edgeSettings.BackgroundColor, [margin, margin, margin, margin]), SKMatrix.CreateTranslation(margin, margin));
        }

        var tornEdges = new TornEdgeElement { Margins = [margin, margin, margin, margin] };
        tornEdges.Style = tornEdges.CreateDefaultStyle();
        tornEdges.ApplySettings(edgeSettings);
        Add(tornEdges);
        return tornEdges;
    }

    /// <summary>
    /// Draw an element into the image and remove it, e.g. to keep cut edges before drawing on
    /// </summary>
    public bool ApplyElementToImage(DrawableElement element)
    {
        if (!_elements.Contains(element))
        {
            return false;
        }

        var newImage = ImageEffects.NewBitmap(Width, Height);
        using (var canvas = new SKCanvas(newImage))
        {
            canvas.DrawBitmap(Image, 0, 0);
            element.Draw(new RenderContext(canvas, newImage, RenderMode.Export, StepNumber, _elements));
            canvas.Flush();
        }

        Checkpoint();
        Remove([element]);
        ReplaceImage(newImage, SKMatrix.Identity);
        return true;
    }
}
