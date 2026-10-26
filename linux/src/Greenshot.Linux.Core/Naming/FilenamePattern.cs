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

using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Configuration;

namespace Greenshot.Linux.Core.Naming;

/// <summary>
/// Fills filename patterns like ${capturetime:d"yyyy-MM-dd HH_mm_ss"}-${title}.
/// This is the FilenameHelper of Greenshot.Base, with the same variables and parameters:
/// p&lt;width&gt;[,char] pads, s&lt;start&gt;[,length] takes a substring, r&lt;old&gt;,&lt;new&gt; replaces and d&lt;format&gt; formats a date.
/// </summary>
public sealed partial class FilenamePattern
{
    private const int MaxTitleLength = 80;
    private const string UnsafeReplacement = "_";
    private const string RandomChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public const string DefaultTitle = "Greenshot";

    [GeneratedRegex(@"\${(?<variable>[^:}]+)[:]?(?<parameters>[^}]*)}")]
    private static partial Regex VarRegex();

    [GeneratedRegex(";(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)")]
    private static partial Regex SplitRegex();

    [GeneratedRegex("^R+$")]
    private static partial Regex RandRegex();

    private readonly Func<uint> _nextNumber;
    private readonly IDictionary _environment;

    /// <param name="nextNumber">Returns the next ${NUM}, the caller increments and stores it</param>
    /// <param name="environment">The environment variables, the process environment when null</param>
    public FilenamePattern(Func<uint> nextNumber, IDictionary? environment = null)
    {
        _nextNumber = nextNumber;
        _environment = environment ?? Environment.GetEnvironmentVariables();
    }

    public CultureInfo Culture { get; init; } = CultureInfo.CurrentCulture;

    public static FilenamePattern ForSettings(CoreSettings settings) => new(() =>
    {
        var number = settings.OutputFileIncrementingNumber + 1;
        settings.OutputFileIncrementingNumber = number;
        return number;
    });

    /// <summary>
    /// Fill the pattern, with filenameSafe every value is made safe to use in a filename
    /// </summary>
    public string Fill(string pattern, CaptureDetails? details, bool filenameSafe = true) =>
        VarRegex().Replace(pattern, match =>
        {
            try
            {
                return Evaluate(match, details, filenameSafe);
            }
            catch (Exception ex)
            {
                Log.Error($"Error filling the pattern {pattern}", ex);
                return string.Empty;
            }
        });

