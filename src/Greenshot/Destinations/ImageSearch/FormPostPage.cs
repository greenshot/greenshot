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

#if !GREENSHOT_LIGHT
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dapplo.Windows.Shell32;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;

namespace Greenshot.Destinations.ImageSearch
{
    /// <summary>
    /// A local page which posts a form to a web site as soon as the browser opens it: the browser sends the data, so the result
    /// opens in the user's own browser session, Greenshot sends nothing itself. The page holds the capture, so it is removed
    /// shortly after the browser read it, at the latest when Greenshot exits.
    /// </summary>
    internal static class FormPostPage
    {
        /// <summary>
        /// The browser reads the page when it opens it, a slow start of the browser included
        /// </summary>
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Write the page to Greenshot's temp folder and open it in the default browser
        /// </summary>
        /// <param name="action">https address the form is posted to</param>
        /// <param name="fields">the fields of the form, sent as multipart/form-data</param>
        public static void Open(string action, IReadOnlyDictionary<string, string> fields)
        {
            string path = Path.Combine(ImageIO.GetTmpDirectory(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".html");
            File.WriteAllText(path, Create(action, fields), new UTF8Encoding(false));
            // Removed at exit, or sooner below
            ImageIO.RegisterTmpFile(path);
            Housekeeping.Current?.Enqueue("Remove an image search page", _ =>
            {
                ImageIO.DeleteNamedTmpFile(path);
                return Task.CompletedTask;
            }, Lifetime);

            DefaultBrowser.Open(path);
        }

        /// <summary>
        /// The page: a form with the fields which submits itself, a button for a browser without JavaScript for local pages
        /// </summary>
        internal static string Create(string action, IReadOnlyDictionary<string, string> fields)
        {
            if (!Uri.TryCreate(action, UriKind.Absolute, out var actionUri) || actionUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("Only an https address can be the target of the form", nameof(action));
            }

            var page = new StringBuilder();
            page.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><meta name=\"referrer\" content=\"no-referrer\"><title>Greenshot</title></head><body>");
            page.Append("<form id=\"search\" method=\"post\" enctype=\"multipart/form-data\" action=\"").Append(WebUtility.HtmlEncode(actionUri.AbsoluteUri)).Append("\">");
            foreach (var field in fields)
            {
                page.Append("<input type=\"hidden\" name=\"").Append(WebUtility.HtmlEncode(field.Key))
                    .Append("\" value=\"").Append(WebUtility.HtmlEncode(field.Value)).Append("\">");
            }
            page.Append("<noscript><button type=\"submit\">").Append(WebUtility.HtmlEncode(actionUri.Host)).Append("</button></noscript></form>");
            page.Append("<script>document.getElementById('search').submit();</script></body></html>");
            return page.ToString();
        }
    }
}
#endif
