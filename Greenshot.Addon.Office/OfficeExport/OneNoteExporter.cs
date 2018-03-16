#region Greenshot GNU General Public License

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

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using GreenshotOfficePlugin.OfficeInterop;
using GreenshotPlugin.Core.Enums;
using GreenshotPlugin.Gfx;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using Dapplo.Log;
using Dapplo.Windows.Com;

#endregion

namespace GreenshotOfficePlugin.OfficeExport
{
	public static class OneNoteExporter
	{
		private const string XmlImageContent =
			"<one:Image format=\"png\"><one:Size width=\"{1}.0\" height=\"{2}.0\" isSetByUser=\"true\" /><one:Data>{0}</one:Data></one:Image>";

		private const string XmlOutline =
			"<?xml version=\"1.0\"?><one:Page xmlns:one=\"{2}\" ID=\"{1}\"><one:Title><one:OE><one:T><![CDATA[{3}]]></one:T></one:OE></one:Title>{0}</one:Page>";

		private const string OnenoteNamespace2010 = "http://schemas.microsoft.com/office/onenote/2010/onenote";
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		///     Create a new page in the "unfiled notes section", with the title of the capture, and export the capture there.
		/// </summary>
		/// <param name="surfaceToUpload">ISurface</param>
		/// <returns>bool true if export worked</returns>
		public static bool ExportToNewPage(ISurface surfaceToUpload)
		{
			using (var oneNoteApplication = ComWrapper.GetOrCreateInstance<IOneNoteApplication>())
			{
				var newPage = new OneNotePage();
				var unfiledNotesSectionId = GetSectionId(oneNoteApplication, SpecialLocation.slUnfiledNotesSection);
			    if (unfiledNotesSectionId == null)
			    {
			        return false;
			    }

			    // ReSharper disable once RedundantAssignment
			    oneNoteApplication.CreateNewPage(unfiledNotesSectionId, out var pageId, NewPageStyle.npsDefault);
			    newPage.ID = pageId;
			    // Set the new name, this is automatically done in the export to page
			    newPage.Name = surfaceToUpload.CaptureDetails.Title;
			    return ExportToPage(oneNoteApplication, surfaceToUpload, newPage);
			}
		}

		/// <summary>
		///     Export the capture to the specified page
		/// </summary>
		/// <param name="surfaceToUpload">ISurface</param>
		/// <param name="page">OneNotePage</param>
		/// <returns>bool true if everything worked</returns>
		public static bool ExportToPage(ISurface surfaceToUpload, OneNotePage page)
		{
			using (var oneNoteApplication = ComWrapper.GetOrCreateInstance<IOneNoteApplication>())
			{
				return ExportToPage(oneNoteApplication, surfaceToUpload, page);
			}
		}

		/// <summary>
		///     Export the capture to the specified page
		/// </summary>
		/// <param name="oneNoteApplication">IOneNoteApplication</param>
		/// <param name="surfaceToUpload">ISurface</param>
		/// <param name="page">OneNotePage</param>
		/// <returns>bool true if everything worked</returns>
		private static bool ExportToPage(IOneNoteApplication oneNoteApplication, ISurface surfaceToUpload, OneNotePage page)
		{
			if (oneNoteApplication == null)
			{
				return false;
			}
			using (var pngStream = new MemoryStream())
			{
				var pngOutputSettings = new SurfaceOutputSettings(OutputFormats.png, 100, false);
				ImageOutput.SaveToStream(surfaceToUpload, pngStream, pngOutputSettings);
				var base64String = Convert.ToBase64String(pngStream.GetBuffer());
				var imageXmlStr = string.Format(XmlImageContent, base64String, surfaceToUpload.Screenshot.Width, surfaceToUpload.Screenshot.Height);
				var pageChangesXml = string.Format(XmlOutline, imageXmlStr, page.ID, OnenoteNamespace2010, page.Name);
				Log.Info().WriteLine("Sending XML: {0}", pageChangesXml);
				oneNoteApplication.UpdatePageContent(pageChangesXml, DateTime.MinValue, XMLSchema.xs2010, false);
				try
				{
					oneNoteApplication.NavigateTo(page.ID, null, false);
				}
				catch (Exception ex)
				{
					Log.Warn().WriteLine(ex, "Unable to navigate to the target page");
				}
				return true;
			}
		}

