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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.HttpExtensions;
using Dapplo.Jira.Entities;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Plugin.Jira.Api;

namespace Greenshot.Plugin.Jira.Destinations;

/// <summary>
/// What the Jira upload dialog (JiraUploadWindow) asks for, the result is the choice of the user or null.
/// </summary>
public sealed class JiraUploadRequest : IDialogViewModel<JiraUploadChoice>
{
    public JiraUploadRequest(string filename)
    {
        Filename = filename;
    }

    /// <summary>
    /// The suggested filename of the attachment
    /// </summary>
    public string Filename { get; }
}

/// <summary>
/// The issue, filename and comment the user chose in the Jira upload dialog.
/// </summary>
public sealed class JiraUploadChoice
{
    public JiraUploadChoice(IssueV2 issue, string filename, string comment)
    {
        Issue = issue;
        Filename = filename;
        Comment = comment;
    }

    public IssueV2 Issue { get; }

    public string Filename { get; }

    public string Comment { get; }
}

/// <summary>
/// The icons of the Jira destinations: the issue type of an issue ("jira:issue:KEY"), the favicon of the server or the Jira logo.
/// </summary>
public sealed class JiraIconProvider : IIconProvider
{
    private const string Prefix = "jira:";
    private const string IssuePrefix = Prefix + "issue:";
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraIconProvider));

    /// <summary>
    /// The icon of the Jira server (or the Jira logo)
    /// </summary>
    public const string Default = Prefix + "default";

    public static string ForIssue(string issueKey) => IssuePrefix + issueKey;

    public bool CanProvide(string iconKey) => iconKey != null && iconKey.StartsWith(Prefix, StringComparison.Ordinal);

    public async Task<Image> GetIconAsync(string iconKey, CancellationToken cancellationToken)
    {
        var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>(isOptional: true);
        if (jiraConnector != null)
        {
            if (iconKey.StartsWith(IssuePrefix, StringComparison.Ordinal))
            {
                string issueKey = iconKey.Substring(IssuePrefix.Length);
                var issue = jiraConnector.Monitor?.RecentJiras.FirstOrDefault(details => details.JiraIssue?.Key == issueKey)?.JiraIssue;
                if (issue != null)
                {
                    try
                    {
                        var issueTypeBitmap = await jiraConnector.GetIssueTypeBitmapAsync(issue, cancellationToken).ConfigureAwait(false);
                        if (issueTypeBitmap != null)
                        {
                            // The cache owns the bitmap, the caller gets a copy
                            lock (issueTypeBitmap)
                            {
                                return ImageHelper.Clone(issueTypeBitmap);
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        Log.Warn($"Problem loading issue type for {issueKey}, ignoring", ex);
                    }
                }
            }

            var favIcon = jiraConnector.FavIcon;
            if (favIcon != null)
            {
                lock (favIcon)
                {
                    return ImageHelper.Clone(favIcon);
                }
            }
        }

        return EmbeddedResources.GetImage(typeof(JiraPlugin), "Jira");
    }
}

/// <summary>
/// Attach the capture to a Jira issue: a recent one (dynamic destination) or one chosen in the Jira dialog.
/// </summary>
public class JiraDestination : DestinationBase, IRequiresRecipeAuthorization
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraDestination));
    private static IJiraConfiguration Config => IniConfigRegistry.GetSection<IJiraConfiguration>();
    private readonly IssueV2 _jiraIssue;

    public JiraDestination(IssueV2 jiraIssue = null)
    {
        _jiraIssue = jiraIssue;
    }

    public override string Designation => "Jira";

    /// <summary>
    /// Uploads the capture: the user has to allow network access when approving a recipe with this destination
    /// </summary>
    public IEnumerable<RecipeGatedAction> GetGatedActions()
    {
        yield return new RecipeGatedAction(RecipeGateType.NetworkAccess, "Jira");
    }

    public override DestinationDescriptor Descriptor
    {
        get
        {
            if (_jiraIssue?.Fields?.Summary == null)
            {
                return new DestinationDescriptor(Texts.Get<IJiraLanguage>().UploadMenuItem, iconKey: JiraIconProvider.Default, hasDynamicDestinations: true);
            }

            // Format the title of this destination
            string displayName = _jiraIssue.Key + ": " + _jiraIssue.Fields.Summary.Substring(0, Math.Min(20, _jiraIssue.Fields.Summary.Length));
            return new DestinationDescriptor(displayName, iconKey: JiraIconProvider.ForIssue(_jiraIssue.Key));
        }
    }

    public override bool IsAvailableFor(ICaptureDetails metadata) => base.IsAvailableFor(metadata) && !string.IsNullOrEmpty(Config.Url);

    public override ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken)
    {
        var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>(isOptional: true);
        if (_jiraIssue != null || jiraConnector == null || !jiraConnector.IsLoggedIn)
        {
            return base.GetDynamicDestinationsAsync(metadata, cancellationToken);
        }

        IReadOnlyList<IDestination> destinations = jiraConnector.Monitor.RecentJiras.Select(jiraDetails => (IDestination) new JiraDestination(jiraDetails.JiraIssue)).ToList();
        return new ValueTask<IReadOnlyList<IDestination>>(destinations);
    }

    public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        string filename = Path.GetFileName(FilenameHelper.GetFilename(Config.UploadFormat, request.Metadata));
        var outputSettings = new SurfaceOutputSettings(Config.UploadFormat, Config.UploadJpegQuality, Config.UploadReduceColors);
        var jiraConnector = SimpleServiceProvider.Current.GetInstance<JiraConnector>();
        var issue = _jiraIssue;
        string comment = null;
        if (issue == null)
        {
            var choice = await request.Ui.ShowDialogAsync(new JiraUploadRequest(filename), cancellationToken).ConfigureAwait(false);
            if (choice?.Issue == null)
            {
                return ExportResult.Declined;
            }

            issue = choice.Issue;
            filename = string.IsNullOrEmpty(choice.Filename) ? filename : choice.Filename;
            comment = choice.Comment;
        }

        try
        {
            var image = await request.Source.EncodeAsync(outputSettings, cancellationToken).ConfigureAwait(false);
            await request.Ui.RunWithProgressAsync(Texts.Get<IJiraLanguage>().CommunicationWait, async (progress, token) =>
            {
                await jiraConnector.AttachAsync(issue.Key, image, filename, token).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(comment))
                {
                    await jiraConnector.AddCommentAsync(issue.Key, comment, null, token).ConfigureAwait(false);
                }

                return true;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Log.Error($"Upload to Jira {issue.Key} failed", e);
            return ExportResult.Failed(Texts.Get<IJiraLanguage>().UploadFailure + " " + e.Message, e);
        }

        Log.DebugFormat("Uploaded to Jira {0}", issue.Key);
        return ExportResult.Succeeded(uri: jiraConnector.JiraBaseUri.AppendSegments("browse", issue.Key), target: issue.Key);
    }
}
