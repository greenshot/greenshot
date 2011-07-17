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
using System.IO;
using System.Text;
using System.Threading;

using Greenshot.Helpers.IEInterop;
using Greenshot.Interop;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Microsoft.Win32;

namespace Greenshot.Helpers.OfficeInterop {
	/// <summary>
	/// Wrapper for Outlook.Application
	/// </summary>
	[ComProgId("Outlook.Application")]
	public interface IOutlookApplication : Common {
		string Name { get; }
		string Version { get; }
		object CreateItem(OlItemType ItemType);
		object CreateItemFromTemplate(string TemplatePath, object InFolder);
		object CreateObject(string ObjectName);
		Inspector ActiveInspector();
	}

	/// <summary>
	/// Outlook exporter has all the functionality to export to outlook
	/// </summary>
	public class OutlookExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OutlookExporter));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();

		private static readonly string SIGNATURE_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Signatures");

		// The signature key can be found at:
		// HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles\<DefaultProfile>\9375CFF0413111d3B88A00104B2A6676\<xxxx> [New Signature]
		private const string PROFILES_KEY = @"Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles\";
		private const string ACCOUNT_KEY = "9375CFF0413111d3B88A00104B2A6676";
		private const string NEW_SIGNATURE_VALUE = "New Signature";
		private const string DEFAULT_PROFILE_VALUE = "DefaultProfile";
		private const string OUTLOOK_PATH_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\OUTLOOK.EXE";

		/// <summary>
		/// Check if Outlook is installed
		/// </summary>
		/// <returns>Returns true if outlook is installed</returns>
		public static bool HasOutlook() {
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(OUTLOOK_PATH_KEY, false)) {
				if (key != null) {
					// "" is the default key, which should point to the outlook location
					string outlookPath = (string)key.GetValue("");
					if (outlookPath != null) {
						if (File.Exists(outlookPath)) {
							return true;
						}
					}
				}
			}
			return false;
		}
		
		/// <summary>
		/// Export to currently opened Email
		/// </summary>
		/// <param name="activeInspector"></param>
		private static bool ExportToOpenEmail(Inspector activeInspector, string tmpFile, string subject) {
			object objectItem = activeInspector.CurrentItem;
			if (objectItem == null) {
				LOG.Debug("No current item");
				return false;
			}
			if (activeInspector.IsWordMail()) {
				IWordDocument wordDocument = activeInspector.WordEditor;
				if (wordDocument != null) {
					if (WordExporter.InsertIntoExistingDocument(wordDocument, tmpFile)) {
						LOG.Debug("Inserted into Wordmail");
						return true;
					}
				} else {
					LOG.Debug("Wordmail editor is not supported");
				}
			}
			using (MailItem currentMail = (MailItem)COMWrapper.Wrap(objectItem, typeof(MailItem))) {
				if (currentMail.Sent) {
					LOG.Debug("Item already sent");
					return false;
				}
				LOG.DebugFormat("Current email with format: {0}", currentMail.BodyFormat);

				if (OlBodyFormat.olFormatHTML.Equals(currentMail.BodyFormat)) {
					// if html we can inline it
					
					// This will cause a security popup... can't ignore it.
					try {
						IHTMLDocument2 document2 = (IHTMLDocument2)activeInspector.HTMLEditor;
						if (document2 == null) {
							return false;
						}
						IHTMLSelectionObject selection = document2.selection;
						if (selection == null) {
							return false;
						}
						IHTMLTxtRange range = selection.createRange();
						if (range == null) {
							return false;
						}
						// First paste, than attach (otherwise the range is wrong!)
						range.pasteHTML("<BR/><IMG border=0 hspace=0 alt=\"" + subject + "\" align=baseline src=\"cid:"+ Path.GetFileName(tmpFile) +"\"><BR/>");
					} catch (Exception e) {
						LOG.Warn("Error pasting HTML, most likely due to an ACCESS_DENIED as the user clicked no.", e);
						// Do not continue & add attachment, rather return and try something else
						return false;
					}
					currentMail.Attachments.Add(tmpFile, OlAttachmentType.olByValue, 0, subject);
				} else {
					// attach file to mail
					currentMail.Attachments.Add(tmpFile, OlAttachmentType.olByValue, 1, subject);
				}
				currentMail.Display(false);
			}
			return true;
		}
		
		/// <summary>
		/// Export image to a new email
		/// </summary>
		/// <param name="outlookApplication"></param>
		/// <param name="tmpFile"></param>
		/// <param name="captureDetails"></param>
		private static void ExportToNewEmail(IOutlookApplication outlookApplication, string tmpFile, string subject) {
			object obj = outlookApplication.CreateItem( OlItemType.olMailItem );
			using( MailItem newMail = (MailItem)COMWrapper.Wrap(obj, typeof( MailItem ) ) ) {
				newMail.Subject = subject;
				newMail.BodyFormat = OlBodyFormat.olFormatHTML;
				string bodyString = null;
				// Read the default signature, if nothing found use empty email
				try {
					bodyString = GetOutlookSignature();
				} catch (Exception e) {
					LOG.Error("Problem reading signature!", e);
				}
				switch(conf.OutputEMailFormat) {
					case EmailFormat.TXT:
						newMail.Attachments.Add(tmpFile, OlAttachmentType.olByValue, 1, subject);
						newMail.BodyFormat = OlBodyFormat.olFormatPlain;
						if (bodyString == null) {
							bodyString = "";
						}
						newMail.Body = bodyString;
						break;
					case EmailFormat.HTML:
					default:
						newMail.Attachments.Add(tmpFile, OlAttachmentType.olByValue, 0, subject);
						newMail.BodyFormat = OlBodyFormat.olFormatHTML;
						string htmlImgEmbedded = "<BR/><IMG border=0 hspace=0 alt=\"" + subject + "\" align=baseline src=\"cid:"+ Path.GetFileName(tmpFile) +"\"><BR/>";
						string fallbackBody = "<HTML><BODY>" + htmlImgEmbedded + "</BODY></HTML>";
						if (bodyString == null) {
							bodyString = fallbackBody;
						} else {
							int bodyIndex = bodyString.IndexOf("<body", StringComparison.CurrentCultureIgnoreCase);
							if (bodyIndex >= 0) {
								bodyIndex = bodyString.IndexOf(">", bodyIndex) + 1;
								if (bodyIndex >= 0) {
									bodyString = bodyString.Insert(bodyIndex, htmlImgEmbedded);
								} else {
									bodyString = fallbackBody;
								}
							} else {
								bodyString = fallbackBody;
							}
						}
						newMail.HTMLBody = bodyString;
						break;
				}
				// So not save, otherwise the email is always stored in Draft folder.. (newMail.Save();)
				newMail.Display(false);
			}
		}
		
		private static bool ExportToMail(IOutlookApplication outlookApplication, string tmpFile, string subject) {
			bool exported = false;
			if (outlookApplication != null) {
				using (Inspector activeInspector = outlookApplication.ActiveInspector()) {
					if (conf.OutputOutlookMethod == EmailExport.TryOpenElseNew && activeInspector != null) {
						LOG.Debug("EmailExport.TryOpenElseNew");
						exported = ExportToOpenEmail(activeInspector, tmpFile, subject);
					}
					if (!exported) {
						LOG.Debug("EmailExport.AllwaysNew");
						ExportToNewEmail(outlookApplication, tmpFile, subject);
						exported = true;
					}
				}
			}
			return exported;
		}

		/// <summary>
		/// Helper method to create the outlook mail item with attachment
		/// </summary>
		/// <param name="tmpfile">The file to send</param>
		/// <returns>true if it worked, false if not</returns>
		public static bool ExportToOutlook(string tmpFile, ICaptureDetails captureDetails) {
			try {
				bool exported = false;
				string subject = captureDetails.Title.Replace("\"","'");
				using ( IOutlookApplication outlookApplication = OutlookApplication()) {
					if (outlookApplication != null) {
						exported = ExportToMail(outlookApplication, tmpFile, subject);
					}
				}
				if (exported) {
					Thread.Sleep(400);
				}
				LOG.DebugFormat("Deleting {0}", tmpFile);
				File.Delete(tmpFile);
				return exported;
			} catch(Exception e) {
				LOG.Error("Error while creating an outlook mail item: ", e);
			}
			return false;
		}

		/// <summary>
		/// Helper method to get the Outlook signature
		/// </summary>
		/// <returns></returns>
		private static string GetOutlookSignature() {
			using (RegistryKey profilesKey = Registry.CurrentUser.OpenSubKey(PROFILES_KEY, false)) {
				if (profilesKey == null) {
					return null;
				}
				string defaultProfile = (string)profilesKey.GetValue(DEFAULT_PROFILE_VALUE);
				LOG.DebugFormat("defaultProfile={0}",defaultProfile);
				using (RegistryKey profileKey = profilesKey.OpenSubKey(defaultProfile + @"\" + ACCOUNT_KEY, false)) {
					if (profilesKey == null) {
						return null;
					}
					string [] numbers = profileKey.GetSubKeyNames();
					foreach(string number in numbers) {
						LOG.DebugFormat("Found subkey {0}", number);
						using (RegistryKey numberKey = profileKey.OpenSubKey(number, false)) {
							byte[] val = (byte[])numberKey.GetValue(NEW_SIGNATURE_VALUE);
							if (val == null) {
								continue;
							}
							string signatureName = "";
							foreach(byte b in val) {
								if (b != 0) {
									signatureName += (char) b;
								}
							}
							LOG.DebugFormat("Found email signature: {0}", signatureName);
							string extension;
							switch(conf.OutputEMailFormat) {
								case EmailFormat.TXT:
									extension = ".txt";
									break;
								case EmailFormat.HTML:
								default:
									extension = ".htm";
									break;
							}
							string signatureFile = Path.Combine(SIGNATURE_PATH, signatureName + extension);
							if (File.Exists(signatureFile)) {
								LOG.DebugFormat("Found email signature file: {0}", signatureFile);
								return File.ReadAllText(signatureFile, Encoding.Default);
							}
						}
					}
				}
			}
			return null;
		}

		private static IOutlookApplication OutlookApplication() {
			return (IOutlookApplication)COMWrapper.GetOrCreateInstance(typeof(IOutlookApplication));
		}
	}
}
