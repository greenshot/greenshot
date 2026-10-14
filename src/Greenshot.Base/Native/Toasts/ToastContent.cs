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
using System.Xml.Linq;
using Windows.Data.Xml.Dom;

namespace Greenshot.Base.Native.Toasts
{
    /// <summary>
    /// The content of a toast (the ToastGeneric template): texts, rows with an icon and an image. A click on the toast passes its arguments to
    /// <see cref="ToastActivator.Activated"/>.
    /// </summary>
    public sealed class ToastContent
    {
        private readonly XElement _binding = new XElement("binding", new XAttribute("template", "ToastGeneric"));
        private readonly string _arguments;

        /// <param name="arguments">The arguments of a click on the toast</param>
        public ToastContent(string arguments)
        {
            _arguments = arguments;
        }

        public ToastContent AddText(string text)
        {
            _binding.Add(new XElement("text", text));
            return this;
        }

        /// <summary>
        /// An image below the texts, an app which isn't packaged can only use a local file
        /// </summary>
        public ToastContent AddImage(string path)
        {
            _binding.Add(new XElement("image", new XAttribute("src", new Uri(path).AbsoluteUri)));
            return this;
        }

        /// <summary>
        /// A row with a small icon and a text next to it (an adaptive group), e.g. what a destination did
        /// </summary>
        /// <param name="iconPath">A local image file, null for a row with the text only</param>
        /// <param name="text">The text, it wraps</param>
        public ToastContent AddRow(string iconPath, string text)
        {
            var row = new XElement("group");
            if (!string.IsNullOrEmpty(iconPath))
            {
                row.Add(new XElement("subgroup", new XAttribute("hint-weight", "1"),
                    new XElement("image", new XAttribute("src", new Uri(iconPath).AbsoluteUri), new XAttribute("hint-removeMargin", "true"))));
            }
            row.Add(new XElement("subgroup", new XAttribute("hint-weight", "8"), new XAttribute("hint-textStacking", "center"),
                new XElement("text", new XAttribute("hint-wrap", "true"), text)));
            _binding.Add(row);
            return this;
        }

        public XmlDocument ToXml()
        {
            var toast = new XElement("toast", new XAttribute("launch", _arguments), new XElement("visual", _binding));
            var document = new XmlDocument();
            document.LoadXml(toast.ToString(SaveOptions.DisableFormatting));
            return document;
        }
    }
}
