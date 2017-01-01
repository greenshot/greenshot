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

using System.Runtime.InteropServices;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Greenshot.Interop.Office {

	public class OneNoteExporter {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(OneNoteExporter));
		private const string XmlImageContent = "<one:Image format=\"png\"><one:Size width=\"{1}.0\" height=\"{2}.0\" isSetByUser=\"true\" /><one:Data>{0}</one:Data></one:Image>";
		private const string XmlOutline = "<?xml version=\"1.0\"?><one:Page xmlns:one=\"{2}\" ID=\"{1}\"><one:Title><one:OE><one:T><![CDATA[{3}]]></one:T></one:OE></one:Title>{0}</one:Page>";
		private const string OnenoteNamespace2010 = "http://schemas.microsoft.com/office/onenote/2010/onenote";

		/// <summary>
		/// Create a new page in the "unfiled notes section", with the title of the capture, and export the capture there.
		/// </summary>
		/// <param name="surfaceToUpload">ISurface</param>
		/// <returns>bool true if export worked</returns>
		public static bool ExportToNewPage(ISurface surfaceToUpload) {
			using(IOneNoteApplication oneNoteApplication = COMWrapper.GetOrCreateInstance<IOneNoteApplication>()) {
				OneNotePage newPage = new OneNotePage();
				string unfiledNotesSectionId = GetSectionId(oneNoteApplication, SpecialLocation.slUnfiledNotesSection);
				if(unfiledNotesSectionId != null) {
					string pageId;
					oneNoteApplication.CreateNewPage(unfiledNotesSectionId, out pageId, NewPageStyle.npsDefault);
					newPage.ID = pageId;
					// Set the new name, this is automatically done in the export to page
					newPage.Name = surfaceToUpload.CaptureDetails.Title;
					return ExportToPage(oneNoteApplication, surfaceToUpload, newPage);
				}
			}
			return false;
		}

		/// <summary>
		/// Export the capture to the specified page
		/// </summary>
		/// <param name="surfaceToUpload">ISurface</param>
		/// <param name="page">OneNotePage</param>
		/// <returns>bool true if everything worked</returns>
		public static bool ExportToPage(ISurface surfaceToUpload, OneNotePage page) {
			using(IOneNoteApplication oneNoteApplication = COMWrapper.GetOrCreateInstance<IOneNoteApplication>()) {
				return ExportToPage(oneNoteApplication, surfaceToUpload, page);
			}
		}

		/// <summary>
		/// Export the capture to the specified page
		/// </summary>
		/// <param name="oneNoteApplication">IOneNoteApplication</param>
		/// <param name="surfaceToUpload">ISurface</param>
		/// <param name="page">OneNotePage</param>
		/// <returns>bool true if everything worked</returns>
		private static bool ExportToPage(IOneNoteApplication oneNoteApplication, ISurface surfaceToUpload, OneNotePage page) {
			if(oneNoteApplication == null) {
				return false;
			}
			using (MemoryStream pngStream = new MemoryStream()) {
				SurfaceOutputSettings pngOutputSettings = new SurfaceOutputSettings(OutputFormat.png, 100, false);
				ImageOutput.SaveToStream(surfaceToUpload, pngStream, pngOutputSettings);
				string base64String = Convert.ToBase64String(pngStream.GetBuffer());
				string imageXmlStr = string.Format(XmlImageContent, base64String, surfaceToUpload.Image.Width, surfaceToUpload.Image.Height);
				string pageChangesXml = string.Format(XmlOutline, imageXmlStr, page.ID, OnenoteNamespace2010, page.Name);
				Log.InfoFormat("Sending XML: {0}", pageChangesXml);
				oneNoteApplication.UpdatePageContent(pageChangesXml, DateTime.MinValue, XMLSchema.xs2010, false);
				try {
					oneNoteApplication.NavigateTo(page.ID, null, false);
				} catch(Exception ex) {
					Log.Warn("Unable to navigate to the target page", ex);
				}
				return true;
			}
		}

		/// <summary>
		/// Retrieve the Section ID for the specified special location
		/// </summary>
		/// <param name="oneNoteApplication"></param>
		/// <param name="specialLocation">SpecialLocation</param>
		/// <returns>string with section ID</returns>
		private static string GetSectionId(IOneNoteApplication oneNoteApplication, SpecialLocation specialLocation) {
			if(oneNoteApplication == null) {
				return null;
			}
			string unfiledNotesPath;
			oneNoteApplication.GetSpecialLocation(specialLocation, out unfiledNotesPath);

			string notebookXml;
			oneNoteApplication.GetHierarchy("", HierarchyScope.hsPages, out notebookXml, XMLSchema.xs2010);
			if(!string.IsNullOrEmpty(notebookXml)) {
				Log.Debug(notebookXml);
				StringReader reader = null;
				try {
					reader = new StringReader(notebookXml);
					using(XmlTextReader xmlReader = new XmlTextReader(reader)) {
						while(xmlReader.Read()) {
							if("one:Section".Equals(xmlReader.Name)) {
								string id = xmlReader.GetAttribute("ID");
								string path = xmlReader.GetAttribute("path");
								if(unfiledNotesPath.Equals(path)) {
									return id;
								}
							}
						}
					}
				} finally
				{
					reader?.Dispose();
				}
			}
			return null;
		}

		/// <summary>
		/// Get the captions of all the open word documents
		/// </summary>
		/// <returns></returns>
		public static List<OneNotePage> GetPages() {
			List<OneNotePage> pages = new List<OneNotePage>();
			try {
				using (IOneNoteApplication oneNoteApplication = COMWrapper.GetOrCreateInstance<IOneNoteApplication>()) {
					if (oneNoteApplication != null) {
						string notebookXml;
						oneNoteApplication.GetHierarchy("", HierarchyScope.hsPages, out notebookXml, XMLSchema.xs2010);
						if (!string.IsNullOrEmpty(notebookXml)) {
							Log.Debug(notebookXml);
							StringReader reader = null;
							try {
								reader = new StringReader(notebookXml);
								using (XmlTextReader xmlReader = new XmlTextReader(reader)) {
									reader = null;
									OneNoteSection currentSection = null;
									OneNoteNotebook currentNotebook = null;
									while (xmlReader.Read()) {
										if ("one:Notebook".Equals(xmlReader.Name)) {
											string id = xmlReader.GetAttribute("ID");
											if (id != null && (currentNotebook == null || !id.Equals(currentNotebook.ID))) {
												currentNotebook = new OneNoteNotebook
												{
													ID = xmlReader.GetAttribute("ID"),
													Name = xmlReader.GetAttribute("name")
												};
											}
										}
										if ("one:Section".Equals(xmlReader.Name)) {
											string id = xmlReader.GetAttribute("ID");
											if (id != null && (currentSection == null || !id.Equals(currentSection.ID))) {
												currentSection = new OneNoteSection
												{
													ID = xmlReader.GetAttribute("ID"),
													Name = xmlReader.GetAttribute("name"),
													Parent = currentNotebook
												};
											}
										}
										if ("one:Page".Equals(xmlReader.Name)) {
											// Skip deleted items
											if ("true".Equals(xmlReader.GetAttribute("isInRecycleBin"))) {
												continue;
											}
											OneNotePage page = new OneNotePage
											{
												Parent = currentSection,
												Name = xmlReader.GetAttribute("name"),
												ID = xmlReader.GetAttribute("ID")
											};
											if (page.ID == null || page.Name == null) {
												continue;
											}
											page.IsCurrentlyViewed = "true".Equals(xmlReader.GetAttribute("isCurrentlyViewed"));
											pages.Add(page);
										}
									}
								}
							} finally
							{
								reader?.Dispose();
							}
						}
					}
				}
			} catch (COMException cEx) {
				if (cEx.ErrorCode == unchecked((int)0x8002801D)) {
					Log.Warn("Wrong registry keys, to solve this remove the OneNote key as described here: http://microsoftmercenary.com/wp/outlook-excel-interop-calls-breaking-solved/");
				}
				Log.Warn("Problem retrieving onenote destinations, ignoring: ", cEx);
			} catch (Exception ex) {
					Log.Warn("Problem retrieving onenote destinations, ignoring: ", ex);
			}
			pages.Sort(delegate(OneNotePage p1, OneNotePage p2) {
				if(p1.IsCurrentlyViewed || p2.IsCurrentlyViewed) {
					return p2.IsCurrentlyViewed.CompareTo(p1.IsCurrentlyViewed);
				}
				return String.Compare(p1.DisplayName, p2.DisplayName, StringComparison.Ordinal);
			});
			return pages;
		}
	}
}
