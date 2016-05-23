﻿/*
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

using Greenshot.Interop;
using GreenshotOfficePlugin;
using Greenshot.IniFile;

namespace Greenshot.Interop.Office {
	public class WordExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WordExporter));
		private static Version wordVersion = null;
		private static readonly OfficeConfiguration config = IniConfig.GetIniSection<OfficeConfiguration>();

		/// <summary>
		/// Check if the used version is higher than Office 2003
		/// </summary>
		/// <returns></returns>
		private static bool isAfter2003() {
			return wordVersion.Major > (int)OfficeVersion.OFFICE_2003;
		}

		/// <summary>
		/// Insert the bitmap stored under the tempfile path into the word document with the supplied caption
		/// </summary>
		/// <param name="wordCaption"></param>
		/// <param name="tmpFile"></param>
		/// <returns></returns>
		public static bool InsertIntoExistingDocument(string wordCaption, string tmpFile) {
			using (IWordApplication wordApplication = GetWordApplication()) {
				if (wordApplication == null) {
					return false;
				}
				using (IDocuments documents = wordApplication.Documents) {
					for (int i = 1; i <= documents.Count; i++) {
						using (IWordDocument wordDocument = documents.item(i)) {
							using (IWordWindow activeWindow = wordDocument.ActiveWindow) {
								if (activeWindow.Caption.StartsWith(wordCaption)) {
									return InsertIntoExistingDocument(wordApplication, wordDocument, tmpFile, null, null);
								}
							}
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Internal method for the insert
		/// </summary>
		/// <param name="wordApplication"></param>
		/// <param name="wordDocument"></param>
		/// <param name="tmpFile"></param>
		/// <param name="adress">link for the image</param>
		/// <param name="tooltip">tooltip of the image</param>
		/// <returns></returns>
		internal static bool InsertIntoExistingDocument(IWordApplication wordApplication, IWordDocument wordDocument, string tmpFile, string address, string tooltip) {
			// Bug #1517: image will be inserted into that document, where the focus was last. It will not inserted into the chosen one.
			// Solution: Make sure the selected document is active, otherwise the insert will be made in a different document!
			try {
				wordDocument.Activate();
			} catch {
			}
			using (ISelection selection = wordApplication.Selection) {
				if (selection == null) {
					LOG.InfoFormat("No selection to insert {0} into found.", tmpFile);
					return false;
				}
				// Add Picture
				using (IInlineShape shape = AddPictureToSelection(selection, tmpFile)) {
					if (!string.IsNullOrEmpty(address)) {
						object screentip = Type.Missing;
						if (!string.IsNullOrEmpty(tooltip)) {
							screentip = tooltip;
						}
						try {
							using (IHyperlinks hyperlinks = wordDocument.Hyperlinks) {
								hyperlinks.Add(shape, screentip, Type.Missing, screentip, Type.Missing, Type.Missing);
							}
						} catch (Exception e) {
							LOG.WarnFormat("Couldn't add hyperlink for image: {0}", e.Message);
						}
					}
				}
				try {
					using (IWordWindow activeWindow = wordDocument.ActiveWindow) {
						activeWindow.Activate();
						using (IPane activePane = activeWindow.ActivePane) {
							using (IWordView view = activePane.View) {
								view.Zoom.Percentage = 100;
							}
						}
					}
				} catch (Exception e) {
					if (e.InnerException != null) {
						LOG.WarnFormat("Couldn't set zoom to 100, error: {0}", e.InnerException.Message);
					} else {
						LOG.WarnFormat("Couldn't set zoom to 100, error: {0}", e.Message);
					}
				}
				try {
					wordApplication.Activate();
				} catch {
				}
				try {
					wordDocument.Activate();
				} catch {
				}
				return true;
			}
		}

		/// <summary>
		/// Helper method to add the file as image to the selection
		/// </summary>
		/// <param name="selection"></param>
		/// <param name="tmpFile"></param>
		/// <returns></returns>
		private static IInlineShape AddPictureToSelection(ISelection selection, string tmpFile) {
			using (IInlineShapes shapes = selection.InlineShapes) {
				IInlineShape shape = shapes.AddPicture(tmpFile, false, true, Type.Missing);
				// Lock aspect ratio
				if (config.WordLockAspectRatio) {
					shape.LockAspectRatio = MsoTriState.msoTrue;
				}
				selection.InsertAfter("\r\n");
				selection.MoveDown(WdUnits.wdLine, 1, Type.Missing);
				return shape;
			}
		}

		public static void InsertIntoNewDocument(string tmpFile, string address, string tooltip) {
			using (IWordApplication wordApplication = GetOrCreateWordApplication()) {
				if (wordApplication == null) {
					return;
				}
				wordApplication.Visible = true;
				wordApplication.Activate();
				// Create new Document
				object template = string.Empty;
				object newTemplate = false;
				object documentType = 0;
				object documentVisible = true;
				using (IDocuments documents = wordApplication.Documents) {
					using (IWordDocument wordDocument = documents.Add(ref template, ref newTemplate, ref documentType, ref documentVisible)) {
						using (ISelection selection = wordApplication.Selection) {
							// Add Picture
							using (IInlineShape shape = AddPictureToSelection(selection, tmpFile)) {
								if (!string.IsNullOrEmpty(address)) {
									object screentip = Type.Missing;
									if (!string.IsNullOrEmpty(tooltip)) {
										screentip = tooltip;
									}
									try {
										using (IHyperlinks hyperlinks = wordDocument.Hyperlinks) {
											hyperlinks.Add(shape, screentip, Type.Missing, screentip, Type.Missing, Type.Missing);
										}
									} catch (Exception e) {
										LOG.WarnFormat("Couldn't add hyperlink for image: {0}", e.Message);
									}
								}
							}
						}
						try {
							wordDocument.Activate();
						} catch {
						}
						try {
							wordDocument.ActiveWindow.Activate();
						} catch {
						}
					}
				}
			}
		}

		/// <summary>
		/// Get the captions of all the open word documents
		/// </summary>
		/// <returns></returns>
		public static List<string> GetWordDocuments() {
			List<string> openDocuments = new List<string>();
			try {
				using (IWordApplication wordApplication = GetWordApplication()) {
					if (wordApplication == null) {
						return openDocuments;
					}
					using (IDocuments documents = wordApplication.Documents) {
						for (int i = 1; i <= documents.Count; i++) {
							using (IWordDocument document = documents.item(i)) {
								if (document.ReadOnly) {
									continue;
								}
								if (isAfter2003()) {
									if (document.Final) {
										continue;
									}
								}
								using (IWordWindow activeWindow = document.ActiveWindow) {
									openDocuments.Add(activeWindow.Caption);
								}
							}
						}
					}
				}
			} catch (Exception ex) {
				LOG.Warn("Problem retrieving word destinations, ignoring: ", ex);
			}
			openDocuments.Sort();
			return openDocuments;
		}

		/// <summary>
		/// Call this to get the running outlook application, returns null if there isn't any.
		/// </summary>
		/// <returns>IWordApplication or null</returns>
		private static IWordApplication GetWordApplication() {
			IWordApplication wordApplication = COMWrapper.GetInstance<IWordApplication>();
			InitializeVariables(wordApplication);
			return wordApplication;
		}

		/// <summary>
		/// Call this to get the running word application, or create a new instance
		/// </summary>
		/// <returns>IWordApplication</returns>
		private static IWordApplication GetOrCreateWordApplication() {
			IWordApplication wordApplication = COMWrapper.GetOrCreateInstance<IWordApplication>();
			InitializeVariables(wordApplication);
			return wordApplication;
		}

		/// <summary>
		/// Initialize static outlook variables like version and currentuser
		/// </summary>
		/// <param name="wordApplication"></param>
		private static void InitializeVariables(IWordApplication wordApplication) {
			if (wordApplication == null || wordVersion != null) {
				return;
			}
			try {
				wordVersion = new Version(wordApplication.Version);
				LOG.InfoFormat("Using Word {0}", wordVersion);
			} catch (Exception exVersion) {
				LOG.Error(exVersion);
				LOG.Warn("Assuming Word version 1997.");
				wordVersion = new Version((int)OfficeVersion.OFFICE_97, 0, 0, 0);
			}
		}
	}
}
