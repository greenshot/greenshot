/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Windows.Forms;

using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace GreenshotOfficeCommunicatorPlugin {
	/// <summary>
	/// Description of OfficeCommunicatorConfiguration.
	/// </summary>
	[IniSection("OfficeCommunicator", Description="Greenshot OfficeCommunicator Plugin configuration")]
	public class OfficeCommunicatorConfiguration : IniSection {
		[IniProperty("Destination", Description = "The designation of the destination which is used to export via, the returned URL is used in the message.", DefaultValue = "FileDialog")]
		public string DestinationDesignation;

		/// <summary>
		/// A form for username/password
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		public bool ShowConfigDialog() {
			SettingsForm settingsForm = null;

			new PleaseWaitForm().ShowAndWait(OfficeCommunicatorPlugin.Attributes.Name, Language.GetString("officecommunicator", LangKey.communication_wait), 
				delegate() {
					settingsForm = new SettingsForm(this);
				}
			);
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK) {
				return true;
			}
			return false;
		}
	}
}
