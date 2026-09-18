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
using System.Threading;
using System.Windows;
using Greenshot.Base.Controls;
using Greenshot.Base.Core;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Confluence.Entities;

namespace Greenshot.Plugin.Confluence;

/// <summary>
/// Description of ConfluenceDestination.
/// </summary>
public class ConfluenceDestination : AbstractDestination
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ConfluenceDestination));
    private static IConfluenceConfiguration ConfluenceConfig => IniConfigHelper.EnsureSection<IConfluenceConfiguration>(() => new ConfluenceConfigurationImpl());
    private static ICoreConfiguration CoreConfig => IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());
    private static readonly object IconLock = new object();
    private static Image _confluenceIcon;
    private readonly Page _page;

    private static Image LoadConfluenceIcon()
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

    public static bool IsInitialized { get; private set; }

    public ConfluenceDestination()
    {
    }

    public ConfluenceDestination(Page page)
    {
        _page = page;
    }

    public override string Designation
    {
        get { return "Confluence"; }
    }

    public override string Description
    {
        get
        {
            if (_page == null)
            {
                return Language.GetString("confluence", LangKey.upload_menu_item);
            }
            else
            {
                return Language.GetString("confluence", LangKey.upload_menu_item) + ": \"" + _page.Title + "\"";
            }
        }
    }

    public override bool IsDynamic
    {
        get { return true; }
    }

    public override bool IsActive
    {
        get { return base.IsActive && !string.IsNullOrEmpty(ConfluenceConfig.Url); }
    }

    public override Image DisplayIcon
    {
        get { return ConfluenceIcon; }
    }

    public override IEnumerable<IDestination> DynamicDestinations()
    {
        if (ConfluencePlugin.ConfluenceConnectorNoLogin == null || !ConfluencePlugin.ConfluenceConnectorNoLogin.IsLoggedIn)
        {
            yield break;
        }

        List<Page> currentPages = ConfluenceUtils.GetCurrentPages();
        if (currentPages == null || currentPages.Count == 0)
        {
            yield break;
        }

        foreach (Page currentPage in currentPages)
        {
            yield return new ConfluenceDestination(currentPage);
        }
    }

    public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
    {
        ExportInformation exportInformation = new ExportInformation(Designation, Description);
        // force password check to take place before the pages load
        if (!ConfluencePlugin.ConfluenceConnector.IsLoggedIn)
        {
            return exportInformation;
        }

        Page selectedPage = _page;
        bool openPage = (_page == null) && ConfluenceConfig.OpenPageAfterUpload;
        string filename = FilenameHelper.GetFilenameWithoutExtensionFromPattern(CoreConfig.OutputFileFilenamePattern, captureDetails);
        if (selectedPage == null)
        {
            Forms.ConfluenceUpload confluenceUpload = new Forms.ConfluenceUpload(filename);
            bool? dialogResult = confluenceUpload.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
            {
                selectedPage = confluenceUpload.SelectedPage;
                if (confluenceUpload.IsOpenPageSelected)
                {
                    openPage = false;
                }

                filename = confluenceUpload.Filename;
            }
        }

        string extension = "." + ConfluenceConfig.UploadFormat;
        if (!filename.ToLower().EndsWith(extension))
        {
            filename += extension;
        }

        if (selectedPage != null)
        {
            bool uploaded = Upload(surface, selectedPage, filename, out var errorMessage);
            if (uploaded)
            {
                if (openPage)
                {
                    try
                    {
                        Process.Start(selectedPage.Url);
                    }
                    catch
                    {
                        // Ignore
                    }
                }

                exportInformation.ExportMade = true;
                exportInformation.Uri = selectedPage.Url;
            }
            else
            {
                exportInformation.ErrorMessage = errorMessage;
            }
        }

        ProcessExport(exportInformation, surface);
        return exportInformation;
    }

    private bool Upload(ISurface surfaceToUpload, Page page, string filename, out string errorMessage)
    {
        SurfaceOutputSettings outputSettings =
            new SurfaceOutputSettings(ConfluenceConfig.UploadFormat, ConfluenceConfig.UploadJpegQuality, ConfluenceConfig.UploadReduceColors);
        errorMessage = null;
        try
        {
            new PleaseWaitForm().ShowAndWait(Description, Language.GetString("confluence", LangKey.communication_wait),
                delegate
                {
                    ConfluencePlugin.ConfluenceConnector.AddAttachment(page.Id, "image/" + ConfluenceConfig.UploadFormat.ToString().ToLower(), null, filename,
                        new SurfaceContainer(surfaceToUpload, outputSettings, filename));
                }
            );
            Log.Debug("Uploaded to Confluence.");
            if (!ConfluenceConfig.CopyWikiMarkupForImageToClipboard)
            {
                return true;
            }

            int retryCount = 2;
            while (retryCount >= 0)
            {
                try
                {
                    Clipboard.SetText("!" + filename + "!");
                    break;
                }
                catch (Exception ee)
                {
                    if (retryCount == 0)
                    {
                        Log.Error(ee);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                finally
                {
                    --retryCount;
                }
            }

            return true;
        }
        catch (Exception e)
        {
            errorMessage = e.Message;
        }

        return false;
    }
}