/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Plugin.CustomUploader;

/// <summary>
/// The texts of the [CustomUploader] section of greenshot.customuploader.{ietf}.ini, with the English text shown per property.
/// Add a key to the en-US file and a corresponding property here.
/// </summary>
[IniLanguageSection("CustomUploader", ModuleName = "customuploader")]
public interface ICustomUploaderLanguage : INotifyPropertyChanged
{
    /// <summary>
    /// Custom uploader
    /// </summary>
    string UploadMenuItem { get; }

    /// <summary>
    /// Uploading to {0}...
    /// </summary>
    string CommunicationWait { get; }

    /// <summary>
    /// The service didn't return a link to the upload.
    /// </summary>
    string NoLink { get; }

    /// <summary>
    /// Upload your captures to a service of your choice: import the uploader file (.sxcu) the service offers, the same file ShareX uses. Greenshot asks before it imports a file, and keeps it encrypted.
    /// </summary>
    string SettingsDescription { get; }

    /// <summary>
    /// Uploaders
    /// </summary>
    string UploadersTitle { get; }

    /// <summary>
    /// Your uploads
    /// </summary>
    string UploadsTitle { get; }

    /// <summary>
    /// No uploaders yet. Import the .sxcu file of your service.
    /// </summary>
    string NoUploaders { get; }

    /// <summary>
    /// Nothing uploaded yet.
    /// </summary>
    string NoUploads { get; }

    /// <summary>
    /// Import...
    /// </summary>
    string Import { get; }

    /// <summary>
    /// Remove
    /// </summary>
    string Remove { get; }

    /// <summary>
    /// Open
    /// </summary>
    string Open { get; }

    /// <summary>
    /// Delete...
    /// </summary>
    string Delete { get; }

    /// <summary>
    /// Remove from list
    /// </summary>
    string Forget { get; }

    /// <summary>
    /// Can't be read, import it again
    /// </summary>
    string Unreadable { get; }

    /// <summary>
    /// Uploader files (*.sxcu)|*.sxcu|All files (*.*)|*.*
    /// </summary>
    string ImportFileFilter { get; }

    /// <summary>
    /// Import an uploader
    /// </summary>
    string ImportTitle { get; }

    /// <summary>
    /// Import the uploader "{0}"?\n\nWhen you choose it, Greenshot sends your capture to {1}.
    /// </summary>
    string ImportQuestion { get; }

    /// <summary>
    /// It sends these headers with it: {0}
    /// </summary>
    string ImportHeaders { get; }

    /// <summary>
    /// It replaces your uploader with the same name.
    /// </summary>
    string ImportReplaces { get; }

    /// <summary>
    /// Uploads with it can't be deleted from Greenshot, {0} decides how long they stay online. With "Import and remind me" Greenshot shows you the capture before each upload and asks.
    /// </summary>
    string ImportNoDeletion { get; }

    /// <summary>
    /// Only import uploader files from a service you trust: the file decides where your captures go, and it can hold the key to your account there.
    /// </summary>
    string ImportTrust { get; }

    /// <summary>
    /// Import
    /// </summary>
    string ImportButton { get; }

    /// <summary>
    /// Import and remind me
    /// </summary>
    string ImportWithReminder { get; }

    /// <summary>
    /// Your values for "{0}"
    /// </summary>
    string ValuesTitle { get; }

    /// <summary>
    /// Fill in what the service gave you, e.g. the API key of your account where the file says something like "YOUR_API_KEY". Next, Greenshot shows you where the captures go.
    /// </summary>
    string ValuesIntro { get; }

    /// <summary>
    /// Address
    /// </summary>
    string ValuesRequestUrl { get; }

    /// <summary>
    /// Address parameter {0}
    /// </summary>
    string ValuesParameter { get; }

    /// <summary>
    /// Header {0}
    /// </summary>
    string ValuesHeader { get; }

    /// <summary>
    /// Form field {0}
    /// </summary>
    string ValuesArgument { get; }

    /// <summary>
    /// Next
    /// </summary>
    string ValuesNext { get; }

