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
using CommunicatorAPI;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using GreenshotPlugin.Core;

namespace GreenshotOfficeCommunicatorPlugin {

	public class Communicator {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(Communicator));
		private bool connected = false;

		private CommunicatorAPI.Messenger communicator = null;
		private Dictionary<long, CommunicatorConversation> communicatorConversations = new Dictionary<long, CommunicatorConversation>();
		public Communicator() {
		}
		public bool isConnected {
			get {
				return connected;
			}
		}

		public bool hasConversations {
			get {
				return communicatorConversations.Count > 0;
			}
		}

		public IEnumerable<CommunicatorConversation> Conversations {
			get {
				foreach (CommunicatorConversation conversation in communicatorConversations.Values) {
					yield return conversation;
				}
			}
		}

		// A simple implementation of signing in.
		public void Signin(string account, string passwd) {
			if (connected)
				return;

			if (communicator == null) {
				// Create a Messenger object, if necessary
				communicator = new CommunicatorAPI.Messenger();

				// Register event handlers for OnSignin and Signout events
				communicator.OnSignin += new DMessengerEvents_OnSigninEventHandler(communicator_OnSignin);
				communicator.OnSignout += new DMessengerEvents_OnSignoutEventHandler(communicator_OnSignout);
				communicator.OnIMWindowCreated += new DMessengerEvents_OnIMWindowCreatedEventHandler(communicator_OnIMWindowCreated);
				communicator.OnIMWindowDestroyed += new DMessengerEvents_OnIMWindowDestroyedEventHandler(communicator_OnIMWindowDestroyed);
			}

			if (account == null) {
				communicator.AutoSignin();
			} else {
				communicator.Signin(0, account, passwd);
			}
		}

		// Event handler for OnSignin event.
		void communicator_OnSignin(int hr) {
			if (hr != 0) {
				Console.WriteLine("Signin failed.");
				return;
			}
			connected = true;
		}

		void communicator_OnSignout() {
			connected = false;

			// Release the unmanaged resource.
			Marshal.ReleaseComObject(communicator);
			communicator = null;
		}

		// Register for IMWindowCreated event to receive the
		// conversation window object. Other window objects can
		// received via this event handling as well.
		void communicator_OnIMWindowCreated(object pIMWindow) {
			try {
				IMessengerConversationWndAdvanced imWindow = (IMessengerConversationWndAdvanced)pIMWindow;
				CommunicatorConversation conversation = null;
				if (communicatorConversations.ContainsKey(imWindow.HWND)) {
					conversation = communicatorConversations[imWindow.HWND];
				} else {
					conversation = new CommunicatorConversation(imWindow.HWND);
					communicatorConversations.Add(imWindow.HWND, conversation);
				}
				conversation.ImWindow = imWindow;
			} catch (Exception exception) {
				LOG.Error(exception);
			}
		}

		void communicator_OnIMWindowDestroyed(object pIMWindow) {
			try {
				IMessengerConversationWndAdvanced imWindow = (IMessengerConversationWndAdvanced)pIMWindow;
				CommunicatorConversation foundConversation = null;
				long foundHwnd = 0;
				foreach (long hwndKey in communicatorConversations.Keys) {
					if (imWindow.Equals(communicatorConversations[hwndKey].ImWindow)) {
						foundConversation = communicatorConversations[hwndKey];
						foundConversation.ImWindow = null;
						foundHwnd = hwndKey;
						break;
					}
				}
				if (foundConversation != null) {
					communicatorConversations.Remove(foundHwnd);
				}
				Marshal.ReleaseComObject(pIMWindow);
			} catch (Exception exception) {
				LOG.Error(exception);
			}
		}

		public CommunicatorConversation StartConversation(string account) {
			object[] sipUris = { account };
			CommunicatorAPI.IMessengerAdvanced msgrAdv = communicator as CommunicatorAPI.IMessengerAdvanced;
			CommunicatorConversation communicatorConversation = null;
			if (msgrAdv != null) {
				try {
					object obj = msgrAdv.StartConversation(
					   CONVERSATION_TYPE.CONVERSATION_TYPE_IM, // Type of conversation
					   sipUris, // object array of signin names for having multiple conversations
					   null,
					   "Testing",
					   "1",
					   null);
					long hwnd = long.Parse(obj.ToString());
					if (!communicatorConversations.ContainsKey(hwnd)) {
						communicatorConversations.Add(hwnd, new CommunicatorConversation(hwnd));
					}
					communicatorConversation = communicatorConversations[hwnd]; 
				} catch (Exception ex) {
					LOG.Error(ex);
				}
			}

			return communicatorConversation;
		}


		public void ShowContacts() {
			// Display contacts to a console window(for illustration only).
			foreach (IMessengerContact contact in communicator.MyContacts as IMessengerContacts) {
				if (!contact.IsSelf) {
					Console.WriteLine("{0} ({1})", contact.SigninName, contact.Status);
				}
			}
		}

		// A simple implementation of signing out.
		public void Signout() {
			if (!connected) {
				return;
			}

			if (communicator == null) {
				return;
			}

			communicator.Signout();
		}
	}
}
