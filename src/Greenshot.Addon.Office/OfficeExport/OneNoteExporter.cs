// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using Dapplo.Log;
using Dapplo.Windows.Com;
using Greenshot.Addon.Office.OfficeExport.Entities;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Core.Enums;
using Microsoft.Office.Interop.OneNote;

namespace Greenshot.Addon.Office.OfficeExport
{
    /// <summary>
    ///     OneNote exporter
    ///     More details about OneNote: http://msdn.microsoft.com/en-us/magazine/ff796230.aspx
    /// </summary>
    public class OneNoteExporter
    {
        private const string XmlImageContent = "<one:Image format=\"png\"><one:Size width=\"{1}.0\" height=\"{2}.0\" isSetByUser=\"true\" /><one:Data>{0}</one:Data></one:Image>";
        private const string XmlOutline = "<?xml version=\"1.0\"?><one:Page xmlns:one=\"{2}\" ID=\"{1}\"><one:Title><one:OE><one:T><![CDATA[{3}]]></one:T></one:OE></one:Title>{0}</one:Page>";
        private const string OnenoteNamespace2010 = "http://schemas.microsoft.com/office/onenote/2010/onenote";
        private static readonly LogSource Log = new LogSource();
        private readonly ICoreConfiguration _coreConfiguration;

        public OneNoteExporter(ICoreConfiguration coreConfiguration)
        {
            _coreConfiguration = coreConfiguration;
        }

