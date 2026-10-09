//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: https://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using Dapplo.Ini;
using Dapplo.Windows.Com;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Word;

namespace Greenshot.Plugin.Office.OfficeExport
{
    /// <summary>
    /// This makes it possible to export to word
    /// </summary>
    public class WordExporter
    {
        private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(WordExporter));

        private static readonly IOfficeConfiguration _officeConfiguration = IniConfigRegistry.GetSection<IOfficeConfiguration>();

        /// <summary>
        ///     Helper method to add the file as image to the selection
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="tmpFile"></param>
        private void AddPictureToSelection(IDisposableCom<Selection> selection, string tmpFile)
        {
            using var shapes = DisposableCom.Create(selection.ComObject.InlineShapes);
            using var shape = DisposableCom.Create(shapes.ComObject.AddPicture(tmpFile, false, true, Type.Missing));
            // Lock aspect ratio
            if (_officeConfiguration.WordLockAspectRatio)
            {
                shape.ComObject.LockAspectRatio = MsoTriState.msoTrue;
            }

            selection.ComObject.InsertAfter("\r\n");
            selection.ComObject.MoveDown(WdUnits.wdLine, 1, Type.Missing);
        }

        /// <summary>
        ///     Call this to get the running Word application, or create a new instance
        /// </summary>
        /// <returns>ComDisposable for Word.Application</returns>
        private IDisposableCom<Application> GetOrCreateWordApplication() => GetWordApplication() ?? DisposableCom.Create(new Application());

        /// <summary>
        ///     Call this to get the running Word application, returns null if there isn't any.
        /// </summary>
        /// <returns>ComDisposable for Word.Application or null</returns>
        private IDisposableCom<Application> GetWordApplication()
        {
            try
            {
                return OleAut32Api.GetActiveObject<Application>("Word.Application");
            }
            catch (Exception ex)
            {
                LOG.Warn("Unexpected error while getting Word application instance.", ex);
                return null;
            }
        }

        /// <summary>
        ///     Get the captions of all the open word documents
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetWordDocuments()
        {
            using var wordApplication = GetWordApplication();
            if (wordApplication == null)
            {
                yield break;
            }

            using var documents = DisposableCom.Create(wordApplication.ComObject.Documents);
            for (int i = 1; i <= documents.ComObject.Count; i++)
            {
                using var document = DisposableCom.Create(documents.ComObject[i]);
                if (document.ComObject.ReadOnly || document.ComObject.Final)
                {
                    continue;
                }

                using var activeWindow = DisposableCom.Create(document.ComObject.ActiveWindow);
                yield return activeWindow.ComObject.Caption;
            }
        }

        /// <summary>
        ///     Insert the bitmap stored under the tempfile path into the word document with the supplied caption
        /// </summary>
        /// <param name="wordCaption"></param>
        /// <param name="tmpFile"></param>
        /// <returns>bool</returns>
        public bool InsertIntoExistingDocument(string wordCaption, string tmpFile)
        {
            using (var wordApplication = GetWordApplication())
            {
                if (wordApplication == null)
                {
                    return false;
                }

                using var documents = DisposableCom.Create(wordApplication.ComObject.Documents);
                for (int i = 1; i <= documents.ComObject.Count; i++)
                {
                    using var wordDocument = DisposableCom.Create((_Document) documents.ComObject[i]);
                    using var activeWindow = DisposableCom.Create(wordDocument.ComObject.ActiveWindow);
                    if (activeWindow.ComObject.Caption.StartsWith(wordCaption))
                    {
                        return InsertIntoExistingDocument(wordApplication, wordDocument, tmpFile);
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Internal method for the insert
        /// </summary>
        /// <param name="wordApplication">IDisposableCom with Application</param>
        /// <param name="wordDocument">IDisposableCom with _Document</param>
        /// <param name="tmpFile">string</param>
        /// <returns>bool</returns>
        internal bool InsertIntoExistingDocument(IDisposableCom<Application> wordApplication, IDisposableCom<_Document> wordDocument, string tmpFile)
        {
            // Bug #1517: image will be inserted into that document, where the focus was last. It will not inserted into the chosen one.
            // Solution: Make sure the selected document is active, otherwise the insert will be made in a different document!
            try
            {
                wordDocument.ComObject.Activate();
            }
            catch (Exception ex)
            {
                LOG.Warn("Error activating worddocument", ex);
            }

            using var selection = DisposableCom.Create(wordApplication.ComObject.Selection);
            if (selection == null)
            {
                LOG.InfoFormat("No selection to insert {0} into found.", tmpFile);
                return false;
            }

            AddPictureToSelection(selection, tmpFile);

            try
            {
                // When called for Outlook, the follow error is created: This object model command is not available in e-mail
                using var activeWindow = DisposableCom.Create(wordDocument.ComObject.ActiveWindow);
                activeWindow.ComObject.Activate();
                using var activePane = DisposableCom.Create(activeWindow.ComObject.ActivePane);
                using var view = DisposableCom.Create(activePane.ComObject.View);
                view.ComObject.Zoom.Percentage = 100;
            }
            catch (Exception e)
            {
                LOG.WarnFormat("Couldn't set zoom to 100, error: {0}", e.InnerException?.Message ?? e.Message);
            }

            try
            {
                wordApplication.ComObject.Activate();
            }
            catch (Exception ex)
            {
                LOG.Warn("Error activating word application", ex);
            }

            try
            {
                wordDocument.ComObject.Activate();
            }
            catch (Exception ex)
            {
                LOG.Warn("Error activating word document", ex);
            }

            return true;
        }

        /// <summary>
        /// Insert a capture into a new document
        /// </summary>
        /// <param name="tmpFile">string</param>
        /// <returns>true if it worked</returns>
        public bool InsertIntoNewDocument(string tmpFile)
        {
            using var wordApplication = GetOrCreateWordApplication();
            wordApplication.ComObject.Visible = true;
            wordApplication.ComObject.Activate();
            // Create new Document
            object template = string.Empty;
            object newTemplate = false;
            object documentType = 0;
            object documentVisible = true;
            using var documents = DisposableCom.Create(wordApplication.ComObject.Documents);
            using var wordDocument = DisposableCom.Create(documents.ComObject.Add(template, newTemplate, documentType, documentVisible));
            using (var selection = DisposableCom.Create(wordApplication.ComObject.Selection))
            {
                AddPictureToSelection(selection, tmpFile);
            }

            try
            {
                wordDocument.ComObject.Activate();
            }
            catch (Exception ex)
            {
                LOG.Warn("Error activating word document", ex);
            }

            try
            {
                using var activeWindow = DisposableCom.Create(wordDocument.ComObject.ActiveWindow);
                activeWindow.ComObject.Activate();
            }
            catch (Exception ex)
            {
                LOG.Warn("Error activating window", ex);
            }

            return true;
        }
    }
}