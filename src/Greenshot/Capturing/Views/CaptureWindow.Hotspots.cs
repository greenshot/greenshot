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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Languages;
using Point = System.Windows.Point;

namespace Greenshot.Capturing.Views
{
    /// <summary>
    /// The numbered hotspots of the active tool (CaptureFormHotspot.IsNumbered, e.g. found links in the text tool): a box and a number
    /// badge on the capture, a list of them in a panel which takes the mouse, the digit keys 1 to 9 and 0 for the first ten.
    /// A click on a badge or a row shows the menu of the hotspot, Ctrl+click runs its default action (the first one).
    /// </summary>
    public partial class CaptureWindow
    {
        // The digit keys pick the first ten: 1 to 9, then 0
        private const int NumberKeyCount = 10;
        // Rows of the list which are shown without scrolling
        private const int VisibleListRows = 10;

        private readonly HotspotNumbering _hotspotNumbering = new HotspotNumbering();
        private IList<(CaptureFormHotspot Hotspot, int Number)> _numberedHotspots = new List<(CaptureFormHotspot Hotspot, int Number)>();
        // The list of the numbered hotspots is a panel of its own
        private readonly object _hotspotListOwner = new object();
        private ScrollViewer _hotspotList;
        // The digit keys while the active tool shows numbered hotspots
        private readonly List<CaptureKeyBinding> _hotspotKeys = new List<CaptureKeyBinding>();
        private StackPanel _hotspotRows;

        /// <summary>
        /// A hotspot of every tool, or of the active one
        /// </summary>
        private bool IsHotspotActive(CaptureFormHotspot hotspot) => hotspot.ToolId == null || hotspot.ToolId == _activeTool?.Id;

        private bool IsMouseOverHotspotList => _hotspotList != null && _panels.ContainsKey(_hotspotListOwner) && _hotspotList.IsMouseOver;

        /// <summary>
        /// The numbers of the hotspots of the active tool and their list, after the hotspots or the tool changed
        /// </summary>
        private void UpdateNumberedHotspots()
        {
            _numberedHotspots = _hotspotNumbering.Number(_hotspots, _activeTool?.Id);
            UpdateHotspotKeys();
            UpdateHotspotList();
        }

        /// <summary>
        /// The digit keys for the numbered hotspots: registered for the active tool while it shows numbered hotspots, removed when they are gone
        /// or another tool is active. A tool which uses a digit itself (the fixed size picks a size with them) picks its hotspots with the mouse.
        /// Each digit only works, and is only in the key help, while that number is there.
        /// </summary>
        private void UpdateHotspotKeys()
        {
            var tool = _numberedHotspots.Count > 0 ? _activeTool : null;
            if (tool != null && _hotspotKeys.Count > 0 && _hotspotKeys[0].Tool == tool)
            {
                return;
            }
            foreach (var binding in _hotspotKeys)
            {
                _keys.Unregister(binding);
            }
            _hotspotKeys.Clear();
            if (tool == null)
            {
                return;
            }
            var bindings = new List<CaptureKeyBinding>();
            for (int index = 0; index < NumberKeyCount; index++)
            {
                int number = index + 1;
                var key = index < 9 ? Key.D1 + index : Key.D0;
                bool HasNumber(ICaptureTool activeTool) => _numberedHotspots.Any(entry => entry.Number == number);
                bindings.Add(new CaptureKeyBinding(this, tool, key, ModifierKeys.None, () => Texts.Core.CaptureKeyHotspotMenu,
                    () => ShowNumberedHotspotMenu(number), HasNumber));
                bindings.Add(new CaptureKeyBinding(this, tool, key, ModifierKeys.Control, () => Texts.Core.CaptureKeyHotspotDefault,
                    () => RunNumberedHotspotDefault(number), HasNumber));
            }
            if (bindings.Any(binding => _keys.Bindings.Any(binding.ConflictsWith)))
            {
                return;
            }
            foreach (var binding in bindings)
            {
                _hotspotKeys.Add(_keys.Register(binding));
            }
        }

