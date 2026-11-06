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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using Greenshot.Base.Expressions;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// Shows under a template box what the template gives right now (a preview, which never changes a counter), or what is
    /// wrong with it. Bind <see cref="Template"/> to the same value as the box and set the <see cref="Purpose"/>.
    /// A live region: a screen reader says the new preview or problem.
    /// </summary>
    public class TemplatePreview : TextBlock
    {
        public static readonly DependencyProperty TemplateProperty = DependencyProperty.Register(
            nameof(Template), typeof(string), typeof(TemplatePreview), new PropertyMetadata(null, OnChanged));

        public static readonly DependencyProperty PurposeProperty = DependencyProperty.Register(
            nameof(Purpose), typeof(TemplatePurpose), typeof(TemplatePreview), new PropertyMetadata(TemplatePurpose.Text, OnChanged));

        /// <summary>For a file name: the extension shown after it, e.g. ".png"</summary>
        public static readonly DependencyProperty ExtensionProperty = DependencyProperty.Register(
            nameof(Extension), typeof(string), typeof(TemplatePreview), new PropertyMetadata(null, OnChanged));

        private readonly DispatcherTimer _delay;

        public TemplatePreview()
        {
            TextWrapping = TextWrapping.Wrap;
            Margin = new Thickness(0, 2, 0, 6);
            LiveRegion.SetMode(this, AutomationLiveSetting.Polite);
            // Typing changes the template on every key: show (and say) the result once the typing paused
            _delay = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = TimeSpan.FromMilliseconds(300) };
            _delay.Tick += (s, e) =>
            {
                _delay.Stop();
                Update();
            };
            Loaded += (s, e) => Update();
        }

        public string Template
        {
            get => (string)GetValue(TemplateProperty);
            set => SetValue(TemplateProperty, value);
        }

        public TemplatePurpose Purpose
        {
            get => (TemplatePurpose)GetValue(PurposeProperty);
            set => SetValue(PurposeProperty, value);
        }

        public string Extension
        {
            get => (string)GetValue(ExtensionProperty);
            set => SetValue(ExtensionProperty, value);
        }

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var preview = (TemplatePreview)d;
            preview._delay.Stop();
            preview._delay.Start();
        }

        private void Update()
        {
            var (text, isError) = Describe(Template, Purpose, Extension);
            Text = text;
            SetResourceReference(ForegroundProperty, isError ? "ThemeErrorBrush" : "ThemeMutedBrush");
            LiveRegion.Announce(this);
        }

        /// <summary>The preview of a template, or its first problem (exposed for tests)</summary>
        internal static (string Text, bool IsError) Describe(string template, TemplatePurpose purpose, string extension)
        {
            if (string.IsNullOrEmpty(template))
            {
                return (string.Empty, false);
            }
            var resolver = VariableResolver.Instance;
            var problem = resolver.Validate(template, purpose).FirstOrDefault(e => !e.IsWarning);
            if (problem != null)
            {
                return (problem.Message, true);
            }
            try
            {
                var scope = VariableScope.Empty;
                scope.Preview = true;
                string result = purpose == TemplatePurpose.FileName
                    ? resolver.RenderFileName(template, extension, scope).Name
                    : resolver.Render(template, purpose, scope);
                return (result, false);
            }
            catch (TemplateException ex)
            {
                return (ex.Message, true);
            }
        }

        /// <summary>
        /// The help for a purpose: the syntax, then every value it can use and the functions, made from the same list the
        /// resolver uses
        /// </summary>
        public static string HelpText(TemplatePurpose purpose, string introduction)
        {
            var resolver = VariableResolver.Instance;
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(introduction))
            {
                sb.AppendLine(introduction.Trim()).AppendLine();
            }
            foreach (var variable in resolver.Describe(purpose).OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append("${").Append(variable.Name).Append("}  ").AppendLine(variable.Description);
            }
            sb.AppendLine();
            foreach (var function in resolver.Functions)
            {
                sb.Append(function.Signature).Append("  ").AppendLine(function.Description);
            }
            return sb.ToString();
        }
    }
}
