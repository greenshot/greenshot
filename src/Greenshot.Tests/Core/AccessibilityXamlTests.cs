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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// Keeps the WPF views accessible, see docs/vNext/accessibility.md: the rules a screen reader, a keyboard user and
    /// the Windows text size depend on, checked in every .xaml file of the source tree.
    /// </summary>
    public class AccessibilityXamlTests
    {
        // Styles which give their buttons a name themselves (title bar buttons, the x and + of the pills)
        private static readonly string[] NamedButtonStyles = { "TitleCloseButtonStyle", "TitleBarButtonStyle", "RemoveEntryButtonStyle", "AddPillButtonStyle" };
        private static readonly HashSet<string> ButtonElements = new(StringComparer.Ordinal) { "Button", "ToggleButton", "RepeatButton" };
        private static readonly HashSet<string> TextAttributes = new(StringComparer.Ordinal)
        {
            "Text", "Content", "Header", "ToolTip", "ToolTipService.ToolTip", "Title", "AutomationProperties.Name", "AutomationProperties.HelpText"
        };
        // Elements whose inner text is shown, e.g. <TextBlock>Hello</TextBlock>
        private static readonly HashSet<string> TextElements = new(StringComparer.Ordinal)
        {
            "TextBlock", "Run", "Label", "AccessText", "Button", "CheckBox", "RadioButton", "Hyperlink", "ComboBoxItem", "ListBoxItem"
        };
        // Design time values (d:Text, d:Title) are never shown to users
        private static readonly XNamespace Designer = "http://schemas.microsoft.com/expression/blend/2008";
        // Names of products and formats, the same in every language; compared without emoji and punctuation
        private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
        {
            "Greenshot", "Mermaid", "Dropbox", "Atlassian Jira", "Microsoft Office", "Microsoft Word", "Microsoft Excel", "Microsoft PowerPoint",
            "Microsoft OneNote", "Microsoft Outlook", "MP4 H 264 AVC", "MP4 HEVC H 265", "mm", "SHA 256",
            // Key names, as on the key badges of the hotkeys
            "Ctrl", "Alt", "Shift", "Win"
        };
        private static readonly Regex NotLetterOrDigit = new(@"[^\p{L}\p{N}]+");
        // A word: two letters in a row, so "X:", "✕" or "ℹ️" are not texts to translate
        private static readonly Regex Word = new(@"\p{L}{2,}");
        private static readonly Regex FixedFontSize = new(@"FontSize=""\d|Property=""FontSize""\s+Value=""\d");
        private static readonly Regex PlainSlider = new(@"<Slider[\s>]");
        private static readonly Regex AutomationTextBinding = new(@"AutomationProperties\.(Name|HelpText|ItemStatus|ItemType|AcceleratorKey|AccessKey|AutomationId)(=""|""\s+Value="")\{Binding[^""]*""");
        private static readonly Regex HiddenFocus = new(@"FocusVisualStyle=""\{x:Null\}""|Property=""FocusVisualStyle""\s+Value=""\{x:Null\}""");

        [Fact]
        public void FontSizes_FollowTheWindowsTextSize()
        {
            // {wpf:FontSize 12} instead of FontSize="12", so the text grows with Settings > Accessibility > Text size
            AssertNoProblems(XamlLines().Where(l => FixedFontSize.IsMatch(l.Text)).Select(l => $"{l.Location}: {l.Text.Trim()}"));
        }

        [Fact]
        public void KeyboardFocus_IsNeverHidden()
        {
            // Use GreenshotFocusVisual (FocusStyles.xaml) instead of switching the focus frame off
            AssertNoProblems(XamlLines().Where(l => HiddenFocus.IsMatch(l.Text)).Select(l => $"{l.Location}: {l.Text.Trim()}"));
        }

        [Fact]
        public void AutomationTexts_AreNeverNull()
        {
            // The text properties of AutomationProperties refuse null: a binding to null fails (a binding error in the log), TargetNullValue='' doesn't
            AssertNoProblems(XamlLines()
                .SelectMany(l => AutomationTextBinding.Matches(l.Text).Cast<Match>().Where(m => !m.Value.Contains("TargetNullValue")).Select(m => $"{l.Location}: {m.Value}")));
        }

        [Fact]
        public void Sliders_SayTheValueTheyShow()
        {
            // A plain Slider is read as a percentage of its range ("32 %" for "7 px"), wpf:ValueSlider says its ValueText
            AssertNoProblems(XamlLines().Where(l => PlainSlider.IsMatch(l.Text)).Select(l => $"{l.Location}: {l.Text.Trim()}"));
        }

        [Fact]
        public void ButtonsWithoutText_HaveAnAccessibleName()
        {
            var problems = new List<string>();
            foreach (var (file, document) in XamlDocuments())
            {
                foreach (var button in document.Descendants().Where(e => ButtonElements.Contains(e.Name.LocalName)))
                {
                    // A control template defines how a button looks, the button using it gets the name
                    if (button.Ancestors().Any(a => a.Name.LocalName == "ControlTemplate"))
                    {
                        continue;
                    }
                    if (Attribute(button, "AutomationProperties.Name") != null || Attribute(button, "AutomationProperties.LabeledBy") != null)
                    {
                        continue;
                    }
                    string style = Attribute(button, "Style") ?? string.Empty;
                    if (NamedButtonStyles.Any(style.Contains) || HasVisibleText(button))
                    {
                        continue;
                    }
                    problems.Add($"{file}:{((System.Xml.IXmlLineInfo)button).LineNumber}: {button.Name.LocalName} has only an icon or glyph, set AutomationProperties.Name");
                }
            }
            AssertNoProblems(problems);
        }

        [Fact]
        public void Texts_ComeFromTheLanguagePacks()
        {
            // A literal English text is neither translated nor read in the user's language by a screen reader
            var problems = new List<string>();
            foreach (var (file, document) in XamlDocuments())
            {
                foreach (var element in document.Descendants())
                {
                    foreach (var attribute in element.Attributes())
                    {
                        if (attribute.Name.Namespace != Designer && TextAttributes.Contains(attribute.Name.LocalName) && IsLiteralText(attribute.Value))
                        {
                            problems.Add($"{file}:{((System.Xml.IXmlLineInfo)attribute).LineNumber}: {element.Name.LocalName} {attribute.Name.LocalName}=\"{attribute.Value}\"");
                        }
                    }
                    if (TextElements.Contains(element.Name.LocalName))
                    {
                        string innerText = string.Concat(element.Nodes().OfType<XText>().Select(t => t.Value)).Trim();
                        if (IsLiteralText(innerText))
                        {
                            problems.Add($"{file}:{((System.Xml.IXmlLineInfo)element).LineNumber}: <{element.Name.LocalName}>{innerText}</{element.Name.LocalName}>");
                        }
                    }
                    if (element.Name.LocalName == "Setter" && TextAttributes.Contains(Attribute(element, "Property") ?? string.Empty)
                        && IsLiteralText(Attribute(element, "Value")))
                    {
                        problems.Add($"{file}:{((System.Xml.IXmlLineInfo)element).LineNumber}: Setter {Attribute(element, "Property")}=\"{Attribute(element, "Value")}\"");
                    }
                }
            }
            AssertNoProblems(problems);
        }

        private static bool IsLiteralText(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.StartsWith("{", StringComparison.Ordinal) || !Word.IsMatch(value))
            {
                return false;
            }
            // Product names and links are the same in every language
            if (Names.Contains(NotLetterOrDigit.Replace(value, " ").Trim()) || (!value.Contains(" ") && (value.Contains("://") || value.Contains("."))))
            {
                return false;
            }
            return true;
        }

        private static bool HasVisibleText(XElement button)
        {
            string content = Attribute(button, "Content");
            if (content != null)
            {
                return content.StartsWith("{", StringComparison.Ordinal) || Word.IsMatch(content);
            }
            foreach (var element in button.Descendants())
            {
                string name = element.Name.LocalName;
                if (name == "ContentPresenter")
                {
                    return true;
                }
                if (name is "TextBlock" or "Run" or "AccessText" or "Label")
                {
                    string text = Attribute(element, "Text") ?? Attribute(element, "Content") ?? element.Value;
                    if (!string.IsNullOrEmpty(text) && (text.StartsWith("{", StringComparison.Ordinal) || Word.IsMatch(text)))
                    {
                        return true;
                    }
                }
            }
            return Word.IsMatch(button.Nodes().OfType<XText>().Aggregate(string.Empty, (all, t) => all + t.Value));
        }

        /// <summary>
        /// An attribute by its name without namespace, also attached properties like AutomationProperties.Name
        /// </summary>
        private static string Attribute(XElement element, string localName) =>
            element.Attributes().FirstOrDefault(a => a.Name.LocalName == localName)?.Value;

        private static IEnumerable<string> XamlFiles() =>
            Directory.EnumerateFiles(LanguagePackTests.FindSourceDirectory(), "*.xaml", SearchOption.AllDirectories)
                .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\") && !f.Contains("/obj/") && !f.Contains("/bin/"));

        private static IEnumerable<(string Location, string Text)> XamlLines()
        {
            foreach (string file in XamlFiles())
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    yield return ($"{Path.GetFileName(file)}:{i + 1}", lines[i]);
                }
            }
        }

        private static IEnumerable<(string File, XDocument Document)> XamlDocuments()
        {
            var documents = XamlFiles().Select(f => (Path.GetFileName(f), XDocument.Load(f, LoadOptions.SetLineInfo))).ToList();
            Assert.True(documents.Count > 40, $"Only {documents.Count} XAML files found, wrong source directory?");
            return documents;
        }

        private static void AssertNoProblems(IEnumerable<string> problems)
        {
            var list = problems.ToList();
            Assert.True(list.Count == 0, string.Join(Environment.NewLine, list));
        }
    }
}
