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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Greenshot.Helpers.IEInterop;
using Greenshot.Interop;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using IniFile;
using Microsoft.Win32;

/// <summary>
/// This utils class should help setting the content-id on the attachment for Outlook < 2007
/// But this somehow doesn't work yet
/// </summary>
namespace Greenshot.Helpers.OfficeInterop {
	/// <summary>
	/// Wrapper for Outlook.Application
	/// </summary>
	[ComProgId("Outlook.Application")]
	public interface IOutlookApplication : Common {
		string Name { get; }
		string Version { get; }
		Item CreateItem(OlItemType ItemType);
		object CreateItemFromTemplate(string TemplatePath, object InFolder);
		object CreateObject(string ObjectName);
		Inspector ActiveInspector();
		Inspectors Inspectors { get; }
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook.inspector_members.aspx
	public interface Inspector : Common {
		Item CurrentItem { get;  }
		OlEditorType EditorType { get;  }
		object ModifiedFormPages { get;  }
		void Close(OlInspectorClose SaveMode);
		void Display(object Modal);
		void HideFormPage(string PageName);
		bool IsWordMail();
		void SetCurrentFormPage(string PageName);
		void ShowFormPage(string PageName);
		object HTMLEditor { get;  }
		IWordDocument WordEditor { get;  }
		string Caption { get;  }
		int Height { get; set; }
		int Left { get; set; }
		int Top { get; set; }
		int Width { get; set; }
		OlWindowState WindowState { get; set; }
		void Activate();
		void SetControlItemProperty(object Control, string PropertyName);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook._application.inspectors.aspx
	public interface Inspectors : Common, Collection, IEnumerable {
		Inspector this[Object Index] { get; }
	}

	/// <summary>
	/// Outlook exporter has all the functionality to export to outlook
	/// </summary>
	public class OutlookExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OutlookExporter));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private static readonly string SIGNATURE_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Signatures");
		private static Version outlookVersion = new Version(1,1,1,1);

		// The signature key can be found at:
		// HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles\<DefaultProfile>\9375CFF0413111d3B88A00104B2A6676\<xxxx> [New Signature]
		private const string PROFILES_KEY = @"Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles\";
		private const string ACCOUNT_KEY = "9375CFF0413111d3B88A00104B2A6676";
		private const string NEW_SIGNATURE_VALUE = "New Signature";
		private const string DEFAULT_PROFILE_VALUE = "DefaultProfile";
		
		/// <summary>
		/// A method to retrieve all inspectors which can act as an export target
		/// </summary>
		/// <param name="outlookApplication">IOutlookApplication</param>
		/// <returns>List<string> with inspector captions (window title)</returns>
		public static List<string> RetrievePossibleTargets() {
			List<string> inspectorCaptions = new List<string>();
			try {
				using ( IOutlookApplication outlookApplication = GetOutlookApplication()) {
					if (outlookApplication == null) {
						return null;
					}
					Inspectors inspectors = outlookApplication.Inspectors;
					if (inspectors != null && inspectors.Count > 0) {
						LOG.DebugFormat("Got {0} inspectors to check", inspectors.Count);
						for(int i=1; i <= inspectors.Count; i++) {
							Inspector inspector = outlookApplication.Inspectors[i];
							LOG.DebugFormat("Checking inspector '{0}'", inspector.Caption);
							try {
								Item currentMail = inspector.CurrentItem;
								if (currentMail != null && OlObjectClass.olMail.Equals(currentMail.Class) ) {
									if (currentMail != null && !currentMail.Sent) {
										inspectorCaptions.Add(inspector.Caption);
									}
								}
							} catch (Exception) {
							}
						}
					}
				}
			} catch (Exception ex) {
				LOG.Warn("Problem retrieving word destinations, ignoring: ", ex);
			}
			return inspectorCaptions;

		}

	
		/// <summary>
		/// Export the image stored in tmpFile to the Inspector with the caption
		/// </summary>
		/// <param name="inspectorCaption">Caption of the inspector</param>
		/// <param name="tmpFile">Path to image file</param>
		/// <param name="attachmentName">name of the attachment (used as the tooltip of the image)</param>
		/// <returns>true if it worked</returns>
		public static bool ExportToInspector(string inspectorCaption, string tmpFile, string attachmentName) {
			using ( IOutlookApplication outlookApplication = GetOrCreateOutlookApplication()) {
				if (outlookApplication != null) {
					Inspectors inspectors = outlookApplication.Inspectors;
					if (inspectors != null && inspectors.Count > 0) {
						LOG.DebugFormat("Got {0} inspectors to check", inspectors.Count);
						for(int i=1; i <= inspectors.Count; i++) {
							Inspector inspector = outlookApplication.Inspectors[i];
							if (inspector.Caption.StartsWith(inspectorCaption)) {
								try {
									Item currentMail = inspector.CurrentItem;
									if (currentMail != null && OlObjectClass.olMail.Equals(currentMail.Class)) {
										if (currentMail != null && !currentMail.Sent) {
											return ExportToInspector(inspector, tmpFile, attachmentName);
										}
									}
								} catch (Exception) {
								}
							}
						}
					}
				}
			}
			return false;
		}
		
