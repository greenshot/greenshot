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
using System.IO;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// The file names and texts made from the templates in the settings (OutputFileNameTemplate, OutputPrintFooterTemplate).
    /// A broken template never stops an export: the default is used and the problem is logged.
    /// </summary>
    public static class OutputTemplates
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OutputTemplates));

        /// <summary>The default of OutputFileNameTemplate, the 1.3 default converted</summary>
        public const string DefaultFileName = "${payload.time:yyyy-MM-dd HH_mm_ss}-${payload.title}";

        /// <summary>The default of OutputPrintFooterTemplate</summary>
        public const string DefaultPrintFooter = "${payload.time:D} ${payload.time:T} - ${payload.title}";

        private static ICoreConfiguration CoreConfig => IniConfigRegistry.GetSection<ICoreConfiguration>();

        /// <summary>The configured file name template, the default when none is set</summary>
        public static string FileNameTemplate
        {
            get
            {
                string template = CoreConfig?.OutputFileNameTemplate;
                return string.IsNullOrWhiteSpace(template) ? DefaultFileName : template;
            }
        }

        /// <summary>The configured print footer template, the default when none is set</summary>
        public static string PrintFooterTemplate
        {
            get
            {
                string template = CoreConfig?.OutputPrintFooterTemplate;
                return string.IsNullOrWhiteSpace(template) ? DefaultPrintFooter : template;
            }
        }

        /// <summary>
        /// A file name from a template; the default template when it has errors. Call <see cref="RenderedFileName.Commit"/> when the file
        /// was written, so the counters go up.
        /// </summary>
        /// <param name="template">The template, null for the configured one</param>
        /// <param name="extension">The extension with its dot, null for none</param>
        /// <param name="scope">The values; its TargetFolder is the folder the name is in</param>
        public static RenderedFileName RenderFileName(string template, string extension, VariableScope scope)
        {
            template ??= FileNameTemplate;
            try
            {
                return VariableResolver.Instance.RenderFileName(template, extension, scope);
            }
            catch (TemplateException ex)
            {
                Log.WarnFormat("The file name template '{0}' can't be used, the default is used: {1}", template, ex.Message);
                return VariableResolver.Instance.RenderFileName(DefaultFileName, extension, scope);
            }
        }

        /// <summary>
        /// The name of a file for the capture outside a folder, e.g. for an upload or an attachment: the last part of the configured template.
        /// The counters it uses go up (an upload is a use), counter.folder and counter.name are 1.
        /// </summary>
        /// <param name="captureDetails">The capture, can be null</param>
        /// <param name="formatId">The file format, for the extension</param>
        public static string UploadFileName(ICaptureDetails captureDetails, string formatId)
        {
            var name = RenderFileName(null, FileFormatRegistry.GetPreferredExtensionWithDot(formatId), VariableScope.ForCapture(captureDetails));
            name.Commit();
            return Path.GetFileName(name.Name);
        }

        /// <summary>
        /// The configured file name without extension, as it would be now, e.g. to suggest a name in a dialog; nothing goes up.
        /// May hold sub folders.
        /// </summary>
        public static string PreviewFileName(ICaptureDetails captureDetails, string targetFolder = null)
        {
            var scope = VariableScope.ForCapture(captureDetails);
            scope.TargetFolder = targetFolder;
            scope.Preview = true;
            return RenderFileName(null, null, scope).Name;
        }

        /// <summary>A text from a template (a footer, a subject): problems are logged, missing values stay empty</summary>
        public static string RenderText(string template, ICaptureDetails captureDetails)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;
            try
            {
                return VariableResolver.Instance.Render(template, TemplatePurpose.Text, VariableScope.ForCapture(captureDetails));
            }
            catch (Exception ex) when (ex is TemplateException || ex is FormatException)
            {
                Log.WarnFormat("The template '{0}' can't be used: {1}", template, ex.Message);
                return template;
            }
        }

        /// <summary>
        /// The program of a command from a template (e.g. ${user.localappdata}\Programs\x.exe): null with the reason when it can't
        /// be used. Nothing of the capture can choose the program.
        /// </summary>
        public static string RenderCommand(string template, VariableScope scope, out string problem)
        {
            problem = null;
            if (string.IsNullOrWhiteSpace(template)) return template;
            try
            {
                return VariableResolver.Instance.Render(template.Trim(), TemplatePurpose.Command, scope);
            }
            catch (TemplateException ex)
            {
                problem = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// A folder from a template, e.g. a folder in a setting: null with the reason when it can't be used
        /// </summary>
        /// <summary>The known folders a typed or picked folder is written with, the longest path first wins</summary>
        private static readonly string[] KnownFolders =
        {
            "user.desktop", "user.pictures", "user.documents", "user.videos", "user.music", "user.localappdata", "user.appdata",
            "user.profile", "machine.programfiles", "machine.commonappdata"
        };

        /// <summary>
        /// A folder as a template: a path in a known folder starts with its value instead, e.g. ${user.pictures}\Greenshots, so it
        /// moves with the folder (OneDrive, another user). A template or a path elsewhere stays as it is.
        /// </summary>
        public static string FolderTemplateOf(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.IndexOf("${", StringComparison.Ordinal) >= 0)
            {
                return path;
            }
            string best = null;
            string bestFolder = null;
            foreach (string name in KnownFolders)
            {
                string folder = VariableResolver.Instance.Render("${" + name + "}", TemplatePurpose.Folder, VariableScope.Empty);
                if (string.IsNullOrEmpty(folder)) continue;
                folder = folder.TrimEnd('\\', '/');
                bool inside = path.Length == folder.Length
                    ? string.Equals(path, folder, StringComparison.OrdinalIgnoreCase)
                    : path.Length > folder.Length && path.StartsWith(folder, StringComparison.OrdinalIgnoreCase) && (path[folder.Length] == '\\' || path[folder.Length] == '/');
                if (inside && (bestFolder == null || folder.Length > bestFolder.Length))
                {
                    best = name;
                    bestFolder = folder;
                }
            }
            return best == null ? path : "${" + best + "}" + path.Substring(bestFolder.Length);
        }

        public static string RenderFolder(string template, VariableScope scope, out string problem)
        {
            problem = null;
            if (string.IsNullOrWhiteSpace(template)) return template;
            try
            {
                return VariableResolver.Instance.Render(template.Trim(), TemplatePurpose.Folder, scope);
            }
            catch (TemplateException ex)
            {
                problem = ex.Message;
                return null;
            }
        }
    }
}
