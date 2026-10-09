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
using Dapplo.Ini;
using Dapplo.Windows.Com;
using Greenshot.Plugin.Office.OfficeInterop;
using Microsoft.Office.Interop.Outlook;
using Microsoft.Office.Interop.Word;
using Application = Microsoft.Office.Interop.Outlook.Application;
using Exception = System.Exception;

namespace Greenshot.Plugin.Office.OfficeExport
{
    /// <summary>
    ///     Outlook exporter has all the functionality to export to outlook
    /// </summary>
    public class OutlookEmailExporter
    {
        private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OutlookEmailExporter));
        private static readonly IOfficeConfiguration _officeConfiguration = IniConfigRegistry.GetSection<IOfficeConfiguration>();

        // Schema definitions for the MAPI properties, see: http://msdn.microsoft.com/en-us/library/aa454438.aspx and: http://msdn.microsoft.com/en-us/library/bb446117.aspx
        private const string AttachmentContentId = @"http://schemas.microsoft.com/mapi/proptag/0x3712001E";

        private static string _currentUser;
        private readonly WordExporter _wordExporter = new WordExporter();

        /// <summary>
        ///     Export the image stored in tmpFile to the Inspector with the caption
        /// </summary>
        /// <param name="inspectorCaption">Caption of the inspector</param>
        /// <param name="tmpFile">Path to image file</param>
        /// <param name="attachmentName">name of the attachment (used as the tooltip of the image)</param>
        /// <returns>true if it worked</returns>
        public bool ExportToInspector(string inspectorCaption, string tmpFile, string attachmentName)
        {
            using (var outlookApplication = GetOrCreateOutlookApplication())
            {
                // Check the inline response "panel" of the explorer
                using var activeExplorer = DisposableCom.Create((_Explorer) outlookApplication.ComObject.ActiveExplorer());
                // Only if we have one and if the capture is the one we selected
                if ((activeExplorer != null) && activeExplorer.ComObject.Caption.StartsWith(inspectorCaption))
                {
                    var untypedInlineResponse = activeExplorer.ComObject.ActiveInlineResponse;
                    using (DisposableCom.Create(untypedInlineResponse))
                    {
                        switch (untypedInlineResponse)
                        {
                            case MailItem mailItem:
                                if (!mailItem.Sent)
                                {
                                    return ExportToInspector(null, activeExplorer, mailItem.Class, mailItem, tmpFile, attachmentName);
                                }

                                break;
                            case AppointmentItem appointmentItem:
                                if (_officeConfiguration.OutlookAllowExportInMeetings && !string.IsNullOrEmpty(appointmentItem.Organizer) && appointmentItem.Organizer.Equals(_currentUser))
                                {
                                    return ExportToInspector(null, activeExplorer, appointmentItem.Class, null, tmpFile, attachmentName);
                                }

                                break;
                        }
                    }
                }

                using var inspectors = DisposableCom.Create(outlookApplication.ComObject.Inspectors);
                if ((inspectors == null) || (inspectors.ComObject.Count == 0))
                {
                    return false;
                }

                LOG.DebugFormat("Got {0} inspectors to check", inspectors.ComObject.Count);
                for (int i = 1; i <= inspectors.ComObject.Count; i++)
                {
                    using var inspector = DisposableCom.Create((_Inspector) inspectors.ComObject[i]);
                    string currentCaption = inspector.ComObject.Caption;
                    if (!currentCaption.StartsWith(inspectorCaption))
                    {
                        continue;
                    }

                    var currentItemUntyped = inspector.ComObject.CurrentItem;
                    using (DisposableCom.Create(currentItemUntyped))
                    {
                        switch (currentItemUntyped)
                        {
                            case MailItem mailItem:
                                if (mailItem.Sent)
                                {
                                    continue;
                                }

                                try
                                {
                                    return ExportToInspector(inspector, null, mailItem.Class, mailItem, tmpFile, attachmentName);
                                }
                                catch (Exception exExport)
                                {
                                    LOG.Error($"Export to {currentCaption} failed.", exExport);
                                }

                                break;
                            case AppointmentItem appointmentItem:
                                if (!_officeConfiguration.OutlookAllowExportInMeetings)
                                {
                                    // skip, can't export to olAppointment
                                    continue;
                                }

                                if (!string.IsNullOrEmpty(appointmentItem.Organizer) && !appointmentItem.Organizer.Equals(_currentUser))
                                {
                                    LOG.DebugFormat("Not exporting, as organizer is set to {0} and currentuser {1} is not him.", appointmentItem.Organizer, _currentUser);
                                    continue;
                                }

                                try
                                {
                                    return ExportToInspector(inspector, null, appointmentItem.Class, null, tmpFile, attachmentName);
                                }
                                catch (Exception exExport)
                                {
                                    LOG.Error($"Export to {currentCaption} failed.", exExport);
                                }

                                break;
                            default:
                                continue;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Export the file to the supplied inspector
        /// </summary>
        /// <param name="inspector">Inspector</param>
        /// <param name="explorer">Explorer, for the inline response</param>
        /// <param name="itemClass">OlObjectClass</param>
        /// <param name="mailItem">MailItem, null for an appointment</param>
        /// <param name="tmpFile"></param>
        /// <param name="attachmentName"></param>
        /// <returns>true if it worked</returns>
        private bool ExportToInspector(IDisposableCom<_Inspector> inspector, IDisposableCom<_Explorer> explorer, OlObjectClass itemClass, MailItem mailItem, string tmpFile,
            string attachmentName)
        {
            bool isMail = OlObjectClass.olMail.Equals(itemClass);
            bool isAppointment = OlObjectClass.olAppointment.Equals(itemClass);
            if (!isMail && !isAppointment)
            {
                LOG.Warn("Item is no mail or appointment.");
                return false;
            }

            try
            {
                // Make sure the inspector is activated, only this way the word editor is active!
                // This also ensures that the window is visible!
                inspector?.ComObject.Activate();
                bool isTextFormat = isMail && OlBodyFormat.olFormatPlain.Equals(mailItem.BodyFormat);
                if (isAppointment || !isTextFormat)
                {
                    // Word is the editor of Outlook, use the word exporter
                    IDisposableCom<_Document> wordDocument = null;
                    if (explorer != null)
                    {
                        wordDocument = DisposableCom.Create((_Document) explorer.ComObject.ActiveInlineResponseWordEditor);
                    }
                    else if (inspector != null && inspector.ComObject.IsWordMail() && inspector.ComObject.EditorType == OlEditorType.olEditorWord)
                    {
                        wordDocument = DisposableCom.Create((_Document) inspector.ComObject.WordEditor);
                    }

                    if (wordDocument != null)
                    {
                        using (wordDocument)
                        {
                            using var application = DisposableCom.Create(wordDocument.ComObject.Application);
                            try
                            {
                                if (_wordExporter.InsertIntoExistingDocument(application, wordDocument, tmpFile))
                                {
                                    LOG.Info("Inserted into Wordmail");
                                    return true;
                                }
                            }
                            catch (Exception exportException)
                            {
                                LOG.Error("Error exporting to the word editor, adding it as attachment", exportException);
                            }
                        }
                    }
                }

                // An appointment can only be changed with the word editor
                if (mailItem == null)
                {
                    LOG.Info("Can't export to an appointment if no word editor is used");
                    return false;
                }

                LOG.InfoFormat("Item '{0}' has format: {1}", mailItem.Subject, mailItem.BodyFormat);

                // Create the attachment
                using var attachments = DisposableCom.Create(mailItem.Attachments);
                using var attachment = DisposableCom.Create(attachments.ComObject.Add(tmpFile, OlAttachmentType.olByValue, 1, attachmentName));
            }
            catch (Exception ex)
            {
                string caption = "n.a.";
                if (inspector != null)
                {
                    caption = inspector.ComObject.Caption;
                }
                else if (explorer != null)
                {
                    caption = explorer.ComObject.Caption;
                }

                LOG.Warn($"Problem while trying to add attachment to Item '{caption}'", ex);
                return false;
            }

            try
            {
                if (inspector != null)
                {
                    inspector.ComObject.Activate();
                }
                else
                {
                    explorer?.ComObject.Activate();
                }
            }
            catch (Exception ex)
            {
                LOG.Warn("Problem activating inspector/explorer: ", ex);
                return false;
            }

            LOG.Debug("Finished!");
            return true;
        }

        /// <summary>
        ///     Export image to a new email
        /// </summary>
        /// <param name="outlookApplication"></param>
        /// <param name="format"></param>
        /// <param name="tmpFile"></param>
        /// <param name="subject"></param>
        /// <param name="attachmentName"></param>
        /// <param name="to"></param>
        /// <param name="cc"></param>
        /// <param name="bcc"></param>
        private void ExportToNewEmail(IDisposableCom<Application> outlookApplication, EmailFormat format, string tmpFile, string subject, string attachmentName, string to,
            string cc, string bcc)
        {
            using var newItem = DisposableCom.Create((MailItem) outlookApplication.ComObject.CreateItem(OlItemType.olMailItem));
            if (newItem == null)
            {
                return;
            }

            var newMail = newItem.ComObject;
            newMail.Subject = subject;
            if (!string.IsNullOrEmpty(to))
            {
                newMail.To = to;
            }

            if (!string.IsNullOrEmpty(cc))
            {
                newMail.CC = cc;
            }

            if (!string.IsNullOrEmpty(bcc))
            {
                newMail.BCC = bcc;
            }

            newMail.BodyFormat = format == EmailFormat.Text ? OlBodyFormat.olFormatPlain : OlBodyFormat.olFormatHTML;
            // Getting the inspector makes Outlook add the default signature to the body, also a signature stored in the mailbox
            using var inspector = DisposableCom.Create((_Inspector) newMail.GetInspector);

            // Create the attachment (and dispose the COM object after using)
            using (var attachments = DisposableCom.Create(newMail.Attachments))
            {
                using var attachment = DisposableCom.Create(attachments.ComObject.Add(tmpFile, OlAttachmentType.olByValue, format == EmailFormat.Text ? 1 : 0, attachmentName));
                if (format != EmailFormat.Text)
                {
                    // Show the attachment in the body, it's referenced by the content id
                    string contentId = Guid.NewGuid().ToString();
                    using var propertyAccessor = DisposableCom.Create(attachment.ComObject.PropertyAccessor);
                    propertyAccessor.ComObject.SetProperty(AttachmentContentId, contentId);

                    string htmlImgEmbedded = $"<BR/><IMG border=0 hspace=0 alt=\"{attachmentName}\" align=baseline src=\"cid:{contentId}\"><BR/>";
                    string body = newMail.HTMLBody ?? string.Empty;
                    int bodyIndex = body.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
                    bodyIndex = bodyIndex >= 0 ? body.IndexOf(">", bodyIndex, StringComparison.Ordinal) + 1 : 0;
                    newMail.HTMLBody = bodyIndex > 0 ? body.Insert(bodyIndex, htmlImgEmbedded) : $"<HTML><BODY>{htmlImgEmbedded}{body}</BODY></HTML>";
                }
            }

            // So not save, otherwise the email is always stored in Draft folder.. (newMail.Save();)
            newMail.Display(false);
            try
            {
                inspector?.ComObject.Activate();
            }
            catch (Exception ex)
            {
                LOG.Debug("Couldn't activate the inspector", ex);
            }
        }

        /// <summary>
        ///     Helper method to create an outlook mail item with attachment
        /// </summary>
        /// <param name="format"></param>
        /// <param name="tmpFile">The file to send, do not delete the file right away!</param>
        /// <param name="subject"></param>
        /// <param name="attachmentName"></param>
        /// <param name="to"></param>
        /// <param name="cc"></param>
        /// <param name="bcc"></param>
        /// <returns>true if it worked, false if not</returns>
        public bool ExportToOutlook(EmailFormat format, string tmpFile, string subject, string attachmentName, string to, string cc, string bcc)
        {
            try
            {
                using var outlookApplication = GetOrCreateOutlookApplication();
                ExportToNewEmail(outlookApplication, format, tmpFile, subject, attachmentName, to, cc, bcc);
                return true;
            }
            catch (Exception e)
            {
                LOG.Error("Error while creating an outlook mail item: ", e);
            }

            return false;
        }

        /// <summary>
        ///     Call this to get the running Outlook application, or create a new instance
        /// </summary>
        /// <returns>IDisposableCom for Outlook.Application</returns>
        private IDisposableCom<Application> GetOrCreateOutlookApplication()
        {
            var outlookApplication = GetOutlookApplication() ?? DisposableCom.Create(new Application());
            InitializeVariables(outlookApplication);
            return outlookApplication;
        }

        /// <summary>
        ///     Call this to get the running Outlook application, returns null if there isn't any.
        /// </summary>
        /// <returns>IDisposableCom for Outlook.Application or null</returns>
        private IDisposableCom<Application> GetOutlookApplication()
        {
            IDisposableCom<Application> outlookApplication;
            try
            {
                outlookApplication = OleAut32Api.GetActiveObject<Application>("Outlook.Application");
            }
            catch (Exception ex)
            {
                LOG.Warn("Unexpected error while getting Outlook application instance.", ex);
                return null;
            }

            InitializeVariables(outlookApplication);
            return outlookApplication;
        }

        /// <summary>
        ///     Initialize the current user, used to check if appointments are ours
        /// </summary>
        /// <param name="outlookApplication"></param>
        private void InitializeVariables(IDisposableCom<Application> outlookApplication)
        {
            if (outlookApplication == null || _currentUser != null)
            {
                return;
            }

            try
            {
                using (var mapiNamespace = DisposableCom.Create(outlookApplication.ComObject.GetNamespace("MAPI")))
                {
                    using var currentUser = DisposableCom.Create(mapiNamespace.ComObject.CurrentUser);
                    _currentUser = currentUser.ComObject.Name;
                }

                LOG.InfoFormat("Current user: {0}", _currentUser);
            }
            catch (Exception exNs)
            {
                LOG.Error("Reading Outlook currentuser failed", exNs);
            }
        }

        /// <summary>
        ///     A method to retrieve all inspectors which can act as an export target
        /// </summary>
        /// <returns>IDictionary with inspector captions (window title) and object class</returns>
        public IDictionary<string, OlObjectClass> RetrievePossibleTargets()
        {
            IDictionary<string, OlObjectClass> inspectorCaptions = new SortedDictionary<string, OlObjectClass>();
            try
            {
                using var outlookApplication = GetOutlookApplication();
                if (outlookApplication == null)
                {
                    return inspectorCaptions;
                }

                // Check the inline response "panel" of the explorer
                using var activeExplorer = DisposableCom.Create(outlookApplication.ComObject.ActiveExplorer());
                if (activeExplorer != null)
                {
                    var untypedInlineResponse = activeExplorer.ComObject.ActiveInlineResponse;
                    if (untypedInlineResponse != null)
                    {
                        string caption = activeExplorer.ComObject.Caption;
                        using (DisposableCom.Create(untypedInlineResponse))
                        {
                            switch (untypedInlineResponse)
                            {
                                case MailItem mailItem:
                                    if (!mailItem.Sent)
                                    {
                                        inspectorCaptions.Add(caption, mailItem.Class);
                                    }

                                    break;
                                case AppointmentItem appointmentItem:
                                    if (_officeConfiguration.OutlookAllowExportInMeetings && !string.IsNullOrEmpty(appointmentItem.Organizer) && appointmentItem.Organizer.Equals(_currentUser))
                                    {
                                        inspectorCaptions.Add(caption, appointmentItem.Class);
                                    }

                                    break;
                            }
                        }
                    }
                }

                using var inspectors = DisposableCom.Create(outlookApplication.ComObject.Inspectors);
                if ((inspectors != null) && (inspectors.ComObject.Count > 0))
                {
                    for (int i = 1; i <= inspectors.ComObject.Count; i++)
                    {
                        using var inspector = DisposableCom.Create(inspectors.ComObject[i]);
                        string caption = inspector.ComObject.Caption;
                        // Fix double entries in the directory, TODO: store on something unique
                        if (inspectorCaptions.ContainsKey(caption))
                        {
                            continue;
                        }

                        var currentItemUntyped = inspector.ComObject.CurrentItem;
                        using (DisposableCom.Create(currentItemUntyped))
                        {
                            switch (currentItemUntyped)
                            {
                                case MailItem mailItem:
                                    if (mailItem.Sent)
                                    {
                                        continue;
                                    }

                                    inspectorCaptions.Add(caption, mailItem.Class);
                                    break;
                                case AppointmentItem appointmentItem:
                                    if (!_officeConfiguration.OutlookAllowExportInMeetings)
                                    {
                                        // skip, can't export to olAppointment
                                        continue;
                                    }

                                    if (!string.IsNullOrEmpty(appointmentItem.Organizer) && !appointmentItem.Organizer.Equals(_currentUser))
                                    {
                                        LOG.DebugFormat("Not exporting, as organizer is set to {0} and currentuser {1} is not him.", appointmentItem.Organizer, _currentUser);
                                        continue;
                                    }

                                    inspectorCaptions.Add(caption, appointmentItem.Class);
                                    break;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LOG.Warn("Problem retrieving word destinations, ignoring: ", ex);
            }

            return inspectorCaptions;
        }
    }
}