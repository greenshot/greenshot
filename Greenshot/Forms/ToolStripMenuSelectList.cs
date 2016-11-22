//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Dapplo.Config.Ini;
using Greenshot.Addon.Configuration;
using Greenshot.Core.Gfx;

#endregion

namespace Greenshot.Forms
{
	/// <summary>
	///     the ToolStripMenuSelectList makes it possible to have a single or multi-check menu
	/// </summary>
	public class ToolStripMenuSelectList : ToolStripMenuItem
	{
		private static readonly ICoreConfiguration coreConfiguration = IniConfig.Current.Get<ICoreConfiguration>();
		private static Image _defaultImage;
		private readonly bool _multiCheckAllowed;
		private bool _updateInProgress;

		public ToolStripMenuSelectList(object identifier, bool allowMultiCheck)
		{
			Identifier = identifier;
			CheckOnClick = false;
			_multiCheckAllowed = allowMultiCheck;
			if ((_defaultImage == null) || (_defaultImage.Size != coreConfiguration.IconSize))
			{
				_defaultImage?.Dispose();
				_defaultImage = ImageHelper.CreateEmpty(coreConfiguration.IconSize.Width, coreConfiguration.IconSize.Height, PixelFormat.Format32bppArgb, Color.Transparent, 96f, 96f);
			}
			Image = _defaultImage;
		}

		public ToolStripMenuSelectList() : this(null, false)
		{
		}

		public ToolStripMenuSelectList(object identifier) : this(identifier, false)
		{
		}

		/// <summary>
		///     gets or sets the currently checked item
		/// </summary>
		public ToolStripMenuSelectListItem CheckedItem
		{
			get
			{
				var items = DropDownItems.GetEnumerator();
				while (items.MoveNext())
				{
					ToolStripMenuSelectListItem tsmi = (ToolStripMenuSelectListItem) items.Current;
					if (tsmi.Checked)
					{
						return tsmi;
					}
				}
				return null;
			}
			set
			{
				IEnumerator items = DropDownItems.GetEnumerator();
				while (items.MoveNext())
				{
					var toolStripMenuSelectListItem = (ToolStripMenuSelectListItem) items.Current;
					if (!_multiCheckAllowed && !toolStripMenuSelectListItem.Equals(value))
					{
						toolStripMenuSelectListItem.Checked = false;
					}
					else if (toolStripMenuSelectListItem.Equals(value))
					{
						toolStripMenuSelectListItem.Checked = true;
					}
				}
			}
		}

		/// <summary>
		///     gets or sets the currently checked items
		/// </summary>
		public ToolStripMenuSelectListItem[] CheckedItems
		{
			get
			{
				var toolStripMenuSelectListItems = new List<ToolStripMenuSelectListItem>();
				var items = DropDownItems.GetEnumerator();
				while (items.MoveNext())
				{
					var toolStripMenuSelectListItem = (ToolStripMenuSelectListItem) items.Current;
					if (toolStripMenuSelectListItem.Checked)
					{
						toolStripMenuSelectListItems.Add(toolStripMenuSelectListItem);
					}
				}
				return toolStripMenuSelectListItems.ToArray();
			}
			set
			{
				if (!_multiCheckAllowed)
				{
					throw new ArgumentException("Writing to checkedItems is only allowed in multi-check mode. Either set allowMultiCheck to true or use set SelectedItem instead of SelectedItems.");
				}
				IEnumerator items = DropDownItems.GetEnumerator();
				IEnumerator sel = value.GetEnumerator();
				while (items.MoveNext())
				{
					var toolStripMenuSelectListItem = (ToolStripMenuSelectListItem) items.Current;
					while (sel.MoveNext())
					{
						if (toolStripMenuSelectListItem.Equals(sel.Current))
						{
							toolStripMenuSelectListItem.Checked = true;
						}
						else
						{
							toolStripMenuSelectListItem.Checked = false;
						}
						if (!_multiCheckAllowed && !toolStripMenuSelectListItem.Equals(sel.Current))
						{
							toolStripMenuSelectListItem.Checked = false;
						}
						else if (toolStripMenuSelectListItem.Equals(value))
						{
							toolStripMenuSelectListItem.Checked = true;
						}
					}
				}
			}
		}

