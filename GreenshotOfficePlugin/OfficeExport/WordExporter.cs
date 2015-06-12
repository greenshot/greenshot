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

using Greenshot.IniFile;
using Greenshot.Interop;
using Microsoft.Office.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Word = Microsoft.Office.Interop.Word;

namespace GreenshotOfficePlugin.OfficeExport {
	public class WordExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WordExporter));
		private static Version wordVersion = null;
		private static OfficeConfiguration config = IniConfig.GetIniSection<OfficeConfiguration>();

		/// <summary>
		/// Check if the used version is higher than Office 2003
		/// </summary>
		/// <returns></returns>
		private static bool isAfter2003() {
			return wordVersion.Major > (int)Greenshot.Interop.Office.OfficeVersion.OFFICE_2003;
		}

		/// <summary>
		/// Insert the bitmap stored under the tempfile path into the word document with the supplied caption
		/// </summary>
		/// <param name="wordCaption"></param>
		/// <param name="tmpFile"></param>
		/// <returns></returns>
		public static bool InsertIntoExistingDocument(string wordCaption, string tmpFile) {
			using (var wordApplication = GetWordApplication()) {
				if (wordApplication == null) {
					return false;
				}
				using (var documents = DisposableCom.Create(wordApplication.ComObject.Documents)) {
					for (int i = 1; i <= documents.ComObject.Count; i++) {
						using (var wordDocument =  DisposableCom.Create((Word._Document)documents.ComObject[i])) {
							using (var activeWindow = DisposableCom.Create(wordDocument.ComObject.ActiveWindow)) {
								if (activeWindow.ComObject.Caption.StartsWith(wordCaption)) {
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
		internal static bool InsertIntoExistingDocument(IDisposableCom<Word.Application> wordApplication, IDisposableCom<Word._Document> wordDocument, string tmpFile, string address, string tooltip) {
			// Bug #1517: image will be inserted into that document, where the focus was last. It will not inserted into the chosen one.
			// Solution: Make sure the selected document is active, otherwise the insert will be made in a different document!
			try {
				wordDocument.ComObject.Activate();
			} catch {
			}
			using (var selection = DisposableCom.Create(wordApplication.ComObject.Selection)) {
				if (selection == null) {
					LOG.InfoFormat("No selection to insert {0} into found.", tmpFile);
					return false;
				}
				// Add Picture
				using (var shape = AddPictureToSelection(selection, tmpFile)) {
					if (!string.IsNullOrEmpty(address)) {
						object screentip = Type.Missing;
						if (!string.IsNullOrEmpty(tooltip)) {
							screentip = tooltip;
						}
						try {
							using (var hyperlinks = DisposableCom.Create(wordDocument.ComObject.Hyperlinks)) {
								hyperlinks.ComObject.Add(shape, screentip, Type.Missing, screentip, Type.Missing, Type.Missing);
							}
						} catch (Exception e) {
							LOG.WarnFormat("Couldn't add hyperlink for image: {0}", e.Message);
						}
					}
				}
				try {
					using (var activeWindow = DisposableCom.Create(wordDocument.ComObject.ActiveWindow)) {
						activeWindow.ComObject.Activate();
						using (var activePane = DisposableCom.Create(activeWindow.ComObject.ActivePane)) {
							using (var view = DisposableCom.Create(activePane.ComObject.View)) {
								view.ComObject.Zoom.Percentage = 100;
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
					wordApplication.ComObject.Activate();
				} catch {
				}
				try {
					wordDocument.ComObject.Activate();
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
		private static IDisposableCom<Word.InlineShape> AddPictureToSelection(IDisposableCom<Word.Selection> selection, string tmpFile) {
			using (var shapes = DisposableCom.Create(selection.ComObject.InlineShapes)) {
				var shape = DisposableCom.Create(shapes.ComObject.AddPicture(tmpFile, false, true, Type.Missing));
				// Lock aspect ratio
				if (config.WordLockAspectRatio) {
					shape.ComObject.LockAspectRatio = MsoTriState.msoTrue;
				}
				selection.ComObject.InsertAfter("\r\n");
				selection.ComObject.MoveDown(Word.WdUnits.wdLine, 1, Type.Missing);
				return shape;
			}
		}

		public static void InsertIntoNewDocument(string tmpFile, string address, string tooltip) {
			using (var wordApplication = GetOrCreateWordApplication()) {
				if (wordApplication == null) {
					return;
				}
				wordApplication.ComObject.Visible = true;
				wordApplication.ComObject.Activate();
				// Create new Document
				object template = string.Empty;
				object newTemplate = false;
				object documentType = 0;
				object documentVisible = true;
				using (var documents = DisposableCom.Create(wordApplication.ComObject.Documents)) {
					using (var wordDocument = DisposableCom.Create(documents.ComObject.Add(ref template, ref newTemplate, ref documentType, ref documentVisible))) {
						using (var selection = DisposableCom.Create(wordApplication.ComObject.Selection)) {
							// Add Picture
							using (var shape = AddPictureToSelection(selection, tmpFile)) {
								if (!string.IsNullOrEmpty(address)) {
									object screentip = Type.Missing;
									if (!string.IsNullOrEmpty(tooltip)) {
										screentip = tooltip;
									}
									try {
										using (var hyperlinks = DisposableCom.Create(wordDocument.ComObject.Hyperlinks)) {
											using (DisposableCom.Create(hyperlinks.ComObject.Add(shape, screentip, Type.Missing, screentip, Type.Missing, Type.Missing))) {
												// Do nothing
											}
										}
									} catch (Exception e) {
										LOG.WarnFormat("Couldn't add hyperlink for image: {0}", e.Message);
									}
								}
							}
						}
						try {
							wordDocument.ComObject.Activate();
						} catch {
						}
						try {
							using (var activeWindow = DisposableCom.Create(wordDocument.ComObject.ActiveWindow)) {
								activeWindow.ComObject.Activate();
							}
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
		public static IEnumerable<string> GetWordDocuments() {
			using (var wordApplication = GetWordApplication()) {
				if (wordApplication == null) {
					yield break;
				}
				using (var documents = DisposableCom.Create(wordApplication.ComObject.Documents)) {
					for (int i = 1; i <= documents.ComObject.Count; i++) {
						using (var document =  DisposableCom.Create(documents.ComObject[i])) {
							if (document.ComObject.ReadOnly) {
								continue;
							}
							if (isAfter2003()) {
								if (document.ComObject.Final) {
									continue;
								}
							}
							using (var activeWindow = DisposableCom.Create(document.ComObject.ActiveWindow)) {
								yield return activeWindow.ComObject.Caption;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Call this to get the running Word application, returns null if there isn't any.
		/// </summary>
		/// <returns>ComDisposable for Word.Application or null</returns>
		private static IDisposableCom<Word.Application> GetWordApplication() {
			IDisposableCom<Word.Application> wordApplication = null;
			try {
				wordApplication = DisposableCom.Create((Word.Application)Marshal.GetActiveObject("Word.Application"));
			} catch (Exception) {
				// Ignore, probably no excel running
				return null;
			}
			if (wordApplication != null && wordApplication.ComObject != null) {
				InitializeVariables(wordApplication);
			}
			return wordApplication;
		}

		/// <summary>
		/// Call this to get the running Excel application, or create a new instance
		/// </summary>
		/// <returns>ComDisposable for Word.Application</returns>
		private static IDisposableCom<Word.Application> GetOrCreateWordApplication() {
			IDisposableCom<Word.Application> excelApplication = GetWordApplication();
			if (excelApplication == null) {
				excelApplication = DisposableCom.Create(new Word.Application());
			}
			InitializeVariables(excelApplication);
			return excelApplication;
		}

		/// <summary>
		/// Initialize static outlook variables like version and currentuser
		/// </summary>
		/// <param name="excelApplication"></param>
		private static void InitializeVariables(IDisposableCom<Word.Application> wordApplication) {
			if (wordApplication == null || wordApplication.ComObject == null || wordVersion != null) {
				return;
			}
			try {
				wordVersion = new Version(wordApplication.ComObject.Version);
				LOG.InfoFormat("Using Excel {0}", wordVersion);
			} catch (Exception exVersion) {
				LOG.Error(exVersion);
				LOG.Warn("Assuming Word version 1997.");
				wordVersion = new Version((int)Greenshot.Interop.Office.OfficeVersion.OFFICE_97, 0, 0, 0);
			}
		}
	}
}