        /// <summary>
        ///     Create a new page in the "unfiled notes section", with the title of the capture, and export the capture there.
        /// </summary>
        /// <param name="surfaceToUpload">ISurface</param>
        /// <returns>bool true if export worked</returns>
        public bool ExportToNewPage(ISurface surfaceToUpload)
        {
            using (var oneNoteApplication = GetOrCreateOneNoteApplication())
            {
                var newPage = new OneNotePage();
                string unfiledNotesSectionId = GetSectionId(oneNoteApplication, SpecialLocation.slUnfiledNotesSection);
                if (unfiledNotesSectionId == null)
                {
                    return false;
                }

                string pageId;
                oneNoteApplication.ComObject.CreateNewPage(unfiledNotesSectionId, out pageId, NewPageStyle.npsDefault);
                newPage.Id = pageId;
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
        public bool ExportToPage(ISurface surfaceToUpload, OneNotePage page)
        {
            using (var oneNoteApplication = GetOrCreateOneNoteApplication())
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
        private bool ExportToPage(IDisposableCom<Application> oneNoteApplication, ISurface surfaceToUpload, OneNotePage page)
        {
            if (oneNoteApplication == null)
            {
                return false;
            }

            using (var pngStream = new MemoryStream())
            {
                var pngOutputSettings = new SurfaceOutputSettings(_coreConfiguration, OutputFormats.png, 100, false);
                ImageOutput.SaveToStream(surfaceToUpload, pngStream, pngOutputSettings);
                var base64String = Convert.ToBase64String(pngStream.GetBuffer());
                var imageXmlStr = string.Format(XmlImageContent, base64String, surfaceToUpload.Screenshot.Width, surfaceToUpload.Screenshot.Height);
                var pageChangesXml = string.Format(XmlOutline, imageXmlStr, page.Id, OnenoteNamespace2010, page.Name);
                Log.Info().WriteLine("Sending XML: {0}", pageChangesXml);
                oneNoteApplication.ComObject.UpdatePageContent(pageChangesXml, DateTime.MinValue, XMLSchema.xs2010, false);
                try
                {
                    oneNoteApplication.ComObject.NavigateTo(page.Id, null, false);
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine(ex, "Unable to navigate to the target page");
                }
                return true;
            }
        }

        /// <summary>
        ///     Call this to get the running Excel application, returns null if there isn't any.
        /// </summary>
        /// <returns>ComDisposable for Excel.Application or null</returns>
        private IDisposableCom<Application> GetOneNoteApplication()
        {
            IDisposableCom<Application> oneNoteApplication;
            try
            {
                oneNoteApplication = OleAut32Api.GetActiveObject<Application>("OneNote.Application");
            }
            catch
            {
                // Ignore, probably no OneNote running
                return null;
            }
            return oneNoteApplication;
        }

        /// <summary>
        ///     Call this to get the running OneNote application, or create a new instance
        /// </summary>
        /// <returns>ComDisposable for OneNote.Application</returns>
        private IDisposableCom<Application> GetOrCreateOneNoteApplication()
        {
            var oneNoteApplication = GetOneNoteApplication();
            if (oneNoteApplication == null)
            {
                oneNoteApplication = DisposableCom.Create(new Application());
            }
            return oneNoteApplication;
        }

        /// <summary>
        ///     Get the captions of all the open word documents
        /// </summary>
        /// <returns></returns>
        public IList<OneNotePage> GetPages()
        {
            var pages = new List<OneNotePage>();
            try
            {
                using (var oneNoteApplication = GetOrCreateOneNoteApplication())
                {
                    if (oneNoteApplication != null)
                    {
                        // ReSharper disable once RedundantAssignment
                        string notebookXml = "";
                        oneNoteApplication.ComObject.GetHierarchy("", HierarchyScope.hsPages, out notebookXml, XMLSchema.xs2010);
                        if (!string.IsNullOrEmpty(notebookXml))
                        {
                            Log.Debug().WriteLine(notebookXml);
                            StringReader reader = null;
                            try
                            {
                                reader = new StringReader(notebookXml);
                                using (var xmlReader = new XmlTextReader(reader))
                                {
                                    reader = null;
                                    OneNoteSection currentSection = null;
                                    OneNoteNotebook currentNotebook = null;
                                    while (xmlReader.Read())
                                    {
                                        if ("one:Notebook".Equals(xmlReader.Name))
                                        {
                                            string id = xmlReader.GetAttribute("ID");
                                            if ((id != null) && ((currentNotebook == null) || !id.Equals(currentNotebook.Id)))
                                            {
                                                currentNotebook = new OneNoteNotebook();
                                                currentNotebook.Id = xmlReader.GetAttribute("ID");
                                                currentNotebook.Name = xmlReader.GetAttribute("name");
                                            }
                                        }
                                        if ("one:Section".Equals(xmlReader.Name))
                                        {
                                            string id = xmlReader.GetAttribute("ID");
                                            if ((id != null) && ((currentSection == null) || !id.Equals(currentSection.Id)))
                                            {
                                                currentSection = new OneNoteSection
                                                {
                                                    Id = xmlReader.GetAttribute("ID"),
                                                    Name = xmlReader.GetAttribute("name"),
                                                    Parent = currentNotebook
                                                };
                                            }
                                        }
                                        if ("one:Page".Equals(xmlReader.Name))
                                        {
                                            // Skip deleted items
                                            if ("true".Equals(xmlReader.GetAttribute("isInRecycleBin")))
                                            {
                                                continue;
                                            }

                                            var page = new OneNotePage
                                            {
                                                Parent = currentSection,
                                                Name = xmlReader.GetAttribute("name"),
                                                Id = xmlReader.GetAttribute("ID")
                                            };
                                            if ((page.Id == null) || (page.Name == null))
                                            {
                                                continue;
                                            }
                                            page.IsCurrentlyViewed = "true".Equals(xmlReader.GetAttribute("isCurrentlyViewed"));
                                            pages.Add(page);
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (reader != null)
                                {
                                    reader.Dispose();
                                }
                            }
                        }
                    }
                }
            }
            catch (COMException cEx)
            {
                if (cEx.ErrorCode == unchecked((int)0x8002801D))
                {
                    Log.Warn().WriteLine("Wrong registry keys, to solve this remove the OneNote key as described here: http://microsoftmercenary.com/wp/outlook-excel-interop-calls-breaking-solved/");
                }
                Log.Warn().WriteLine(cEx, "Problem retrieving onenote destinations, ignoring: ");
            }
            catch (Exception ex)
            {
                Log.Warn().WriteLine(ex, "Problem retrieving onenote destinations, ignoring: ");
            }
            pages.Sort((page1, page2) =>
            {
                if (page1.IsCurrentlyViewed || page2.IsCurrentlyViewed)
                {
                    return page2.IsCurrentlyViewed.CompareTo(page1.IsCurrentlyViewed);
                }
                return string.Compare(page1.DisplayName, page2.DisplayName, StringComparison.Ordinal);
            });
            return pages;
        }

        /// <summary>
        ///     Retrieve the Section ID for the specified special location
        /// </summary>
        /// <param name="oneNoteApplication"></param>
        /// <param name="specialLocation">SpecialLocation</param>
        /// <returns>string with section ID</returns>
        private string GetSectionId(IDisposableCom<Application> oneNoteApplication, SpecialLocation specialLocation)
        {
            if (oneNoteApplication == null)
            {
                return null;
            }
            // ReSharper disable once RedundantAssignment
            string unfiledNotesPath = "";
            oneNoteApplication.ComObject.GetSpecialLocation(specialLocation, out unfiledNotesPath);

            // ReSharper disable once RedundantAssignment
            string notebookXml = "";
            oneNoteApplication.ComObject.GetHierarchy("", HierarchyScope.hsPages, out notebookXml, XMLSchema.xs2010);
            if (!string.IsNullOrEmpty(notebookXml))
            {
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
                            string id = xmlReader.GetAttribute("ID");
                            string path = xmlReader.GetAttribute("path");
                            if (unfiledNotesPath.Equals(path))
                            {
                                return id;
                            }
                        }
                    }
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Dispose();
                    }
                }
            }
            return null;
        }
    }
}