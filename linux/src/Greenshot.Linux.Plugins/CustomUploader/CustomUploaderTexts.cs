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

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Greenshot.Linux.Core.Languages;

namespace Greenshot.Linux.Plugins.CustomUploader;

/// <summary>
/// The texts of [CustomUploader] in greenshot.customuploader.*.ini, with the property names of ICustomUploaderLanguage
/// of Windows: ErrorNotJson is the key error_not_json. So the ported code reads its texts unchanged.
/// </summary>
public sealed partial class CustomUploaderTexts
{
    public static readonly CustomUploaderTexts Instance = new();

    [GeneratedRegex("(?<=[a-z0-9])([A-Z])")]
    private static partial Regex WordStart();

    public static string Key(string propertyName) => WordStart().Replace(propertyName, "_$1").ToLowerInvariant();

    public static string Get([CallerMemberName] string name = "") => Texts.Get("CustomUploader", Key(name));

    public string UploadMenuItem => Get();

    public string CommunicationWait => Get();

    public string NoLink => Get();

    public string ErrorFileTooLarge => Get();

    public string ErrorNotJson => Get();

    public string ErrorNotFileUploader => Get();

    public string ErrorMethod => Get();

    public string ErrorNoUrl => Get();

    public string ErrorOldSyntax => Get();

    public string ErrorNoFileFormName => Get();

    public string ErrorBody => Get();

    public string ErrorHeader => Get();

    public string ErrorDynamicHost => Get();

    public string ErrorNotHttps => Get();

    public string ErrorSyntax => Get();

    public string ErrorFunction => Get();
}
