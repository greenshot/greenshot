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
using Greenshot.Interop;

namespace Greenshot.Helpers.OfficeInterop {
	// See http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.applicationclass_members%28v=Office.11%29.aspx
	[ComProgId("Word.Application")]
	public interface IWordApplication : Common {
		IWordDocument ActiveDocument { get; }
		ISelection Selection { get; }
		IDocuments Documents { get; }
		bool Visible { get; set; }
	}

	// See: http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.documents_members(v=office.11).aspx
	public interface IDocuments : Common, Collection {
		void Add(ref object Template, ref object NewTemplate, ref object DocumentType, ref object Visible);
		IWordDocument item(int index);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.document.aspx
	public interface IWordDocument : Common {
		IWordApplication Application { get; }
		Window ActiveWindow { get; }
	}
	
	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.window_members.aspx
	public interface Window : Common {
		Pane ActivePane { get; }
		string Caption {
			get;
		}
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.pane_members.aspx
	public interface Pane : Common {
		View View { get; }
	}
	
	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.view_members.aspx
	public interface View : Common {
		Zoom Zoom { get; }
	}
	
	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.zoom_members.aspx
	public interface Zoom : Common {
		int Percentage { get; set; }
	}
		
	// See: http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.selection_members(v=office.11).aspx
	public interface ISelection : Common {
		IInlineShapes InlineShapes { get; }
		void InsertAfter(string text);
	}
	
	public interface IInlineShapes : Common {
		object AddPicture(string FileName,  object LinkToFile, object SaveWithDocument, object Range);
	}

	public class WordExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WordExporter));

		private static IWordApplication GetOrCreateWordApplication() {
			return (IWordApplication)COMWrapper.GetOrCreateInstance(typeof(IWordApplication));
		}
		private static IWordApplication GetWordApplication() {
			return (IWordApplication)COMWrapper.GetInstance(typeof(IWordApplication));
		}

		/// <summary>
		/// Insert the bitmap stored under the tempfile path into the word document with the supplied caption
		/// </summary>
		/// <param name="wordCaption"></param>
		/// <param name="tmpFile"></param>
		/// <returns></returns>
		public static bool InsertIntoExistingDocument(string wordCaption, string tmpFile) {
			using( IWordApplication wordApplication = GetWordApplication() ) {
				if (wordApplication != null) {
					for(int i = 1; i <= wordApplication.Documents.Count ; i ++) {
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
				} catch(Exception e) {
					if (e.InnerException != null) {
						LOG.WarnFormat("Couldn't set zoom to 100, error: {0}", e.InnerException.Message);
					} else {
						LOG.WarnFormat("Couldn't set zoom to 100, error: {0}", e.Message);
					}
				}
				return true;
			}
			return false;
		}
		
		private static void AddPictureToSelection(ISelection selection, string tmpFile) {
			selection.InlineShapes.AddPicture(tmpFile, Type.Missing, Type.Missing, Type.Missing);
			//selection.InsertAfter("blablub\r\n");
		}

		public static void InsertIntoNewDocument(string tmpFile) {
			using( IWordApplication wordApplication = GetOrCreateWordApplication() ) {
				if (wordApplication != null) {
					wordApplication.Visible = true;
					
					// Create new Document
					object template = string.Empty;
					object newTemplate = false;
					object documentType = 0;
					object documentVisible = true;
					wordApplication.Documents.Add(ref template, ref newTemplate, ref documentType, ref documentVisible);
					// Add Picture
					AddPictureToSelection(wordApplication.Selection, tmpFile);
				}
			}
		}
		
		/// <summary>
		/// Get the captions of all the open word documents
		/// </summary>
		/// <returns></returns>
		public static System.Collections.Generic.List<string> GetWordDocuments() {
			System.Collections.Generic.List<string> documents = new System.Collections.Generic.List<string>();
			try {
				using(IWordApplication wordApplication = GetWordApplication()) {
					if (wordApplication != null) {
						//documents.Add(wordApplication.ActiveDocument);
						for(int i = 1; i <= wordApplication.Documents.Count ; i ++) {
							IWordDocument document = wordApplication.Documents.item(i);
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
