﻿/*
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

using System.ComponentModel;
using System.Windows.Forms;
using Greenshot.Base.Controls;

namespace Greenshot.Editor.Controls
{
    /// <summary>
    /// A simple ToolStripDropDownButton implementing INotifyPropertyChanged for data binding.
    /// Also, when a DropDownItem is selected, the DropDownButton adopts its Tag and Image.
    /// The selected tag can be accessed via SelectedTag property.
    /// </summary>
    public class BindableToolStripDropDownButton : ToolStripDropDownButton, INotifyPropertyChanged, IGreenshotLanguageBindable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [Category("Greenshot"), DefaultValue(null), Description("Specifies key of the language file to use when displaying the text.")]
        public string LanguageKey { get; set; }

        public object SelectedTag
        {
            get
            {
                if (Tag == null && DropDownItems.Count > 0) Tag = DropDownItems[0].Tag;
                return Tag;
            }
            set { AdoptFromTag(value); }
        }

        protected override void OnDropDownItemClicked(ToolStripItemClickedEventArgs e)
        {
            ToolStripItem clickedItem = e.ClickedItem;
            if (Tag?.Equals(clickedItem.Tag) != true)
            {
                Tag = clickedItem.Tag;
                Image = clickedItem.Image;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedTag"));
            }

            base.OnDropDownItemClicked(e);
        }

        private void AdoptFromTag(object tag)
        {
            if (Tag?.Equals(tag) != true)
            {
                Tag = tag;
                foreach (ToolStripItem item in DropDownItems)
                {
                    if (item.Tag?.Equals(tag) == true)
                    {
                        Image = item.Image;
                        break;
                    }
                }

                Tag = tag;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedTag"));
            }
        }
    }
}