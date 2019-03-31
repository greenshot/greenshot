//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
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
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Dapplo.Log;
using Dapplo.Windows.Com;
using Greenshot.Addon.Office.Configuration;
using Greenshot.Addon.Office.OfficeInterop;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Word;
using Version = System.Version;

namespace Greenshot.Addon.Office.OfficeExport
{
    /// <summary>
    /// This makes it possible to export to word
    /// </summary>
    public class WordExporter
    {
        private static readonly LogSource Log = new LogSource();
        private static Version _wordVersion;
        private readonly IOfficeConfiguration _officeConfiguration;

        /// <summary>
        /// Constructor used for dependency injection
        /// </summary>
        /// <param name="officeConfiguration"></param>
        public WordExporter(IOfficeConfiguration officeConfiguration)
        {
            _officeConfiguration = officeConfiguration;
        }

        /// <summary>
        ///     Helper method to add the file as image to the selection
        /// </summary>
        /// <param name="selection"></param>
        /// <param name="tmpFile"></param>
        /// <returns></returns>
        private IDisposableCom<InlineShape> AddPictureToSelection(IDisposableCom<Selection> selection, string tmpFile)
        {
            using (var shapes = DisposableCom.Create(selection.ComObject.InlineShapes))
            {
                var shape = DisposableCom.Create(shapes.ComObject.AddPicture(tmpFile, false, true, Type.Missing));
                // Lock aspect ratio
                if (_officeConfiguration.WordLockAspectRatio)
                {
                    shape.ComObject.LockAspectRatio = MsoTriState.msoTrue;
                }
                selection.ComObject.InsertAfter("\r\n");
                selection.ComObject.MoveDown(WdUnits.wdLine, 1, Type.Missing);
                return shape;
            }
        }

        /// <summary>
        ///     Call this to get the running Word application, or create a new instance
        /// </summary>
        /// <returns>ComDisposable for Word.Application</returns>
        private IDisposableCom<Application> GetOrCreateWordApplication()
        {
            var wordApplication = GetWordApplication();
            if (wordApplication == null)
            {
                wordApplication = DisposableCom.Create(new Application());
            }
            InitializeVariables(wordApplication);
            return wordApplication;
        }

        /// <summary>
        ///     Call this to get the running Word application, returns null if there isn't any.
        /// </summary>
        /// <returns>ComDisposable for Word.Application or null</returns>
        private IDisposableCom<Application> GetWordApplication()
        {
            IDisposableCom<Application> wordApplication;
            try
            {
                wordApplication = OleAut32Api.GetActiveObject<Application>("Word.Application");
            }
            catch (Exception)
            {
                // Ignore, probably no word running
                return null;
            }
            if ((wordApplication != null) && (wordApplication.ComObject != null))
            {
                InitializeVariables(wordApplication);
            }
            return wordApplication;
        }

