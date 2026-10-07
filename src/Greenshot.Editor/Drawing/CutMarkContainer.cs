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
using Greenshot.Editor.Drawing.Adorners;
using Greenshot.Editor.Drawing.Fields;
using Greenshot.Editor.Helpers;
using Greenshot.Editor.Views;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Marks the place where a strip was cut out of the image (crop out horizontally / vertically).
    /// It always spans the whole image: the full width for a horizontal cut, the full height for a vertical one,
    /// it can only be moved and sized across the cut.
    /// Between the two edges (straight, zig-zag, wavy or torn, like the torn edge effect) is a gap,
    /// transparent or filled with the fill color, with the shadow of the torn edge effect and optionally a line along the edges.
    /// The edges come from a stored seed, so they never change unless a new random edge is asked for.
    /// </summary>
    [Serializable]
    public sealed class CutMarkContainer : DrawableContainer
    {
        private readonly bool _horizontal;
        private int _seed;
        private float _shadowDarkness;
        private int _shadowSize;
        private int _shadowOffsetX;
        private int _shadowOffsetY;

        // The tooth height the band was sized for, when it changes the band grows or shrinks so the gap keeps its size
        [NonSerialized] private int _bandToothHeight;

        // The shadow only changes with the settings, so it is kept
        [NonSerialized] private Bitmap _shadowCache;
        [NonSerialized] private string _shadowCacheKey;

        /// <summary>
        /// Create a cut mark, the tooth sizes are the last used ones
        /// </summary>
        /// <param name="parent">ISurface</param>
        /// <param name="horizontal">true for a horizontal cut (full width), false for a vertical cut (full height)</param>
        /// <param name="shadowSettings">TornEdgeEffect with the shadow settings to start with</param>
        public CutMarkContainer(ISurface parent, bool horizontal, TornEdgeEffect shadowSettings) : base(parent)
        {
            _horizontal = horizontal;
            ApplyShadowSettings(shadowSettings ?? new TornEdgeEffect());
            NewSeed();
            Init();
        }

        protected override void OnDeserialized(StreamingContext streamingContext)
        {
            base.OnDeserialized(streamingContext);
            Init();
        }

        private void Init()
        {
            if (_horizontal)
            {
                Adorners.Add(new ResizeAdorner(this, Positions.TopCenter));
                Adorners.Add(new ResizeAdorner(this, Positions.BottomCenter));
            }
            else
            {
                Adorners.Add(new ResizeAdorner(this, Positions.MiddleLeft));
                Adorners.Add(new ResizeAdorner(this, Positions.MiddleRight));
            }

            _bandToothHeight = ToothHeight;
            FieldChanged += OnOwnFieldChanged;
        }

        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.LINE_THICKNESS, 0);
            AddField(GetType(), FieldType.LINE_COLOR, Color.DimGray);
            AddField(GetType(), FieldType.FILL_COLOR, Color.Transparent);
            AddField(GetType(), FieldType.SHADOW, true);
            AddField(GetType(), FieldType.CUT_MARK_STYLE, CutMarkStyle.Torn);
            AddField(GetType(), FieldType.TOOTH_HEIGHT, 12);
            AddField(GetType(), FieldType.TOOTH_RANGE, 20);
        }

        /// <summary>
        /// true for a horizontal cut, the mark spans the full width
        /// </summary>
        public bool IsHorizontal => _horizontal;

        /// <summary>
        /// The seed for the random parts of the edges
        /// </summary>
        public int Seed => _seed;

        /// <summary>
        /// How deep the edges go into the image parts
        /// </summary>
        public int ToothHeight => Math.Max(1, GetFieldValueAsInt(FieldType.TOOTH_HEIGHT));

        /// <summary>
        /// How wide a tooth is
        /// </summary>
        public int ToothRange => Math.Max(2, GetFieldValueAsInt(FieldType.TOOTH_RANGE));

        /// <summary>
        /// The gap between the edges is transparent, this needs an image with an alpha channel on export
        /// </summary>
        public bool HasTransparentGap => !Colors.IsVisible(GetFieldValueAsColor(FieldType.FILL_COLOR, Color.Transparent));

        /// <summary>
        /// The size of the band for a gap, two teeth and the gap
        /// </summary>
        public static int GetBandSize(int toothHeight, int gap) => 2 * Math.Max(1, toothHeight) + gap;

        /// <summary>
        /// Use the tooth sizes of the settings
        /// </summary>
        public void SetToothSize(int toothHeight, int toothRange)
        {
            SetFieldValue(FieldType.TOOTH_HEIGHT, Math.Max(1, toothHeight));
            SetFieldValue(FieldType.TOOTH_RANGE, Math.Max(2, toothRange));
        }

        /// <summary>
        /// Take the shadow from the settings
        /// </summary>
        public void ApplyShadowSettings(TornEdgeEffect settings)
        {
            _shadowDarkness = settings.Darkness;
            _shadowSize = Math.Max(1, settings.ShadowSize);
            _shadowOffsetX = settings.ShadowOffset.X;
            _shadowOffsetY = settings.ShadowOffset.Y;
            ClearShadowCache();
            Invalidate();
        }

        /// <summary>
        /// The tooth sizes and the shadow as torn edge effect settings, e.g. for the settings window
        /// </summary>
        public TornEdgeEffect GetSettings()
        {
            return new TornEdgeEffect
            {
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
            SetSeed(CutOutHelper.NewSeed(_seed));
        }

        /// <summary>
        /// Use the given seed for the edges
        /// </summary>
        public void SetSeed(int seed)
        {
            _seed = seed;
            ClearShadowCache();
            Invalidate();
        }

        /// <summary>
        /// Keep the gap the same size when the teeth change: the band grows or shrinks on both sides
        /// </summary>
        private void OnOwnFieldChanged(object sender, FieldChangedEventArgs e)
        {
            if (!Equals(e.Field.FieldType, FieldType.TOOTH_HEIGHT))
            {
                ClearShadowCache();
                return;
            }

            int toothHeight = ToothHeight;
            int delta = toothHeight - _bandToothHeight;
            _bandToothHeight = toothHeight;
            if (delta == 0)
            {
                return;
            }

            Invalidate();
            var band = new NativeRect(Left, Top, Width, Height).Normalize();
            if (_horizontal)
            {
                Top = band.Top - delta;
                Height = Math.Max(2 * toothHeight, band.Height + 2 * delta);
            }
            else
            {
                Left = band.Left - delta;
                Width = Math.Max(2 * toothHeight, band.Width + 2 * delta);
            }

            ClearShadowCache();
            Invalidate();
        }

        /// <summary>
        /// The mark always spans the whole image along the cut
        /// </summary>
        private void SpanImage()
        {
            if (_parent?.Image is not { } image)
            {
                return;
            }

            if (_horizontal)
            {
                Left = 0;
                Width = image.Width;
            }
            else
            {
                Top = 0;
                Height = image.Height;
            }
        }

        public override void MoveBy(int dx, int dy)
        {
            base.MoveBy(_horizontal ? 0 : dx, _horizontal ? dy : 0);
            SpanImage();
        }

        public override void ApplyBounds(NativeRectFloat newBounds)
        {
            base.ApplyBounds(newBounds);
            SpanImage();
        }

        public override void Transform(Matrix matrix)
        {
            base.Transform(matrix);
            SpanImage();
        }

        /// <summary>
        /// The rectangle of the band, over the whole image
        /// </summary>
        private NativeRect GetBand()
        {
            var rect = new NativeRect(Left, Top, Width, Height).Normalize();
            if (_parent?.Image is { } image)
            {
                rect = _horizontal ? new NativeRect(0, rect.Top, image.Width, rect.Height) : new NativeRect(rect.Left, 0, rect.Width, image.Height);
            }

            return rect;
        }

        /// <summary>
        /// The two edges in image coordinates: the first one goes into the part before the cut, the second one into the part after it
        /// </summary>
        private void CreateEdges(NativeRect band, CutMarkStyle style, out PointF[] firstEdge, out PointF[] secondEdge)
        {
            int length = _horizontal ? band.Width : band.Height;
            int thickness = _horizontal ? band.Height : band.Width;
            int toothHeight = Math.Min(ToothHeight, thickness / 2);
            var random = new Random(_seed);
            var first = CutOutHelper.CreateEdge(style, length, toothHeight, ToothRange, random);
            var second = CutOutHelper.CreateEdge(style, length, toothHeight, ToothRange, random);

            // along the cut, across the cut
            PointF Map(float along, float across) => _horizontal ? new PointF(band.Left + along, band.Top + across) : new PointF(band.Left + across, band.Top + along);
            firstEdge = first.Select(p => Map(p.X, toothHeight - p.Y)).ToArray();
            secondEdge = second.Select(p => Map(p.X, thickness - toothHeight + p.Y)).ToArray();
        }

        /// <summary>
        /// The outline of the gap between the two edges
        /// </summary>
        private static GraphicsPath CreateGapPath(PointF[] firstEdge, PointF[] secondEdge)
        {
            var path = new GraphicsPath();
            path.AddLines(firstEdge);
            path.AddLines(Enumerable.Reverse(secondEdge).ToArray());
            path.CloseFigure();
            return path;
        }

        public override void Draw(Graphics graphics, RenderMode rm)
        {
            if (GetFieldValue(FieldType.CUT_MARK_STYLE) is not CutMarkStyle style || style == CutMarkStyle.None)
            {
                return;
            }

            var band = GetBand();
            if (band.Width <= 0 || band.Height <= 0)
            {
                return;
            }

            Color fillColor = GetFieldValueAsColor(FieldType.FILL_COLOR, Color.Transparent);
            CreateEdges(band, style, out var firstEdge, out var secondEdge);
            using var gapPath = CreateGapPath(firstEdge, secondEdge);

            var state = graphics.Save();
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            if (Colors.IsVisible(fillColor))
            {
                using var brush = new SolidBrush(fillColor);
                graphics.FillPath(brush, gapPath);
            }
            else if (rm == RenderMode.EXPORT)
            {
                // Really transparent, the image is 32 bit when exporting (see Surface.GetImage)
                graphics.CompositingMode = CompositingMode.SourceCopy;
                using var clear = new SolidBrush(Color.Transparent);
                graphics.FillPath(clear, gapPath);
                graphics.CompositingMode = CompositingMode.SourceOver;
            }
            else if (InternalParent?.TransparencyBackgroundBrush is TextureBrush checkerBoard)
            {
                // Show the transparency like the rest of the editor, the squares don't zoom
                using var brush = (TextureBrush)checkerBoard.Clone();
                var elements = graphics.Transform.Elements;
                if (elements[0] != 0 && elements[3] != 0)
                {
                    brush.ScaleTransform(1 / elements[0], 1 / elements[3]);
                }

                graphics.FillPath(brush, gapPath);
            }
            else
            {
                graphics.FillPath(Brushes.White, gapPath);
            }

            if (GetFieldValueAsBool(FieldType.SHADOW))
            {
                DrawShadow(graphics);
            }

            int lineThickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
            Color lineColor = GetFieldValueAsColor(FieldType.LINE_COLOR, Color.DimGray);
            if (lineThickness > 0 && Colors.IsVisible(lineColor))
            {
                // The edges are part of the image, where another element cut the image away they don't show
                CutShadow.ExcludeOtherCuts(graphics, this, CutShadow.GetCuts(InternalParent, this));
                using var pen = new Pen(lineColor, lineThickness)
                {
                    LineJoin = LineJoin.Round
                };
                graphics.DrawLines(pen, firstEdge);
                graphics.DrawLines(pen, secondEdge);
            }

            graphics.Restore(state);
        }

        /// <summary>
        /// Describes the shape, other elements use it to know when their shadow changes
        /// </summary>
        internal string ShapeKey => $"{GetBand()}|{GetFieldValue(FieldType.CUT_MARK_STYLE)}|{_seed}|{ToothHeight}|{ToothRange}";

        /// <summary>
        /// The gap in image coordinates, this is cut out of the image
        /// </summary>
        /// <returns>GraphicsPath or null when nothing is cut out</returns>
        internal GraphicsPath CreateCutPath()
        {
            if (GetFieldValue(FieldType.CUT_MARK_STYLE) is not CutMarkStyle style || style == CutMarkStyle.None)
            {
                return null;
            }

            var band = GetBand();
            if (band.Width <= 0 || band.Height <= 0)
            {
                return null;
            }

            CreateEdges(band, style, out var firstEdge, out var secondEdge);
            return CreateGapPath(firstEdge, secondEdge);
        }

        /// <summary>
        /// The shadow of the torn edge effect, cast by what is left of the image into the gap
        /// </summary>
        private void DrawShadow(Graphics graphics)
        {
            var cuts = CutShadow.GetCuts(InternalParent, this);
            var imageSize = InternalParent?.Image?.Size ?? new Size(Width, Height);
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

        public override bool ClickableAt(int x, int y)
        {
            return GetBand().Contains(x, y);
        }

        public override void AddContextMenuItems(ContextMenuStrip menu, ISurface surface, MouseEventArgs mouseEventArgs)
        {
            // The surface asks every element, only the selected cut mark adds its items
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

            var settingsItem = new ToolStripMenuItem(Texts.Editor.CutMarkSettings);
            settingsItem.Click += (_, _) =>
            {
                var settings = GetSettings();
                var window = new TornEdgeSettingsWindow(settings, false);
                if (window.ShowDialog(surface as IWin32Window) != true)
                {
                    return;
                }

                ApplyShadowSettings(settings);
                SetToothSize(settings.ToothHeight, _horizontal ? settings.HorizontalToothRange : settings.VerticalToothRange);
                SetSeed(settings.Seed);
                SetFieldValue(FieldType.SHADOW, settings.GenerateShadow);
                SetFieldValue(FieldType.FILL_COLOR, settings.BackgroundColor);
                surface.Invalidate();
            };
            menu.Items.Add(settingsItem);

            var applyItem = new ToolStripMenuItem(Texts.Editor.ApplyToImage);
            applyItem.Click += (_, _) => InternalParent?.ApplyElementToImage(this);
            menu.Items.Add(applyItem);
        }
    }
}
