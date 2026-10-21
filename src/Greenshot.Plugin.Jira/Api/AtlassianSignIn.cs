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

using System;
using Greenshot.Base.Http;

namespace Greenshot.Plugin.Jira.Api;

/// <summary>
/// How to sign in to Atlassian: Cloud with the e-mail address and an API token
/// (https://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/),
/// Data Center with user name and password or a personal access token.
/// </summary>
internal static class AtlassianSignIn
{
    /// <summary>
    /// Server and Data Center answer 403 with this header after failed sign-ins, until the user solved a CAPTCHA in the browser
    /// </summary>
    public const string DeniedReasonHeader = "X-Authentication-Denied-Reason";

    public static ServerSignInOptions Options { get; } = new ServerSignInOptions(
        "https://id.atlassian.com/manage-profile/security/api-tokens",
        uri => uri.Host.EndsWith(".atlassian.net", StringComparison.OrdinalIgnoreCase));
}