    /// <summary>
    /// The filename without the folder, falls back to "greenshot ${capturetime}" for an empty pattern like Windows does
    /// </summary>
    public string GetFilename(string pattern, string extension, CaptureDetails? details)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            pattern = "greenshot ${capturetime}";
        }

        return Fill(pattern, details) + "." + extension.TrimStart('.');
    }

    public static string MakeFilenameSafe(string filename)
    {
        foreach (char disallowed in Path.GetInvalidFileNameChars())
        {
            filename = filename.Replace(disallowed.ToString(), UnsafeReplacement);
        }

        return filename;
    }

    public static string MakePathSafe(string path)
    {
        foreach (char disallowed in Path.GetInvalidPathChars())
        {
            path = path.Replace(disallowed.ToString(), UnsafeReplacement);
        }

        return path;
    }

    private string Evaluate(Match match, CaptureDetails? details, bool filenameSafe)
    {
        int padWidth = 0;
        int startIndex = 0;
        int endIndex = 0;
        char padChar = ' ';
        string dateFormat = "yyyy-MM-dd HH-mm-ss";
        var replacements = new Dictionary<string, string>();
        string variable = match.Groups["variable"].Value;
        string parameters = match.Groups["parameters"].Value;
        string value = string.Empty;

        if (parameters.Length > 0)
        {
            foreach (string parameter in SplitRegex().Split(parameters))
            {
                if (parameter.Length == 0)
                {
                    continue;
                }

                switch (parameter[0])
                {
                    case 'p':
                        var padParams = parameter[1..].Split(',');
                        if (int.TryParse(padParams[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width))
                        {
                            padWidth = width;
                        }

                        if (padParams.Length > 1 && padParams[1].Length > 0)
                        {
                            padChar = padParams[1][0];
                        }

                        break;
                    case 'r':
                        var replaceParams = parameter[1..].Split(',');
                        if (replaceParams.Length == 2)
                        {
                            replacements[replaceParams[0]] = replaceParams[1];
                        }

                        break;
                    case 'd':
                        dateFormat = parameter[1..];
                        if (dateFormat.StartsWith('"'))
                        {
                            dateFormat = dateFormat[1..];
                        }

                        if (dateFormat.EndsWith('"'))
                        {
                            dateFormat = dateFormat[..^1];
                        }

                        break;
                    case 's':
                        var range = parameter[1..].Split(',');
                        if (int.TryParse(range[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var start))
                        {
                            startIndex = start;
                        }

                        if (range.Length > 1 && int.TryParse(range[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var end))
                        {
                            endIndex = end;
                        }

                        break;
                }
            }
        }

        if (_environment.Contains(variable))
        {
            value = (string?)_environment[variable] ?? string.Empty;
            if (filenameSafe)
            {
                value = MakePathSafe(value);
            }
        }
        else if (details?.MetaData.TryGetValue(variable, out var metaValue) == true)
        {
            value = filenameSafe ? MakePathSafe(metaValue) : metaValue;
        }
        else if (RandRegex().IsMatch(variable))
        {
            var chars = new char[variable.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = RandomChars[Random.Shared.Next(RandomChars.Length)];
            }

            value = new string(chars);
        }
        else
        {
            var captureTime = details?.DateTime ?? DateTime.Now;
            var title = details?.Title ?? DefaultTitle;
            if (title.Length > MaxTitleLength)
            {
                title = title[..MaxTitleLength];
            }

            switch (variable)
            {
                case "domain":
                    value = Environment.UserDomainName;
                    break;
                case "user":
                    value = Environment.UserName;
                    break;
                case "hostname":
                    value = Environment.MachineName;
                    break;
                case "YYYY":
                    DefaultPadding(ref padWidth, ref padChar, 4);
                    value = captureTime.Year.ToString(CultureInfo.InvariantCulture);
                    break;
                case "MM":
                    DefaultPadding(ref padWidth, ref padChar, 2);
                    value = captureTime.Month.ToString(CultureInfo.InvariantCulture);
                    break;
                case "DD":
                    DefaultPadding(ref padWidth, ref padChar, 2);
                    value = captureTime.Day.ToString(CultureInfo.InvariantCulture);
                    break;
                case "hh":
                    DefaultPadding(ref padWidth, ref padChar, 2);
                    value = captureTime.Hour.ToString(CultureInfo.InvariantCulture);
                    break;
                case "mm":
                    DefaultPadding(ref padWidth, ref padChar, 2);
                    value = captureTime.Minute.ToString(CultureInfo.InvariantCulture);
                    break;
                case "ss":
                    DefaultPadding(ref padWidth, ref padChar, 2);
                    value = captureTime.Second.ToString(CultureInfo.InvariantCulture);
                    break;
                case "now":
                    value = DateTime.Now.ToString(dateFormat, Culture);
                    if (filenameSafe)
                    {
                        value = MakeFilenameSafe(value);
                    }

                    break;
                case "capturetime":
                    value = captureTime.ToString(dateFormat, Culture);
                    if (filenameSafe)
                    {
                        value = MakeFilenameSafe(value);
                    }

                    break;
                case "NUM":
                    DefaultPadding(ref padWidth, ref padChar, 6);
                    value = _nextNumber().ToString(CultureInfo.InvariantCulture);
                    break;
                case "title":
                    value = filenameSafe ? MakeFilenameSafe(title) : title;
                    break;
                case "MyPictures":
                    value = XdgDirectories.Pictures;
                    break;
                case "MyMusic":
                    value = XdgDirectories.GetUserDir("XDG_MUSIC_DIR") ?? Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                    break;
                case "MyDocuments":
                case "Personal":
                    value = XdgDirectories.GetUserDir("XDG_DOCUMENTS_DIR") ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    break;
                case "Desktop":
                    value = XdgDirectories.GetUserDir("XDG_DESKTOP_DIR") ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    break;
                case "ApplicationData":
                    value = XdgDirectories.ConfigHome;
                    break;
                case "LocalApplicationData":
                    value = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    break;
            }
        }

        if (padWidth > 0)
        {
            value = value.PadRight(padWidth, padChar);
        }
        else if (padWidth < 0)
        {
            value = value.PadLeft(-padWidth, padChar);
        }

        if (startIndex != 0 || endIndex != 0)
        {
            if (startIndex < 0)
            {
                startIndex = value.Length + startIndex;
            }

            if (endIndex < 0)
            {
                endIndex = value.Length + endIndex;
            }

            // Like Windows, a range outside the value leaves the value as it is
            if (startIndex >= 0 && startIndex <= value.Length)
            {
                if (endIndex != 0)
                {
                    if (endIndex > 0 && startIndex + endIndex <= value.Length)
                    {
                        value = value.Substring(startIndex, endIndex);
                    }
                }
                else
                {
                    value = value[startIndex..];
                }
            }
        }

        foreach (var replacement in replacements)
        {
            value = value.Replace(replacement.Key, replacement.Value, StringComparison.Ordinal);
        }

        return value;
    }

    /// <summary>
    /// Numbers are padded with zeros on the left, unless the pattern asks for something else
    /// </summary>
    private static void DefaultPadding(ref int padWidth, ref char padChar, int width)
    {
        if (padWidth == 0)
        {
            padWidth = -width;
            padChar = '0';
        }
    }
}
