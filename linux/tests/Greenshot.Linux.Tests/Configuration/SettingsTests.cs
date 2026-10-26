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

using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Configuration;

namespace Greenshot.Linux.Tests.Configuration;

public sealed class SettingsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "greenshot-tests-" + Guid.NewGuid().ToString("N"));

    private string IniPath => Path.Combine(_folder, "greenshot.ini");

    [Fact]
    public void Defaults_are_the_ones_of_windows()
    {
        var settings = CoreSettings.Load(IniPath);
        Assert.Equal("PrintScreen", settings.RegionHotkey);
        Assert.Equal("${capturetime:d\"yyyy-MM-dd HH_mm_ss\"}-${title}", settings.OutputFileFilenamePattern);
        Assert.Equal(["Picker"], settings.OutputDestinations);
        Assert.Equal(100, settings.CaptureDelay);
        Assert.True(settings.CaptureMousepointer);
        Assert.True(settings.LastCapturedRegion.IsEmpty);
    }

    [Fact]
    public void A_windows_ini_keeps_unknown_sections_and_keys()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(IniPath, """
            ; Greenshot core configuration
            [Core]
            Language=de-DE
            OutputFileFormat=jpg
            OutputDestinations=FileDefault,Clipboard
            LastCapturedRegion=10,20,300,200

            [Editor]
            FreehandSensitivity=3
            """);

        var settings = CoreSettings.Load(IniPath);
        Assert.Equal("jpg", settings.OutputFileFormat);
        Assert.Equal(["FileDefault", "Clipboard"], settings.OutputDestinations);
        Assert.Equal(new LayoutRect(10, 20, 300, 200), settings.LastCapturedRegion);

        settings.CaptureDelay = 250;
        settings.Save();

        var text = File.ReadAllText(IniPath);
        Assert.Contains("Language=de-DE", text);
        Assert.Contains("[Editor]", text);
        Assert.Contains("FreehandSensitivity=3", text);
        Assert.Contains("; Greenshot core configuration", text);
        Assert.Equal(250, CoreSettings.Load(IniPath).CaptureDelay);
    }

    [Fact]
    public void Invalid_values_fall_back_to_the_default()
    {
        var ini = IniFile.Parse("[Core]\nCaptureDelay=soon\nCaptureMousepointer=maybe\nOutputFileJpegQuality=500\n");
        Directory.CreateDirectory(_folder);
        ini.Save(IniPath);
        var settings = CoreSettings.Load(IniPath);
        Assert.Equal(100, settings.CaptureDelay);
        Assert.True(settings.CaptureMousepointer);
        Assert.Equal(100, settings.OutputFileJpegQuality);
    }

    [Fact]
    public void Ini_keys_are_case_insensitive()
    {
        var ini = IniFile.Parse("[core]\noutputfileformat=webp\n");
        Assert.Equal("webp", ini.Get("Core", "OutputFileFormat"));
    }

    [Fact]
    public void User_dirs_are_read_with_home_expanded()
    {
        var lines = new[] { "# comment", "XDG_DESKTOP_DIR=\"$HOME/Desktop\"", "XDG_PICTURES_DIR=\"$HOME/Bilder\"" };
        Assert.Equal("/home/someone/Bilder", XdgDirectories.ParseUserDir(lines, "XDG_PICTURES_DIR", "/home/someone"));
        Assert.Null(XdgDirectories.ParseUserDir(lines, "XDG_MUSIC_DIR", "/home/someone"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
