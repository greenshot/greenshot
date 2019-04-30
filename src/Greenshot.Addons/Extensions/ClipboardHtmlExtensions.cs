// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Text;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Core.Enums;

namespace Greenshot.Addons.Extensions
{
    /// <summary>
    /// Extensions for the Clipboard functionality which have to do with HTML
    /// </summary>
    public static class ClipboardHtmlExtensions
    {
        // Defintion of the Html Clipboard format
        private static readonly string HtmlFormat = "CF_HTML";
        // Defintion of the Url Clipboard format
        private static readonly string UrlFormat = "CFSTR_INETURL";

        // Store the ID for the Html clipboard format
        private static uint? _htmlFormatId;
        // Store the ID for the Url clipboard format
        private static uint? _urlFormatId;

        // Template for the HTML Text on the clipboard
        // see: http://msdn.microsoft.com/en-us/library/ms649015%28v=vs.85%29.aspx
        // or:  http://msdn.microsoft.com/en-us/library/Aa767917.aspx
        private const string HtmlClipboardString = @"Version:0.9
StartHTML:<<<<<<<1
EndHTML:<<<<<<<2
StartFragment:<<<<<<<3
EndFragment:<<<<<<<4
StartSelection:<<<<<<<3
EndSelection:<<<<<<<4
<!DOCTYPE>
<HTML>
<HEAD>
<TITLE>Greenshot capture</TITLE>
</HEAD>
<BODY>
<!--StartFragment -->
<img border='0' src='file:///${file}' width='${width}' height='${height}'>
<!--EndFragment -->
</BODY>
</HTML>";

        // Template for the HTML Text on the clipboard
        // see: http://msdn.microsoft.com/en-us/library/ms649015%28v=vs.85%29.aspx
        // or:  http://msdn.microsoft.com/en-us/library/Aa767917.aspx
        private const string HtmlClipboardBase64String = @"Version:0.9
StartHTML:<<<<<<<1
EndHTML:<<<<<<<2
StartFragment:<<<<<<<3
EndFragment:<<<<<<<4
StartSelection:<<<<<<<3
EndSelection:<<<<<<<4
<!DOCTYPE>
<HTML>
<HEAD>
<TITLE>Greenshot capture</TITLE>
</HEAD>
<BODY>
<!--StartFragment -->
<img border='0' src='data:image/${format};base64,${data}' width='${width}' height='${height}'>
<!--EndFragment -->
</BODY>
</HTML>";
        /// <summary>
        /// Generate a HTML string for the clipboard
        /// </summary>
        /// <param name="size">Size</param>
        /// <param name="filename">string</param>
        /// <returns></returns>
        private static string GenerateHtmlString(NativeSize size, string filename)
        {
            var utf8EncodedHtmlString = Encoding.GetEncoding(0).GetString(Encoding.UTF8.GetBytes(HtmlClipboardString));
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${width}", size.Width.ToString());
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${height}", size.Height.ToString());
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${file}", filename.Replace("\\", "/"));
            var sb = new StringBuilder();
            sb.Append(utf8EncodedHtmlString);
            sb.Replace("<<<<<<<1", (utf8EncodedHtmlString.IndexOf("<HTML>", StringComparison.Ordinal) + "<HTML>".Length).ToString("D8"));
            sb.Replace("<<<<<<<2", utf8EncodedHtmlString.IndexOf("</HTML>", StringComparison.Ordinal).ToString("D8"));
            sb.Replace("<<<<<<<3", (utf8EncodedHtmlString.IndexOf("<!--StartFragment -->", StringComparison.Ordinal) + "<!--StartFragment -->".Length).ToString("D8"));
            sb.Replace("<<<<<<<4", utf8EncodedHtmlString.IndexOf("<!--EndFragment -->", StringComparison.Ordinal).ToString("D8"));
            return sb.ToString();
        }