		public object Identifier { get; private set; }

		/// <summary>
		///     adds an item to the select list
		/// </summary>
		/// <param name="label">the label to be displayed</param>
		/// <param name="image">the icon to be displayed</param>
		/// <param name="data">the data to be returned when an item is queried</param>
		/// <param name="isChecked">whether the item is initially checked</param>
		public void AddItem(string label, Image image, object data, bool isChecked)
		{
			if (image == null)
			{
				image = _defaultImage;
			}
			ToolStripMenuSelectListItem newItem = new ToolStripMenuSelectListItem
			{
				Text = label,
				DisplayStyle = ToolStripItemDisplayStyle.Text,
				Image = image,
				CheckOnClick = true,
				Data = data
			};
			newItem.CheckStateChanged += ItemCheckStateChanged;
			if (isChecked)
			{
				if (!_multiCheckAllowed)
				{
					_updateInProgress = true;
					UncheckAll();
					_updateInProgress = false;
				}
				newItem.Checked = isChecked;
			}
			DropDownItems.Add(newItem);
		}

		/// <summary>
		///     adds an item to the select list
		/// </summary>
		/// <param name="label">the label to be displayed</param>
		/// <param name="image">the icon to be displayed</param>
		public void AddItem(string label, Image image)
		{
			AddItem(label, image, null, false);
		}

		/// <summary>
		///     adds an item to the select list
		/// </summary>
		/// <param name="label">the label to be displayed</param>
		/// <param name="data">the data to be returned when an item is queried</param>
		public void AddItem(string label, object data)
		{
			AddItem(label, null, data, false);
		}

		/// <summary>
		///     adds an item to the select list
		/// </summary>
		/// <param name="label">the label to be displayed</param>
		public void AddItem(string label)
		{
			AddItem(label, null, null, false);
		}


		// <summary>
		/// adds an item to the select list
		/// </summary>
		/// <param name="label">the label to be displayed</param>
		/// <param name="image">the icon to be displayed</param>
		/// <param name="selected">whether the item is initially selected</param>
		/// <param name="isChecked">whether the item is initially checked</param>
		public void AddItem(string label, Image image, bool isChecked)
		{
			AddItem(label, image, null, isChecked);
		}

		/// <summary>
		///     adds an item to the select list
		/// </summary>
		/// <param name="label">the label to be displayed</param>
		/// <param name="data">the data to be returned when an item is queried</param>
		/// <param name="isChecked">whether the item is initially checked</param>
		public void AddItem(string label, object data, bool isChecked)
		{
			AddItem(label, null, data, isChecked);
		}

		/// <summary>
		///     adds an item to the select list
		/// </summary>
		/// <param name="label">the label to be displayed</param>
		/// <param name="isChecked">whether the item is initially checked</param>
		public void AddItem(string label, bool isChecked)
		{
			AddItem(label, null, null, isChecked);
		}

		/// <summary>
		///     Occurs when one of the list's child element's Checked state changes.
		/// </summary>
		public new event EventHandler CheckedChanged;

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
		///     unchecks all items of the list
		/// </summary>
		public void UncheckAll()
		{
			IEnumerator items = DropDownItems.GetEnumerator();
			while (items.MoveNext())
			{
				((ToolStripMenuSelectListItem) items.Current).Checked = false;
			}
		}
	}

	/// <summary>
	///     Event class for the CheckedChanged event in the ToolStripMenuSelectList
	/// </summary>
	public class ItemCheckedChangedEventArgs : EventArgs
	{
		public ItemCheckedChangedEventArgs(ToolStripMenuSelectListItem item)
		{
			Item = item;
		}

		public ToolStripMenuSelectListItem Item { get; set; }
	}

	/// <summary>
	///     Wrapper around the ToolStripMenuItem, which can contain an object
	///     Also the Checked property hides the normal checked property so we can render our own check
	/// </summary>
	public class ToolStripMenuSelectListItem : ToolStripMenuItem
	{
		public object Data { get; set; }
	}
}