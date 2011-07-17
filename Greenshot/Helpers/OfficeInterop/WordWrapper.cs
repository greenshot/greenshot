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
using Greenshot.Interop;
using Greenshot.Plugin;

namespace Greenshot.Helpers.OfficeInterop {
	// See http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.applicationclass_members%28v=Office.11%29.aspx
	[ComProgId("Word.Application")]
	public interface IWordApplication : Common {
		IWordDocument ActiveDocument { get; }
		ISelection Selection {get;}
		IDocuments Documents {get;}
		bool Visible {get; set;}
	}

	// See: http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.documents_members(v=office.11).aspx
	public interface IDocuments : Common {
		int Count {get;}
		void Add(ref object Template, ref object NewTemplate, ref object DocumentType, ref object Visible);
	}

	// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.document.aspx
	public interface IWordDocument : Common {
		IWordApplication Application{ get;}
	}
	
	// See: http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.selection_members(v=office.11).aspx
	public interface ISelection : Common {
		IInlineShapes InlineShapes { get;}
		void InsertAfter(string text);
	}
	
	public interface IInlineShapes : Common {
		object AddPicture(string FileName,  object LinkToFile, object SaveWithDocument, object Range);
	}

	public class WordExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WordExporter));

		private static IWordApplication WordApplication() {
			return (IWordApplication)COMWrapper.GetOrCreateInstance(typeof(IWordApplication));
		}

		public static bool InsertIntoExistingDocument(IWordDocument wordDocument, string tmpFile) {
			if (wordDocument.Application.Selection != null) {
				AddPictureToSelection(wordDocument.Application.Selection, tmpFile);
				return true;
			}
			return false;
		}
		
		private static void AddPictureToSelection(ISelection selection, string tmpFile) {
			selection.InlineShapes.AddPicture(tmpFile, Type.Missing, Type.Missing, Type.Missing);
			//selection.InsertAfter("blablub\r\n");
		}
		private static void InsertIntoNewDocument(IWordApplication wordApplication, string tmpFile) {
			LOG.Debug("No Document, creating a new Document");
			// Create new Document
			object template = string.Empty;
			object newTemplate = false;
			object documentType = 0;
			object documentVisible = true;
			wordApplication.Documents.Add(ref template, ref newTemplate, ref documentType, ref documentVisible);
			// Add Picture
			AddPictureToSelection(wordApplication.Selection, tmpFile);
		}

		public static void ExportToWord(string tmpFile) {
			using( IWordApplication wordApplication = WordApplication() ) {
				if (wordApplication != null) {
					wordApplication.Visible = true;
					LOG.DebugFormat("Open Documents: {0}", wordApplication.Documents.Count);
					if (wordApplication.Documents.Count > 0) {
						if (wordApplication.Selection != null) {
							LOG.Debug("Selection found!");
							AddPictureToSelection(wordApplication.Selection, tmpFile);
							return;
						}
					} else {
						InsertIntoNewDocument(wordApplication, tmpFile);
						return;
					}
				}
			}
		}
	}
}
