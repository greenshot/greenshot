/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Text;
using System.IO;

using Microsoft.Win32;

using Greenshot.Interop;
using Greenshot.Interop.IE;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GreenshotPlugin.Core;

namespace Greenshot.Interop.Office {
	/// <summary>
	/// Outlook exporter has all the functionality to export to outlook
	/// </summary>
	public class OutlookEmailExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OutlookEmailExporter));
		private static readonly string SIGNATURE_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Signatures");
		private static Version outlookVersion = null;
		private static string currentUser = null;
		private const int OUTLOOK_2003 = 11;
		private const int OUTLOOK_2007 = 12;
		private const int OUTLOOK_2010 = 14;

		// The signature key can be found at:
		// HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles\<DefaultProfile>\9375CFF0413111d3B88A00104B2A6676\<xxxx> [New Signature]
		private const string PROFILES_KEY = @"Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles\";
		private const string ACCOUNT_KEY = "9375CFF0413111d3B88A00104B2A6676";
		private const string NEW_SIGNATURE_VALUE = "New Signature";
		private const string DEFAULT_PROFILE_VALUE = "DefaultProfile";

		/// <summary>
		/// A method to retrieve all inspectors which can act as an export target
		/// </summary>
		/// <param name="allowMeetingAsTarget">bool true if also exporting to meetings</param>
		/// <returns>List<string> with inspector captions (window title)</returns>
		public static Dictionary<string, OlObjectClass> RetrievePossibleTargets(bool allowMeetingAsTarget) {
			Dictionary<string, OlObjectClass> inspectorCaptions = new Dictionary<string, OlObjectClass>();
			try {
				using (IOutlookApplication outlookApplication = GetOutlookApplication()) {
					if (outlookApplication == null) {
						return null;
					}

					using (Inspectors inspectors = outlookApplication.Inspectors) {
						if (inspectors != null && inspectors.Count > 0) {
							for (int i = 1; i <= inspectors.Count; i++) {
								using (Inspector inspector = outlookApplication.Inspectors[i]) {
									string inspectorCaption = inspector.Caption;
									using (Item currentItem = inspector.CurrentItem) {
										if (canExportToInspector(currentItem, allowMeetingAsTarget)) {
											OlObjectClass currentItemClass = currentItem.Class;
											inspectorCaptions.Add(inspector.Caption, currentItemClass);
										}
									}
								}
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
		/// Return true if we can export to the supplied inspector
		/// </summary>
		/// <param name="currentItem">the Item to check</param>
		/// <param name="allowMeetingAsTarget">bool true if also exporting to meetings</param>
		/// <returns></returns>
		private static bool canExportToInspector(Item currentItem, bool allowMeetingAsTarget) {
			try {
				if (currentItem != null) {
					OlObjectClass currentItemClass = currentItem.Class;
					if (OlObjectClass.olMail.Equals(currentItemClass)) {
						MailItem mailItem = (MailItem)currentItem;
						//MailItem mailItem = COMWrapper.Cast<MailItem>(currentItem);
						LOG.DebugFormat("Mail sent: {0}", mailItem.Sent);
						if (!mailItem.Sent) {
							return true;
						}
					} else if (outlookVersion.Major >= OUTLOOK_2010 && allowMeetingAsTarget && OlObjectClass.olAppointment.Equals(currentItemClass)) {
						//AppointmentItem appointmentItem = COMWrapper.Cast<AppointmentItem>(currentItem);
						AppointmentItem appointmentItem = (AppointmentItem)currentItem;
						if (string.IsNullOrEmpty(appointmentItem.Organizer) || (currentUser != null && currentUser.Equals(appointmentItem.Organizer))) {
							return true;
						} else {
							LOG.DebugFormat("Not exporting, as organizer is {1} and currentuser {2}", appointmentItem.Organizer, currentUser);
						}
					}
				}
			} catch (Exception ex) {
				LOG.WarnFormat("Couldn't process item due to: {0}", ex.Message);
			}
			return false;
		}


		/// <summary>
		/// Export the image stored in tmpFile to the Inspector with the caption
		/// </summary>
		/// <param name="inspectorCaption">Caption of the inspector</param>
		/// <param name="tmpFile">Path to image file</param>
		/// <param name="attachmentName">name of the attachment (used as the tooltip of the image)</param>
		/// <returns>true if it worked</returns>
		public static bool ExportToInspector(string inspectorCaption, string tmpFile, string attachmentName) {
			// Assume true, although this might cause issues.
			bool allowMeetingAsTarget = true;
			using (IOutlookApplication outlookApplication = GetOrCreateOutlookApplication()) {
				if (outlookApplication != null) {
					Inspectors inspectors = outlookApplication.Inspectors;
					if (inspectors != null && inspectors.Count > 0) {
						LOG.DebugFormat("Got {0} inspectors to check", inspectors.Count);
						for (int i = 1; i <= inspectors.Count; i++) {
							using (Inspector inspector = outlookApplication.Inspectors[i]) {
								string currentCaption = inspector.Caption;
								if (currentCaption.StartsWith(inspectorCaption)) {
									using (Item currentItem = inspector.CurrentItem) {
										if (canExportToInspector(currentItem, allowMeetingAsTarget)) {
											try {
												return ExportToInspector(inspector, currentItem, tmpFile, attachmentName);
											} catch (Exception exExport) {
												LOG.Error("Export to " + currentCaption + " failed.", exExport);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Export the file to the supplied inspector
		/// </summary>
		/// <param name="inspector">Inspector</param>
		/// <param name="currentItem">Item</param>
		/// <param name="tmpFile"></param>
		/// <param name="attachmentName"></param>
		/// <returns></returns>
		private static bool ExportToInspector(Inspector inspector, Item currentItem, string tmpFile, string attachmentName) {
			if (currentItem == null) {
				LOG.Warn("No current item.");
				return false;
			}
			OlObjectClass itemClass = currentItem.Class;
			bool isMail = OlObjectClass.olMail.Equals(itemClass);
			bool isAppointment = OlObjectClass.olAppointment.Equals(itemClass);
			if (!isMail && !isAppointment) {
				LOG.Warn("Item is no mail or appointment.");
				return false;
			}
			MailItem mailItem = null;
			try {
				if (isMail) {
					//mailItem = COMWrapper.Cast<MailItem>(currentItem);
					mailItem = (MailItem)currentItem;
					if (mailItem.Sent) {
						LOG.WarnFormat("Item already sent, can't export to {0}", currentItem.Subject);
						return false;
					}
				}

				// Make sure the inspector is activated, only this way the word editor is active!
				// This also ensures that the window is visible!
				inspector.Activate();
				bool isTextFormat = OlBodyFormat.olFormatPlain.Equals(mailItem.BodyFormat);
				if (isAppointment || !isTextFormat) {
					// Check for wordmail, if so use the wordexporter
					// http://msdn.microsoft.com/en-us/library/dd492012%28v=office.12%29.aspx
					// Earlier versions of Outlook also supported an Inspector.HTMLEditor object property, but since Internet Explorer is no longer the rendering engine for HTML messages and posts, HTMLEditor is no longer supported.
					if (inspector.IsWordMail() && inspector.WordEditor != null) {
						try {
							if (WordExporter.InsertIntoExistingDocument(inspector.WordEditor.Application, inspector.WordEditor, tmpFile, null, null)) {
								LOG.Info("Inserted into Wordmail");

								// check the format afterwards, otherwise we lose the selection
								//if (!OlBodyFormat.olFormatHTML.Equals(currentMail.BodyFormat)) {
								//	LOG.Info("Changing format to HTML.");
								//	currentMail.BodyFormat = OlBodyFormat.olFormatHTML;
								//}
								return true;
							}
						} catch (Exception exportException) {
							LOG.Error("Error exporting to the word editor, trying to do it via another method", exportException);
						}
					} else if (isAppointment) {
						LOG.Info("Can't export to an appointment if no word editor is used");
						return false;
					} else {
						LOG.Info("Trying export for outlook < 2007.");
					}
				}
				// Only use mailitem as it should be filled!!
				LOG.InfoFormat("Item '{0}' has format: {1}", mailItem.Subject, mailItem.BodyFormat);

				string contentID;
				if (outlookVersion.Major >= OUTLOOK_2007) {
					contentID = Guid.NewGuid().ToString();
				} else {
					LOG.Info("Older Outlook (<2007) found, using filename as contentid.");
					contentID = Path.GetFileName(tmpFile);
				}

				// Use this to change the format, it will probably lose the current selection.
				//if (!OlBodyFormat.olFormatHTML.Equals(currentMail.BodyFormat)) {
				//	LOG.Info("Changing format to HTML.");
				//	currentMail.BodyFormat = OlBodyFormat.olFormatHTML;
				//}

				bool inlinePossible = false;
				if (OlBodyFormat.olFormatHTML.Equals(mailItem.BodyFormat)) {
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
									range.pasteHTML("<BR/><IMG border=0 hspace=0 alt=\"" + attachmentName + "\" align=baseline src=\"cid:" + contentID + "\"><BR/>");
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
						// Continue with non inline image
						LOG.Warn("Error pasting HTML, most likely due to an ACCESS_DENIED as the user clicked no.", e);
					}
				}

				// Create the attachment (if inlined the attachment isn't visible as attachment!)
				using (Attachment attachment = mailItem.Attachments.Add(tmpFile, OlAttachmentType.olByValue, inlinePossible ? 0 : 1, attachmentName)) {
					if (outlookVersion.Major >= OUTLOOK_2007) {
						// Add the content id to the attachment, this only works for Outlook >= 2007
						try {
							PropertyAccessor propertyAccessor = attachment.PropertyAccessor;
							propertyAccessor.SetProperty(PropTag.ATTACHMENT_CONTENT_ID, contentID);
						} catch {
						}
					}
				}
			} catch (Exception ex) {
				LOG.WarnFormat("Problem while trying to add attachment to Item '{0}' : {1}", inspector.Caption, ex);
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
		private static void ExportToNewEmail(IOutlookApplication outlookApplication, EmailFormat format, string tmpFile, string subject, string attachmentName, string to, string CC, string BCC, string url) {
			Item newItem = outlookApplication.CreateItem(OlItemType.olMailItem);
			if (newItem == null) {
				return;
			}
			//MailItem newMail = COMWrapper.Cast<MailItem>(newItem);
			MailItem newMail = (MailItem)newItem;
			newMail.Subject = subject;
			if (!string.IsNullOrEmpty(to)) {
				newMail.To = to;
			}
			if (!string.IsNullOrEmpty(CC)) {
				newMail.CC = CC;
			}
			if (!string.IsNullOrEmpty(BCC)) {
				newMail.BCC = BCC;
			}
			newMail.BodyFormat = OlBodyFormat.olFormatHTML;
			string bodyString = null;
			// Read the default signature, if nothing found use empty email
			try {
				bodyString = GetOutlookSignature(format);
			} catch (Exception e) {
				LOG.Error("Problem reading signature!", e);
			}
			switch (format) {
				case EmailFormat.Text:
					newMail.Attachments.Add(tmpFile, OlAttachmentType.olByValue, 1, attachmentName);
					newMail.BodyFormat = OlBodyFormat.olFormatPlain;
					if (bodyString == null) {
						bodyString = "";
					}
					newMail.Body = bodyString;
					break;
				case EmailFormat.HTML:
				default:
					string contentID = Path.GetFileName(tmpFile);
					// Create the attachment
					using (Attachment attachment = newMail.Attachments.Add(tmpFile, OlAttachmentType.olByValue, 0, attachmentName)) {
						// add content ID to the attachment
						if (outlookVersion.Major >= OUTLOOK_2007) {
							try {
								contentID = Guid.NewGuid().ToString();
								PropertyAccessor propertyAccessor = attachment.PropertyAccessor;
								propertyAccessor.SetProperty(PropTag.ATTACHMENT_CONTENT_ID, contentID);
							} catch {
								LOG.Info("Error working with the PropertyAccessor, using filename as contentid");
								contentID = Path.GetFileName(tmpFile);
							}
						}
					}

					newMail.BodyFormat = OlBodyFormat.olFormatHTML;
					string href = "";
					string hrefEnd = "";
					if (!string.IsNullOrEmpty(url)) {
						href = string.Format("<A HREF=\"{0}\">", url);
						hrefEnd = "</A>";
					}
					string htmlImgEmbedded = string.Format("<BR/>{0}<IMG border=0 hspace=0 alt=\"{1}\" align=baseline src=\"cid:{2}\"><BR/>", href, attachmentName, contentID, hrefEnd);
					string fallbackBody = string.Format("<HTML><BODY>{0}</BODY></HTML>", htmlImgEmbedded);
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
			newMail.GetInspector().Activate();

			if (newItem != null) {
				newItem.Dispose();
			}
		}

		/// <summary>
		/// Helper method to create an outlook mail item with attachment
		/// </summary>
		/// <param name="tmpfile">The file to send, do not delete the file right away!</param>
		/// <returns>true if it worked, false if not</returns>
		public static bool ExportToOutlook(EmailFormat format, string tmpFile, string subject, string attachmentName, string to, string CC, string BCC, string url) {
			bool exported = false;
			try {
				using (IOutlookApplication outlookApplication = GetOrCreateOutlookApplication()) {
					if (outlookApplication != null) {
						ExportToNewEmail(outlookApplication, format, tmpFile, subject, attachmentName, to, CC, BCC, url);
						exported = true;
					}
				}
				return exported;
			} catch (Exception e) {
				LOG.Error("Error while creating an outlook mail item: ", e);
			}
			return exported;
		}

		/// <summary>
		/// Helper method to get the Outlook signature
		/// </summary>
		/// <returns></returns>
		private static string GetOutlookSignature(EmailFormat format) {
			using (RegistryKey profilesKey = Registry.CurrentUser.OpenSubKey(PROFILES_KEY, false)) {
				if (profilesKey == null) {
					return null;
				}
				string defaultProfile = (string)profilesKey.GetValue(DEFAULT_PROFILE_VALUE);
				LOG.DebugFormat("defaultProfile={0}", defaultProfile);
				using (RegistryKey profileKey = profilesKey.OpenSubKey(defaultProfile + @"\" + ACCOUNT_KEY, false)) {
					if (profilesKey == null) {
						return null;
					}
					string[] numbers = profileKey.GetSubKeyNames();
					foreach (string number in numbers) {
						LOG.DebugFormat("Found subkey {0}", number);
						using (RegistryKey numberKey = profileKey.OpenSubKey(number, false)) {
							byte[] val = (byte[])numberKey.GetValue(NEW_SIGNATURE_VALUE);
							if (val == null) {
								continue;
							}
							string signatureName = "";
							foreach (byte b in val) {
								if (b != 0) {
									signatureName += (char)b;
								}
							}
							LOG.DebugFormat("Found email signature: {0}", signatureName);
							string extension;
							switch (format) {
								case EmailFormat.Text:
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

		/// <summary>
		/// Initialize static outlook variables like version and currentuser
		/// </summary>
		/// <param name="outlookApplication"></param>
		private static void InitializeVariables(IOutlookApplication outlookApplication) {
			if (outlookApplication == null || outlookVersion != null) {
				return;
			}
			try {
				outlookVersion = new Version(outlookApplication.Version);
				LOG.InfoFormat("Using Outlook {0}", outlookVersion);
			} catch (Exception exVersion) {
				LOG.Error(exVersion);
				LOG.Warn("Assuming outlook version 1.");
				outlookVersion = new Version(1, 1, 1, 1);
			}
			// Preventing retrieval of currentUser if Outlook is older than 2007
			if (outlookVersion.Major >= OUTLOOK_2007) {
				try {
					INameSpace mapiNamespace = outlookApplication.GetNameSpace("MAPI");
					currentUser = mapiNamespace.CurrentUser.Name;
					LOG.InfoFormat("Current user: {0}", currentUser);
				} catch (Exception exNS) {
					LOG.Error(exNS);
				}
			}
		}

		/// <summary>
		/// Call this to get the running outlook application, returns null if there isn't any.
		/// </summary>
		/// <returns>IOutlookApplication or null</returns>
		private static IOutlookApplication GetOutlookApplication() {
			IOutlookApplication outlookApplication = COMWrapper.GetInstance<IOutlookApplication>();
			InitializeVariables(outlookApplication);
			return outlookApplication;
		}

		/// <summary>
		/// Call this to get the running outlook application, or create a new instance
		/// </summary>
		/// <returns>IOutlookApplication</returns>
		private static IOutlookApplication GetOrCreateOutlookApplication() {
			IOutlookApplication outlookApplication = COMWrapper.GetOrCreateInstance<IOutlookApplication>();
			InitializeVariables(outlookApplication);
			return outlookApplication;
		}
	}

}