        /// <summary>
        /// Generate a HTML snippet
        /// </summary>
        /// <param name="size">NativeSize</param>
        /// <param name="pngStream">Stream</param>
        /// <returns>string with the snippet</returns>
        private static string GenerateHtmlDataUrlString(NativeSize size, MemoryStream pngStream)
        {
            var utf8EncodedHtmlString = Encoding.GetEncoding(0).GetString(Encoding.UTF8.GetBytes(HtmlClipboardBase64String));
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${width}", size.Width.ToString());
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${height}", size.Height.ToString());
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${format}", "png");
            utf8EncodedHtmlString = utf8EncodedHtmlString.Replace("${data}", Convert.ToBase64String(pngStream.GetBuffer(), 0, (int)pngStream.Length));
            var sb = new StringBuilder();
            sb.Append(utf8EncodedHtmlString);
            sb.Replace("<<<<<<<1", (utf8EncodedHtmlString.IndexOf("<HTML>", StringComparison.Ordinal) + "<HTML>".Length).ToString("D8"));
            sb.Replace("<<<<<<<2", utf8EncodedHtmlString.IndexOf("</HTML>", StringComparison.Ordinal).ToString("D8"));
            sb.Replace("<<<<<<<3", (utf8EncodedHtmlString.IndexOf("<!--StartFragment -->", StringComparison.Ordinal) + "<!--StartFragment -->".Length).ToString("D8"));
            sb.Replace("<<<<<<<4", utf8EncodedHtmlString.IndexOf("<!--EndFragment -->", StringComparison.Ordinal).ToString("D8"));
            return sb.ToString();
        }

        /// <summary>
        /// Place the bitmap as HTML on the clipboard
        /// </summary>
        /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
        /// <param name="surface">ISurface</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        public static void SetAsHtml(this IClipboardAccessToken clipboardAccessToken, ISurface surface, ICoreConfiguration coreConfiguration)
        {
            var pngOutputSettings = new SurfaceOutputSettings(coreConfiguration, OutputFormats.png, 100, false);
            // This file is automatically deleted when Greenshot exits.
            var filename = ImageOutput.SaveNamedTmpFile(surface, surface.CaptureDetails, pngOutputSettings);
            // Set the PNG stream
            var htmlText = GenerateHtmlString(new NativeSize(surface.Width, surface.Height), filename);

            clipboardAccessToken.SetAsHtml(htmlText);
        }

        /// <summary>
        /// Place the bitmap as embedded HTML on the clipboard
        /// </summary>
        /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
        /// <param name="surface">ISurface</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        public static void SetAsEmbeddedHtml(this IClipboardAccessToken clipboardAccessToken, ISurface surface, ICoreConfiguration coreConfiguration)
        {
            using (var pngStream = new MemoryStream())
            {
                var pngOutputSettings = new SurfaceOutputSettings(coreConfiguration, OutputFormats.png, 100, false);
                ImageOutput.SaveToStream(surface, pngStream, pngOutputSettings);
                pngStream.Seek(0, SeekOrigin.Begin);
                // Set the PNG stream
                var htmlText = GenerateHtmlDataUrlString(new NativeSize(surface.Width, surface.Height), pngStream);
                clipboardAccessToken.SetAsHtml(htmlText);
            }
        }

        /// <summary>
        /// Place HTML on the clipboard
        /// </summary>
        /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
        /// <param name="htmlText">string with the html</param>
        public static void SetAsHtml(this IClipboardAccessToken clipboardAccessToken, string htmlText)
        {
            if (!_htmlFormatId.HasValue)
            {
                _htmlFormatId = ClipboardFormatExtensions.RegisterFormat(HtmlFormat);
            }

            // Set the Html stream
            clipboardAccessToken.SetAsUnicodeString(htmlText, _htmlFormatId.Value);
        }

        /// <summary>
        /// Place Uri on the clipboard
        /// </summary>
        /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
        /// <param name="url">string with the url</param>
        public static void SetAsUrl(this IClipboardAccessToken clipboardAccessToken, string url)
        {
            if (!_urlFormatId.HasValue)
            {
                _urlFormatId = ClipboardFormatExtensions.RegisterFormat(UrlFormat);
            }

            // Set the Html stream
            clipboardAccessToken.SetAsUnicodeString(url, _urlFormatId.Value);
        }
    }
}
