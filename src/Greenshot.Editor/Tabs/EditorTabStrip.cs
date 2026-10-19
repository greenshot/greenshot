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
using System.Windows.Forms;
using Greenshot.Editor.Forms;

namespace Greenshot.Editor.Tabs
{
    /// <summary>
    /// The tabs of an editor window, like the tabs of a browser: a click selects a tab, the cross or the middle mouse button closes it,
    /// dragging moves it to another place, into another editor window or out into a new window. The "+" after the tabs adds a new image.
    /// Drawn with the system colors, so it follows high contrast.
    /// </summary>
    public sealed class EditorTabStrip : Control
    {
        private const int MaximumTabWidth = 220;
        // Below this width only the selected tab and the tab under the mouse show the cross
        private const int AlwaysShowCloseWidth = 90;
        private const int TabMargin = 4;
        private const int CloseSize = 16;

        private readonly List<EditorTab> _tabs = new();
        private readonly ToolTip _toolTip = new();
        private EditorTab _selectedTab;
        private int _hotIndex = -1;
        private bool _hotClose;
        private int _pressedCloseIndex = -1;
        private MouseButtons _pressedCloseButton;
        private bool _hotNewTab;
        private bool _pressedNewTab;
        // Where a tab from another window lands, the tabs make room for it there
        private int _dropIndex = -1;
        // The tab which is dragged out of this strip, it leaves a gap
        private EditorTab _draggedTab;
        private EditorTabDrag _drag;

        public EditorTabStrip()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            BackColor = SystemColors.Control;
            // Docked at the top, the layout takes the height from GetPreferredSize
            AutoSize = true;
        }

        /// <summary>
        /// The tabs, from left to right
        /// </summary>
        public IReadOnlyList<EditorTab> Tabs => _tabs;

