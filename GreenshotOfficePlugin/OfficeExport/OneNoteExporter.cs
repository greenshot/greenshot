/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2014 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Reflection;
using System.Xml;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace Greenshot.Interop.Office {
	public class OneNotePage {
		public string PageName { get; set; }
		public string PageID { get; set; }
		public bool IsCurrentlyViewed { get; set; }
	}
	public class OneNoteExporter {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OneNoteExporter));
		private const string XML_IMAGE_CONTENT = "<one:Image format=\"png\"><one:Size width=\"{1}.0\" height=\"{2}.0\" isSetByUser=\"true\" /><one:Data>{0}</one:Data></one:Image>";
		private const string XML_OUTLINE = "<?xml version=\"1.0\"?><one:Page xmlns:one=\"{2}\" ID=\"{1}\"><one:Title><one:OE><one:T><![CDATA[{3}]]></one:T></one:OE></one:Title>{0}</one:Page>";
		private const string ONENOTE_NAMESPACE_2007 = "http://schemas.microsoft.com/office/onenote/2007/onenote";
		private const string ONENOTE_NAMESPACE_2010 = "http://schemas.microsoft.com/office/onenote/2010/onenote";

		public static void ExportToPage(ISurface surfaceToUpload, OneNotePage page) {
			using (MemoryStream pngStream = new MemoryStream()) {
				SurfaceOutputSettings pngOutputSettings = new SurfaceOutputSettings(OutputFormat.png, 100, false);
				ImageOutput.SaveToStream(surfaceToUpload, pngStream, pngOutputSettings);
				string base64String = Convert.ToBase64String(pngStream.GetBuffer());
				string imageXmlStr = string.Format(XML_IMAGE_CONTENT, base64String, surfaceToUpload.Image.Width, surfaceToUpload.Image.Height);
				string pageChangesXml = string.Format(XML_OUTLINE, new object[] { imageXmlStr, page.PageID, ONENOTE_NAMESPACE_2010, page.PageName });
				using (IOneNoteApplication oneNoteApplication = COMWrapper.GetOrCreateInstance<IOneNoteApplication>()) {
					LOG.InfoFormat("Sending XML: {0}", pageChangesXml);
					oneNoteApplication.UpdatePageContent(pageChangesXml, DateTime.MinValue, XMLSchema.xs2010, false);
					try {
						oneNoteApplication.NavigateTo(page.PageID, null, false);
					} catch(Exception ex) {
						LOG.Warn("Unable to navigate to the target page", ex);
					}
				}
			}
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
						string notebookXml = "";
						oneNoteApplication.GetHierarchy("", HierarchyScope.hsPages, out notebookXml, XMLSchema.xs2010);
						if (!string.IsNullOrEmpty(notebookXml)) {
							LOG.Debug(notebookXml);
							StringReader reader = null;
							try {
								reader = new StringReader(notebookXml);
								using (XmlTextReader xmlReader = new XmlTextReader(reader)) {
									reader = null;
									while (xmlReader.Read()) {
										if ("one:Page".Equals(xmlReader.Name)) {
											// Skip deleted items
											if("true".Equals(xmlReader.GetAttribute("isInRecycleBin"))) {
												continue;
											}
											OneNotePage page = new OneNotePage();
											page.PageName = xmlReader.GetAttribute("name");
											page.PageID = xmlReader.GetAttribute("ID");
											if(page.PageID == null || page.PageName == null) {
												continue;
											}
											page.IsCurrentlyViewed = "true".Equals(xmlReader.GetAttribute("isCurrentlyViewed"));
											pages.Add(page);
										}
									}
								}
							} finally {
								if (reader != null) {
									reader.Dispose();
								}
							}
						}
					}
				}
			} catch (Exception ex) {
				LOG.Warn("Problem retrieving onenote destinations, ignoring: ", ex);
			}
			pages.Sort(delegate(OneNotePage p1, OneNotePage p2) {
				if(p1.IsCurrentlyViewed || p2.IsCurrentlyViewed) {
					return p2.IsCurrentlyViewed.CompareTo(p1.IsCurrentlyViewed);
				}
				return p1.PageName.CompareTo(p2.PageName);
			});
			return pages;
		}
	}
}