		/// <summary>
		///     Retrieve the Section ID for the specified special location
		/// </summary>
		/// <param name="oneNoteApplication"></param>
		/// <param name="specialLocation">SpecialLocation</param>
		/// <returns>string with section ID</returns>
		private static string GetSectionId(IOneNoteApplication oneNoteApplication, SpecialLocation specialLocation)
		{
			if (oneNoteApplication == null)
			{
				return null;
			}
			// ReSharper disable once RedundantAssignment
			var unfiledNotesPath = "";
			oneNoteApplication.GetSpecialLocation(specialLocation, out unfiledNotesPath);

			// ReSharper disable once RedundantAssignment
			var notebookXml = "";
			oneNoteApplication.GetHierarchy("", HierarchyScope.hsPages, out notebookXml, XMLSchema.xs2010);
		    if (string.IsNullOrEmpty(notebookXml))
		    {
		        return null;
		    }

		    Log.Debug().WriteLine(notebookXml);
		    StringReader reader = null;
		    try
		    {
		        reader = new StringReader(notebookXml);
		        using (var xmlReader = new XmlTextReader(reader))
		        {
		            while (xmlReader.Read())
		            {
		                if (!"one:Section".Equals(xmlReader.Name))
		                {
		                    continue;
		                }

		                var id = xmlReader.GetAttribute("ID");
		                var path = xmlReader.GetAttribute("path");
		                if (unfiledNotesPath.Equals(path))
		                {
		                    return id;
		                }
		            }
		        }
		    }
		    finally
		    {
		        reader?.Dispose();
		    }
		    return null;
		}

		/// <summary>
		///     Get the captions of all the open word documents
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<OneNotePage> GetPages()
		{
			using (var oneNoteApplication = ComWrapper.GetOrCreateInstance<IOneNoteApplication>())
			{
			    if (oneNoteApplication == null)
			    {
			        yield break;
			    }

			    // ReSharper disable once RedundantAssignment
			    var notebookXml = "";
			    oneNoteApplication.GetHierarchy("", HierarchyScope.hsPages, out notebookXml, XMLSchema.xs2010);
			    if (string.IsNullOrEmpty(notebookXml))
			    {
			        yield break;
			    }

			    using (var reader = new StringReader(notebookXml))
			    using (var xmlReader = new XmlTextReader(reader))
			    {
			        OneNoteSection currentSection = null;
			        OneNoteNotebook currentNotebook = null;
			        while (xmlReader.Read())
			        {
			            switch (xmlReader.Name)
			            {
			                case "one:Notebook":
			                {
			                    var id = xmlReader.GetAttribute("ID");
			                    if (id != null && (currentNotebook == null || !id.Equals(currentNotebook.ID)))
			                    {
			                        currentNotebook = new OneNoteNotebook
			                        {
			                            ID = xmlReader.GetAttribute("ID"),
			                            Name = xmlReader.GetAttribute("name")
			                        };
			                    }

			                    break;
			                }
			                case "one:Section":
			                {
			                    var id = xmlReader.GetAttribute("ID");
			                    if (id != null && (currentSection == null || !id.Equals(currentSection.ID)))
			                    {
			                        currentSection = new OneNoteSection
			                        {
			                            ID = xmlReader.GetAttribute("ID"),
			                            Name = xmlReader.GetAttribute("name"),
			                            Parent = currentNotebook
			                        };
			                    }

			                    break;
			                }
			                case "one:Page":
			                    // Skip deleted items
			                    if ("true".Equals(xmlReader.GetAttribute("isInRecycleBin")))
			                    {
			                        continue;
			                    }
			                    var page = new OneNotePage
			                    {
			                        Parent = currentSection,
			                        Name = xmlReader.GetAttribute("name"),
			                        ID = xmlReader.GetAttribute("ID")
			                    };
			                    if (page.ID == null || page.Name == null)
			                    {
			                        continue;
			                    }
			                    page.IsCurrentlyViewed = "true".Equals(xmlReader.GetAttribute("isCurrentlyViewed"));
			                    yield return page;
			                    break;
			            }
			        }
			    }
			}
		}
	}
}