/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Windows.Forms;
using Greenshot.Plugin;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Description of PluginUtils.
	/// </summary>
	public class PluginUtils {
		private PluginUtils() {
		}
		
		/// <summary>
		/// Helper method to add a MenuItem to the File MenuItem of an ImageEditor
		/// </summary>
		/// <param name="imageEditor"></param>
		/// <param name="item"></param>
		public static void AddToFileMenu(IImageEditor imageEditor, ToolStripMenuItem item) {
			ToolStripMenuItem toolStripMenuItem = imageEditor.GetFileMenuItem();
			bool added = false;
			for(int i = 0; i< toolStripMenuItem.DropDownItems.Count; i++) {
				if (toolStripMenuItem.DropDownItems[i].GetType() == typeof(ToolStripSeparator)) {
					toolStripMenuItem.DropDownItems.Insert(i, item);
					added = true;
					break;
				}
			}
			if (!added) {
				toolStripMenuItem.DropDownItems.Add(item);
			}
		}
		
		/// <summary>
		/// Helper method to add a MenuItem to the Plugin MenuItem of an ImageEditor
		/// </summary>
		/// <param name="imageEditor"></param>
		/// <param name="item"></param>
		public static void AddToPluginMenu(IImageEditor imageEditor, ToolStripMenuItem item) {
			ToolStripMenuItem toolStripMenuItem = imageEditor.GetPluginMenuItem();
			bool added = false;
			for(int i = 0; i< toolStripMenuItem.DropDownItems.Count; i++) {
				if (toolStripMenuItem.DropDownItems[i].GetType() == typeof(ToolStripSeparator)) {
					toolStripMenuItem.DropDownItems.Insert(i, item);
					added = true;
					break;
				}
			}
			if (!added) {
				toolStripMenuItem.DropDownItems.Add(item);
			}
		}
	}
}
