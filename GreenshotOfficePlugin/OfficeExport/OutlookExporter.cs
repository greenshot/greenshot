/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using GreenshotPlugin.Interop;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using mshtml;
using Word = Microsoft.Office.Interop.Word;
using Outlook = Microsoft.Office.Interop.Outlook;
using Dapplo.Config.Ini;

namespace GreenshotOfficePlugin.OfficeExport
{
	/// <summary>
	/// Outlook exporter has all the functionality to export to outlook
	/// </summary>
	public class OutlookExporter
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OutlookExporter));
		private static readonly IOfficeConfiguration Conf = IniConfig.Current.Get<IOfficeConfiguration>();
		private static readonly string SignaturePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Signatures");
		private static Version _outlookVersion;
		private static string _currentUser;

		// The signature key can be found at:
		// HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles\<DefaultProfile>\9375CFF0413111d3B88A00104B2A6676\<xxxx> [New Signature]
		private const string ProfilesKey = @"Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles\";
		private const string AccountKey = "9375CFF0413111d3B88A00104B2A6676";
		private const string NewSignatureValue = "New Signature";
		private const string DefaultProfileValue = "DefaultProfile";
		// Schema definitions for the MAPI properties, see: http://msdn.microsoft.com/en-us/library/aa454438.aspx and: http://msdn.microsoft.com/en-us/library/bb446117.aspx
		private const string AttachmentContentId = @"http://schemas.microsoft.com/mapi/proptag/0x3712001E";

		/// <summary>
		/// A method to retrieve all inspectors which can act as an export target
		/// </summary>
		/// <returns>IDictionary with inspector captions (window title) and object class</returns>
		public static IDictionary<string, Outlook.OlObjectClass> RetrievePossibleTargets()
		{
			IDictionary<string, Outlook.OlObjectClass> inspectorCaptions = new SortedDictionary<string, Outlook.OlObjectClass>();
			try
			{
				using (var outlookApplication = GetOutlookApplication())
				{
					if (outlookApplication == null)
					{
						return null;
					}

					// The activeexplorer inline response only works with >= 2013, Microsoft Outlook 15.0 Object Library
					if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2013)
					{
						// Check inline "panel" for Outlook 2013
						using (var activeExplorer = DisposableCom.Create(outlookApplication.ComObject.ActiveExplorer()))
						{
							if (activeExplorer != null)
							{
								var untypedInlineResponse = activeExplorer.ComObject.ActiveInlineResponse;
								string caption = activeExplorer.ComObject.Caption;
								using (DisposableCom.Create(untypedInlineResponse))
								{
									var inlineResponseClass = (Outlook.OlObjectClass)untypedInlineResponse.Class;
									switch (inlineResponseClass)
									{
										case Outlook.OlObjectClass.olMail:
											var mailItem = (Outlook.MailItem)untypedInlineResponse;
											if (!mailItem.Sent)
											{
												inspectorCaptions.Add(caption, inlineResponseClass);
											}
											break;
										case Outlook.OlObjectClass.olAppointment:
											var appointmentItem = (Outlook.AppointmentItem)untypedInlineResponse;
											if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2010 && Conf.OutlookAllowExportInMeetings)
											{
												if (!string.IsNullOrEmpty(appointmentItem.Organizer) && appointmentItem.Organizer.Equals(_currentUser))
												{
													inspectorCaptions.Add(caption, inlineResponseClass);
												}
											}
											break;
									}
								}
							}
						}
					}

					using (var inspectors = DisposableCom.Create(outlookApplication.ComObject.Inspectors))
					{
						if (inspectors != null && inspectors.ComObject.Count > 0)
						{
							for (int i = 1; i <= inspectors.ComObject.Count; i++)
							{
								using (var inspector = DisposableCom.Create(inspectors.ComObject[i]))
								{
									string caption = inspector.ComObject.Caption;
									// Fix double entries in the directory, TODO: store on something uniq
									if (inspectorCaptions.ContainsKey(caption)) {
										continue;
									}

									var currentItemUntyped = inspector.ComObject.CurrentItem;
									using (DisposableCom.Create(currentItemUntyped))
									{
										var currentItemClass = (Outlook.OlObjectClass)currentItemUntyped.Class;
										switch (currentItemClass)
										{
											case Outlook.OlObjectClass.olMail:
												Outlook.MailItem mailItem = currentItemUntyped;
												if (mailItem.Sent)
												{
													continue;
												}
												inspectorCaptions.Add(caption, currentItemClass);
												break;
											case Outlook.OlObjectClass.olAppointment:
												Outlook.AppointmentItem appointmentItem = currentItemUntyped;
												if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2010 && Conf.OutlookAllowExportInMeetings)
												{
													if (!string.IsNullOrEmpty(appointmentItem.Organizer) && !appointmentItem.Organizer.Equals(_currentUser))
													{
														LOG.DebugFormat("Not exporting, as organizer is set to {0} and currentuser {1} is not him.", appointmentItem.Organizer, _currentUser);
														continue;
													}
												}
												else
												{
													// skip, can't export to olAppointment
													continue;
												}
												inspectorCaptions.Add(caption, currentItemClass);
												break;
											default:
												continue;
										}
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
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
		public static bool ExportToInspector(string inspectorCaption, string tmpFile, string attachmentName)
		{
			using (var outlookApplication = GetOrCreateOutlookApplication())
			{
				if (outlookApplication == null)
				{
					return false;
				}

				// The activeexplorer inline response only works with >= 2013, Microsoft Outlook 15.0 Object Library
				if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2013)
				{
					// Check inline "panel" for Outlook 2013
					using (var activeExplorer = DisposableCom.Create((Outlook._Explorer)outlookApplication.ComObject.ActiveExplorer()))
					{
						// Only if we have one and if the capture is the one we selected
						if (activeExplorer != null && activeExplorer.ComObject.Caption.StartsWith(inspectorCaption))
						{
							var untypedInlineResponse = activeExplorer.ComObject.ActiveInlineResponse;
							using (DisposableCom.Create(untypedInlineResponse))
							{
								var inlineResponseClass = (Outlook.OlObjectClass)untypedInlineResponse.Class;
								switch (inlineResponseClass)
								{
									case Outlook.OlObjectClass.olMail:
										var mailItem = (Outlook.MailItem)untypedInlineResponse;
										if (!mailItem.Sent)
										{
											return ExportToInspector(null, activeExplorer, inlineResponseClass, mailItem, tmpFile, attachmentName);
										}
										break;
									case Outlook.OlObjectClass.olAppointment:
										var appointmentItem = (Outlook.AppointmentItem)untypedInlineResponse;
										if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2010 && Conf.OutlookAllowExportInMeetings)
										{
											if (!string.IsNullOrEmpty(appointmentItem.Organizer) && appointmentItem.Organizer.Equals(_currentUser))
											{
												return ExportToInspector(null, activeExplorer, inlineResponseClass, null, tmpFile, attachmentName);
											}
										}
										break;
								}
							}
						}
					}
				}

				using (var inspectors = DisposableCom.Create(outlookApplication.ComObject.Inspectors))
				{
					if (inspectors == null || inspectors.ComObject.Count == 0)
					{
						return false;
					}
					LOG.DebugFormat("Got {0} inspectors to check", inspectors.ComObject.Count);
					for (int i = 1; i <= inspectors.ComObject.Count; i++)
					{
						using (var inspector = DisposableCom.Create((Outlook._Inspector)inspectors.ComObject[i]))
						{
							string currentCaption = inspector.ComObject.Caption;
							if (currentCaption.StartsWith(inspectorCaption))
							{
								var currentItemUntyped = inspector.ComObject.CurrentItem;
								using (DisposableCom.Create(currentItemUntyped))
								{
									var currentItemClass = (Outlook.OlObjectClass)currentItemUntyped.Class;
									switch (currentItemClass)
									{
										case Outlook.OlObjectClass.olMail:
											Outlook.MailItem mailItem = currentItemUntyped;
											if (mailItem.Sent)
											{
												continue;
											}
											try
											{
												return ExportToInspector(inspector, null, currentItemClass, mailItem, tmpFile, attachmentName);
											}
											catch (Exception exExport)
											{
												LOG.Error("Export to " + currentCaption + " failed.", exExport);
											}
											break;
										case Outlook.OlObjectClass.olAppointment:
											Outlook.AppointmentItem appointmentItem = currentItemUntyped;
											if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2010 && Conf.OutlookAllowExportInMeetings)
											{
												if (!string.IsNullOrEmpty(appointmentItem.Organizer) && !appointmentItem.Organizer.Equals(_currentUser))
												{
													LOG.DebugFormat("Not exporting, as organizer is set to {0} and currentuser {1} is not him.", appointmentItem.Organizer, _currentUser);
													continue;
												}
											}
											else
											{
												// skip, can't export to olAppointment
												continue;
											}
											try
											{
												return ExportToInspector(inspector, null, currentItemClass, null, tmpFile, attachmentName);
											}
											catch (Exception exExport)
											{
												LOG.Error("Export to " + currentCaption + " failed.", exExport);
											}
											break;
										default:
											continue;
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
		/// <param name="mailItem"></param>
		/// <param name="tmpFile"></param>
		/// <param name="attachmentName"></param>
		/// <param name="explorer"></param>
		/// <param name="itemClass"></param>
		/// <returns></returns>
		private static bool ExportToInspector(IDisposableCom<Outlook._Inspector> inspector, IDisposableCom<Outlook._Explorer> explorer, Outlook.OlObjectClass itemClass, Outlook.MailItem mailItem, string tmpFile, string attachmentName)
		{
			bool isMail = Outlook.OlObjectClass.olMail.Equals(itemClass);
			bool isAppointment = Outlook.OlObjectClass.olAppointment.Equals(itemClass);
			if (!isMail && !isAppointment)
			{
				LOG.Warn("Item is no mail or appointment.");
				return false;
			}
			try
			{
				// Make sure the inspector is activated, only this way the word editor is active!
				// This also ensures that the window is visible!
				if (inspector != null)
				{
					inspector.ComObject.Activate();
				}
				bool isTextFormat = false;
				if (isMail)
				{
					isTextFormat = Outlook.OlBodyFormat.olFormatPlain.Equals(mailItem.BodyFormat);
				}
				if (isAppointment || !isTextFormat)
				{
					// Check for wordmail, if so use the wordexporter
					// http://msdn.microsoft.com/en-us/library/dd492012%28v=office.12%29.aspx
					// Earlier versions of Outlook also supported an Inspector.HTMLEditor object property, but since Internet Explorer is no longer the rendering engine for HTML messages and posts, HTMLEditor is no longer supported.
					IDisposableCom<Word._Document> wordDocument = null;
					if (explorer != null && _outlookVersion.Major >= (int)OfficeVersion.OFFICE_2013)
					{
						// TODO: Needs to have the Microsoft Outlook 15.0 Object Library installed
						wordDocument = DisposableCom.Create((Word._Document)explorer.ComObject.ActiveInlineResponseWordEditor);
					}
					else if (inspector != null)
					{
						if (inspector.ComObject.IsWordMail() && inspector.ComObject.EditorType == Outlook.OlEditorType.olEditorWord)
						{
							var tmpWordDocument = (Word._Document)inspector.ComObject.WordEditor;
							wordDocument = DisposableCom.Create(tmpWordDocument);
						}
					}
					if (wordDocument != null)
					{
						using (wordDocument)
						using (var application = DisposableCom.Create(wordDocument.ComObject.Application))
						{
							try
							{
								if (WordExporter.InsertIntoExistingDocument(application, wordDocument, tmpFile, null, null))
								{
									LOG.Info("Inserted into Wordmail");
									return true;
								}
							}
							catch (Exception exportException)
							{
								LOG.Error("Error exporting to the word editor, trying to do it via another method", exportException);
							}
						}
					}
					else if (isAppointment)
					{
						LOG.Info("Can't export to an appointment if no word editor is used");
						return false;
					}
					else
					{
						LOG.Info("Trying export for outlook < 2007.");
					}
				}
				// Only use mailitem as it should be filled!!
				if (mailItem != null)
				{
					LOG.InfoFormat("Item '{0}' has format: {1}", mailItem.Subject, mailItem.BodyFormat);
				}

				string contentId;
				if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2007)
				{
					contentId = Guid.NewGuid().ToString();
				}
				else
				{
					LOG.Info("Older Outlook (<2007) found, using filename as contentid.");
					contentId = Path.GetFileName(tmpFile);
				}

				// Use this to change the format, it will probably lose the current selection.
				//if (!OlBodyFormat.olFormatHTML.Equals(currentMail.BodyFormat)) {
				//	LOG.Info("Changing format to HTML.");
				//	currentMail.BodyFormat = OlBodyFormat.olFormatHTML;
				//}

				bool inlinePossible = false;
				if (mailItem != null && (inspector != null && Outlook.OlBodyFormat.olFormatHTML.Equals(mailItem.BodyFormat)))
				{
					// if html we can try to inline it
					// The following might cause a security popup... can't ignore it.
					try
					{
						var document2 = inspector.ComObject.HTMLEditor as IHTMLDocument2;
						if (document2 != null)
						{
							var selection = document2.selection;
							if (selection != null)
							{
								var range = (IHTMLTxtRange)selection.createRange();
								if (range != null)
								{
									// First paste, than attach (otherwise the range is wrong!)
									range.pasteHTML("<BR/><IMG border=0 hspace=0 alt=\"" + attachmentName + "\" align=baseline src=\"cid:" + contentId + "\"><BR/>");
									inlinePossible = true;
								}
								else
								{
									LOG.DebugFormat("No range for '{0}'", inspector.ComObject.Caption);
								}
							}
							else
							{
								LOG.DebugFormat("No selection for '{0}'", inspector.ComObject.Caption);
							}
						}
						else
						{
							LOG.DebugFormat("No HTML editor for '{0}'", inspector.ComObject.Caption);
						}
					}
					catch (Exception e)
					{
						// Continue with non inline image
						LOG.Warn("Error pasting HTML, most likely due to an ACCESS_DENIED as the user clicked no.", e);
					}
				}

				// Create the attachment (if inlined the attachment isn't visible as attachment!)
				using (var attachments = DisposableCom.Create(mailItem.Attachments))
				using (var attachment = DisposableCom.Create(attachments.ComObject.Add(tmpFile, Outlook.OlAttachmentType.olByValue, inlinePossible ? 0 : 1, attachmentName)))
				{
					if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2007)
					{
						// Add the content id to the attachment, this only works for Outlook >= 2007
						try
						{
							var propertyAccessor = attachment.ComObject.PropertyAccessor;
							propertyAccessor.SetProperty(AttachmentContentId, contentId);
						}
// ReSharper disable once EmptyGeneralCatchClause
						catch
						{
							// Ignore
						}
					}
				}
			}
			catch (Exception ex)
			{
				string caption = "n.a.";
				if (inspector != null)
				{
					caption = inspector.ComObject.Caption;
				}
				else if (explorer != null)
				{
					caption = explorer.ComObject.Caption;
				}
				LOG.WarnFormat("Problem while trying to add attachment to Item '{0}' : {1}", caption, ex);
				return false;
			}
			try
			{
				if (inspector != null)
				{
					inspector.ComObject.Activate();
				}
				else if (explorer != null)
				{
					explorer.ComObject.Activate();
				}
			}
			catch (Exception ex)
			{
				LOG.Warn("Problem activating inspector/explorer: ", ex);
				return false;
			}
			LOG.Debug("Finished!");
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
		private static void ExportToNewEmail(IDisposableCom<Outlook.Application> outlookApplication, EmailFormat format, string tmpFile, string subject, string attachmentName, string to, string cc, string bcc, string url)
		{
			using (var newItem = DisposableCom.Create((Outlook.MailItem)outlookApplication.ComObject.CreateItem(Outlook.OlItemType.olMailItem)))
			{
				if (newItem == null)
				{
					return;
				}
				//MailItem newMail = COMWrapper.Cast<MailItem>(newItem);
				var newMail = newItem.ComObject;
				newMail.Subject = subject;
				if (!string.IsNullOrEmpty(to))
				{
					newMail.To = to;
				}
				if (!string.IsNullOrEmpty(cc))
				{
					newMail.CC = cc;
				}
				if (!string.IsNullOrEmpty(bcc))
				{
					newMail.BCC = bcc;
				}
				newMail.BodyFormat = Outlook.OlBodyFormat.olFormatHTML;
				string bodyString = null;
				// Read the default signature, if nothing found use empty email
				try
				{
					bodyString = GetOutlookSignature(format);
				}
				catch (Exception e)
				{
					LOG.Error("Problem reading signature!", e);
				}
				switch (format)
				{
					case EmailFormat.Text:
						// Create the attachment (and dispose the COM object after using)
						using (var attachments = DisposableCom.Create(newMail.Attachments))
						using (DisposableCom.Create(attachments.ComObject.Add(tmpFile, Outlook.OlAttachmentType.olByValue, 1, attachmentName)))
						{
							newMail.BodyFormat = Outlook.OlBodyFormat.olFormatPlain;
							if (bodyString == null)
							{
								bodyString = "";
							}
							newMail.Body = bodyString;
						}
						break;
					default:
						string contentId = Path.GetFileName(tmpFile);
						// Create the attachment (and dispose the COM object after using)
						using (var attachments = DisposableCom.Create(newMail.Attachments))
						using (var attachment = DisposableCom.Create(attachments.ComObject.Add(tmpFile, Outlook.OlAttachmentType.olByValue, 0, attachmentName)))
						{
							// add content ID to the attachment
							if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2007)
							{
								try
								{
									contentId = Guid.NewGuid().ToString();
									using (var propertyAccessor = DisposableCom.Create(attachment.ComObject.PropertyAccessor))
									{
										propertyAccessor.ComObject.SetProperty(AttachmentContentId, contentId);
									}
								}
								catch
								{
									LOG.Info("Error working with the PropertyAccessor, using filename as contentid");
									contentId = Path.GetFileName(tmpFile);
								}
							}
						}

						newMail.BodyFormat = Outlook.OlBodyFormat.olFormatHTML;
						string href = "";
						string hrefEnd = "";
						if (!string.IsNullOrEmpty(url))
						{
							href = string.Format("<A HREF=\"{0}\">", url);
							hrefEnd = "</A>";
						}
						string htmlImgEmbedded = string.Format("<BR/>{0}<IMG border=0 hspace=0 alt=\"{1}\" align=baseline src=\"cid:{2}\">{3}<BR/>", href, attachmentName, contentId, hrefEnd);
						string fallbackBody = string.Format("<HTML><BODY>{0}</BODY></HTML>", htmlImgEmbedded);
						if (bodyString == null)
						{
							bodyString = fallbackBody;
						}
						else
						{
							int bodyIndex = bodyString.IndexOf("<body", StringComparison.CurrentCultureIgnoreCase);
							if (bodyIndex >= 0)
							{
								bodyIndex = bodyString.IndexOf(">", bodyIndex, StringComparison.Ordinal) + 1;
								if (bodyIndex >= 0)
								{
									bodyString = bodyString.Insert(bodyIndex, htmlImgEmbedded);
								}
								else
								{
									bodyString = fallbackBody;
								}
							}
							else
							{
								bodyString = fallbackBody;
							}
						}
						newMail.HTMLBody = bodyString;
						break;
				}
				// So not save, otherwise the email is always stored in Draft folder.. (newMail.Save();)
				newMail.Display(false);

				using (var inspector = DisposableCom.Create((Outlook._Inspector)newMail.GetInspector))
				{
					if (inspector != null)
					{
						try
						{
							inspector.ComObject.Activate();
						}
						// ReSharper disable once EmptyGeneralCatchClause
						catch
						{
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
		public static bool ExportToOutlook(EmailFormat format, string tmpFile, string subject, string attachmentName, string to, string cc, string bcc, string url)
		{
			bool exported = false;
			try
			{
				using (var outlookApplication = GetOrCreateOutlookApplication())
				{
					if (outlookApplication != null)
					{
						ExportToNewEmail(outlookApplication, format, tmpFile, subject, attachmentName, to, cc, bcc, url);
						exported = true;
					}
				}
				return exported;
			}
			catch (Exception e)
			{
				LOG.Error("Error while creating an outlook mail item: ", e);
			}
			return exported;
		}

		/// <summary>
		/// Helper method to get the Outlook signature
		/// </summary>
		/// <returns></returns>
		private static string GetOutlookSignature(EmailFormat format)
		{
			using (RegistryKey profilesKey = Registry.CurrentUser.OpenSubKey(ProfilesKey, false))
			{
				if (profilesKey == null)
				{
					return null;
				}
				string defaultProfile = (string)profilesKey.GetValue(DefaultProfileValue);
				LOG.DebugFormat("defaultProfile={0}", defaultProfile);
				using (RegistryKey profileKey = profilesKey.OpenSubKey(defaultProfile + @"\" + AccountKey, false))
				{
					if (profileKey != null)
					{
						string[] numbers = profileKey.GetSubKeyNames();
						foreach (string number in numbers)
						{
							LOG.DebugFormat("Found subkey {0}", number);
							using (RegistryKey numberKey = profileKey.OpenSubKey(number, false))
							{
								if (numberKey != null)
								{
									byte[] val = (byte[])numberKey.GetValue(NewSignatureValue);
									if (val == null)
									{
										continue;
									}
									string signatureName = "";
									foreach (byte b in val)
									{
										if (b != 0)
										{
											signatureName += (char)b;
										}
									}
									LOG.DebugFormat("Found email signature: {0}", signatureName);
									string extension;
									switch (format)
									{
										case EmailFormat.Text:
											extension = ".txt";
											break;
										default:
											extension = ".htm";
											break;
									}
									string signatureFile = Path.Combine(SignaturePath, signatureName + extension);
									if (File.Exists(signatureFile))
									{
										LOG.DebugFormat("Found email signature file: {0}", signatureFile);
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
		/// Call this to get the running Outlook application, returns null if there isn't any.
		/// </summary>
		/// <returns>IDisposableCom for Outlook.Application or null</returns>
		private static IDisposableCom<Outlook.Application> GetOutlookApplication()
		{
			IDisposableCom<Outlook.Application> outlookApplication;
			try
			{
				outlookApplication = DisposableCom.Create((Outlook.Application)Marshal.GetActiveObject("Outlook.Application"));
			}
			catch (Exception)
			{
				// Ignore, probably no outlook running
				return null;
			}
			if (outlookApplication != null && outlookApplication.ComObject != null)
			{
				InitializeVariables(outlookApplication);
			}
			return outlookApplication;
		}

		/// <summary>
		/// Call this to get the running Outlook application, or create a new instance
		/// </summary>
		/// <returns>IDisposableCom for Outlook.Application</returns>
		private static IDisposableCom<Outlook.Application> GetOrCreateOutlookApplication()
		{
			IDisposableCom<Outlook.Application> outlookApplication = GetOutlookApplication();
			if (outlookApplication == null)
			{
				outlookApplication = DisposableCom.Create(new Outlook.Application());
			}
			InitializeVariables(outlookApplication);
			return outlookApplication;
		}


		/// <summary>
		/// Initialize static outlook variables like version and currentuser
		/// </summary>
		/// <param name="outlookApplication"></param>
		private static void InitializeVariables(IDisposableCom<Outlook.Application> outlookApplication)
		{
			if (outlookApplication == null || outlookApplication.ComObject == null || _outlookVersion != null)
			{
				return;
			}
			if (!Version.TryParse(outlookApplication.ComObject.Version, out _outlookVersion)) {
				LOG.Warn("Assuming outlook version 1997.");
				_outlookVersion = new Version((int)OfficeVersion.OFFICE_97, 0, 0, 0);
			}
			// Preventing retrieval of currentUser if Outlook is older than 2007
			if (_outlookVersion.Major >= (int)OfficeVersion.OFFICE_2007)
			{
				try
				{
					using (var mapiNamespace = DisposableCom.Create(outlookApplication.ComObject.GetNamespace("MAPI")))
					{
						using (var currentUser = DisposableCom.Create(mapiNamespace.ComObject.CurrentUser))
						{
							_currentUser = currentUser.ComObject.Name;
						}
					}
					LOG.InfoFormat("Current user: {0}", _currentUser);
				}
				catch (Exception exNs)
				{
					LOG.Error(exNs);
				}
			}
		}
	}
}