		private static bool ExportToInspector(Inspector inspector, string tmpFile, string attachmentName) {
			Item currentMail = inspector.CurrentItem;
			if (currentMail == null) {
				LOG.Debug("No current item.");
				return false;
			}
			if (!OlObjectClass.olMail.Equals(currentMail.Class)) {
				LOG.Debug("Item is no mail.");
				return false;
			}
			try {
				if (currentMail.Sent) {
					LOG.Debug("Item already sent");
					return false;
				}
				
				// Make sure the inspector is activated, only this way the word editor is active!
				// This also ensures that the window is visible!
				inspector.Activate();

				// Check for wordmail, if so use the wordexporter
				if (inspector.IsWordMail() && inspector.WordEditor != null) {
					if (WordExporter.InsertIntoExistingDocument(inspector.WordEditor, tmpFile)) {
						LOG.Debug("Inserted into Wordmail");
						return true;
					}
				} else {
					LOG.Debug("Wordmail editor is not supported");
				}
				
				LOG.DebugFormat("Email '{0}' has format: {1}", currentMail.Subject, currentMail.BodyFormat);
				
				string contentID;
				if (outlookVersion.Major >=12 ) {
					contentID = Guid.NewGuid().ToString();
				} else {
					LOG.Info("Older Outlook (<2007) found, using filename as contentid.");
					contentID = Path.GetFileName(tmpFile);
				}

				bool inlinePossible = false;
				if (OlBodyFormat.olFormatHTML.Equals(currentMail.BodyFormat)) {
					// if html we can try to inline it
					// The following might cause a security popup... can't ignore it.
					try {
						IHTMLDocument2 document2 = inspector.HTMLEditor as IHTMLDocument2;
						if (document2 != null) {
							IHTMLSelectionObject selection = document2.selection;
							if (selection != null) {
								IHTMLTxtRange range = selection.createRange();
								if (range != null) {
									// First paste, than attach (otherwise the range is wrong!)
									range.pasteHTML("<BR/><IMG border=0 hspace=0 alt=\"" + attachmentName + "\" align=baseline src=\"cid:"+ contentID +"\"><BR/>");
									inlinePossible = true;
								} else {
									LOG.DebugFormat("No range for '{0}'", inspector.Caption);
								}
							} else {
								LOG.DebugFormat("No selection for '{0}'", inspector.Caption);
							}
						} else {
							LOG.DebugFormat("No HTML editor for '{0}'", inspector.Caption);
						}
					} catch (Exception e) {
						LOG.Warn("Error pasting HTML, most likely due to an ACCESS_DENIED as the user clicked no.", e);
						// Continue with non inline image
					}
				}
				
				// Create the attachment (if inlined the attachment isn't visible as attachment!)
				Attachment attachment = currentMail.Attachments.Add(tmpFile, OlAttachmentType.olByValue, inlinePossible?0:1, attachmentName);
				if (outlookVersion.Major >=12) {
					// Add the content id to the attachment
					try {
						PropertyAccessor propertyAccessor = attachment.PropertyAccessor;
						propertyAccessor.SetProperty(PropTag.ATTACHMENT_CONTENT_ID, contentID);
					} catch {
					}
				}
			} catch (Exception ex) {
				LOG.DebugFormat("Problem while trying to add attachment to  MailItem '{0}' : {1}", inspector.Caption, ex);
				return false;
			}
			LOG.Debug("Finished!");
			return true;
		}
		/// <summary>
		/// Export image to a new email
		/// </summary>
		/// <param name="outlookApplication"></param>
		/// <param name="tmpFile"></param>
		/// <param name="captureDetails"></param>
		private static void ExportToNewEmail(IOutlookApplication outlookApplication, string tmpFile, string subject, string attachmentName) {
			Item newMail = outlookApplication.CreateItem( OlItemType.olMailItem );
			if (newMail == null) {
				return;
			}
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
				case EmailFormat.OUTLOOK_TXT:
					newMail.Attachments.Add(tmpFile, OlAttachmentType.olByValue, 1, attachmentName);
					newMail.BodyFormat = OlBodyFormat.olFormatPlain;
					if (bodyString == null) {
						bodyString = "";
					}
					newMail.Body = bodyString;
					break;
				case EmailFormat.OUTLOOK_HTML:
				default:
					// Create the attachment
					Attachment attachment = newMail.Attachments.Add(tmpFile, OlAttachmentType.olByValue, 0, attachmentName);
					// add content ID to the attachment
					string contentID = Path.GetFileName(tmpFile);
					if (outlookVersion.Major >=12) {
						// Add the content id to the attachment
						try {
							contentID = Guid.NewGuid().ToString();
							PropertyAccessor propertyAccessor = attachment.PropertyAccessor;
							propertyAccessor.SetProperty(PropTag.ATTACHMENT_CONTENT_ID, contentID);
						} catch {
							LOG.Info("Error working with the PropertyAccessor, using filename as contentid");
							contentID = Path.GetFileName(tmpFile);
						}
					}
					
					newMail.BodyFormat = OlBodyFormat.olFormatHTML;
					string htmlImgEmbedded = "<BR/><IMG border=0 hspace=0 alt=\"" + attachmentName + "\" align=baseline src=\"cid:"+ contentID +"\"><BR/>";
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
		
		private static bool ExportToMail(IOutlookApplication outlookApplication, string tmpFile, string subject, string attachmentName) {
			bool exported = false;
			if (outlookApplication != null) {
				ExportToNewEmail(outlookApplication, tmpFile, subject, attachmentName);
				exported = true;
			}
			return exported;
		}

		/// <summary>
		/// Helper method to create the outlook mail item with attachment
		/// </summary>
		/// <param name="tmpfile">The file to send</param>
		/// <returns>true if it worked, false if not</returns>
		public static bool ExportToOutlook(string tmpFile, string subject, string attachmentName) {
			try {
				bool exported = false;
				
				using ( IOutlookApplication outlookApplication = GetOrCreateOutlookApplication()) {
					if (outlookApplication != null) {
						exported = ExportToMail(outlookApplication, tmpFile, subject, attachmentName);
					}
				}
				if (exported) {
					// Wait to make sure the system "imported" the file
					// TODO: this should be handled differently some time
					Thread.Sleep(600);
				}
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
								case EmailFormat.OUTLOOK_TXT:
									extension = ".txt";
									break;
								case EmailFormat.OUTLOOK_HTML:
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

		private static IOutlookApplication GetOutlookApplication() {
			IOutlookApplication outlookApplication = (IOutlookApplication)COMWrapper.GetInstance(typeof(IOutlookApplication));
			try {
				if (outlookApplication != null) {
					outlookVersion = new Version(outlookApplication.Version);
				}
			} catch {
			}
			return outlookApplication;
		}

		private static IOutlookApplication GetOrCreateOutlookApplication() {
			IOutlookApplication outlookApplication = (IOutlookApplication)COMWrapper.GetOrCreateInstance(typeof(IOutlookApplication));
			try {
				outlookVersion = new Version(outlookApplication.Version);
			} catch {
			}
			return outlookApplication;
		}
	}
}
