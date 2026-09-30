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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Confluence.Entities;

namespace Greenshot.Plugin.Confluence;

/// <summary>
/// Description of ConfluenceDestination.
/// </summary>
public class ConfluenceDestination : DestinationBase
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ConfluenceDestination));
    private static IConfluenceConfiguration ConfluenceConfig => IniConfigHelper.EnsureSection<IConfluenceConfiguration>(() => new ConfluenceConfigurationImpl());
    private static ICoreConfiguration CoreConfig => IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());
    private static readonly object IconLock = new object();
    private static Image _confluenceIcon;
    private readonly Page _page;

    internal static Image LoadConfluenceIcon()
    {
        if (_confluenceIcon != null)
        {
            return _confluenceIcon;
        }

        lock (IconLock)
        {
            if (_confluenceIcon != null)
            {
                return _confluenceIcon;
            }

            try
            {
                var assembly = typeof(ConfluenceDestination).Assembly;
                var resourceManager = new System.Resources.ResourceManager(assembly.GetName().Name + ".g", assembly);
                using Stream iconStream = resourceManager.GetStream("images/confluence.ico");
                if (iconStream != null)
                {
                    using var icon = new Icon(iconStream);
                    _confluenceIcon = icon.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                Log.WarnFormat("Could not load confluence icon from g.resources: {0}", ex.Message);
            }

            if (_confluenceIcon == null)
            {
                try
                {
                    Uri confluenceIconUri = new Uri("/Greenshot.Plugin.Confluence;component/Images/Confluence.ico", UriKind.Relative);
                    using Stream iconStream = Application.GetResourceStream(confluenceIconUri)?.Stream;
                    if (iconStream != null)
                    {
                        using var icon = new Icon(iconStream);
                        _confluenceIcon = icon.ToBitmap();
                    }
                }
                catch (Exception ex)
                {
                    Log.WarnFormat("Could not load confluence icon from Application resource stream: {0}", ex.Message);
                }
            }

            IsInitialized = _confluenceIcon != null;
            return _confluenceIcon;
        }
    }

    static ConfluenceDestination()
    {
        LoadConfluenceIcon();
    }

    public static Image ConfluenceIcon => LoadConfluenceIcon();

    /// <summary>
    /// The icon key of the Confluence destinations
    /// </summary>
    public const string IconKey = "confluence:icon";

    public static bool IsInitialized { get; private set; }

    public ConfluenceDestination()
    {
    }

    public ConfluenceDestination(Page page)
    {
        _page = page;
    }

    public override string Designation => "Confluence";

    public override DestinationDescriptor Descriptor
    {
        get
        {
            string displayName = _page == null
                ? Language.GetString("confluence", LangKey.upload_menu_item)
                : Language.GetString("confluence", LangKey.upload_menu_item) + ": \"" + _page.Title + "\"";
            return new DestinationDescriptor(displayName, iconKey: IconKey, hasDynamicDestinations: _page == null);
        }
    }

    public override bool IsAvailableFor(ICaptureDetails metadata) => base.IsAvailableFor(metadata) && !string.IsNullOrEmpty(ConfluenceConfig.Url);

    public override async ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken)
    {
        if (_page != null || ConfluencePlugin.ConfluenceConnectorNoLogin == null || !ConfluencePlugin.ConfluenceConnectorNoLogin.IsLoggedIn)
        {
            return Array.Empty<IDestination>();
        }

        List<Page> currentPages = await ConfluenceUtils.GetCurrentPagesAsync(cancellationToken).ConfigureAwait(false);
        return currentPages.Select(currentPage => (IDestination) new ConfluenceDestination(currentPage)).ToList();
    }

    public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
    {
        var connector = ConfluencePlugin.ConfluenceConnector;
        Page selectedPage = _page;
        bool openPage = (_page == null) && ConfluenceConfig.OpenPageAfterUpload;
        string filename = FilenameHelper.GetFilenameWithoutExtensionFromPattern(CoreConfig.OutputFileFilenamePattern, request.Metadata);
        try
        {
            // force password check to take place before the pages load
            if (!await connector.EnsureLoggedInAsync(cancellationToken).ConfigureAwait(false))
            {
                return ExportResult.Declined;
            }

            if (selectedPage == null)
            {
                // Everything the dialog shows is loaded before it opens, the dialog itself only loads the page tree
                var currentPages = await ConfluenceUtils.GetCurrentPagesAsync(cancellationToken).ConfigureAwait(false);
                var spaces = await connector.GetSpaceSummariesAsync(cancellationToken).ConfigureAwait(false);
                var choice = await request.Ui.ShowDialogAsync(new ConfluenceUploadRequest(filename, currentPages, spaces), cancellationToken).ConfigureAwait(false);
                if (choice?.Page == null)
                {
                    return ExportResult.Declined;
                }

                selectedPage = choice.Page;
                if (choice.IsOpenPageSelected)
                {
                    openPage = false;
                }

                filename = choice.Filename;
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Log.Error("Connecting to Confluence failed", e);
            return ExportResult.Failed(e.Message, e);
        }

        var formatRegistry = SimpleServiceProvider.Current.GetInstance<IFileFormatRegistry>(true);
        string uploadFormat = formatRegistry.ResolveFormatId(ConfluenceConfig.UploadFormat, WellKnownFileFormats.Png);
        string extension = formatRegistry != null && formatRegistry.TryGet(uploadFormat, out var formatDefinition)
            ? "." + formatDefinition.PreferredExtension
            : ".png";
        if (!filename.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            filename += extension;
        }

        var outputSettings = new SurfaceOutputSettings(uploadFormat, ConfluenceConfig.UploadJpegQuality, ConfluenceConfig.UploadReduceColors);
        try
        {
            var image = await request.Source.EncodeAsync(outputSettings, cancellationToken).ConfigureAwait(false);
            await request.Ui.RunWithProgressAsync(Language.GetString("confluence", LangKey.communication_wait), async (progress, token) =>
            {
                await connector.AddAttachmentAsync(selectedPage.Id, image, filename, null, token).ConfigureAwait(false);
                return true;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            Log.Error("Upload to Confluence failed", e);
            return ExportResult.Failed(e.Message, e);
        }

        Log.Debug("Uploaded to Confluence.");
        if (ConfluenceConfig.CopyWikiMarkupForImageToClipboard)
        {
            try
            {
                await ClipboardService.Current.SetTextAsync("!" + filename + "!", cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error("Couldn't copy the wiki markup to the clipboard", ex);
            }
        }

        if (openPage)
        {
            try
            {
                Process.Start(selectedPage.Url)?.Dispose();
            }
            catch
            {
                // Ignore
            }
        }

        return ExportResult.Succeeded(uri: new Uri(selectedPage.Url));
    }
}

/// <summary>
/// The icon of the Confluence destinations
/// </summary>
public sealed class ConfluenceIconProvider : IIconProvider
{
    public bool CanProvide(string iconKey) => iconKey == ConfluenceDestination.IconKey;

    public Task<Image> GetIconAsync(string iconKey, CancellationToken cancellationToken)
    {
        var icon = ConfluenceDestination.ConfluenceIcon;
        if (icon == null)
        {
            return Task.FromResult<Image>(null);
        }

        // The destination owns the icon, the caller gets a copy
        lock (icon)
        {
            return Task.FromResult<Image>(ImageHelper.Clone(icon));
        }
    }
}

/// <summary>
/// What the Confluence upload dialog shows: the open pages and the spaces, the result is the choice or null.
/// </summary>
public sealed class ConfluenceUploadRequest : IDialogViewModel<ConfluenceUploadChoice>
{
    public ConfluenceUploadRequest(string filename, IList<Page> currentPages, IList<Space> spaces)
    {
        Filename = filename;
        CurrentPages = currentPages ?? new List<Page>();
        Spaces = spaces ?? new List<Space>();
    }

    public string Filename { get; }

    /// <summary>
    /// The Confluence pages which are open in a browser
    /// </summary>
    public IList<Page> CurrentPages { get; }

    public IList<Space> Spaces { get; }
}

/// <summary>
/// The page and filename the user chose in the Confluence upload dialog.
/// </summary>
public sealed class ConfluenceUploadChoice
{
    public ConfluenceUploadChoice(Page page, string filename, bool isOpenPageSelected)
    {
        Page = page;
        Filename = filename;
        IsOpenPageSelected = isOpenPageSelected;
    }

    public Page Page { get; }

    public string Filename { get; }

    /// <summary>
    /// True when the page was picked from the pages which are open in a browser (no need to open it)
    /// </summary>
    public bool IsOpenPageSelected { get; }
}
