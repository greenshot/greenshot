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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;

using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace GreenshotOfficeCommunicatorPlugin  {
	/// <summary>
	/// Description of OfficeCommunicatorDestination.
	/// </summary>
	public class OfficeCommunicatorDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OfficeCommunicatorDestination));
		private static OfficeCommunicatorConfiguration config = IniConfig.GetIniSection<OfficeCommunicatorConfiguration>();
		private OfficeCommunicatorPlugin plugin = null;
		private static string exePath = null;
		private static Image applicationIcon = null;
		private CommunicatorConversation conversation = null;

		static OfficeCommunicatorDestination() {
			exePath = PluginUtils.GetExePath("communicator.exe");
			if (exePath != null && File.Exists(exePath)) {
				applicationIcon = PluginUtils.GetExeIcon(exePath, 0);
			} else {
				exePath = null;
			}
		}
		public OfficeCommunicatorDestination(OfficeCommunicatorPlugin plugin) {
			this.plugin = plugin;
		}

		public OfficeCommunicatorDestination(OfficeCommunicatorPlugin plugin, CommunicatorConversation conversation) : this(plugin) {
			this.conversation = conversation;
		}
		
		public override string Designation {
			get {
				return "OfficeCommunicator";
			}
		}

		public override string Description {
			get {
				if (conversation == null) {
					return Language.GetString("officecommunicator", LangKey.upload_menu_item);
				} else {
					return Language.GetString("officecommunicator", LangKey.upload_menu_item) + " - " + conversation.Title;
				}
			}
		}

		public override Image DisplayIcon {
			get {
				return applicationIcon;
			}
		}

		public override bool isActive {
			get {
				return base.isActive && exePath != null && ((conversation == null && plugin.Communicator.hasConversations) || (conversation != null && conversation.isActive));
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			foreach (CommunicatorConversation conversation in plugin.Communicator.Conversations) {
				if (conversation.isActive) {
					yield return new OfficeCommunicatorDestination(plugin, conversation);
				}
			}
		}

		public override bool isDynamic {
			get {
				return true;
			}
		}

		public override bool useDynamicsOnly {
			get {
				return true;
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			if (conversation != null) {
				ExportInformation internalExportInformation = plugin.Host.ExportCapture(false, config.DestinationDesignation, surface, captureDetails);
				if (internalExportInformation != null && internalExportInformation.ExportMade) {
					exportInformation.ExportMade = true;
					if (!string.IsNullOrEmpty(internalExportInformation.Uri)) {
						conversation.SendTextMessage("Greenshot sends you: " + internalExportInformation.Uri);
					} else if (!string.IsNullOrEmpty(internalExportInformation.Filepath)) {
						conversation.SendTextMessage(@"Greenshot sends you: file://" + internalExportInformation.Filepath);
					}
				}
			}
			return exportInformation;
		}
	}
}