        /// <summary>
        /// The tab which is shown, setting it only changes the strip
        /// </summary>
        public EditorTab SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                Invalidate();
            }
        }

        /// <summary>
        /// The window of the strip
        /// </summary>
        internal ImageEditorForm Window => FindForm() as ImageEditorForm;

        /// <summary>
        /// The tooltip of the "+" after the tabs
        /// </summary>
        public string NewTabText { get; set; }

        /// <summary>
        /// The user clicked on a tab which isn't selected
        /// </summary>
        public event EventHandler<EditorTabEventArgs> TabSelected;

        /// <summary>
        /// The user clicked on the cross of a tab, or with the middle mouse button on a tab
        /// </summary>
        public event EventHandler<EditorTabEventArgs> TabCloseRequested;

        /// <summary>
        /// The user clicked on the "+" after the tabs
        /// </summary>
        public event EventHandler NewTabRequested;

        internal int IndexOf(EditorTab tab) => _tabs.IndexOf(tab);

        internal void Insert(int index, EditorTab tab)
        {
            _tabs.Insert(Math.Max(0, Math.Min(index, _tabs.Count)), tab);
            ResetMouseState();
        }

        internal void Remove(EditorTab tab)
        {
            _tabs.Remove(tab);
            if (_selectedTab == tab)
            {
                _selectedTab = null;
            }

            ResetMouseState();
        }

        internal void MoveTab(EditorTab tab, int index)
        {
            int currentIndex = _tabs.IndexOf(tab);
            index = Math.Max(0, Math.Min(index, _tabs.Count - 1));
            if (currentIndex < 0 || currentIndex == index)
            {
                return;
            }

            _tabs.RemoveAt(currentIndex);
            _tabs.Insert(index, tab);
            ResetMouseState();
        }

        /// <summary>
        /// Show where a tab, which is dragged over this strip, lands: the strip is marked and the tabs make room at the index.
        /// -1 removes the mark.
        /// </summary>
        internal void ShowDropIndex(int index)
        {
            if (_dropIndex == index)
            {
                return;
            }

            _dropIndex = index;
            Invalidate();
        }

        /// <summary>
        /// The tab which is dragged out of this strip, it is shown as a gap until it is dropped somewhere else
        /// </summary>
        internal EditorTab DraggedTab
        {
            get => _draggedTab;
            set
            {
                if (_draggedTab == value)
                {
                    return;
                }

                _draggedTab = value;
                Invalidate();
            }
        }

        /// <summary>
        /// The place (screen coordinates) of the room the tabs make for a dropped tab
        /// </summary>
        internal Rectangle DropBounds => RectangleToScreen(GetSlotBounds(_dropIndex));

        /// <summary>
        /// The size of a tab of this strip
        /// </summary>
        internal Size TabSize => GetSlotBounds(0).Size;

        /// <summary>
        /// Cancel the drag of a tab, it goes back to its place
        /// </summary>
        /// <returns>true when a tab was dragged</returns>
        internal bool CancelDrag()
        {
            if (_drag == null)
            {
                return false;
            }

            var drag = _drag;
            _drag = null;
            drag.Cancel();
            Capture = false;
            return true;
        }

        private void ResetMouseState()
        {
            _hotIndex = -1;
            _hotClose = false;
            _hotNewTab = false;
            _pressedCloseIndex = -1;
            _pressedNewTab = false;
            Invalidate();
        }

        private int Scale(int value) => value * DeviceDpi / 96;

        public override Size GetPreferredSize(Size proposedSize) => new(proposedSize.Width, Font.Height + Scale(14));

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            Parent?.PerformLayout(this, nameof(Height));
        }

        // The room for a dropped tab is a slot like a tab
        private int SlotCount => _tabs.Count + (_dropIndex >= 0 ? 1 : 0);

        private int TabWidth => GetTabWidth(SlotCount);

        private int GetTabWidth(int slotCount)
        {
            if (slotCount == 0)
            {
                return Scale(MaximumTabWidth);
            }

            // The "+" follows the tabs
            int available = Width - 3 * Scale(TabMargin) - NewTabSize;
            // Many tabs get narrow, but all of them and the "+" stay in the window
            return Math.Max(1, Math.Min(Scale(MaximumTabWidth), available / slotCount));
        }

        /// <summary>
        /// The left side of the first tab, in client coordinates
        /// </summary>
        internal int FirstTabLeft => Scale(TabMargin);

        private Rectangle GetTabBounds(int index) => GetSlotBounds(_dropIndex >= 0 && index >= _dropIndex ? index + 1 : index);

        /// <summary>
        /// The "+" after the tabs
        /// </summary>
        private Rectangle NewTabBounds =>
            new(Scale(TabMargin) + SlotCount * TabWidth + Scale(TabMargin), Scale(TabMargin) + Scale(1), NewTabSize, NewTabSize);

        private int NewTabSize => Math.Max(0, Height - Scale(TabMargin) - Scale(3));

        private Rectangle GetSlotBounds(int slot)
        {
            int top = Scale(TabMargin);
            return new Rectangle(Scale(TabMargin) + slot * TabWidth, top, TabWidth, Height - top);
        }

        // A very narrow tab has no room for the cross, the middle mouse button or Ctrl+W close it
        private bool ShowsClose(int index) =>
            TabWidth >= Scale(AlwaysShowCloseWidth) || (TabWidth >= Scale(CloseSize + 12) && (index == _hotIndex || _tabs[index] == _selectedTab));

        private Rectangle GetCloseBounds(int index)
        {
            var tabBounds = GetTabBounds(index);
            int size = Scale(CloseSize);
            return new Rectangle(tabBounds.Right - size - Scale(6), tabBounds.Top + (tabBounds.Height - size) / 2, size, size);
        }

        /// <summary>
        /// The index of the tab at the location, -1 when there is none
        /// </summary>
        internal int HitTest(Point location)
        {
            if (location.Y < 0 || location.Y >= Height || TabWidth == 0)
            {
                return -1;
            }

            int x = location.X - Scale(TabMargin);
            int slot = x < 0 ? -1 : x / TabWidth;
            if (_dropIndex >= 0 && slot >= _dropIndex)
            {
                slot = slot == _dropIndex ? -1 : slot - 1;
            }

            return slot < _tabs.Count ? slot : -1;
        }

        /// <summary>
        /// The index a tab, which is dragged inside this strip, moves to: the place of the tab under the mouse
        /// </summary>
        internal int GetMoveIndex(Point location)
        {
            if (TabWidth == 0)
            {
                return 0;
            }

            int index = (location.X - Scale(TabMargin)) / TabWidth;
            return Math.Max(0, Math.Min(index, _tabs.Count - 1));
        }

        /// <summary>
        /// The index a tab from another window is inserted at: the slot under the mouse, when the tabs made room for one more tab
        /// </summary>
        internal int GetInsertIndex(Point location)
        {
            int index = (location.X - Scale(TabMargin)) / GetTabWidth(_tabs.Count + 1);
            return Math.Max(0, Math.Min(index, _tabs.Count));
        }

        /// <summary>
        /// True when the location (client coordinates) is on the strip or close to it, a tab dragged there stays in the strip
        /// </summary>
        internal bool IsNear(Point location)
        {
            var area = ClientRectangle;
            area.Inflate(Scale(24), Scale(24));
            return area.Contains(location);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            bool dropping = _dropIndex >= 0;
            // A tab can be dropped here: the strip takes a tint of the highlight color
            graphics.Clear(dropping ? Blend(BackColor, SystemColors.Highlight, 0.25) : BackColor);
            using var borderPen = new Pen(SystemColors.ControlDark);
            // The line between the tabs and the canvas, the selected tab is open to the canvas
            graphics.DrawLine(borderPen, 0, Height - 1, Width, Height - 1);

            for (int index = 0; index < _tabs.Count; index++)
            {
                if (_tabs[index] == _draggedTab)
                {
                    PaintGap(graphics, GetTabBounds(index), SystemColors.ControlDark);
                    continue;
                }

                PaintTab(graphics, borderPen, index);
            }

            PaintNewTab(graphics);
            if (dropping)
            {
                PaintGap(graphics, GetSlotBounds(_dropIndex), SystemColors.Highlight);
                using var dropPen = new Pen(SystemColors.Highlight, Scale(2));
                graphics.DrawRectangle(dropPen, Scale(1), Scale(1), Width - Scale(2), Height - Scale(2));
            }
        }

        /// <summary>
        /// The place of a tab which isn't there: the room for a dropped tab, or the place of the tab which is dragged out
        /// </summary>
        private void PaintGap(Graphics graphics, Rectangle bounds, Color color)
        {
            using var gapPen = new Pen(color, Math.Max(1, Scale(1)))
            {
                DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
            };
            graphics.DrawRectangle(gapPen, bounds.Left + Scale(1), bounds.Top, bounds.Width - Scale(3), bounds.Height - Scale(2));
        }

        private static Color Blend(Color background, Color color, double amount) => Color.FromArgb(
            (int)(background.R + (color.R - background.R) * amount),
            (int)(background.G + (color.G - background.G) * amount),
            (int)(background.B + (color.B - background.B) * amount));

        /// <summary>
        /// Paint the tab as the selected tab of this strip, for the image of a dragged tab
        /// </summary>
        internal void PaintTabImage(Graphics graphics, Rectangle bounds, EditorTab tab)
        {
            graphics.FillRectangle(SystemBrushes.Window, bounds);
            graphics.DrawRectangle(SystemPens.ControlDark, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
            PaintTitle(graphics, new Rectangle(bounds.Left, bounds.Top, bounds.Width - Scale(6), bounds.Height), tab, SystemColors.WindowText);
        }

        private void PaintTitle(Graphics graphics, Rectangle bounds, EditorTab tab, Color color)
        {
            var textBounds = new Rectangle(bounds.Left + Scale(8), bounds.Top, Math.Max(0, bounds.Width - Scale(8)), bounds.Height);
            TextRenderer.DrawText(graphics, tab.Title, Font, textBounds, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private void PaintTab(Graphics graphics, Pen borderPen, int index)
        {
            var tab = _tabs[index];
            var bounds = GetTabBounds(index);
            bool selected = tab == _selectedTab;
            if (selected)
            {
                graphics.FillRectangle(SystemBrushes.Window, bounds.Left, bounds.Top, bounds.Width, bounds.Height);
                graphics.DrawLines(borderPen, new[]
                {
                    new Point(bounds.Left, bounds.Bottom), new Point(bounds.Left, bounds.Top),
                    new Point(bounds.Right - 1, bounds.Top), new Point(bounds.Right - 1, bounds.Bottom)
                });
            }
            else
            {
                if (index == _hotIndex)
                {
                    graphics.FillRectangle(SystemBrushes.ControlLightLight, bounds.Left, bounds.Top, bounds.Width, bounds.Height - 1);
                }

                // A separator, but not next to the selected tab
                bool nextSelected = index + 1 < _tabs.Count && _tabs[index + 1] == _selectedTab;
                if (!nextSelected)
                {
                    int inset = bounds.Height / 4;
                    graphics.DrawLine(borderPen, bounds.Right - 1, bounds.Top + inset, bounds.Right - 1, bounds.Bottom - inset);
                }
            }

            bool showsClose = ShowsClose(index);
            int textRight = showsClose ? GetCloseBounds(index).Left - Scale(2) : bounds.Right - Scale(6);
            PaintTitle(graphics, new Rectangle(bounds.Left, bounds.Top, textRight - bounds.Left, bounds.Height), tab,
                selected ? SystemColors.WindowText : SystemColors.ControlText);

            if (showsClose)
            {
                PaintClose(graphics, index);
            }
        }

        private void PaintNewTab(Graphics graphics)
        {
            var bounds = NewTabBounds;
            if (_hotNewTab)
            {
                graphics.FillRectangle(SystemBrushes.ControlLightLight, bounds);
            }

            int inset = bounds.Width / 4;
            int middleX = bounds.Left + bounds.Width / 2;
            int middleY = bounds.Top + bounds.Height / 2;
            using var plusPen = new Pen(SystemColors.ControlText, Math.Max(1, Scale(1)));
            graphics.DrawLine(plusPen, bounds.Left + inset, middleY, bounds.Right - inset - 1, middleY);
            graphics.DrawLine(plusPen, middleX, bounds.Top + inset, middleX, bounds.Bottom - inset - 1);
        }

        private void PaintClose(Graphics graphics, int index)
        {
            var closeBounds = GetCloseBounds(index);
            if (index == _hotIndex && _hotClose)
            {
                graphics.FillRectangle(SystemBrushes.ControlDark, closeBounds);
            }

            int inset = Scale(4);
            using var crossPen = new Pen(_tabs[index] == _selectedTab ? SystemColors.WindowText : SystemColors.ControlText, Math.Max(1, Scale(1)));
            graphics.DrawLine(crossPen, closeBounds.Left + inset, closeBounds.Top + inset, closeBounds.Right - inset - 1, closeBounds.Bottom - inset - 1);
            graphics.DrawLine(crossPen, closeBounds.Left + inset, closeBounds.Bottom - inset - 1, closeBounds.Right - inset - 1, closeBounds.Top + inset);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && NewTabBounds.Contains(e.Location))
            {
                _pressedNewTab = true;
                return;
            }

            int index = HitTest(e.Location);
            if (index < 0)
            {
                return;
            }

            if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && ShowsClose(index) && GetCloseBounds(index).Contains(e.Location)))
            {
                _pressedCloseIndex = index;
                _pressedCloseButton = e.Button;
                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var tab = _tabs[index];
            if (tab != _selectedTab)
            {
                TabSelected?.Invoke(this, new EditorTabEventArgs(tab));
            }

            var tabBounds = GetTabBounds(index);
            _toolTip.Hide(this);
            _drag = new EditorTabDrag(this, tab, PointToScreen(e.Location), new Point(e.X - tabBounds.Left, e.Y));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_drag != null)
            {
                _drag.MoveTo(Cursor.Position);
                return;
            }

            int index = HitTest(e.Location);
            bool hotClose = index >= 0 && ShowsClose(index) && GetCloseBounds(index).Contains(e.Location);
            bool hotNewTab = NewTabBounds.Contains(e.Location);
            if (index == _hotIndex && hotClose == _hotClose && hotNewTab == _hotNewTab)
            {
                return;
            }

            if (index != _hotIndex || hotNewTab != _hotNewTab)
            {
                _toolTip.SetToolTip(this, index >= 0 ? _tabs[index].Description : hotNewTab ? NewTabText : null);
            }

            _hotIndex = index;
            _hotClose = hotClose;
            _hotNewTab = hotNewTab;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_drag != null)
            {
                FinishDrag();
                return;
            }

            if (_pressedNewTab)
            {
                _pressedNewTab = false;
                if (e.Button == MouseButtons.Left && NewTabBounds.Contains(e.Location))
                {
                    NewTabRequested?.Invoke(this, EventArgs.Empty);
                }

                return;
            }

            int pressedIndex = _pressedCloseIndex;
            _pressedCloseIndex = -1;
            if (pressedIndex < 0 || e.Button != _pressedCloseButton || HitTest(e.Location) != pressedIndex)
            {
                return;
            }

            if (e.Button == MouseButtons.Middle || GetCloseBounds(pressedIndex).Contains(e.Location))
            {
                TabCloseRequested?.Invoke(this, new EditorTabEventArgs(_tabs[pressedIndex]));
            }
        }

        /// <summary>
        /// The mouse capture can also end without a mouse up (e.g. another window was activated), the tab then goes back
        /// </summary>
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (_drag == null)
            {
                return;
            }

            if ((MouseButtons & MouseButtons.Left) == 0)
            {
                FinishDrag();
                return;
            }

            CancelDrag();
        }

        private void FinishDrag()
        {
            var drag = _drag;
            _drag = null;
            drag.Finish(Cursor.Position);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hotIndex >= 0 || _hotNewTab)
            {
                _hotIndex = -1;
                _hotClose = false;
                _hotNewTab = false;
                Invalidate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    public sealed class EditorTabEventArgs : EventArgs
    {
        public EditorTabEventArgs(EditorTab tab)
        {
            Tab = tab;
        }

        public EditorTab Tab { get; }
    }
}
