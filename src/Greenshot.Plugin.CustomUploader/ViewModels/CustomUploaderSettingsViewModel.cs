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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Plugin.CustomUploader.Sxcu;
using log4net;
using Newtonsoft.Json;

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
            OnPropertyChanged(nameof(NoDeletionHint));
        }
    }

    public bool HasSelectedUpload => SelectedUpload != null;

    public bool CanDeleteSelectedUpload => SelectedUpload?.DeletionUrl != null;

    /// <summary>
    /// Why Delete is off for the selected upload: the service gave no delete link. Null otherwise.
    /// </summary>
    public string NoDeletionHint => SelectedUpload != null && SelectedUpload.DeletionUrl == null ? string.Format(Language.NoDeletionLink, SelectedUpload.Uploader) : null;

    public void Refresh()
    {
        string selectedUploader = SelectedUploader?.Id;
        string selectedUpload = SelectedUpload?.Id;
        Uploaders.Clear();
        var icons = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        foreach (var uploader in _store.GetUploaders())
        {
            var icon = LoadIcon(_store.GetIcon(uploader.Id));
            if (uploader.Name != null)
            {
                icons[uploader.Name] = icon;
            }

            Uploaders.Add(new UploaderItem(uploader, icon));
        }

        Uploads.Clear();
        foreach (var upload in _store.GetUploads())
        {
            // An upload of a removed uploader has the generic icon
            Uploads.Add(new UploadItem(upload.Key, upload.Value, upload.Value.Uploader != null && icons.TryGetValue(upload.Value.Uploader, out var icon) ? icon : GenericIcon.Value));
        }

        SelectedUploader = Uploaders.FirstOrDefault(item => item.Id == selectedUploader);
        SelectedUpload = Uploads.FirstOrDefault(item => item.Id == selectedUpload);
        OnPropertyChanged(nameof(HasUploaders));
        OnPropertyChanged(nameof(HasUploads));
    }

    private static readonly Lazy<ImageSource> GenericIcon = new Lazy<ImageSource>(() =>
    {
        using var image = EmbeddedResources.GetImage(typeof(CustomUploaderPlugin), "Upload");
        var source = image.ToBitmapSource();
        source?.Freeze();
        return source;
    });

    /// <summary>
    /// The icon of the service (PNG), the generic one without it
    /// </summary>
    private static ImageSource LoadIcon(byte[] png)
    {
        if (png == null)
        {
            return GenericIcon.Value;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = new MemoryStream(png);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is NotSupportedException or FileFormatException or ArgumentException)
        {
            return GenericIcon.Value;
        }
    }

    /// <summary>
    /// True when the uploads can't be deleted from Greenshot: the import offers the reminder before each upload
    /// </summary>
    internal static bool HasNoDeletion(UploaderDefinition definition) => string.IsNullOrWhiteSpace(definition.DeletionURL);

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
    /// The values the user can fill in before the import: shared files have a text like "YOUR_API_KEY" where the key of
    /// the user belongs. The request url (the server is confirmed afterwards), then parameters, headers and form fields.
    /// </summary>
    internal static IReadOnlyList<UploaderValue> GetValues(UploaderDefinition definition)
    {
        var values = new List<UploaderValue> { new UploaderValue(UploaderValueKind.RequestUrl, null, definition.RequestURL) };
        values.AddRange((definition.Parameters ?? new Dictionary<string, string>()).Select(entry => new UploaderValue(UploaderValueKind.Parameter, entry.Key, entry.Value)));
        values.AddRange((definition.Headers ?? new Dictionary<string, string>()).Select(entry => new UploaderValue(UploaderValueKind.Header, entry.Key, entry.Value)));
        values.AddRange((definition.Arguments ?? new Dictionary<string, string>()).Select(entry => new UploaderValue(UploaderValueKind.Argument, entry.Key, entry.Value)));
        return values;
    }

    /// <summary>
    /// The uploader with the values of the user, checked again like an imported file (a value can't add a placeholder
    /// which asks, a line break to a header or a server without HTTPS).
    /// </summary>
    /// <exception cref="UploaderFileException">the values can't be used, the message says why</exception>
    internal static UploaderDefinition WithValues(UploaderDefinition definition, IEnumerable<UploaderValue> values)
    {
        var copy = JsonConvert.DeserializeObject<UploaderDefinition>(JsonConvert.SerializeObject(definition));
        foreach (var value in values)
        {
            string text = value.Value ?? string.Empty;
            switch (value.Kind)
            {
                case UploaderValueKind.RequestUrl:
                    copy.RequestURL = text;
                    break;
                case UploaderValueKind.Parameter:
                    copy.Parameters[value.Name] = text;
                    break;
                case UploaderValueKind.Header:
                    copy.Headers[value.Name] = text;
                    break;
                case UploaderValueKind.Argument:
                    copy.Arguments[value.Name] = text;
                    break;
            }
        }

        return UploaderFile.Read(JsonConvert.SerializeObject(copy));
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

        if (HasNoDeletion(definition))
        {
            text.Append("\n\n").Append(string.Format(Language.ImportNoDeletion, UploaderFile.DescribeServer(definition)));
        }

        if (_store.Find(definition.Name) != null)
        {
            text.Append("\n\n").Append(Language.ImportReplaces);
        }

        text.Append("\n\n").Append(string.Format(Language.ImportNotGreenshot, definition.Name, UploaderFile.DescribeServer(definition)));
        text.Append("\n\n").Append(Language.ImportTrust);
        return text.ToString();
    }

    /// <summary>
    /// Add the confirmed uploader, it replaces the one with the same name.
    /// </summary>
    /// <param name="definition">the checked uploader</param>
    /// <param name="remindBeforeUpload">show the capture and ask before each upload (the uploads can't be deleted)</param>
    internal void Import(UploaderDefinition definition, bool remindBeforeUpload = false)
    {
        string id = _store.Add(definition, remindBeforeUpload);
        Log.InfoFormat("Imported the uploader {0} for {1}", definition.Name, UploaderFile.DescribeServer(definition));
        Refresh();
        SelectedUploader = Uploaders.FirstOrDefault(item => item.Id == id);
        ServiceIcon.FetchInBackground(_store, id, definition, again: true);
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
    internal UploaderItem(StoredUploader uploader, ImageSource icon)
    {
        Icon = icon;
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

    /// <summary>
    /// The icon of the service, the generic one without it
    /// </summary>
    public ImageSource Icon { get; }

    /// <summary>
    /// The text a screen reader reads for the row
    /// </summary>
    public override string ToString() => string.IsNullOrEmpty(Description) ? Name : $"{Name}, {Description}";
}

/// <summary>
/// An upload in the list
/// </summary>
public sealed class UploadItem
{
    internal UploadItem(string id, UploadRecord upload, ImageSource icon)
    {
        Icon = icon;
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

    /// <summary>
    /// The icon of the uploader's service
    /// </summary>
    public ImageSource Icon { get; }

    /// <summary>
    /// The text a screen reader reads for the row
    /// </summary>
    public override string ToString() => $"{Date}, {Uploader}, {Url}";
}

internal enum UploaderValueKind
{
    RequestUrl,
    Parameter,
    Header,
    Argument
}

/// <summary>
/// A value of the uploader the user can change before the import
/// </summary>
public sealed class UploaderValue : INotifyPropertyChanged
{
    // Names under which services expect a key: the value is masked
    private static readonly Regex SecretName = new Regex("key|token|secret|password|auth", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    // The texts shared files put where the key of the user belongs: <API KEY HERE>, [insert key], YOUR_TOKEN, "Your key goes here"
    private static readonly Regex PlaceholderText = new Regex(@"<[^<>]*>|\[[^\[\]]*\]|\byour[\s_-]|\binsert\b|\bhere\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private bool _isRevealed;

    internal UploaderValue(UploaderValueKind kind, string name, string value)
    {
        Kind = kind;
        Name = name;
        Value = value;
        IsSecret = kind != UploaderValueKind.RequestUrl && name != null && SecretName.IsMatch(name);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>The value is a key: shown masked unless the user reveals it</summary>
    public bool IsSecret { get; }

    public bool IsRevealed
    {
        get => _isRevealed;
        set
        {
            _isRevealed = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRevealed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMasked)));
        }
    }

    /// <summary>The PasswordBox is shown instead of the TextBox</summary>
    public bool IsMasked => IsSecret && !IsRevealed;

    /// <summary>
    /// The value still looks like the text a shared file has where the key belongs (the user asked once before going on)
    /// </summary>
    internal bool LooksLikePlaceholder
    {
        get
        {
            try
            {
                return !string.IsNullOrEmpty(Value) && PlaceholderText.IsMatch(Value);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }

    internal UploaderValueKind Kind { get; }

    /// <summary>The name of the parameter, header or form field, null for the request url</summary>
    public string Name { get; }

    /// <summary>What the user sees: the kind and the name</summary>
    public string Label
    {
        get
        {
            var language = Texts.Get<ICustomUploaderLanguage>();
            return Kind switch
            {
                UploaderValueKind.RequestUrl => language.ValuesRequestUrl,
                UploaderValueKind.Parameter => string.Format(language.ValuesParameter, Name),
                UploaderValueKind.Header => string.Format(language.ValuesHeader, Name),
                _ => string.Format(language.ValuesArgument, Name)
            };
        }
    }

    public string Value { get; set; }
}
