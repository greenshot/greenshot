/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Greenshot.IniFile;
using Greenshot.Interop;
using Greenshot.Interop.Office;
using mshtml;
using Microsoft.Win32;

namespace GreenshotOfficePlugin.OfficeExport {
	/// <summary>
	/// Outlook exporter has all the functionality to export to outlook
	/// </summary>
	public class OutlookEmailExporter {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(OutlookEmailExporter));
		private static readonly OfficeConfiguration OfficeConfig = IniConfig.GetIniSection<OfficeConfiguration>();
		private static readonly string SignaturePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Signatures");
		private static Version _outlookVersion;
		private static string _currentUser;

		// The signature key can be found at:
		// HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles\<DefaultProfile>\9375CFF0413111d3B88A00104B2A6676\<xxxx> [New Signature]
		private const string ProfilesKey = @"Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles\";
		private const string AccountKey = "9375CFF0413111d3B88A00104B2A6676";
		private const string NewSignatureValue = "New Signature";
		private const string DefaultProfileValue = "DefaultProfile";

		/// <summary>
		/// A method to retrieve all inspectors which can act as an export target
		/// </summary>
		/// <returns>List of strings with inspector captions (window title)</returns>
		public static IDictionary<string, OlObjectClass> RetrievePossibleTargets() {
			IDictionary<string, OlObjectClass> inspectorCaptions = new SortedDictionary<string, OlObjectClass>();
			try {
				using (IOutlookApplication outlookApplication = GetOutlookApplication()) {
					if (outlookApplication == null) {
						return null;
					}

					if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2013) {
						// Check inline "panel" for Outlook 2013
						using (var activeExplorer = outlookApplication.ActiveExplorer()) {
							if (activeExplorer != null) {
								using (var inlineResponse = activeExplorer.ActiveInlineResponse) {
									if (CanExportToInspector(inlineResponse)) {
										OlObjectClass currentItemClass = inlineResponse.Class;
										inspectorCaptions.Add(activeExplorer.Caption, currentItemClass);
									}
								}
							}
						}
					}

					using (IInspectors inspectors = outlookApplication.Inspectors) {
						if (inspectors != null && inspectors.Count > 0) {
							for (int i = 1; i <= inspectors.Count; i++) {
								using (IInspector inspector = outlookApplication.Inspectors[i]) {
									using (IItem currentItem = inspector.CurrentItem) {
										if (CanExportToInspector(currentItem)) {
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
				Log.Warn("Problem retrieving word destinations, ignoring: ", ex);
			}
			return inspectorCaptions;
		}

		/// <summary>
		/// Return true if we can export to the supplied inspector
		/// </summary>
		/// <param name="currentItem">the Item to check</param>
		/// <returns></returns>
		private static bool CanExportToInspector(IItem currentItem) {
			try {
				if (currentItem != null) {
					OlObjectClass currentItemClass = currentItem.Class;
					if (OlObjectClass.olMail.Equals(currentItemClass)) {
						MailItem mailItem = (MailItem)currentItem;
						//MailItem mailItem = COMWrapper.Cast<MailItem>(currentItem);
						Log.DebugFormat("Mail sent: {0}", mailItem.Sent);
						if (!mailItem.Sent) {
							return true;
						}
					} else if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2010 && OfficeConfig.OutlookAllowExportInMeetings && OlObjectClass.olAppointment.Equals(currentItemClass)) {
						//AppointmentItem appointmentItem = COMWrapper.Cast<AppointmentItem>(currentItem);
						AppointmentItem appointmentItem = (AppointmentItem)currentItem;
						if (string.IsNullOrEmpty(appointmentItem.Organizer) || (_currentUser != null && _currentUser.Equals(appointmentItem.Organizer))) {
							return true;
						}
						Log.DebugFormat("Not exporting, as organizer is {0} and currentuser {1}", appointmentItem.Organizer, _currentUser);
					}
				}
			} catch (Exception ex) {
				Log.WarnFormat("Couldn't process item due to: {0}", ex.Message);
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
			using (IOutlookApplication outlookApplication = GetOrCreateOutlookApplication()) {
				if (outlookApplication == null) {
					return false;
				}
				if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2013) {
					// Check inline "panel" for Outlook 2013
					using (var activeExplorer = outlookApplication.ActiveExplorer()) {
						if (activeExplorer == null) {
							return false;
						}
						var currentCaption = activeExplorer.Caption;
						if (currentCaption.StartsWith(inspectorCaption)) {
							using (var inlineResponse = activeExplorer.ActiveInlineResponse) {
								using (IItem currentItem = activeExplorer.ActiveInlineResponse) {
									if (CanExportToInspector(inlineResponse)) {
										try {
											return ExportToInspector(activeExplorer, currentItem, tmpFile, attachmentName);
										} catch (Exception exExport) {
											Log.Error("Export to " + currentCaption + " failed.", exExport);
										}
									}
								}
							}
						}
					}
				}

				using (IInspectors inspectors = outlookApplication.Inspectors) {
					if (inspectors == null || inspectors.Count == 0) {
						return false;
					}
					Log.DebugFormat("Got {0} inspectors to check", inspectors.Count);
					for (int i = 1; i <= inspectors.Count; i++) {
						using (IInspector inspector = outlookApplication.Inspectors[i]) {
							string currentCaption = inspector.Caption;
							if (currentCaption.StartsWith(inspectorCaption)) {
								using (IItem currentItem = inspector.CurrentItem) {
									if (CanExportToInspector(currentItem)) {
										try {
											return ExportToInspector(inspector, currentItem, tmpFile, attachmentName);
										} catch (Exception exExport) {
											Log.Error("Export to " + currentCaption + " failed.", exExport);
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
		/// <param name="inspectorOrExplorer">ICommonExplorer</param>
		/// <param name="currentItem">Item</param>
		/// <param name="tmpFile"></param>
		/// <param name="attachmentName"></param>
		/// <returns></returns>
		private static bool ExportToInspector(ICommonExplorer inspectorOrExplorer, IItem currentItem, string tmpFile, string attachmentName) {
			if (currentItem == null) {
				Log.Warn("No current item.");
				return false;
			}
			OlObjectClass itemClass = currentItem.Class;
			bool isMail = OlObjectClass.olMail.Equals(itemClass);
			bool isAppointment = OlObjectClass.olAppointment.Equals(itemClass);
			if (!isMail && !isAppointment) {
				Log.Warn("Item is no mail or appointment.");
				return false;
			}
			MailItem mailItem = null;
			try {
				if (isMail) {
					//mailItem = COMWrapper.Cast<MailItem>(currentItem);
					mailItem = (MailItem)currentItem;
					if (mailItem.Sent) {
						Log.WarnFormat("Item already sent, can't export to {0}", currentItem.Subject);
						return false;
					}
				}

				// Make sure the inspector is activated, only this way the word editor is active!
				// This also ensures that the window is visible!
				inspectorOrExplorer.Activate();
				bool isTextFormat = false;
				if (isMail) {
					isTextFormat = OlBodyFormat.olFormatPlain.Equals(mailItem.BodyFormat);
				}
				if (isAppointment || !isTextFormat) {
					// Check for wordmail, if so use the wordexporter
					// http://msdn.microsoft.com/en-us/library/dd492012%28v=office.12%29.aspx
					// Earlier versions of Outlook also supported an Inspector.HTMLEditor object property, but since Internet Explorer is no longer the rendering engine for HTML messages and posts, HTMLEditor is no longer supported.
					IWordDocument wordDocument = null;
					var explorer = inspectorOrExplorer as IExplorer;
					if (explorer != null) {
						wordDocument = explorer.ActiveInlineResponseWordEditor;
					}
					else
					{
						var inspector1 = inspectorOrExplorer as IInspector;
						if (inspector1 != null) {
							var inspector = inspector1;
							if (inspector.IsWordMail()) {
								wordDocument = inspector.WordEditor;
							}
						}
					}
					if (wordDocument != null) {
						try {
							if (WordExporter.InsertIntoExistingDocument(wordDocument.Application, wordDocument, tmpFile, null, null)) {
								Log.Info("Inserted into Wordmail");
								wordDocument.Dispose();
								return true;
							}
						} catch (Exception exportException) {
							Log.Error("Error exporting to the word editor, trying to do it via another method", exportException);
						}
					} else if (isAppointment) {
						Log.Info("Can't export to an appointment if no word editor is used");
						return false;
					} else {
						Log.Info("Trying export for outlook < 2007.");
					}
				}
				// Only use mailitem as it should be filled!!
				Log.InfoFormat("Item '{0}' has format: {1}", mailItem?.Subject, mailItem?.BodyFormat);

				string contentId;
				if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2007) {
					contentId = Guid.NewGuid().ToString();
				} else {
					Log.Info("Older Outlook (<2007) found, using filename as contentid.");
					contentId = Path.GetFileName(tmpFile);
				}

				// Use this to change the format, it will probably lose the current selection.
				//if (!OlBodyFormat.olFormatHTML.Equals(currentMail.BodyFormat)) {
				//	LOG.Info("Changing format to HTML.");
				//	currentMail.BodyFormat = OlBodyFormat.olFormatHTML;
				//}

				bool inlinePossible = false;
				if (inspectorOrExplorer is IInspector && OlBodyFormat.olFormatHTML.Equals(mailItem?.BodyFormat)) {
					// if html we can try to inline it
					// The following might cause a security popup... can't ignore it.
					try {
						IHTMLDocument2 document2 = (inspectorOrExplorer as IInspector).HTMLEditor as IHTMLDocument2;
						if (document2 != null) {
							IHTMLSelectionObject selection = document2.selection;
							if (selection != null) {
								IHTMLTxtRange range = selection.createRange();
								if (range != null) {
									// First paste, than attach (otherwise the range is wrong!)
									range.pasteHTML("<BR/><IMG border=0 hspace=0 alt=\"" + attachmentName + "\" align=baseline src=\"cid:" + contentId + "\"><BR/>");
									inlinePossible = true;
								} else {
									Log.DebugFormat("No range for '{0}'", inspectorOrExplorer.Caption);
								}
							} else {
								Log.DebugFormat("No selection for '{0}'", inspectorOrExplorer.Caption);
							}
						} else {
							Log.DebugFormat("No HTML editor for '{0}'", inspectorOrExplorer.Caption);
						}
					} catch (Exception e) {
						// Continue with non inline image
						Log.Warn("Error pasting HTML, most likely due to an ACCESS_DENIED as the user clicked no.", e);
					}
				}

				// Create the attachment (if inlined the attachment isn't visible as attachment!)
				using (IAttachment attachment = mailItem.Attachments.Add(tmpFile, OlAttachmentType.olByValue, inlinePossible ? 0 : 1, attachmentName)) {
					if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2007) {
						// Add the content id to the attachment, this only works for Outlook >= 2007
						try {
							IPropertyAccessor propertyAccessor = attachment.PropertyAccessor;
							propertyAccessor.SetProperty(PropTag.ATTACHMENT_CONTENT_ID, contentId);
						} catch {
							// Ignore
						}
					}
				}
			} catch (Exception ex) {
				Log.WarnFormat("Problem while trying to add attachment to Item '{0}' : {1}", inspectorOrExplorer.Caption, ex);
				return false;
			}
			try {
				inspectorOrExplorer.Activate();
			} catch (Exception ex) {
				Log.Warn("Problem activating inspector/explorer: ", ex);
				return false;
			}
			Log.Debug("Finished!");
			return true;
		}

		/// <summary>
		/// Export image to a new email
		/// </summary>
		/// <param name="outlookApplication"></param>
		/// <param name="format"></param>
		/// <param name="tmpFile"></param>
		/// <param name="subject"></param>
		/// <param name="attachmentName"></param>
		/// <param name="to"></param>
		/// <param name="cc"></param>
		/// <param name="bcc"></param>
		/// <param name="url"></param>
		private static void ExportToNewEmail(IOutlookApplication outlookApplication, EmailFormat format, string tmpFile, string subject, string attachmentName, string to, string cc, string bcc, string url) {
			using (IItem newItem = outlookApplication.CreateItem(OlItemType.olMailItem)) {
				if (newItem == null) {
					return;
				}
				//MailItem newMail = COMWrapper.Cast<MailItem>(newItem);
				MailItem newMail = (MailItem)newItem;
				newMail.Subject = subject;
				if (!string.IsNullOrEmpty(to)) {
					newMail.To = to;
				}
				if (!string.IsNullOrEmpty(cc)) {
					newMail.CC = cc;
				}
				if (!string.IsNullOrEmpty(bcc)) {
					newMail.BCC = bcc;
				}
				newMail.BodyFormat = OlBodyFormat.olFormatHTML;
				string bodyString = null;
				// Read the default signature, if nothing found use empty email
				try {
					bodyString = GetOutlookSignature(format);
				} catch (Exception e) {
					Log.Error("Problem reading signature!", e);
				}
				switch (format) {
					case EmailFormat.Text:
						// Create the attachment (and dispose the COM object after using)
						using (newMail.Attachments.Add(tmpFile, OlAttachmentType.olByValue, 1, attachmentName))
						{
							newMail.BodyFormat = OlBodyFormat.olFormatPlain;
							if (bodyString == null) {
								bodyString = "";
							}
							newMail.Body = bodyString;
						}
						break;
					default:
						string contentId = Path.GetFileName(tmpFile);
						// Create the attachment (and dispose the COM object after using)
						using (IAttachment attachment = newMail.Attachments.Add(tmpFile, OlAttachmentType.olByValue, 0, attachmentName)) {
							// add content ID to the attachment
							if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2007) {
								try {
									contentId = Guid.NewGuid().ToString();
									IPropertyAccessor propertyAccessor = attachment.PropertyAccessor;
									propertyAccessor.SetProperty(PropTag.ATTACHMENT_CONTENT_ID, contentId);
								} catch {
									Log.Info("Error working with the PropertyAccessor, using filename as contentid");
									contentId = Path.GetFileName(tmpFile);
								}
							}
						}

						newMail.BodyFormat = OlBodyFormat.olFormatHTML;
						string href = "";
						string hrefEnd = "";
						if (!string.IsNullOrEmpty(url)) {
							href = $"<A HREF=\"{url}\">";
							hrefEnd = "</A>";
						}
						string htmlImgEmbedded = $"<BR/>{href}<IMG border=0 hspace=0 alt=\"{attachmentName}\" align=baseline src=\"cid:{contentId}\">{hrefEnd}<BR/>";
						string fallbackBody = $"<HTML><BODY>{htmlImgEmbedded}</BODY></HTML>";
						if (bodyString == null) {
							bodyString = fallbackBody;
						} else {
							int bodyIndex = bodyString.IndexOf("<body", StringComparison.CurrentCultureIgnoreCase);
							if (bodyIndex >= 0)
							{
								bodyIndex = bodyString.IndexOf(">", bodyIndex, StringComparison.Ordinal) + 1;
								bodyString = bodyIndex >= 0 ? bodyString.Insert(bodyIndex, htmlImgEmbedded) : fallbackBody;
							} else {
								bodyString = fallbackBody;
							}
						}
						newMail.HTMLBody = bodyString;
						break;
				}
				// So not save, otherwise the email is always stored in Draft folder.. (newMail.Save();)
				newMail.Display(false);

				using (IInspector inspector = newMail.GetInspector()) {
					if (inspector != null) {
						try {
							inspector.Activate();
						} catch {
							// Ignore
						}
					}
				}
			}
		}

		/// <summary>
		/// Helper method to create an outlook mail item with attachment
		/// </summary>
		/// <param name="format"></param>
		/// <param name="tmpFile">The file to send, do not delete the file right away!</param>
		/// <param name="subject"></param>
		/// <param name="attachmentName"></param>
		/// <param name="to"></param>
		/// <param name="cc"></param>
		/// <param name="bcc"></param>
		/// <param name="url"></param>
		/// <returns>true if it worked, false if not</returns>
		public static bool ExportToOutlook(EmailFormat format, string tmpFile, string subject, string attachmentName, string to, string cc, string bcc, string url) {
			bool exported = false;
			try {
				using (IOutlookApplication outlookApplication = GetOrCreateOutlookApplication()) {
					if (outlookApplication != null) {
						ExportToNewEmail(outlookApplication, format, tmpFile, subject, attachmentName, to, cc, bcc, url);
						exported = true;
					}
				}
				return exported;
			} catch (Exception e) {
				Log.Error("Error while creating an outlook mail item: ", e);
			}
			return exported;
		}

		/// <summary>
		/// Helper method to get the Outlook signature
		/// </summary>
		/// <returns></returns>
		private static string GetOutlookSignature(EmailFormat format) {
			using (RegistryKey profilesKey = Registry.CurrentUser.OpenSubKey(ProfilesKey, false)) {
				if (profilesKey == null) {
					return null;
				}
				string defaultProfile = (string)profilesKey.GetValue(DefaultProfileValue);
				Log.DebugFormat("defaultProfile={0}", defaultProfile);
				using (RegistryKey profileKey = profilesKey.OpenSubKey(defaultProfile + @"\" + AccountKey, false)) {
					if (profileKey != null)
					{
						string[] numbers = profileKey.GetSubKeyNames();
						foreach (string number in numbers) {
							Log.DebugFormat("Found subkey {0}", number);
							using (RegistryKey numberKey = profileKey.OpenSubKey(number, false)) {
								if (numberKey != null)
								{
									byte[] val = (byte[])numberKey.GetValue(NewSignatureValue);
									if (val == null) {
										continue;
									}
									string signatureName = "";
									foreach (byte b in val) {
										if (b != 0) {
											signatureName += (char)b;
										}
									}
									Log.DebugFormat("Found email signature: {0}", signatureName);
									string extension;
									switch (format) {
										case EmailFormat.Text:
											extension = ".txt";
											break;
										default:
											extension = ".htm";
											break;
									}
									string signatureFile = Path.Combine(SignaturePath, signatureName + extension);
									if (File.Exists(signatureFile)) {
										Log.DebugFormat("Found email signature file: {0}", signatureFile);
										return File.ReadAllText(signatureFile, Encoding.Default);
									}
								}
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
			if (outlookApplication == null || _outlookVersion != null) {
				return;
			}
			try {
				_outlookVersion = new Version(outlookApplication.Version);
				Log.InfoFormat("Using Outlook {0}", _outlookVersion);
			} catch (Exception exVersion) {
				Log.Error(exVersion);
				Log.Warn("Assuming outlook version 1997.");
				_outlookVersion = new Version((int)OfficeVersion.OFFICE_97, 0, 0, 0);
			}
			// Preventing retrieval of currentUser if Outlook is older than 2007
			if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2007) {
				try {
					INameSpace mapiNamespace = outlookApplication.GetNameSpace("MAPI");
					_currentUser = mapiNamespace.CurrentUser.Name;
					Log.InfoFormat("Current user: {0}", _currentUser);
				} catch (Exception exNs) {
					Log.Error(exNs);
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
