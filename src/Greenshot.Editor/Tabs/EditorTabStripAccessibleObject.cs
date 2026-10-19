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

using System.Drawing;
using System.Windows.Forms;
using Greenshot.Base.Languages;

namespace Greenshot.Editor.Tabs
{
    /// <summary>
    /// The tab strip for a screen reader: a tab list with one tab per capture (named by its title, "modified" when it has changes),
    /// each with its close button, and the "+" button after the tabs.
    /// </summary>
    internal sealed class EditorTabStripAccessibleObject : Control.ControlAccessibleObject
    {
        private readonly EditorTabStrip _strip;

        public EditorTabStripAccessibleObject(EditorTabStrip strip) : base(strip)
        {
            _strip = strip;
        }

        public override AccessibleRole Role => AccessibleRole.PageTabList;

        // The tabs, then the "+"
        public override int GetChildCount() => _strip.Tabs.Count + 1;

        public override AccessibleObject GetChild(int index)
        {
            if (index >= 0 && index < _strip.Tabs.Count)
            {
                return new TabAccessibleObject(this, index);
            }

            return index == _strip.Tabs.Count ? new NewTabAccessibleObject(this) : null;
        }

        /// <summary>
        /// The tab at the index
        /// </summary>
        private sealed class TabAccessibleObject : AccessibleObject
        {
            private readonly EditorTabStripAccessibleObject _parent;
            private readonly int _index;

            public TabAccessibleObject(EditorTabStripAccessibleObject parent, int index)
            {
                _parent = parent;
                _index = index;
            }

            private EditorTabStrip Strip => _parent._strip;

            private EditorTab Tab => _index < Strip.Tabs.Count ? Strip.Tabs[_index] : null;

            public override AccessibleObject Parent => _parent;

            public override AccessibleRole Role => AccessibleRole.PageTab;

            public override string Name
            {
                get
                {
                    var tab = Tab;
                    if (tab == null)
                    {
                        return null;
                    }

                    return tab.Surface?.Modified == true ? string.Format(Texts.Editor.TabModified, tab.Title) : tab.Title;
                }
            }

            public override string Description => Tab?.Description;

            public override Rectangle Bounds => Strip.RectangleToScreen(Strip.GetTabBounds(_index));

            public override AccessibleStates State
            {
                get
                {
                    var state = AccessibleStates.Selectable | AccessibleStates.Focusable;
                    var tab = Tab;
                    if (tab != null && tab == Strip.SelectedTab)
                    {
                        state |= AccessibleStates.Selected;
                        if (Strip.Focused)
                        {
                            state |= AccessibleStates.Focused;
                        }
                    }

                    return state;
                }
            }

            public override void DoDefaultAction() => Strip.SelectTabAt(_index);

            public override void Select(AccessibleSelection flags)
            {
                if ((flags & AccessibleSelection.TakeFocus) != 0)
                {
                    Strip.Select();
                }

                if ((flags & (AccessibleSelection.TakeSelection | AccessibleSelection.AddSelection)) != 0)
                {
                    Strip.SelectTabAt(_index);
                }
            }

            // The close button
            public override int GetChildCount() => 1;

            public override AccessibleObject GetChild(int index) => index == 0 ? new CloseAccessibleObject(this, Strip, _index) : null;

            public override AccessibleObject Navigate(AccessibleNavigation navigation)
            {
                switch (navigation)
                {
                    case AccessibleNavigation.Next:
                    case AccessibleNavigation.Right:
                        return _parent.GetChild(_index + 1);
                    case AccessibleNavigation.Previous:
                    case AccessibleNavigation.Left:
                        return _parent.GetChild(_index - 1);
                    default:
                        return base.Navigate(navigation);
                }
            }
        }

        /// <summary>
        /// The cross which closes the tab at the index
        /// </summary>
        private sealed class CloseAccessibleObject : AccessibleObject
        {
            private readonly AccessibleObject _parent;
            private readonly EditorTabStrip _strip;
            private readonly int _index;

            public CloseAccessibleObject(AccessibleObject parent, EditorTabStrip strip, int index)
            {
                _parent = parent;
                _strip = strip;
                _index = index;
            }

            public override AccessibleObject Parent => _parent;

            public override AccessibleRole Role => AccessibleRole.PushButton;

            public override string Name => Texts.Editor.CloseTab;

            public override Rectangle Bounds => _strip.RectangleToScreen(_strip.GetCloseBounds(_index));

            // A narrow tab shows the cross only when it is selected or under the mouse, the tab still closes with Ctrl+W or Delete
            public override AccessibleStates State => _strip.ShowsClose(_index) ? AccessibleStates.None : AccessibleStates.Invisible;

            public override void DoDefaultAction() => _strip.CloseTabAt(_index);
        }

        /// <summary>
        /// The "+" after the tabs, which adds a new image
        /// </summary>
        private sealed class NewTabAccessibleObject : AccessibleObject
        {
            private readonly EditorTabStripAccessibleObject _parent;

            public NewTabAccessibleObject(EditorTabStripAccessibleObject parent)
            {
                _parent = parent;
            }

            public override AccessibleObject Parent => _parent;

            public override AccessibleRole Role => AccessibleRole.PushButton;

            public override string Name => _parent._strip.NewTabText;

            public override Rectangle Bounds => _parent._strip.RectangleToScreen(_parent._strip.NewTabBounds);

            public override AccessibleStates State => AccessibleStates.None;

            public override void DoDefaultAction() => _parent._strip.RequestNewTab();

            public override AccessibleObject Navigate(AccessibleNavigation navigation)
            {
                switch (navigation)
                {
                    case AccessibleNavigation.Previous:
                    case AccessibleNavigation.Left:
                        return _parent.GetChild(_parent._strip.Tabs.Count - 1);
                    case AccessibleNavigation.Next:
                    case AccessibleNavigation.Right:
                        return null;
                    default:
                        return base.Navigate(navigation);
                }
            }
        }
    }
}
