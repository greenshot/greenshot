/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Forms;
using Greenshot.Editor.Memento;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Dispatches most of a DrawableContainer's public properties and methods to a list of DrawableContainers.
    /// </summary>
    [Serializable]
    public class DrawableContainerList : List<IDrawableContainer>, IDrawableContainerList
    {
        private static readonly ComponentResourceManager EditorFormResources = new ComponentResourceManager(typeof(ImageEditorForm));

        public Guid ParentID { get; private set; }

        public DrawableContainerList()
        {
        }

        public DrawableContainerList(Guid parentId)
        {
            ParentID = parentId;
        }

        public EditStatus Status
        {
            get { return this[Count - 1].Status; }
            set
            {
                foreach (var dc in this)
                {
                    dc.Status = value;
                }
            }
        }

        public List<IDrawableContainer> AsIDrawableContainerList()
        {
            List<IDrawableContainer> interfaceList = new List<IDrawableContainer>();
            foreach (IDrawableContainer container in this)
            {
                interfaceList.Add(container);
            }

            return interfaceList;
        }

        /// <summary>
        /// Gets or sets the selection status of the elements.
        /// If several elements are in the list, true is only returned when all elements are selected.
        /// </summary>
        public bool Selected
        {
            get
            {
                bool ret = true;
                foreach (var dc in this)
                {
                    ret &= dc.Selected;
                }

                return ret;
            }
            set
            {
                foreach (var dc in this)
                {
                    dc.Selected = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the parent control of the elements in the list.
        /// If there are several elements, the parent control of the last added is returned.
        /// </summary>
        public ISurface Parent
        {
            get
            {
                if (Count > 0)
                {
                    return this[Count - 1].Parent;
                }

                return null;
            }
            set
            {
                ParentID = value?.ID ?? Guid.NewGuid();
                foreach (var drawableContainer in this)
                {
                    var dc = (DrawableContainer) drawableContainer;
                    dc.Parent = value;
                }
            }
        }

        /// <summary>
        /// Make a following bounds change on this containerlist undoable!
        /// </summary>
        /// <param name="allowMerge">true means allow the moves to be merged</param>
        public void MakeBoundsChangeUndoable(bool allowMerge)
        {
            if (Count > 0 && Parent != null)
            {
                var clone = new DrawableContainerList();
                clone.AddRange(this);
                Parent.MakeUndoable(new DrawableContainerBoundsChangeMemento(clone), allowMerge);
            }
        }

        /// <summary>
        /// Apply matrix to all elements
        /// </summary>
        public void Transform(Matrix matrix)
        {
            // Track modifications
            bool modified = false;
            Invalidate();
            foreach (var dc in this)
            {
                dc.Transform(matrix);
                modified = true;
            }

            // Invalidate after
            Invalidate();
            // If we moved something, tell the surface it's modified!
            if (modified)
            {
                Parent.Modified = true;
            }
        }

        /// <summary>
        /// Moves all elements in the list by the given amount of pixels.
        /// </summary>
        /// <param name="dx">pixels to move horizontally</param>
        /// <param name="dy">pixels to move vertically</param>
        public void MoveBy(int dx, int dy)
        {
            // Track modifications
            bool modified = false;

            // Invalidate before moving, otherwise the old locations aren't refreshed
            Invalidate();
            foreach (var dc in this)
            {
                dc.Left += dx;
                dc.Top += dy;
                modified = true;
            }

            // Invalidate after
            Invalidate();

            // If we moved something, tell the surface it's modified!
            if (modified)
            {
                Parent.Modified = true;
            }
        }

        /// <summary>
        /// Indicates whether on of the elements is clickable at the given location
        /// </summary>
        /// <param name="x">x coordinate to be checked</param>
        /// <param name="y">y coordinate to be checked</param>
        /// <returns>true if one of the elements in the list is clickable at the given location, false otherwise</returns>
        public bool ClickableAt(int x, int y)
        {
            bool ret = false;
            foreach (var dc in this)
            {
                ret |= dc.ClickableAt(x, y);
            }

            return ret;
        }

        /// <summary>
        /// retrieves the topmost element being clickable at the given location
        /// </summary>
        /// <param name="x">x coordinate to be checked</param>
        /// <param name="y">y coordinate to be checked</param>
        /// <returns>the topmost element from the list being clickable at the given location, null if there is no clickable element</returns>
        public IDrawableContainer ClickableElementAt(int x, int y)
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                if (this[i].ClickableAt(x, y))
                {
                    return this[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Dispatches OnDoubleClick to all elements in the list.
        /// </summary>
        public void OnDoubleClick()
        {
            foreach (var drawableContainer in this)
            {
                var dc = (DrawableContainer) drawableContainer;
                dc.OnDoubleClick();
            }
        }

        /// <summary>
        /// Check if there are any intersecting filters, if so we need to redraw more
        /// </summary>
        /// <param name="clipRectangle"></param>
        /// <returns>true if an filter intersects</returns>
        public bool HasIntersectingFilters(Rectangle clipRectangle)
        {
            foreach (var dc in this)
            {
                if (dc.DrawingBounds.IntersectsWith(clipRectangle) && dc.hasFilters && dc.Status == EditStatus.IDLE)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if any of the drawableContainers are inside the rectangle
        /// </summary>
        /// <param name="clipRectangle"></param>
        /// <returns></returns>
        public bool IntersectsWith(Rectangle clipRectangle)
        {
            foreach (var dc in this)
            {
                if (dc.DrawingBounds.IntersectsWith(clipRectangle))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A rectangle containing DrawingBounds of all drawableContainers in this list,
        /// or empty rectangle if nothing is there.
        /// </summary>
        public Rectangle DrawingBounds
        {
            get
            {
                if (Count == 0)
                {
                    return Rectangle.Empty;
                }
                else
                {
                    var result = this[0].DrawingBounds;
                    for (int i = 1; i < Count; i++)
                    {
                        result = Rectangle.Union(result, this[i].DrawingBounds);
                    }

                    return result;
                }
            }
        }

        /// <summary>
        /// Triggers all elements in the list ot be redrawn.
        /// </summary>
        /// <param name="g">the to the bitmap related Graphics object</param>
        /// <param name="bitmap">Bitmap to draw</param>
        /// <param name="renderMode">the rendermode in which the element is to be drawn</param>
        /// <param name="clipRectangle"></param>
        public void Draw(Graphics g, Bitmap bitmap, RenderMode renderMode, Rectangle clipRectangle)
        {
            if (Parent == null)
            {
                return;
            }

            foreach (var drawableContainer in this)
            {
                var dc = (DrawableContainer) drawableContainer;
                if (dc.Parent == null)
                {
                    continue;
                }

                if (dc.DrawingBounds.IntersectsWith(clipRectangle))
                {
                    dc.DrawContent(g, bitmap, renderMode, clipRectangle);
                }
            }
        }

        /// <summary>
        /// Pass the field changed event to all elements in the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void HandleFieldChangedEvent(object sender, FieldChangedEventArgs e)
        {
            foreach (var drawableContainer in this)
            {
                var dc = (DrawableContainer) drawableContainer;
                dc.HandleFieldChanged(sender, e);
            }
        }

        /// <summary>
        /// Invalidate the bounds of all the DC's in this list
        /// </summary>
        public void Invalidate()
        {
            if (Parent == null)
            {
                return;
            }

            Rectangle region = Rectangle.Empty;
            foreach (var dc in this)
            {
                region = Rectangle.Union(region, dc.DrawingBounds);
            }

            Parent.InvalidateElements(region);
        }

        /// <summary>
        /// Indicates whether the given list of elements can be pulled up, 
        /// i.e. whether there is at least one unselected element higher in hierarchy
        /// </summary>
        /// <param name="elements">list of elements to pull up</param>
        /// <returns>true if the elements could be pulled up</returns>
        public bool CanPullUp(IDrawableContainerList elements)
        {
            if (elements.Count == 0 || elements.Count == Count)
            {
                return false;
            }

            foreach (var element in elements)
            {
                if (IndexOf(element) < Count - elements.Count)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Pulls one or several elements up one level in hierarchy (z-index).
        /// </summary>
        /// <param name="elements">list of elements to pull up</param>
        public void PullElementsUp(IDrawableContainerList elements)
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                var dc = this[i];
                if (!elements.Contains(dc))
                {
                    continue;
                }

                if (Count > i + 1 && !elements.Contains(this[i + 1]))
                {
                    SwapElements(i, i + 1);
                }
            }
        }

        /// <summary>
        /// Pulls one or several elements up to the topmost level(s) in hierarchy (z-index).
        /// </summary>
        /// <param name="elements">of elements to pull to top</param>
        public void PullElementsToTop(IDrawableContainerList elements)
        {
            var dcs = ToArray();
            foreach (var dc in dcs)
            {
                if (!elements.Contains(dc))
                {
                    continue;
                }

                Remove(dc);
                Add(dc);
                Parent.Modified = true;
            }
        }

        /// <summary>
        /// Indicates whether the given list of elements can be pushed down, 
        /// i.e. whether there is at least one unselected element lower in hierarchy
        /// </summary>
        /// <param name="elements">list of elements to push down</param>
        /// <returns>true if the elements could be pushed down</returns>
        public bool CanPushDown(IDrawableContainerList elements)
        {
            if (elements.Count == 0 || elements.Count == Count)
            {
                return false;
            }

            foreach (var element in elements)
            {
                if (IndexOf(element) >= elements.Count)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Pushes one or several elements down one level in hierarchy (z-index).
        /// </summary>
        /// <param name="elements">list of elements to push down</param>
        public void PushElementsDown(IDrawableContainerList elements)
        {
            for (int i = 0; i < Count; i++)
            {
                var dc = this[i];
                if (!elements.Contains(dc))
                {
                    continue;
                }

                if ((i > 0) && !elements.Contains(this[i - 1]))
                {
                    SwapElements(i, i - 1);
                }
            }
        }

        /// <summary>
        /// Pushes one or several elements down to the bottommost level(s) in hierarchy (z-index).
        /// </summary>
        /// <param name="elements">of elements to push to bottom</param>
        public void PushElementsToBottom(IDrawableContainerList elements)
        {
            var dcs = ToArray();
            for (int i = dcs.Length - 1; i >= 0; i--)
            {
                var dc = dcs[i];
                if (!elements.Contains(dc))
                {
                    continue;
                }

                Remove(dc);
                Insert(0, dc);
                Parent.Modified = true;
            }
        }

        /// <summary>
        /// swaps two elements in hierarchy (z-index), 
        /// checks both indices to be in range
        /// </summary>
        /// <param name="index1">index of the 1st element</param>
        /// <param name="index2">index of the 2nd element</param>
        private void SwapElements(int index1, int index2)
        {
            if (index1 < 0 || index1 >= Count || index2 < 0 || index2 >= Count || index1 == index2)
            {
                return;
            }

            var dc = this[index1];
            this[index1] = this[index2];
            this[index2] = dc;
            Parent.Modified = true;
        }

        /// <summary>
        /// Add items to a context menu for the selected item
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="surface"></param>
        public virtual void AddContextMenuItems(ContextMenuStrip menu, ISurface surface)
        {
            bool push = surface.Elements.CanPushDown(this);
            bool pull = surface.Elements.CanPullUp(this);

            ToolStripMenuItem item;

            // Pull "up"
            if (pull)
            {
                item = new ToolStripMenuItem(Language.GetString(LangKey.editor_uptotop));
                item.Click += delegate
                {
                    surface.Elements.PullElementsToTop(this);
                    surface.Elements.Invalidate();
                };
                menu.Items.Add(item);
                item = new ToolStripMenuItem(Language.GetString(LangKey.editor_uponelevel));
                item.Click += delegate
                {
                    surface.Elements.PullElementsUp(this);
                    surface.Elements.Invalidate();
                };
                menu.Items.Add(item);
            }

            // Push "down"
            if (push)
            {
                item = new ToolStripMenuItem(Language.GetString(LangKey.editor_downtobottom));
                item.Click += delegate
                {
                    surface.Elements.PushElementsToBottom(this);
                    surface.Elements.Invalidate();
                };
                menu.Items.Add(item);
                item = new ToolStripMenuItem(Language.GetString(LangKey.editor_downonelevel));
                item.Click += delegate
                {
                    surface.Elements.PushElementsDown(this);
                    surface.Elements.Invalidate();
                };
                menu.Items.Add(item);
            }

            // Duplicate
            item = new ToolStripMenuItem(Language.GetString(LangKey.editor_duplicate));
            item.Click += delegate
            {
                IDrawableContainerList dcs = this.Clone();
                dcs.Parent = surface;
                dcs.MoveBy(10, 10);
                surface.AddElements(dcs);
                surface.DeselectAllElements();
                surface.SelectElements(dcs);
            };
            menu.Items.Add(item);

            // Copy
            item = new ToolStripMenuItem(Language.GetString(LangKey.editor_copytoclipboard))
            {
                Image = (Image) EditorFormResources.GetObject("copyToolStripMenuItem.Image")
            };
            item.Click += delegate { ClipboardHelper.SetClipboardData(typeof(IDrawableContainerList), this); };
            menu.Items.Add(item);

            // Cut
            item = new ToolStripMenuItem(Language.GetString(LangKey.editor_cuttoclipboard))
            {
                Image = (Image) EditorFormResources.GetObject("btnCut.Image")
            };
            item.Click += delegate
            {
                ClipboardHelper.SetClipboardData(typeof(IDrawableContainerList), this);
                surface.RemoveElements(this);
            };
            menu.Items.Add(item);

            // Push out
            ToolStripMenuItem alignSubmenu = new ToolStripMenuItem("Push Out");

            // Right
            item = new ToolStripMenuItem("Right")
            {
                Image = (Image)EditorFormResources.GetObject("copyToolStripMenuItem.Image")
            };
            item.Click += delegate
            {
                PushOut("right", surface, this[0], this);
            };
            alignSubmenu.DropDownItems.Add(item);

            // Left
            item = new ToolStripMenuItem("Left")
            {
                Image = (Image)EditorFormResources.GetObject("copyToolStripMenuItem.Image")
            };
            item.Click += delegate
            {
                PushOut("left", surface, this[0], this);
            };
            alignSubmenu.DropDownItems.Add(item);

            // Top
            item = new ToolStripMenuItem("Top")
            {
                Image = (Image)EditorFormResources.GetObject("copyToolStripMenuItem.Image")
            };
            item.Click += delegate
            {
                PushOut("top", surface, this[0], this);
            };
            alignSubmenu.DropDownItems.Add(item);

            // Bottom
            item = new ToolStripMenuItem("Bottom")
            {
                Image = (Image)EditorFormResources.GetObject("copyToolStripMenuItem.Image")
            };
            item.Click += delegate
            {
                PushOut("bottom", surface, this[0], this);
            };
            alignSubmenu.DropDownItems.Add(item);
            menu.Items.Add(alignSubmenu);

            // Fit menu
            ToolStripMenuItem fitSubmenu = new ToolStripMenuItem("Fit");

            // Fit width
            item = new ToolStripMenuItem("Fit to width")
            {
                Image = (Image)EditorFormResources.GetObject("copyToolStripMenuItem.Image")
            };
            item.Click += delegate
            {
                foreach (var item in this)
                {
                    MakeBoundsChangeUndoable(false);
                    item.Width = surface.Image.Width;
                }
                SnapAllToEdge("left", surface, this);
                surface.Invalidate(); // not sure if this belongs
            };
            fitSubmenu.DropDownItems.Add(item);

            // Fit height
            item = new ToolStripMenuItem("Fit to height")
            {
                Image = (Image)EditorFormResources.GetObject("copyToolStripMenuItem.Image")
            };
            item.Click += delegate
            {
                foreach (var item in this)
                {
                    MakeBoundsChangeUndoable(false);
                    item.Height = surface.Image.Height;
                }
                SnapAllToEdge("top", surface, this);
                surface.Invalidate(); // not sure if this belongs
            };
            fitSubmenu.DropDownItems.Add(item);
            menu.Items.Add(fitSubmenu);

            ToolStripMenuItem snapSubmenu = new ToolStripMenuItem("Snap");

            // Snap left
            item = new ToolStripMenuItem("Snap left")
            {
                Image = (Image)EditorFormResources.GetObject("copyToolStripMenuItem.Image")
            };
            item.Click += delegate
            {
                SnapAllToEdge("left", surface, this);
            };
            snapSubmenu.DropDownItems.Add(item);

            // Snap right
            item = new ToolStripMenuItem("Snap right")
            {
                Image = (Image)EditorFormResources.GetObject("copyToolStripMenuItem.Image")
            };
            item.Click += delegate
            {
                SnapAllToEdge("right", surface, this);
            };
            snapSubmenu.DropDownItems.Add(item);

            // Snap to top
            item = new ToolStripMenuItem("Snap to top")
            {
                Image = (Image)EditorFormResources.GetObject("copyToolStripMenuItem.Image")
            };
            item.Click += delegate
            {
                SnapAllToEdge("top", surface, this);
            };
            snapSubmenu.DropDownItems.Add(item);

            // Snap to bottom
            item = new ToolStripMenuItem("Snap to bottom")
            {
                Image = (Image)EditorFormResources.GetObject("copyToolStripMenuItem.Image")
            };
            item.Click += delegate
            {
                SnapAllToEdge("bottom", surface, this);
            };
            snapSubmenu.DropDownItems.Add(item);
            menu.Items.Add(snapSubmenu);

            // Delete
            item = new ToolStripMenuItem(Language.GetString(LangKey.editor_deleteelement))
            {
                Image = (Image) EditorFormResources.GetObject("removeObjectToolStripMenuItem.Image")
            };
            item.Click += delegate { surface.RemoveElements(this); };
            menu.Items.Add(item);

            // Reset
            bool canReset = false;
            foreach (var drawableContainer in this)
            {
                var container = (DrawableContainer) drawableContainer;
                if (container.HasDefaultSize)
                {
                    canReset = true;
                }
            }

            if (canReset)
            {
                item = new ToolStripMenuItem(Language.GetString(LangKey.editor_resetsize));
                //item.Image = ((System.Drawing.Image)(editorFormResources.GetObject("removeObjectToolStripMenuItem.Image")));
                item.Click += delegate
                {
                    MakeBoundsChangeUndoable(false);
                    foreach (var drawableContainer in this)
                    {
                        var container = (DrawableContainer) drawableContainer;
                        if (!container.HasDefaultSize)
                        {
                            continue;
                        }

                        Size defaultSize = container.DefaultSize;
                        container.MakeBoundsChangeUndoable(false);
                        container.Width = defaultSize.Width;
                        container.Height = defaultSize.Height;
                    }

                    surface.Invalidate();
                };
                menu.Items.Add(item);
            }
        }

        public virtual void ShowContextMenu(MouseEventArgs e, ISurface iSurface)
        {
            if (!(iSurface is Surface surface))
            {
                return;
            }

            bool hasMenu = false;
            foreach (var drawableContainer in this)
            {
                var container = (DrawableContainer) drawableContainer;
                if (!container.HasContextMenu)
                {
                    continue;
                }

                hasMenu = true;
                break;
            }

            if (hasMenu)
            {
                ContextMenuStrip menu = new ContextMenuStrip();
                AddContextMenuItems(menu, surface);
                if (menu.Items.Count > 0)
                {
                    menu.Show(surface, surface.ToSurfaceCoordinates(e.Location));
                    while (true)
                    {
                        if (menu.Visible)
                        {
                            Application.DoEvents();
                            Thread.Sleep(100);
                        }
                        else
                        {
                            menu.Dispose();
                            break;
                        }
                    }
                }
            }
        }

        private bool _disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var drawableContainer in this)
                    {
                        drawableContainer.Dispose();
                    }
                }

                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        /// <summary>
        /// Adjust UI elements to the supplied DPI settings
        /// </summary>
        /// <param name="dpi"></param>
        public void AdjustToDpi(uint dpi)
        {
            foreach (var drawableContainer in this)
            {
                drawableContainer.AdjustToDpi(dpi);
            }
        }

        public void SnapToEdge(string direction, ISurface surface, IDrawableContainer target)
        {
            int xMovement = 0, yMovement = 0;
            if (direction == "left")
                xMovement = -target.Location.X;
            else if (direction == "right")
                xMovement = surface.Image.Width - target.Location.X - target.Width;
            else if (direction == "top")
                yMovement = -target.Location.Y;
            else if (direction == "bottom")
                yMovement = surface.Image.Height - target.Location.Y - target.Height;
            target.MakeBoundsChangeUndoable(false);
            target.MoveBy(xMovement, yMovement);
        }

        public void SnapAllToEdge(string direction, ISurface surface, IDrawableContainerList containers)
        {
            foreach (var item in containers)
            {
                SnapToEdge(direction, surface, item);
            }
            surface.DeselectAllElements();
        }

        public void PushOut(string direction, ISurface surface, IDrawableContainer target, IDrawableContainerList parent)
        {
            // Make calculations
            int left = 0, right = 0, top = 0, bottom = 0;
            if (direction == "left")
                left = target.Width;
            else if (direction == "right")
                right = target.Width;
            else if (direction == "top")
                top = target.Height;
            else if (direction == "bottom")
                bottom = target.Height;
            
            // Use surface function
            surface.ResizeCanvas(left, right, top, bottom);

            // Move target object
            SnapToEdge(direction, surface, target);
            if (direction == "left" || direction == "right")
                SnapToEdge("top", surface, target);
            else if (direction == "top" || direction == "bottom")
                SnapToEdge("left", surface, target);
            surface.DeselectAllElements();
        }
    }
}