        private void ShowNumberedHotspotMenu(int number)
        {
            foreach (var (hotspot, hotspotNumber) in _numberedHotspots)
            {
                if (hotspotNumber == number)
                {
                    ShowHotspotMenu(hotspot, Root, BadgeBounds(hotspot, number));
                    return;
                }
            }
        }

        private void RunNumberedHotspotDefault(int number)
        {
            var hotspot = _numberedHotspots.FirstOrDefault(entry => entry.Number == number).Hotspot;
            if (hotspot?.Actions.Count > 0)
            {
                RunHotspotAction(hotspot.Actions[0]);
            }
        }

        /// <summary>
        /// The numbered hotspot whose badge is at the location, the last drawn (on top) first
        /// </summary>
        private CaptureFormHotspot FindNumberedHotspot(NativePoint location)
        {
            var point = new Point(location.X + 0.5, location.Y + 0.5);
            for (int i = _numberedHotspots.Count - 1; i >= 0; i--)
            {
                var (hotspot, number) = _numberedHotspots[i];
                if (BadgeBounds(hotspot, number).Contains(point))
                {
                    return hotspot;
                }
            }
            return null;
        }

        /// <summary>
        /// The badge with the number sits on the top left corner of the area, outside of it so it doesn't cover the text, inside the capture
        /// </summary>
        private Rect BadgeBounds(CaptureFormHotspot hotspot, int number)
        {
            var text = BadgeText(number);
            double height = Math.Ceiling(text.Height + ToolStyle.Scale(2));
            double width = Math.Max(height, Math.Ceiling(text.Width + ToolStyle.Scale(8)));
            var corner = TopLeftOf(hotspot);
            double overlap = ToolStyle.Scale(4);
            double x = Math.Max(0, corner.X - width + overlap);
            double y = Math.Max(0, corner.Y - height + overlap);
            return new Rect(Math.Round(x), Math.Round(y), width, height);
        }

        private FormattedText BadgeText(int number) => ToolStyle.CreateText(number.ToString(CultureInfo.InvariantCulture), 8, ToolStyle.AccentForeground, true);

        private static Point TopLeftOf(CaptureFormHotspot hotspot) =>
            hotspot.Outline is { Count: 4 } outline ? new Point(outline[0].X, outline[0].Y) : new Point(hotspot.Bounds.Left, hotspot.Bounds.Top);

        /// <summary>
        /// The area (turned when it has an outline) and the badge of every numbered hotspot of the active tool
        /// </summary>
        private void DrawNumberedHotspots()
        {
            using var dc = NumberedHotspotLayer.Open();
            if (_numberedHotspots.Count == 0)
            {
                return;
            }
            var pen = new Pen(ToolStyle.Accent, 2);
            pen.Freeze();
            var hoverPen = new Pen((Brush)FindResource("HotspotBrush"), 3);
            hoverPen.Freeze();
            var hoverBrush = (Brush)FindResource("HotspotHoverBrush");
            double radius = ToolStyle.Scale(3);
            foreach (var (hotspot, number) in _numberedHotspots)
            {
                bool hovered = hotspot == _hoveredHotspot;
                dc.DrawGeometry(hovered ? hoverBrush : null, hovered ? hoverPen : pen, AreaOf(hotspot));
                var badge = BadgeBounds(hotspot, number);
                dc.DrawRoundedRectangle(ToolStyle.Accent, null, badge, radius, radius);
                var text = BadgeText(number);
                dc.DrawText(text, new Point(badge.X + (badge.Width - text.Width) / 2, badge.Y + (badge.Height - text.Height) / 2));
            }
        }

