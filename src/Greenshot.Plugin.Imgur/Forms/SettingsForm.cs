/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System;

namespace Greenshot.Plugin.Imgur.Forms;

/// <summary>
/// Description of PasswordRequestForm.
/// </summary>
public partial class SettingsForm : ImgurForm
{
    public SettingsForm()
    {
        //
        // The InitializeComponent() call is required for Windows Forms designer support.
        //
        InitializeComponent();
        InitializeLanguageBindings();
        CancelButton = buttonCancel;
        AcceptButton = buttonOK;

        historyButton.Enabled = ImgurUtils.IsHistoryLoadingNeeded();
    }

    /// <inheritdoc />
    protected override void InitializeLanguageBindings()
    {
        buttonOK.LanguageKey = "imgur.OK";
        buttonCancel.LanguageKey = "imgur.CANCEL";
        historyButton.LanguageKey = "imgur.history";
        checkbox_anonymous_access.LanguageKey = "imgur.anonymous_access";
        checkbox_anonymous_access.PropertyName = nameof(ImgurConfiguration.AnonymousAccess);
        checkbox_anonymous_access.SectionName = "Imgur";
        checkbox_usepagelink.LanguageKey = "imgur.use_page_link";
        checkbox_usepagelink.PropertyName = nameof(ImgurConfiguration.UsePageLink);
        checkbox_usepagelink.SectionName = "Imgur";
        LanguageKey = "imgur.settings_title";
    }

    private void ButtonHistoryClick(object sender, EventArgs e)
    {
        ImgurHistory.ShowHistory();
    }
}