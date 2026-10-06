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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Languages;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Helpers;
using Greenshot.Editor.Views;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// The torn edge effect as an element: it tears the edges of the whole capture, which can be changed or removed later.
    /// Outside of the torn edges is transparent or the fill color, with the shadow of the torn edge effect.
    /// The image keeps its size, the shadow falls into the torn off parts.
    /// The edges come from a stored seed, so they never change unless new random edges are asked for.
    /// </summary>
    [Serializable]
    public sealed class TornEdgeContainer : DrawableContainer
    {
        private int _seed;
        private bool[] _edges = { true, true, true, true };
        // Room around the torn edges for the shadow, the canvas was made bigger by this on every side
        private int _margin;
        private float _shadowDarkness;
        private int _shadowSize;
        private int _shadowOffsetX;
        private int _shadowOffsetY;

        // The shadow only changes with the settings, so it is kept
        [NonSerialized] private Bitmap _shadowCache;
        [NonSerialized] private string _shadowCacheKey;

        /// <summary>
        /// Create torn edges for the whole image
        /// </summary>
        /// <param name="parent">ISurface</param>
        /// <param name="settings">TornEdgeEffect with the settings to start with</param>
        /// <param name="margin">int room on every side of the image for the shadow</param>
        public TornEdgeContainer(ISurface parent, TornEdgeEffect settings, int margin = 0) : base(parent)
        {
            _margin = Math.Max(0, margin);
            settings ??= new TornEdgeEffect();
            ApplySettings(settings);
            NewSeed();
            SpanImage();
            Init();
        }

        protected override void OnDeserialized(StreamingContext streamingContext)
        {
            base.OnDeserialized(streamingContext);
            Init();
        }

        private void Init()
        {
            // No adorners: it always covers the whole image
            FieldChanged += OnOwnFieldChanged;
        }

        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.LINE_THICKNESS, 0);
            AddField(GetType(), FieldType.LINE_COLOR, Color.DimGray);
            AddField(GetType(), FieldType.FILL_COLOR, Color.Transparent);
            AddField(GetType(), FieldType.SHADOW, true);
            AddField(GetType(), FieldType.TOOTH_HEIGHT, 12);
            AddField(GetType(), FieldType.TOOTH_RANGE, 20);
        }

        /// <summary>
        /// The seed for the random edges
        /// </summary>
        public int Seed => _seed;

        /// <summary>
        /// The room on every side for the shadow
        /// </summary>
        public int Margin => _margin;

        /// <summary>
        /// The room needed on every side for the shadow of the settings
        /// </summary>
        public static int GetShadowMargin(TornEdgeEffect settings) =>
            settings.GenerateShadow ? Math.Max(1, settings.ShadowSize) + Math.Max(Math.Abs(settings.ShadowOffset.X), Math.Abs(settings.ShadowOffset.Y)) : 0;

        /// <summary>
        /// Which edges are torn: top, right, bottom, left
        /// </summary>
        public bool[] Edges => (bool[])_edges.Clone();

        /// <summary>
        /// How deep the edges go into the image
        /// </summary>
        public int ToothHeight => Math.Max(1, GetFieldValueAsInt(FieldType.TOOTH_HEIGHT));

        /// <summary>
        /// How wide a tooth is
        /// </summary>
        public int ToothRange => Math.Max(2, GetFieldValueAsInt(FieldType.TOOTH_RANGE));

        /// <summary>
        /// The torn off parts are transparent, this needs an image with an alpha channel on export
        /// </summary>
        public bool HasTransparentEdge => GetFieldValueAsColor(FieldType.FILL_COLOR, Color.Transparent).A < 255;

        /// <summary>
        /// Take the settings of the torn edge effect
        /// </summary>
        public void ApplySettings(TornEdgeEffect settings)
        {
            _edges = settings.Edges is { Length: 4 } edges ? (bool[])edges.Clone() : new[] { true, true, true, true };
            _shadowDarkness = settings.Darkness;
            _shadowSize = Math.Max(1, settings.ShadowSize);
            _shadowOffsetX = settings.ShadowOffset.X;
            _shadowOffsetY = settings.ShadowOffset.Y;
            SetFieldValue(FieldType.TOOTH_HEIGHT, Math.Max(1, settings.ToothHeight));
            SetFieldValue(FieldType.TOOTH_RANGE, Math.Max(2, settings.HorizontalToothRange));
            SetFieldValue(FieldType.SHADOW, settings.GenerateShadow);
            SetFieldValue(FieldType.FILL_COLOR, settings.BackgroundColor);
            ClearShadowCache();
            Invalidate();
        }

        /// <summary>
        /// The settings as torn edge effect, e.g. for the settings window
        /// </summary>
        public TornEdgeEffect GetSettings()
        {
            return new TornEdgeEffect
            {
                Edges = Edges,
                ToothHeight = ToothHeight,
                HorizontalToothRange = ToothRange,
                VerticalToothRange = ToothRange,
                Darkness = _shadowDarkness,
                ShadowSize = _shadowSize,
                ShadowOffset = new NativePoint(_shadowOffsetX, _shadowOffsetY),
                GenerateShadow = GetFieldValueAsBool(FieldType.SHADOW),
                BackgroundColor = GetFieldValueAsColor(FieldType.FILL_COLOR, Color.Transparent),
                Seed = _seed
            };
        }

        /// <summary>
        /// Make new random edges
        /// </summary>
        public void NewSeed()
        {
            _seed = CutOutHelper.NewSeed(_seed);
            ClearShadowCache();
            Invalidate();
        }

        private void OnOwnFieldChanged(object sender, FieldChangedEventArgs e)
        {
            ClearShadowCache();
        }

        /// <summary>
        /// It always covers the whole image
        /// </summary>
        private void SpanImage()
        {
            if (_parent?.Image is not { } image)
            {
                return;
            }

            Left = 0;
            Top = 0;
            Width = image.Width;
            Height = image.Height;
        }

        private NativeRect ImageBounds => _parent?.Image is { } image ? new NativeRect(0, 0, image.Width, image.Height) : new NativeRect(Left, Top, Width, Height).Normalize();

        public override NativeRect DrawingBounds => ImageBounds;

        public override void MoveBy(int dx, int dy)
        {
            SpanImage();
        }

        public override void ApplyBounds(NativeRectFloat newBounds)
        {
            SpanImage();
        }

        public override void Transform(Matrix matrix)
        {
            SpanImage();
        }

        /// <summary>
        /// The torn edges, clockwise from the top left. Each side which isn't torn is a straight line along the image.
        /// </summary>
        /// <param name="size">Size of the image</param>
        /// <param name="tornSides">the points of every torn side, e.g. to draw a line</param>
        /// <returns>PointF array with the outline of the part which stays</returns>
        private PointF[] CreateOutline(Size size, out List<PointF[]> tornSides)
        {
            int width = size.Width;
            int height = size.Height;
            int toothHeight = Math.Min(ToothHeight, Math.Min(width, height) / 3);
            var random = new Random(_seed);
            var outline = new List<PointF>();
            // A local, the out parameter can't be used in the local function
            var sides = new List<PointF[]>();
            tornSides = sides;
            bool top = _edges[0], right = _edges[1], bottom = _edges[2], left = _edges[3];

            void AddSide(bool torn, bool tornBefore, bool tornAfter, int length, PointF cornerStart, PointF cornerEnd, Func<float, float, PointF> map)
            {
                if (!torn)
                {
                    outline.Add(cornerStart);
                    outline.Add(cornerEnd);
                    return;
                }

                // Leave the corner to the torn side next to it
                int insetStart = tornBefore ? toothHeight : 0;
                int insetEnd = tornAfter ? toothHeight : 0;
                int sideLength = Math.Max(1, length - insetStart - insetEnd);
                var side = CutOutHelper.CreateEdge(CutMarkStyle.Torn, sideLength, toothHeight, ToothRange, random)
                    .Select(p => map(insetStart + p.X, p.Y)).ToArray();
                outline.AddRange(side);
                sides.Add(side);
            }

            AddSide(top, left, right, width, new PointF(0, 0), new PointF(width, 0), (along, depth) => new PointF(along, depth));
            AddSide(right, top, bottom, height, new PointF(width, 0), new PointF(width, height), (along, depth) => new PointF(width - depth, along));
            AddSide(bottom, right, left, width, new PointF(width, height), new PointF(0, height), (along, depth) => new PointF(width - along, height - depth));
            AddSide(left, bottom, top, height, new PointF(0, height), new PointF(0, 0), (along, depth) => new PointF(depth, height - along));
            if (_margin > 0)
            {
                // The torn edges are inside the room for the shadow
                PointF Shift(PointF p) => new PointF(p.X + _margin, p.Y + _margin);
                for (int i = 0; i < sides.Count; i++)
                {
                    sides[i] = sides[i].Select(Shift).ToArray();
                }

                return outline.Select(Shift).ToArray();
            }

            return outline.ToArray();
        }

        public override void Draw(Graphics graphics, RenderMode rm)
        {
            var bounds = ImageBounds;
            if (bounds.Width <= 2 || bounds.Height <= 2 || !_edges.Any(edge => edge))
            {
                return;
            }

            if (bounds.Width - 2 * _margin <= 2 || bounds.Height - 2 * _margin <= 2)
            {
                return;
            }

            var outline = CreateOutline(new Size(bounds.Width - 2 * _margin, bounds.Height - 2 * _margin), out var tornSides);
            using var keptPath = new GraphicsPath();
            keptPath.AddPolygon(outline);
            using var tornOff = new Region(new Rectangle(0, 0, bounds.Width, bounds.Height));
            tornOff.Exclude(keptPath);

            var state = graphics.Save();
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Color fillColor = GetFieldValueAsColor(FieldType.FILL_COLOR, Color.Transparent);
            if (rm == RenderMode.EXPORT)
            {
                // Replace the pixels, the image is 32 bit when the color is (partly) transparent (see Surface.GetImage)
                graphics.CompositingMode = CompositingMode.SourceCopy;
                using var brush = new SolidBrush(fillColor.A == 0 ? Color.Transparent : fillColor);
                graphics.FillRegion(brush, tornOff);
                graphics.CompositingMode = CompositingMode.SourceOver;
            }
            else
            {
                if (fillColor.A < 255 && InternalParent?.TransparencyBackgroundBrush is TextureBrush checkerBoard)
                {
                    // Show the transparency like the rest of the editor, the squares don't zoom
                    using var checkerBrush = (TextureBrush)checkerBoard.Clone();
                    var elements = graphics.Transform.Elements;
                    if (elements[0] != 0 && elements[3] != 0)
                    {
                        checkerBrush.ScaleTransform(1 / elements[0], 1 / elements[3]);
                    }

                    graphics.FillRegion(checkerBrush, tornOff);
                }

                if (fillColor.A > 0)
                {
                    using var brush = new SolidBrush(fillColor);
                    graphics.FillRegion(brush, tornOff);
                }
            }

            if (GetFieldValueAsBool(FieldType.SHADOW))
            {
                DrawShadow(graphics);
            }

            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
            Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR, Color.DimGray);
            if (lineThickness > 0 && Colors.IsVisible(lineColor))
            {
                using var pen = new Pen(lineColor, lineThickness)
                {
                    LineJoin = LineJoin.Round
                };
                foreach (var side in tornSides)
                {
                    graphics.DrawLines(pen, side);
                }
            }

            graphics.Restore(state);
        }

        /// <summary>
        /// Describes the shape, other elements use it to know when their shadow changes
        /// </summary>
        internal string ShapeKey => $"{ImageBounds.Width}x{ImageBounds.Height}|{_margin}|{_seed}|{string.Join(",", _edges)}|{ToothHeight}|{ToothRange}";

        /// <summary>
        /// The torn off parts in image coordinates, these are cut out of the image
        /// </summary>
        /// <returns>GraphicsPath or null when nothing is torn</returns>
        internal GraphicsPath CreateCutPath()
        {
            var bounds = ImageBounds;
            if (bounds.Width - 2 * _margin <= 2 || bounds.Height - 2 * _margin <= 2 || !_edges.Any(edge => edge))
            {
                return null;
            }

            // The image with the part which stays as hole
            var path = new GraphicsPath(FillMode.Alternate);
            path.AddRectangle(new Rectangle(0, 0, bounds.Width, bounds.Height));
            path.AddPolygon(CreateOutline(new Size(bounds.Width - 2 * _margin, bounds.Height - 2 * _margin), out _));
            return path;
        }

        /// <summary>
        /// The shadow of the torn edge effect, cast by what is left of the image into the torn off parts
        /// </summary>
        private void DrawShadow(Graphics graphics)
        {
            var cuts = CutShadow.GetCuts(InternalParent, this);
            var imageSize = new Size(ImageBounds.Width, ImageBounds.Height);
            string key = $"{imageSize}|{_shadowDarkness}|{_shadowSize}|{_shadowOffsetX},{_shadowOffsetY}|{CutShadow.GetKey(cuts)}";
            if (_shadowCache == null || _shadowCacheKey != key)
            {
                ClearShadowCache();
                _shadowCache = CutShadow.CreateShadow(imageSize, cuts, _shadowDarkness, _shadowSize, new NativePoint(_shadowOffsetX, _shadowOffsetY));
                _shadowCacheKey = key;
            }

            CutShadow.Draw(graphics, _shadowCache, this, cuts, _shadowSize, new NativePoint(_shadowOffsetX, _shadowOffsetY));
        }

        private void ClearShadowCache()
        {
            _shadowCache?.Dispose();
            _shadowCache = null;
            _shadowCacheKey = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                FieldChanged -= OnOwnFieldChanged;
                ClearShadowCache();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Only the torn edges can be clicked, not the rest of the image
        /// </summary>
        public override bool ClickableAt(int x, int y)
        {
            var bounds = ImageBounds;
            if (!bounds.Contains(x, y))
            {
                return false;
            }

            int reach = _margin + ToothHeight + 4;
            return (_edges[0] && y < reach) || (_edges[1] && x >= bounds.Width - reach) ||
                   (_edges[2] && y >= bounds.Height - reach) || (_edges[3] && x < reach);
        }

        public override void AddContextMenuItems(ContextMenuStrip menu, ISurface surface, MouseEventArgs mouseEventArgs)
        {
            // The surface asks every element, only the selected torn edges add their items
            if (!Selected)
            {
                return;
            }

            var reseedItem = new ToolStripMenuItem(Texts.Editor.CutMarkReseed);
            reseedItem.Click += (_, _) =>
            {
                NewSeed();
                surface.Invalidate();
            };
            menu.Items.Add(reseedItem);

            var settingsItem = new ToolStripMenuItem(Texts.Editor.TornedgeSettings);
            settingsItem.Click += (_, _) =>
            {
                var settings = GetSettings();
                var window = new TornEdgeSettingsWindow(settings);
                if (window.ShowDialog(surface as IWin32Window) != true)
                {
                    return;
                }

                ApplySettings(settings);
                surface.Invalidate();
            };
            menu.Items.Add(settingsItem);

            var applyItem = new ToolStripMenuItem(Texts.Editor.ApplyToImage);
            applyItem.Click += (_, _) => InternalParent?.ApplyElementToImage(this);
            menu.Items.Add(applyItem);
        }
    }
}