        private static Geometry AreaOf(CaptureFormHotspot hotspot)
        {
            if (hotspot.Outline is not { Count: 4 } outline)
            {
                return new RectangleGeometry(new Rect(hotspot.Bounds.Left, hotspot.Bounds.Top, hotspot.Bounds.Width, hotspot.Bounds.Height));
            }
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(outline[0].X, outline[0].Y), false, true);
                for (int i = 1; i < outline.Count; i++)
                {
                    context.LineTo(new Point(outline[i].X, outline[i].Y), true, false);
                }
            }
            geometry.Freeze();
            return geometry;
        }

        /// <summary>
        /// The list of the numbered hotspots of the active tool in a panel which takes the mouse: number, label (Text) and value (ToolTipText)
        /// </summary>
        private void UpdateHotspotList()
        {
            if (_numberedHotspots.Count == 0)
            {
                HidePanel(_hotspotListOwner);
                return;
            }
            if (_hotspotList == null)
            {
                _hotspotRows = new StackPanel();
                _hotspotList = new ScrollViewer
                {
                    Content = _hotspotRows,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Focusable = false,
                    MaxWidth = 360,
                    // About ten rows of two lines
                    MaxHeight = VisibleListRows * 38
                };
                AutomationProperties.SetName(_hotspotList, Texts.Core.CaptureHotspotList);
            }
            _hotspotRows.Children.Clear();
            foreach (var (hotspot, number) in _numberedHotspots)
            {
                _hotspotRows.Children.Add(CreateHotspotRow(hotspot, number));
            }
            ShowPanel(_hotspotListOwner, _hotspotList, true);
        }

        private FrameworkElement CreateHotspotRow(CaptureFormHotspot hotspot, int number)
        {
            var numberText = new TextBlock
            {
                Text = number.ToString(CultureInfo.InvariantCulture),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                // The brushes directly: a resource reference is looked up while the row isn't in the panel yet, which logs "Resource not found"
                Foreground = ToolStyle.AccentForeground
            };
            var badge = new Border
            {
                CornerRadius = new CornerRadius(3),
                MinWidth = 18,
                Padding = new Thickness(4, 0, 4, 0),
                Margin = new Thickness(0, 1, 8, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = numberText,
                Background = ToolStyle.Accent
            };

            var texts = new StackPanel();
            if (!string.IsNullOrEmpty(hotspot.Text))
            {
                var label = new TextBlock { Text = hotspot.Text, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, Foreground = ToolStyle.MutedForeground };
                texts.Children.Add(label);
            }
            if (!string.IsNullOrEmpty(hotspot.ToolTipText))
            {
                texts.Children.Add(new TextBlock { Text = hotspot.ToolTipText, TextTrimming = TextTrimming.CharacterEllipsis });
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(texts, 1);
            grid.Children.Add(badge);
            grid.Children.Add(texts);

            var row = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 3, 4, 3),
                Cursor = Cursors.Hand,
                ToolTip = hotspot.ToolTipText,
                Child = grid
            };
            AutomationProperties.SetName(row, $"{number}: {hotspot.Text} {hotspot.ToolTipText}".Trim());
            row.MouseEnter += (sender, args) =>
            {
                row.Background = (Brush)FindResource("HotspotHoverBrush");
                _hoveredHotspot = hotspot;
                DrawFeatures();
            };
            row.MouseLeave += (sender, args) =>
            {
                row.Background = Brushes.Transparent;
                if (_hoveredHotspot == hotspot)
                {
                    _hoveredHotspot = null;
                    DrawFeatures();
                }
            };
            row.MouseLeftButtonDown += (sender, args) =>
            {
                args.Handled = true;
                if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && hotspot.Actions.Count > 0)
                {
                    RunHotspotAction(hotspot.Actions[0]);
                    return;
                }
                ShowHotspotMenu(hotspot, row, new Rect(0, 0, row.ActualWidth, row.ActualHeight));
            };
            return row;
        }

        /// <summary>
        /// An interactive panel stays where it is: when a selection is dragged below it, it can be seen through
        /// </summary>
        private void UpdateInteractivePanelOpacity(Panel panel)
        {
            bool covers = IsSelectionVisible && !panel.Bounds.IsEmpty && _selectionRect.IntersectsWith(panel.Bounds) && !IsMouseOverHotspotList;
            if (covers == panel.IsSeeThrough)
            {
                return;
            }
            panel.IsSeeThrough = covers;
            // Ends a fade in, which would hold its value
            panel.Element.BeginAnimation(OpacityProperty, null);
            panel.Element.Opacity = covers ? 0.3 : 1;
        }
    }
}