        /// <summary>
        ///     Get the captions of all the open word documents
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetWordDocuments()
        {
            using (var wordApplication = GetWordApplication())
            {
                if (wordApplication == null)
                {
                    yield break;
                }
                using (var documents = DisposableCom.Create(wordApplication.ComObject.Documents))
                {
                    for (int i = 1; i <= documents.ComObject.Count; i++)
                    {
                        using (var document = DisposableCom.Create(documents.ComObject[i]))
                        {
                            if (document.ComObject.ReadOnly)
                            {
                                continue;
                            }
                            if (IsAfter2003())
                            {
                                if (document.ComObject.Final)
                                {
                                    continue;
                                }
                            }
                            using (var activeWindow = DisposableCom.Create(document.ComObject.ActiveWindow))
                            {
                                yield return activeWindow.ComObject.Caption;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Initialize static word variables like version
        /// </summary>
        /// <param name="wordApplication"></param>
        private void InitializeVariables(IDisposableCom<Application> wordApplication)
        {
            if ((wordApplication == null) || (wordApplication.ComObject == null) || (_wordVersion != null))
            {
                return;
            }
            if (!Version.TryParse(wordApplication.ComObject.Version, out _wordVersion))
            {
                Log.Warn().WriteLine("Assuming Word version 1997.");
                _wordVersion = new Version((int)OfficeVersions.Office97, 0, 0, 0);
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
                using (var documents = DisposableCom.Create(wordApplication.ComObject.Documents))
                {
                    for (int i = 1; i <= documents.ComObject.Count; i++)
                    {
                        using (var wordDocument = DisposableCom.Create((_Document)documents.ComObject[i]))
                        {
                            using (var activeWindow = DisposableCom.Create(wordDocument.ComObject.ActiveWindow))
                            {
                                if (activeWindow.ComObject.Caption.StartsWith(wordCaption))
                                {
                                    return InsertIntoExistingDocument(wordApplication, wordDocument, tmpFile, null, null);
                                }
                            }
                        }
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
        /// <param name="address">string</param>
        /// <param name="tooltip">string with the tooltip of the image</param>
        /// <returns>bool</returns>
        internal bool InsertIntoExistingDocument(IDisposableCom<Application> wordApplication, IDisposableCom<_Document> wordDocument, string tmpFile, string address, string tooltip)
        {
            // Bug #1517: image will be inserted into that document, where the focus was last. It will not inserted into the chosen one.
            // Solution: Make sure the selected document is active, otherwise the insert will be made in a different document!
            try
            {
                wordDocument.ComObject.Activate();
                // ReSharper disable once EmptyGeneralCatchClause
            }
            catch (Exception ex)
            {
                Log.Warn().WriteLine(ex);
            }
            using (var selection = DisposableCom.Create(wordApplication.ComObject.Selection))
            {
                if (selection == null)
                {
                    Log.Info().WriteLine("No selection to insert {0} into found.", tmpFile);
                    return false;
                }
                // Add Picture
                using (var shape = AddPictureToSelection(selection, tmpFile))
                {
                    if (!string.IsNullOrEmpty(address))
                    {
                        object screentip = Type.Missing;
                        if (!string.IsNullOrEmpty(tooltip))
                        {
                            screentip = tooltip;
                        }
                        try
                        {
                            using (var hyperlinks = DisposableCom.Create(wordDocument.ComObject.Hyperlinks))
                            {
                                hyperlinks.ComObject.Add(shape, screentip, Type.Missing, screentip, Type.Missing, Type.Missing);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warn().WriteLine("Couldn't add hyperlink for image: {0}", e.Message);
                        }
                    }
                }
                try
                {
                    using (var activeWindow = DisposableCom.Create(wordDocument.ComObject.ActiveWindow))
                    {
                        activeWindow.ComObject.Activate();
                        using (var activePane = DisposableCom.Create(activeWindow.ComObject.ActivePane))
                        {
                            using (var view = DisposableCom.Create(activePane.ComObject.View))
                            {
                                view.ComObject.Zoom.Percentage = 100;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e.InnerException != null)
                    {
                        Log.Warn().WriteLine("Couldn't set zoom to 100, error: {0}", e.InnerException.Message);
                    }
                    else
                    {
                        Log.Warn().WriteLine("Couldn't set zoom to 100, error: {0}", e.Message);
                    }
                }
                try
                {
                    wordApplication.ComObject.Activate();
                    // ReSharper disable once EmptyGeneralCatchClause
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine(ex);
                }
                try
                {
                    wordDocument.ComObject.Activate();
                    // ReSharper disable once EmptyGeneralCatchClause
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine(ex);
                }
                return true;
            }
        }

        /// <summary>
        /// Insert a capture into a new document
        /// </summary>
        /// <param name="tmpFile">string</param>
        /// <param name="address">string</param>
        /// <param name="tooltip">string</param>
        public void InsertIntoNewDocument(string tmpFile, string address, string tooltip)
        {
            using (var wordApplication = GetOrCreateWordApplication())
            {
                if (wordApplication == null)
                {
                    return;
                }
                wordApplication.ComObject.Visible = true;
                wordApplication.ComObject.Activate();
                // Create new Document
                object template = string.Empty;
                object newTemplate = false;
                object documentType = 0;
                object documentVisible = true;
                using (var documents = DisposableCom.Create(wordApplication.ComObject.Documents))
                {
                    using (var wordDocument = DisposableCom.Create(documents.ComObject.Add(template, newTemplate, documentType, documentVisible)))
                    {
                        using (var selection = DisposableCom.Create(wordApplication.ComObject.Selection))
                        {
                            // Add Picture
                            using (var shape = AddPictureToSelection(selection, tmpFile))
                            {
                                if (!string.IsNullOrEmpty(address))
                                {
                                    object screentip = Type.Missing;
                                    if (!string.IsNullOrEmpty(tooltip))
                                    {
                                        screentip = tooltip;
                                    }
                                    try
                                    {
                                        using (var hyperlinks = DisposableCom.Create(wordDocument.ComObject.Hyperlinks))
                                        {
                                            using (DisposableCom.Create(hyperlinks.ComObject.Add(shape, screentip, Type.Missing, screentip, Type.Missing, Type.Missing)))
                                            {
                                                // Do nothing
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Warn().WriteLine("Couldn't add hyperlink for image: {0}", e.Message);
                                    }
                                }
                            }
                        }
                        try
                        {
                            wordDocument.ComObject.Activate();
                            // ReSharper disable once EmptyGeneralCatchClause
                        }
                        catch (Exception ex)
                        {
                            Log.Warn().WriteLine(ex);
                        }
                        try
                        {
                            using (var activeWindow = DisposableCom.Create(wordDocument.ComObject.ActiveWindow))
                            {
                                activeWindow.ComObject.Activate();
                            }
                            // ReSharper disable once EmptyGeneralCatchClause
                        }
                        catch (Exception ex)
                        {
                            Log.Warn().WriteLine(ex);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Check if the used version is higher than Office 2003
        /// </summary>
        /// <returns></returns>
        private bool IsAfter2003()
        {
            return _wordVersion.Major > (int)OfficeVersions.Office2003;
        }
    }
}