    /// <summary>
    /// Show
    /// </summary>
    string ValuesShow { get; }

    /// <summary>
    /// These values still look like the text of the file, not like your own: {0}\n\nGo on anyway?
    /// </summary>
    string ValuesPlaceholderQuestion { get; }

    /// <summary>
    /// Upload with {0}?
    /// </summary>
    string ReminderTitle { get; }

    /// <summary>
    /// This capture goes to {0}. It can't be deleted from Greenshot afterwards, {0} decides how long it stays online.
    /// </summary>
    string ReminderMessage { get; }

    /// <summary>
    /// Upload
    /// </summary>
    string ReminderUpload { get; }

    /// <summary>
    /// Don't remind me again for {0}
    /// </summary>
    string ReminderStop { get; }

    /// <summary>
    /// "{0}" can't be imported: {1}
    /// </summary>
    string ImportFailed { get; }

    /// <summary>
    /// Remove the uploader "{0}"?
    /// </summary>
    string RemoveQuestion { get; }

    /// <summary>
    /// Open the delete link of this upload in your browser?\n\n{0}
    /// </summary>
    string DeleteQuestion { get; }

    /// <summary>
    /// Remove this upload from the list? It stays online at the service.
    /// </summary>
    string ForgetQuestion { get; }

    /// <summary>
    /// Greenshot then no longer has the link to delete it.
    /// </summary>
    string ForgetLosesDeletion { get; }

    /// <summary>
    /// {0} doesn't offer a way to delete uploads, only the service decides how long this upload stays online.
    /// </summary>
    string NoDeletionLink { get; }

    /// <summary>
    /// The file is too large for an uploader file.
    /// </summary>
    string ErrorFileTooLarge { get; }

    /// <summary>
    /// It isn't an uploader file.
    /// </summary>
    string ErrorNotJson { get; }

    /// <summary>
    /// It isn't an image or file uploader.
    /// </summary>
    string ErrorNotFileUploader { get; }

    /// <summary>
    /// The request method {0} isn't supported, only POST, PUT and PATCH.
    /// </summary>
    string ErrorMethod { get; }

    /// <summary>
    /// It has no request URL.
    /// </summary>
    string ErrorNoUrl { get; }

    /// <summary>
    /// It doesn't use HTTPS. Greenshot only sends captures encrypted, HTTP only to this computer.
    /// </summary>
    string ErrorNotHttps { get; }

    /// <summary>
    /// The address of the server must be fixed, without placeholders.
    /// </summary>
    string ErrorDynamicHost { get; }

    /// <summary>
    /// The body {0} isn't supported, only MultipartFormData and Binary.
    /// </summary>
    string ErrorBody { get; }

    /// <summary>
    /// It has no FileFormName for the file.
    /// </summary>
    string ErrorNoFileFormName { get; }

    /// <summary>
    /// It was made for an older ShareX. Open it in a current ShareX and export it again.
    /// </summary>
    string ErrorOldSyntax { get; }

    /// <summary>
    /// The placeholder {0} isn't supported in {1}.
    /// </summary>
    string ErrorFunction { get; }

    /// <summary>
    /// A placeholder in {0} isn't closed.
    /// </summary>
    string ErrorSyntax { get; }

    /// <summary>
    /// The header {0} isn't valid.
    /// </summary>
    string ErrorHeader { get; }

    /// <summary>
    /// {0} isn't part of Greenshot: whether uploads work depends on the service and on this file. When they fail, {1} or whoever made the file can help, Greenshot can't.
    /// </summary>
    string ImportNotGreenshot { get; }

    /// <summary>
    /// Upload with {0} failed
    /// </summary>
    string UploadFailedTitle { get; }

    /// <summary>
    /// {0} didn't take the capture: {1}
    /// </summary>
    string UploadFailedMessage { get; }

    /// <summary>
    /// "{0}" isn't part of Greenshot: it comes from an uploader file you imported, ... ask {1} or whoever made the file, the details help them.
    /// </summary>
    string UploadFailedNote { get; }
}
