/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Dpi;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;

namespace Greenshot.Forms
{
    /// <summary>
    /// the ToolStripMenuSelectList makes it possible to have a single or multi-check menu
    /// </summary>
    public sealed class ToolStripMenuSelectList : ToolStripMenuItem
    {
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
        private readonly bool _multiCheckAllowed;
        private readonly IProvideDeviceDpi _provideDeviceDpi;
        private bool _updateInProgress;
        private Image _defaultImage;

        /// <summary>
        /// Occurs when one of the list's child element's Checked state changes.
        /// </summary>
        public new event EventHandler CheckedChanged;

        public object Identifier { get; private set; }

        public ToolStripMenuSelectList(object identifier, bool allowMultiCheck, IProvideDeviceDpi provideDeviceDpi)
        {
            Identifier = identifier;
            CheckOnClick = false;
            _multiCheckAllowed = allowMultiCheck;
            _provideDeviceDpi = provideDeviceDpi;
            UpdateImage();
        }


        private void UpdateImage()
        {
            var newSize = DpiCalculator.ScaleWithDpi(CoreConfig.IconSize, _provideDeviceDpi.DeviceDpi);
            if (_defaultImage == null || _defaultImage.Size != newSize)
            {
                _defaultImage?.Dispose();
                _defaultImage = ImageHelper.CreateEmpty(newSize.Width, newSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                    Color.Transparent, 96f, 96f);
            }

            Image = _defaultImage;
        }

        private void ItemCheckStateChanged(object sender, EventArgs e)
        {
            if (_updateInProgress)
            {
                return;
            }

            var toolStripMenuSelectListItem = (ToolStripMenuSelectListItem) sender;
            _updateInProgress = true;
            if (toolStripMenuSelectListItem.Checked && !_multiCheckAllowed)
            {
                UncheckAll();
                toolStripMenuSelectListItem.Checked = true;
            }

            _updateInProgress = false;
            CheckedChanged?.Invoke(this, new ItemCheckedChangedEventArgs(toolStripMenuSelectListItem));
        }

        /// <summary>
        /// adds an item to the select list
        /// </summary>
        /// <param name="label">the label to be displayed</param>
        /// <param name="image">the icon to be displayed</param>
        /// <param name="data">the data to be returned when an item is queried</param>
        /// <param name="isChecked">whether the item is initially checked</param>
        public void AddItem(string label, Image image, object data, bool isChecked)
        {
            var toolStripMenuSelectListItem = new ToolStripMenuSelectListItem
            {
                Text = label
            };
            if (image == null)
            {
                image = _defaultImage;
            }

            toolStripMenuSelectListItem.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripMenuSelectListItem.Image = image;
            toolStripMenuSelectListItem.CheckOnClick = true;
            toolStripMenuSelectListItem.CheckStateChanged += ItemCheckStateChanged;
            toolStripMenuSelectListItem.Data = data;
            if (isChecked)
            {
                if (!_multiCheckAllowed)
                {
                    _updateInProgress = true;
                    UncheckAll();
                    _updateInProgress = false;
                }

                toolStripMenuSelectListItem.Checked = true;
            }

            DropDownItems.Add(toolStripMenuSelectListItem);
        }


        /// <summary>
        /// adds an item to the select list
        /// </summary>
        /// <param name="label">the label to be displayed</param>
        /// <param name="data">the data to be returned when an item is queried</param>
        /// <param name="isChecked">whether the item is initially checked</param>
        public void AddItem(string label, object data, bool isChecked)
        {
            AddItem(label, null, data, isChecked);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _defaultImage?.Dispose();
                _defaultImage = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// unchecks all items of the list
        /// </summary>
        public void UncheckAll()
        {
            IEnumerator items = DropDownItems.GetEnumerator();
            while (items.MoveNext())
            {
                var toolStripMenuSelectListItem = (ToolStripMenuSelectListItem) items.Current;
                if (toolStripMenuSelectListItem != null)
                {
                    toolStripMenuSelectListItem.Checked = false;
                }
            }
        }
    }

    /// <summary>
    /// Event class for the CheckedChanged event in the ToolStripMenuSelectList
    /// </summary>
    public class ItemCheckedChangedEventArgs : EventArgs
    {
        public ToolStripMenuSelectListItem Item { get; set; }

        public ItemCheckedChangedEventArgs(ToolStripMenuSelectListItem item)
        {
            Item = item;
        }
    }

    /// <summary>
    /// Wrapper around the ToolStripMenuItem, which can contain an object
    /// Also the Checked property hides the normal checked property so we can render our own check
    /// </summary>
    public class ToolStripMenuSelectListItem : ToolStripMenuItem
    {
        public object Data { get; set; }
    }
}