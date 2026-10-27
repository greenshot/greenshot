/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

using System.Globalization;
using Greenshot.Linux.Core.Capturing;

namespace Greenshot.Linux.Core.Configuration;

/// <summary>
/// The [Core] section of greenshot.ini. Key names and defaults are the ones of ICoreConfiguration in Greenshot.Base,
/// so a greenshot.ini copied from Windows keeps its meaning.
/// </summary>
public sealed class CoreSettings
{
    public const string SectionName = "Core";

    private IniFile _ini;
    private readonly string _path;
    private readonly object _lock = new();

    private CoreSettings(IniFile ini, string path)
    {
        _ini = ini;
        _path = path;
    }

    /// <summary>
    /// $XDG_CONFIG_HOME/greenshot/greenshot.ini, or ~/.config/greenshot/greenshot.ini
    /// </summary>
    public static string DefaultPath => Path.Combine(XdgDirectories.ConfigHome, "greenshot", "greenshot.ini");

    public static CoreSettings Load(string? path = null)
    {
        path ??= DefaultPath;
        IniFile ini;
        try
        {
            ini = IniFile.Load(path);
        }
        catch (Exception ex)
        {
            Log.Error($"Could not read {path}, using the defaults", ex);
            ini = new IniFile();
        }

        return new CoreSettings(ini, path);
    }

    public string FilePath => _path;

    /// <summary>
    /// Reads greenshot.ini again, after it was changed outside Greenshot (greenshot --reload)
    /// </summary>
    public void Reload()
    {
        var ini = IniFile.Load(_path);
        lock (_lock)
        {
            _ini = ini;
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            _ini.Save(_path);
        }
    }

    /// <summary>
    /// The language in IETF format (e.g. en-US), empty for the language of the system
    /// </summary>
    public string Language
    {
        get => GetString(nameof(Language), string.Empty);
        set => SetString(nameof(Language), value);
    }

    /// <summary>
    /// System, Light or Dark
    /// </summary>
    public string Theme
    {
        get => GetString(nameof(Theme), "System");
        set => SetString(nameof(Theme), value);
    }

    /// <summary>
    /// The welcome window is shown once, at the first start
    /// </summary>
    public bool WelcomeShown
    {
        get => GetBool(nameof(WelcomeShown), false);
        set => SetBool(nameof(WelcomeShown), value);
    }

    public string RegionHotkey
    {
        get => GetString(nameof(RegionHotkey), "PrintScreen");
        set => SetString(nameof(RegionHotkey), value);
    }

    public string WindowHotkey
    {
        get => GetString(nameof(WindowHotkey), "Alt + PrintScreen");
        set => SetString(nameof(WindowHotkey), value);
    }

    public string FullscreenHotkey
    {
        get => GetString(nameof(FullscreenHotkey), "Ctrl + PrintScreen");
        set => SetString(nameof(FullscreenHotkey), value);
    }

    public string LastregionHotkey
    {
        get => GetString(nameof(LastregionHotkey), "Shift + PrintScreen");
        set => SetString(nameof(LastregionHotkey), value);
    }

