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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Dapplo.HttpExtensions;
using Dapplo.Jira.Entities;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Jira.Forms;

namespace Greenshot.Plugin.Jira;

/// <summary>
/// Description of JiraDestination.
/// </summary>
public class JiraDestination : AbstractDestination
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraDestination));
    private static readonly JiraConfiguration Config = IniConfig.GetIniSection<JiraConfiguration>();
    private readonly IssueV2 _jiraIssue;

    public JiraDestination(IssueV2 jiraIssue = null)
    {
        _jiraIssue = jiraIssue;
    }

    public override string Designation => "Jira";

    public override string Description
    {
        get
        {
            if (_jiraIssue?.Fields?.Summary == null)
            {
                return Language.GetString("jira", LangKey.upload_menu_item);
            }

            // Format the title of this destination
            return _jiraIssue.Key + ": " + _jiraIssue.Fields.Summary.Substring(0, Math.Min(20, _jiraIssue.Fields.Summary.Length));
        }
    }

    public override bool IsActive => base.IsActive && !string.IsNullOrEmpty(Config.Url);

    public override bool IsDynamic => true;

    public override Image DisplayIcon
    {
        get
        {
            Image displayIcon = null;
            var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
            if (jiraConnector != null)
            {
                if (_jiraIssue != null)
                {
                    // Try to get the issue type as icon
                    try
                    {
                        displayIcon = jiraConnector.GetIssueTypeBitmapAsync(_jiraIssue).Result;
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"Problem loading issue type for {_jiraIssue.Key}, ignoring", ex);
                    }
                }

                if (displayIcon == null)
                {
                    displayIcon = jiraConnector.FavIcon;
                }
            }

            if (displayIcon == null)
            {
                var resources = new ComponentResourceManager(typeof(JiraPlugin));
                displayIcon = (Image) resources.GetObject("Jira");
            }

            return displayIcon;
        }
    }

    public override IEnumerable<IDestination> DynamicDestinations()
    {
        var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
        if (jiraConnector == null || !jiraConnector.IsLoggedIn)
        {
            yield break;
        }

        foreach (var jiraDetails in jiraConnector.Monitor.RecentJiras)
        {
            yield return new JiraDestination(jiraDetails.JiraIssue);
        }
    }

    public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surfaceToUpload, ICaptureDetails captureDetails)
    {
        ExportInformation exportInformation = new ExportInformation(Designation, Description);
        string filename = Path.GetFileName(FilenameHelper.GetFilename(Config.UploadFormat, captureDetails));
        SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(Config.UploadFormat, Config.UploadJpegQuality, Config.UploadReduceColors);
        var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
        if (_jiraIssue != null)
        {
            try
            {
                // Run upload in the background
                new PleaseWaitForm().ShowAndWait(Description, Language.GetString("jira", LangKey.communication_wait),
                    async () =>
                    {
                        var surfaceContainer = new SurfaceContainer(surfaceToUpload, outputSettings, filename);
                        await jiraConnector.AttachAsync(_jiraIssue.Key, surfaceContainer);
                        surfaceToUpload.UploadUrl = jiraConnector.JiraBaseUri.AppendSegments("browse", _jiraIssue.Key).AbsoluteUri;
                    }
                );
                Log.DebugFormat("Uploaded to Jira {0}", _jiraIssue.Key);
                exportInformation.ExportMade = true;
                exportInformation.Uri = surfaceToUpload.UploadUrl;
            }
            catch (Exception e)
            {
                MessageBox.Show(Language.GetString("jira", LangKey.upload_failure) + " " + e.Message);
            }
        }
        else
        {
            var jiraForm = new JiraForm(jiraConnector);
            jiraForm.SetFilename(filename);
            var dialogResult = jiraForm.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                try
                {
                    surfaceToUpload.UploadUrl = jiraConnector.JiraBaseUri.AppendSegments("browse", jiraForm.GetJiraIssue().Key).AbsoluteUri;
                    // Run upload in the background
                    new PleaseWaitForm().ShowAndWait(Description, Language.GetString("jira", LangKey.communication_wait),
                        async () => { await jiraForm.UploadAsync(new SurfaceContainer(surfaceToUpload, outputSettings, filename)); }
                    );
                    Log.DebugFormat("Uploaded to Jira {0}", jiraForm.GetJiraIssue().Key);
                    exportInformation.ExportMade = true;
                    exportInformation.Uri = surfaceToUpload.UploadUrl;
                }
                catch (Exception e)
                {
                    MessageBox.Show(Language.GetString("jira", LangKey.upload_failure) + " " + e.Message);
                }
            }
        }

        ProcessExport(exportInformation, surfaceToUpload);
        return exportInformation;
    }
}