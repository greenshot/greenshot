// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: https://getgreenshot.org/
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
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Xml;
using Greenshot.Base.Interfaces;
using Dapplo.Windows.Com;
using Greenshot.Plugin.Office.OfficeExport.Entities;
using Greenshot.Plugin.Office.OfficeInterop;
using System.Drawing;

namespace Greenshot.Plugin.Office.OfficeExport
{
    /// <summary>
    ///     OneNote exporter
    ///     More details about OneNote: https://msdn.microsoft.com/en-us/magazine/ff796230.aspx
    /// </summary>
    public class OneNoteExporter
    {
        private const string XmlImageContent = "<one:Image format=\"png\"><one:Size width=\"{1}.0\" height=\"{2}.0\" isSetByUser=\"true\" /><one:Data>{0}</one:Data></one:Image>";

        private const string XmlOutline =
            "<?xml version=\"1.0\"?><one:Page xmlns:one=\"{2}\" ID=\"{1}\"><one:Title><one:OE><one:T>{3}</one:T></one:OE></one:Title>{0}</one:Page>";

        private const string OnenoteNamespace2010 = "http://schemas.microsoft.com/office/onenote/2010/onenote";
        private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OneNoteExporter));

        /// <summary>
        ///     Create a new page in the "unfiled notes section", with the title of the capture, and export the capture there.
        /// </summary>
        /// <param name="png">The capture encoded as PNG</param>
        /// <param name="imageSize">Size of the capture</param>
        /// <param name="title">Title of the new page</param>
        /// <returns>bool true if export worked</returns>
        public bool ExportToNewPage(EncodedImage png, Size imageSize, string title)
        {
            using var oneNoteApplication = GetOrCreateOneNoteApplication();
            if (oneNoteApplication == null)
            {
                LOG.Error("Failed to get or create OneNote application instance");
                return false;
            }

            var newPage = new OneNotePage();
            string unfiledNotesSectionId = GetSectionId(oneNoteApplication, SpecialLocation.slUnfiledNotesSection);
            if (unfiledNotesSectionId == null)
            {
                LOG.Error("Failed to get unfiled notes section ID");
                return false;
            }

            string pageId;
            oneNoteApplication.ComObject.CreateNewPage(unfiledNotesSectionId, out pageId, NewPageStyle.npsDefault);
            newPage.Id = pageId;
            // Set the new name, this is automatically done in the export to page
            newPage.Name = title;
            return ExportToPage(oneNoteApplication, png, imageSize, newPage);
        }

        /// <summary>
        ///     Export the capture to the specified page
        /// </summary>
        /// <param name="png">The capture encoded as PNG</param>
        /// <param name="imageSize">Size of the capture</param>
        /// <param name="page">OneNotePage</param>
        /// <returns>bool true if everything worked</returns>
        public bool ExportToPage(EncodedImage png, Size imageSize, OneNotePage page)
        {
            using var oneNoteApplication = GetOrCreateOneNoteApplication();
            if (oneNoteApplication == null)
            {
                LOG.Error("Failed to get or create OneNote application instance");
                return false;
            }

            return ExportToPage(oneNoteApplication, png, imageSize, page);
        }

        /// <summary>
        ///     Export the capture to the specified page
        /// </summary>
        /// <param name="oneNoteApplication">IOneNoteApplication</param>
        /// <param name="png">The capture encoded as PNG</param>
        /// <param name="imageSize">Size of the capture</param>
        /// <param name="page">OneNotePage</param>
        /// <returns>bool true if everything worked</returns>
        private bool ExportToPage(IDisposableCom<IOneNoteApplication> oneNoteApplication, EncodedImage png, Size imageSize, OneNotePage page)
        {
            if (oneNoteApplication == null)
            {
                LOG.Error("OneNote application instance is null");
                return false;
            }

            var base64String = System.Runtime.InteropServices.MemoryMarshal.TryGetArray(png.Bytes, out var buffer) && buffer.Array != null
                ? Convert.ToBase64String(buffer.Array, buffer.Offset, buffer.Count)
                : Convert.ToBase64String(png.ToArray());
            var imageXmlStr = string.Format(XmlImageContent, base64String, imageSize.Width, imageSize.Height);
            var pageChangesXml = string.Format(XmlOutline, imageXmlStr, page.Id, OnenoteNamespace2010, SecurityElement.Escape(page.Name));
            LOG.DebugFormat("Updating OneNote page {0}", page.Id);
            oneNoteApplication.ComObject.UpdatePageContent(pageChangesXml, DateTime.MinValue, XMLSchema.xs2010, false);
            try
            {
                oneNoteApplication.ComObject.NavigateTo(page.Id, null, false);
            }
            catch (Exception ex)
            {
                LOG.Warn("Unable to navigate to the target page", ex);
            }

            return true;
        }

        /// <summary>
        ///     Is OneNote running? OneNote doesn't register in the Running Object Table, so GetActiveObject never finds it.
        ///     Creating OneNote.Application connects to the running OneNote instead of starting a second one.
        /// </summary>
        private static bool IsOneNoteRunning()
        {
            var processes = Process.GetProcessesByName("ONENOTE");
            foreach (var process in processes)
            {
                process.Dispose();
            }

            return processes.Length > 0;
        }

        /// <summary>
        ///     The running OneNote, or a new instance (see IsOneNoteRunning why this doesn't use GetActiveObject)
        /// </summary>
        private IDisposableCom<IOneNoteApplication> GetOrCreateOneNoteApplication() =>
            DisposableCom.Create((IOneNoteApplication) Activator.CreateInstance(Type.GetTypeFromProgID("OneNote.Application", true)));

        /// <summary>
        ///     Get the pages of a running OneNote, opening the destination menu doesn't start OneNote
        /// </summary>
        /// <returns></returns>
        public IList<OneNotePage> GetPages()
        {
            var pages = new List<OneNotePage>();
            try
            {
                if (!IsOneNoteRunning())
                {
                    return pages;
                }

                using var oneNoteApplication = GetOrCreateOneNoteApplication();
                if (oneNoteApplication != null)
                {
                    // ReSharper disable once RedundantAssignment
                    string notebookXml = "";
                    oneNoteApplication.ComObject.GetHierarchy("", HierarchyScope.hsPages, out notebookXml, XMLSchema.xs2010);
                    if (!string.IsNullOrEmpty(notebookXml))
                    {
                        LOG.Debug(notebookXml);
                        StringReader reader = null;
                        try
                        {
                            reader = new StringReader(notebookXml);
                            using var xmlReader = new XmlTextReader(reader);
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
                                        currentNotebook = new OneNoteNotebook
                                        {
                                            Id = xmlReader.GetAttribute("ID"),
                                            Name = xmlReader.GetAttribute("name")
                                        };
                                    }
                                }

                                if ("one:Section".Equals(xmlReader.Name))
                                {
                                    string id = xmlReader.GetAttribute("ID");
                                    if (id != null && (currentSection == null || !id.Equals(currentSection.Id)))
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
                                    if ((page.Id == null) || (page.Name == null) || (page.Parent == null) || (page.Parent.Parent == null))
                                    {
                                        continue;
                                    }

                                    page.IsCurrentlyViewed = "true".Equals(xmlReader.GetAttribute("isCurrentlyViewed"));
                                    pages.Add(page);
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
            catch (COMException cEx)
            {
                if (cEx.ErrorCode == unchecked((int) 0x8002801D))
                {
                    LOG.Warn(
                        "Wrong registry keys, to solve this remove the OneNote key as described here: https://microsoftmercenary.com/wp/outlook-excel-interop-calls-breaking-solved/");
                }

                LOG.Warn("Problem retrieving onenote destinations, ignoring: ", cEx);
            }
            catch (Exception ex)
            {
                LOG.Warn("Problem retrieving onenote destinations, ignoring: ", ex);
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
        private string GetSectionId(IDisposableCom<IOneNoteApplication> oneNoteApplication, SpecialLocation specialLocation)
        {
            if (oneNoteApplication == null)
            {
                return null;
            }

            oneNoteApplication.ComObject.GetSpecialLocation(specialLocation, out string sectionPath);
            // Opening the section gives its ID, also when it's in no open notebook (where the hierarchy doesn't list it)
            oneNoteApplication.ComObject.OpenHierarchy(sectionPath, string.Empty, out string sectionId, CreateFileType.cftNone);
            return sectionId;
        }
    }
}