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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Greenshot.Base.Languages;
using Greenshot.Plugin.CustomUploader.Sxcu;
using log4net;

namespace Greenshot.Plugin.CustomUploader.ViewModels;

/// <summary>
/// The settings of the plugin: the imported uploaders and the uploads. Used on the UI thread.
/// </summary>
public sealed class CustomUploaderSettingsViewModel : INotifyPropertyChanged
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(CustomUploaderSettingsViewModel));
    private readonly UploaderStore _store;
    private UploaderItem _selectedUploader;
    private UploadItem _selectedUpload;

    internal CustomUploaderSettingsViewModel(UploaderStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        Refresh();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private static ICustomUploaderLanguage Language => Texts.Get<ICustomUploaderLanguage>();

    public ObservableCollection<UploaderItem> Uploaders { get; } = new ObservableCollection<UploaderItem>();

    public ObservableCollection<UploadItem> Uploads { get; } = new ObservableCollection<UploadItem>();

    public bool HasUploaders => Uploaders.Count > 0;

    public bool HasUploads => Uploads.Count > 0;

    public UploaderItem SelectedUploader
    {
        get => _selectedUploader;
        set
        {
            _selectedUploader = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedUploader));
        }
    }

    public bool HasSelectedUploader => SelectedUploader != null;

    public UploadItem SelectedUpload
    {
        get => _selectedUpload;
        set
        {
            _selectedUpload = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedUpload));
            OnPropertyChanged(nameof(CanDeleteSelectedUpload));
        }
    }

    public bool HasSelectedUpload => SelectedUpload != null;

    public bool CanDeleteSelectedUpload => SelectedUpload?.DeletionUrl != null;

    public void Refresh()
    {
        string selectedUploader = SelectedUploader?.Id;
        string selectedUpload = SelectedUpload?.Id;
        Uploaders.Clear();
        foreach (var uploader in _store.GetUploaders())
        {
            Uploaders.Add(new UploaderItem(uploader));
        }

        Uploads.Clear();
        foreach (var upload in _store.GetUploads())
        {
            Uploads.Add(new UploadItem(upload.Key, upload.Value));
        }

        SelectedUploader = Uploaders.FirstOrDefault(item => item.Id == selectedUploader);
        SelectedUpload = Uploads.FirstOrDefault(item => item.Id == selectedUpload);
        OnPropertyChanged(nameof(HasUploaders));
        OnPropertyChanged(nameof(HasUploads));
    }

    /// <summary>
    /// Read and check the uploader file, null with the reason when it can't be imported.
    /// </summary>
    internal static UploaderDefinition ReadFile(string path, out string error)
    {
        error = null;
        try
        {
            if (new FileInfo(path).Length > UploaderFile.MaximumLength)
            {
                error = Language.ErrorFileTooLarge;
                return null;
            }

            return UploaderFile.Read(File.ReadAllText(path, Encoding.UTF8));
        }
        catch (UploaderFileException ex)
        {
            error = ex.Message;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = ex.Message;
        }

        return null;
    }

    /// <summary>
    /// What the user confirms before the import: where the captures go, which headers go with them.
    /// The values of the headers and fields aren't shown, they are the keys.
    /// </summary>
    internal string DescribeImport(UploaderDefinition definition)
    {
        var text = new StringBuilder(string.Format(Language.ImportQuestion, definition.Name, UploaderFile.DescribeServer(definition)));
        var headers = definition.Headers?.Keys.ToList();
        if (headers?.Count > 0)
        {
            text.Append("\n\n").Append(string.Format(Language.ImportHeaders, string.Join(", ", headers)));
        }

        if (_store.Find(definition.Name) != null)
        {
            text.Append("\n\n").Append(Language.ImportReplaces);
        }

        text.Append("\n\n").Append(Language.ImportTrust);
        return text.ToString();
    }

    /// <summary>
    /// Add the confirmed uploader, it replaces the one with the same name.
    /// </summary>
    internal void Import(UploaderDefinition definition)
    {
        string id = _store.Add(definition);
        Log.InfoFormat("Imported the uploader {0} for {1}", definition.Name, UploaderFile.DescribeServer(definition));
        Refresh();
        SelectedUploader = Uploaders.FirstOrDefault(item => item.Id == id);
    }

    public void RemoveSelectedUploader()
    {
        if (SelectedUploader == null) return;
        _store.Remove(SelectedUploader.Id);
        Refresh();
    }

    public void ForgetSelectedUpload()
    {
        if (SelectedUpload == null) return;
        _store.ForgetUpload(SelectedUpload.Id);
        Refresh();
    }

    /// <summary>
    /// Open an http(s) link in the browser, nothing else: the links come from the response of a service
    /// </summary>
    internal static bool OpenInBrowser(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            // Not the link, it can be a deletion link
            Log.Warn($"Couldn't open a link of {uri.Host}", ex);
            return false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// An uploader in the list
/// </summary>
public sealed class UploaderItem
{
    internal UploaderItem(StoredUploader uploader)
    {
        Id = uploader.Id;
        IsReadable = uploader.Definition != null;
        Name = uploader.Definition?.Name ?? Texts.Get<ICustomUploaderLanguage>().Unreadable;
        Description = IsReadable ? UploaderFile.DescribeServer(uploader.Definition) : null;
    }

    public string Id { get; }

    public string Name { get; }

    /// <summary>
    /// The server, null when the uploader can't be read
    /// </summary>
    public string Description { get; }

    public bool IsReadable { get; }
}

/// <summary>
/// An upload in the list
/// </summary>
public sealed class UploadItem
{
    internal UploadItem(string id, UploadRecord upload)
    {
        Id = id;
        Uploader = upload.Uploader;
        Url = upload.Url;
        DeletionUrl = string.IsNullOrEmpty(upload.DeletionUrl) ? null : upload.DeletionUrl;
        Date = upload.Uploaded.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
    }

    public string Id { get; }

    public string Uploader { get; }

    public string Url { get; }

    /// <summary>
    /// A secret, never shown: it is only opened
    /// </summary>
    internal string DeletionUrl { get; }

    public string Date { get; }
}
