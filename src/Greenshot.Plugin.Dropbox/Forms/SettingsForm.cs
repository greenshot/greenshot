/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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

namespace Greenshot.Plugin.Dropbox.Forms;

/// <summary>
/// Description of PasswordRequestForm.
/// </summary>
public partial class SettingsForm : DropboxForm
{
    public SettingsForm()
    {
        //
        // The InitializeComponent() call is required for Windows Forms designer support.
        //
        InitializeComponent();
        InitializeLanguageBindings();
        AcceptButton = buttonOK;
        CancelButton = buttonCancel;
    }

    protected override void InitializeLanguageBindings()
    {
        buttonOK.LanguageKey = "OK";
        buttonCancel.LanguageKey = "CANCEL";
        combobox_uploadimageformat.PropertyName = nameof(DropboxConfiguration.UploadFormat);
        combobox_uploadimageformat.SectionName = "Dropbox";
        label_upload_format.LanguageKey = "dropbox.label_upload_format";
        label_AfterUpload.LanguageKey = "dropbox.label_AfterUpload";
        checkboxAfterUploadLinkToClipBoard.LanguageKey = "dropbox.label_AfterUploadLinkToClipBoard";
        checkboxAfterUploadLinkToClipBoard.PropertyName = nameof(DropboxConfiguration.AfterUploadLinkToClipBoard);
        checkboxAfterUploadLinkToClipBoard.SectionName = "Dropbox";
        LanguageKey = "dropbox.settings_title";
    }
}