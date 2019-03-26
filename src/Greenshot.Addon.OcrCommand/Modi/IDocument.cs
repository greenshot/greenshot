// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace Greenshot.Addon.OcrCommand.Modi
{
	/// <summary>
	///     The MODI Document object represents an ordered collection of document images saved as a single file.
	///     You can use the Create method to load an existing MDI or TIF file, or to create an empty document that you can
	///     populate with images from other documents.
	///     The OCR method performs OCR on all pages in the document, and the OnOCRProgress event reports the status of the
	///     operation and allows the user to cancel it.
	///     The Dirty property lets you know whether your document has unsaved OCR results or changes.
	///     The SaveAs method allows you to specify an image file format and a compression level.
	///     You can also use the PrintOut method to print the document to a printer or a file.
	/// </summary>
	[ComProgId("MODI.Document")]
	public interface IDocument : ICommon
	{
		/// <summary>
		///     The document's collection of pages.
		/// </summary>
		IImages Images { get; }

		/// <summary>
		///     Occurs periodically during an optical character recognition (OCR) operation. Returns the estimated percentage of
		///     the OCR operation that is complete, and allows the user to cancel the operation.
		/// </summary>
		/// <summary>
		///     Indicates whether the active document has unsaved changes.
		/// </summary>
		bool Dirty { get; }

		/// <summary>
		///     Closes the document.
		/// </summary>
		/// <param name="saveCall"></param>
		void Close(bool saveCall);

		/// <summary>
		///     Creates a new document.
		/// </summary>
		/// <param name="file">
		///     Optional String. The path and filename of the optional document file that is to be loaded into the
		///     new document.
		/// </param>
		void Create(string file);

		/// <summary>
		///     Performs optical character recognition (OCR) on the specified document or image.
		/// </summary>
		/// <param name="language">ModiLanguage</param>
		/// <param name="orientimage">
		///     Optional Boolean. Specifies whether the OCR engine attempts to determine the orientation of
		///     the page. Default is true.
		/// </param>
		/// <param name="straightenImage">
		///     Optional Boolean. Specifies whether the OCR engine attempts to "de-skew" the page to
		///     correct for small angles of misalignment from the vertical. Default is true.
		/// </param>
		void OCR(ModiLanguage language, bool orientimage, bool straightenImage);

		/// <summary>
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="fileFormat"></param>
		/// <param name="compressionLevel"></param>
		void SaveAs(string filename, FileFormat fileFormat, CompressionLevel compressionLevel);
	}
}