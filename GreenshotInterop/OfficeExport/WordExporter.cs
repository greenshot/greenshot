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
using System.Text;

using Greenshot.Interop;

namespace Greenshot.Interop.Office {
	public class WordExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WordExporter));
		private static string version = null;

		public static bool isAfter2003() {
			if (version != null) {
				return !version.StartsWith("11");
			}
			return false;
		}
		/// <summary>
		/// Insert the bitmap stored under the tempfile path into the word document with the supplied caption
		/// </summary>
		/// <param name="wordCaption"></param>
		/// <param name="tmpFile"></param>
		/// <returns></returns>
		public static bool InsertIntoExistingDocument(string wordCaption, string tmpFile) {
			using (IWordApplication wordApplication = COMWrapper.GetInstance<IWordApplication>()) {
				if (wordApplication != null) {
					for (int i = 1; i <= wordApplication.Documents.Count; i++) {
						IWordDocument wordDocument = wordApplication.Documents.item(i);
						if (wordDocument.ActiveWindow.Caption.StartsWith(wordCaption)) {
							return InsertIntoExistingDocument(wordDocument, tmpFile);
						}
					}
				}
			}
			return false;
		}

		internal static bool InsertIntoExistingDocument(IWordDocument wordDocument, string tmpFile) {
			if (wordDocument.Application.Selection != null) {
				AddPictureToSelection(wordDocument.Application.Selection, tmpFile);
				try {
					wordDocument.ActiveWindow.ActivePane.View.Zoom.Percentage = 100;
				} catch (Exception e) {
					if (e.InnerException != null) {
						LOG.WarnFormat("Couldn't set zoom to 100, error: {0}", e.InnerException.Message);
					} else {
						LOG.WarnFormat("Couldn't set zoom to 100, error: {0}", e.Message);
					}
				}
				wordDocument.Application.Activate();
				wordDocument.Activate();
				wordDocument.ActiveWindow.Activate();
				return true;
			}
			return false;
		}

		private static void AddPictureToSelection(ISelection selection, string tmpFile) {
			selection.InlineShapes.AddPicture(tmpFile, Type.Missing, Type.Missing, Type.Missing);
			selection.InsertAfter("\r\n");
		}

		public static void InsertIntoNewDocument(string tmpFile) {
			using (IWordApplication wordApplication = COMWrapper.GetOrCreateInstance<IWordApplication>()) {
				if (wordApplication != null) {
					wordApplication.Visible = true;
					wordApplication.Activate();
					// Create new Document
					object template = string.Empty;
					object newTemplate = false;
					object documentType = 0;
					object documentVisible = true;
					IWordDocument wordDocument = wordApplication.Documents.Add(ref template, ref newTemplate, ref documentType, ref documentVisible);
					// Add Picture
					AddPictureToSelection(wordApplication.Selection, tmpFile);
					wordDocument.Activate();
					wordDocument.ActiveWindow.Activate();
				}
			}
		}

		/// <summary>
		/// Get the captions of all the open word documents
		/// </summary>
		/// <returns></returns>
		public static List<string> GetWordDocuments() {
			List<string> documents = new List<string>();
			try {
				using (IWordApplication wordApplication = COMWrapper.GetInstance<IWordApplication>()) {
					if (wordApplication != null) {
						if (version == null) {
							version = wordApplication.Version;
						}
						for (int i = 1; i <= wordApplication.Documents.Count; i++) {
							IWordDocument document = wordApplication.Documents.item(i);
							if (document.ReadOnly) {
								continue;
							}
							if (isAfter2003()) {
								if (document.Final) {
									continue;
								}
							}
							documents.Add(document.ActiveWindow.Caption);
						}
					}
				}
			} catch (Exception ex) {
				LOG.Warn("Problem retrieving word destinations, ignoring: ", ex);
			}
			return documents;
		}
	}
}
