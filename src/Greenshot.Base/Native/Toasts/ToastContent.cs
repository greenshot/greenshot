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
    /// The content of a toast (the ToastGeneric template): texts, an image and buttons. A click on the toast or a button
    /// passes its arguments to <see cref="ToastActivator.Activated"/>.
    /// </summary>
    public sealed class ToastContent
    {
        private readonly XElement _binding = new XElement("binding", new XAttribute("template", "ToastGeneric"));
        private readonly XElement _actions = new XElement("actions");
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

        public ToastContent AddButton(string text, string arguments)
        {
            _actions.Add(new XElement("action", new XAttribute("content", text), new XAttribute("arguments", arguments)));
            return this;
        }

        public XmlDocument ToXml()
        {
            var toast = new XElement("toast", new XAttribute("launch", _arguments), new XElement("visual", _binding));
            if (_actions.HasElements)
            {
                toast.Add(_actions);
            }
            var document = new XmlDocument();
            document.LoadXml(toast.ToString(SaveOptions.DisableFormatting));
            return document;
        }
    }
}