    /// <summary>
    /// Editor, FileDefault, FileWithDialog, Clipboard, Picker
    /// </summary>
    public IReadOnlyList<string> OutputDestinations
    {
        get => GetList(nameof(OutputDestinations), "Picker").Select(Destinations.WellKnownDestinations.Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        set => SetString(nameof(OutputDestinations), string.Join(',', value));
    }

    public bool CaptureMousepointer
    {
        get => GetBool(nameof(CaptureMousepointer), true);
        set => SetBool(nameof(CaptureMousepointer), value);
    }

    /// <summary>
    /// Alt + PrintScreen: false captures the active window, true opens the capture screen in the window mode
    /// </summary>
    public bool CaptureWindowsInteractive
    {
        get => GetBool(nameof(CaptureWindowsInteractive), false);
        set => SetBool(nameof(CaptureWindowsInteractive), value);
    }

    public int CaptureDelay
    {
        get => GetInt(nameof(CaptureDelay), 100);
        set => SetInt(nameof(CaptureDelay), value);
    }

    public bool PlayCameraSound
    {
        get => GetBool(nameof(PlayCameraSound), false);
        set => SetBool(nameof(PlayCameraSound), value);
    }

    public bool ShowTrayNotification
    {
        get => GetBool(nameof(ShowTrayNotification), true);
        set => SetBool(nameof(ShowTrayNotification), value);
    }

    /// <summary>
    /// The folder captures are saved in, defaults to the XDG pictures folder (~/Pictures)
    /// </summary>
    public string OutputFilePath
    {
        get
        {
            var value = GetString(nameof(OutputFilePath), string.Empty);
            return string.IsNullOrWhiteSpace(value) ? XdgDirectories.Pictures : value;
        }
        set => SetString(nameof(OutputFilePath), value);
    }

    public bool OutputFileAllowOverwrite
    {
        get => GetBool(nameof(OutputFileAllowOverwrite), true);
        set => SetBool(nameof(OutputFileAllowOverwrite), value);
    }

    public string OutputFileFilenamePattern
    {
        get => GetString(nameof(OutputFileFilenamePattern), "${capturetime:d\"yyyy-MM-dd HH_mm_ss\"}-${title}");
        set => SetString(nameof(OutputFileFilenamePattern), value);
    }

    public string OutputFileFormat
    {
        get => GetString(nameof(OutputFileFormat), "png");
        set => SetString(nameof(OutputFileFormat), value);
    }

    public bool OutputFileCopyPathToClipboard
    {
        get => GetBool(nameof(OutputFileCopyPathToClipboard), true);
        set => SetBool(nameof(OutputFileCopyPathToClipboard), value);
    }

    /// <summary>
    /// The file chosen last in "Save as", its folder is offered the next time
    /// </summary>
    public string OutputFileAsFullpath
    {
        get => GetString(nameof(OutputFileAsFullpath), string.Empty);
        set => SetString(nameof(OutputFileAsFullpath), value);
    }

    public int OutputFileJpegQuality
    {
        get => Math.Clamp(GetInt(nameof(OutputFileJpegQuality), 80), 1, 100);
        set => SetInt(nameof(OutputFileJpegQuality), value);
    }

    public uint OutputFileIncrementingNumber
    {
        get => (uint)Math.Max(0, GetInt(nameof(OutputFileIncrementingNumber), 1));
        set => SetString(nameof(OutputFileIncrementingNumber), value.ToString(CultureInfo.InvariantCulture));
    }

    public bool OutputPrintPromptOptions
    {
        get => GetBool(nameof(OutputPrintPromptOptions), true);
        set => SetBool(nameof(OutputPrintPromptOptions), value);
    }

    public bool OutputPrintAllowRotate
    {
        get => GetBool(nameof(OutputPrintAllowRotate), false);
        set => SetBool(nameof(OutputPrintAllowRotate), value);
    }

    public bool OutputPrintAllowEnlarge
    {
        get => GetBool(nameof(OutputPrintAllowEnlarge), false);
        set => SetBool(nameof(OutputPrintAllowEnlarge), value);
    }

    public bool OutputPrintAllowShrink
    {
        get => GetBool(nameof(OutputPrintAllowShrink), true);
        set => SetBool(nameof(OutputPrintAllowShrink), value);
    }

    public bool OutputPrintCenter
    {
        get => GetBool(nameof(OutputPrintCenter), true);
        set => SetBool(nameof(OutputPrintCenter), value);
    }

    public bool OutputPrintInverted
    {
        get => GetBool(nameof(OutputPrintInverted), false);
        set => SetBool(nameof(OutputPrintInverted), value);
    }

    public bool OutputPrintGrayscale
    {
        get => GetBool(nameof(OutputPrintGrayscale), false);
        set => SetBool(nameof(OutputPrintGrayscale), value);
    }

    public bool OutputPrintMonochrome
    {
        get => GetBool(nameof(OutputPrintMonochrome), false);
        set => SetBool(nameof(OutputPrintMonochrome), value);
    }

    public bool OutputPrintFooter
    {
        get => GetBool(nameof(OutputPrintFooter), true);
        set => SetBool(nameof(OutputPrintFooter), value);
    }

    public string OutputPrintFooterPattern
    {
        get => GetString(nameof(OutputPrintFooterPattern), "${capturetime:d\"D\"} ${capturetime:d\"T\"} - ${title}");
        set => SetString(nameof(OutputPrintFooterPattern), value);
    }

    /// <summary>
    /// Linux only: the tesseract languages for text recognition, e.g. deu+eng; empty for the language of Greenshot and English
    /// </summary>
    public string OcrLanguages
    {
        get => GetValue("Ocr", "Languages") ?? string.Empty;
        set => SetValue("Ocr", "Languages", value);
    }

    /// <summary>
    /// The last region in layout coordinates, stored as "x,y,width,height" like the NativeRect of Windows
    /// </summary>
    public LayoutRect LastCapturedRegion
    {
        get => LayoutRect.TryParse(GetString(nameof(LastCapturedRegion), string.Empty), out var rect) ? rect : LayoutRect.Empty;
        set => SetString(nameof(LastCapturedRegion), value.ToString());
    }

    /// <summary>
    /// Linux only: which screen grabber to use, Auto, Grim or Portal
    /// </summary>
    public string CaptureBackend
    {
        get => GetString(nameof(CaptureBackend), "Auto");
        set => SetString(nameof(CaptureBackend), value);
    }

    public bool ZoomerEnabled
    {
        get => GetBool(nameof(ZoomerEnabled), true);
        set => SetBool(nameof(ZoomerEnabled), value);
    }

    /// <summary>
    /// A value of any section, for the settings of other parts (e.g. the editor) which share greenshot.ini
    /// </summary>
    public string? GetValue(string section, string key)
    {
        lock (_lock)
        {
            return _ini.Get(section, key);
        }
    }

    public void SetValue(string section, string key, string value)
    {
        lock (_lock)
        {
            _ini.Set(section, key, value);
        }
    }

    /// <summary>
    /// All keys and values of a section
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> GetValues(string section)
    {
        lock (_lock)
        {
            return _ini.GetSection(section);
        }
    }

    public void RemoveValue(string section, string key)
    {
        lock (_lock)
        {
            _ini.Remove(section, key);
        }
    }

    private string GetString(string key, string defaultValue) => GetValue(SectionName, key) ?? defaultValue;

    private void SetString(string key, string value) => SetValue(SectionName, key, value);

    private bool GetBool(string key, bool defaultValue) =>
        bool.TryParse(GetString(key, string.Empty), out var value) ? value : defaultValue;

    private void SetBool(string key, bool value) => SetString(key, value ? "True" : "False");

    private int GetInt(string key, int defaultValue) =>
        int.TryParse(GetString(key, string.Empty), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : defaultValue;

    private void SetInt(string key, int value) => SetString(key, value.ToString(CultureInfo.InvariantCulture));

    private IReadOnlyList<string> GetList(string key, string defaultValue)
    {
        var value = GetString(key, defaultValue);
        var items = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return items.Length > 0 ? items : [defaultValue];
    }
}
