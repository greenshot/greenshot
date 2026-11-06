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
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using Dapplo.Ini;
using Dapplo.Ini.Configuration;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using static Greenshot.Base.Expressions.VariableTrust;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// The namespaces of the core: time, payload, output, user, machine, env, config, option, var, recipe.
    /// The counters are part of the resolver, see <see cref="VariableResolver"/>.
    /// </summary>
    internal static class CoreVariables
    {
        public static IEnumerable<IVariableProvider> Create()
        {
            yield return Time();
            yield return Payload();
            yield return Output();
            yield return User();
            yield return Machine();
            yield return Env();
            yield return new ConfigVariables();
            yield return Option();
            yield return Var();
            yield return Recipe();
        }

        private static VariableProvider Time()
        {
            var p = new VariableProvider("time");
            p.Add<DateTime>("now", Trusted, "The time now (local time)", s => s.FixedNow ?? DateTime.Now);
            p.Add<DateTime>("utc", Trusted, "The time now in UTC", s => (s.FixedNow ?? DateTime.Now).ToUniversalTime());
            return p;
        }

        private static ICaptureDetails CaptureOf(VariableScope scope) =>
            scope.Capture ?? scope.Flow?.Payload?.RawCapture?.CaptureDetails ?? scope.Flow?.Payload?.Surface?.CaptureDetails;

        private static string Meta(VariableScope scope, string key)
        {
            var meta = CaptureOf(scope)?.MetaData;
            return meta != null && meta.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : null;
        }

        private static Size? ImageSizeOf(VariableScope scope)
        {
            if (scope.ImageSize.HasValue) return scope.ImageSize;
            var payload = scope.Flow?.Payload;
            var image = payload?.Surface?.Image ?? payload?.RawCapture?.Image;
            return image?.Size;
        }

        private static VariableProvider Payload()
        {
            var p = new VariableProvider("payload");
            p.Add<int>("width", Trusted, "The width of the image in pixels (now, after cropping or resizing)", s => (object)ImageSizeOf(s)?.Width);
            p.Add<int>("height", Trusted, "The height of the image in pixels (now)", s => (object)ImageSizeOf(s)?.Height);
            p.Add<DateTime>("time", Trusted, "When it was taken; for an opened file the time stored in the file, else the file's time", s =>
            {
                var details = CaptureOf(s);
                if (details != null && details.DateTime != default) return details.DateTime;
                return s.FixedNow ?? DateTime.Now;
            });
            p.Add<string>("source", Trusted, "Where it came from: Screen, Window, Clipboard or file", s => Meta(s, "source"));
            p.Add<string>("hdr", Trusted, "Available (can be saved as HDR), Lost (an edit removed it) or None", s =>
                s.Flow?.Payload != null ? HdrOutput.StateOf(s.Flow.Payload) : null);
            p.Add<string>("title", Untrusted, "The window title, or the title stored in an opened file", s =>
            {
                string title = CaptureOf(s)?.Title;
                return string.IsNullOrEmpty(title) ? null : title;
            });
            p.Add<string>("file", Untrusted, "The full path of an opened file", s => Meta(s, "file"));
            p.Add<string>("url", Untrusted, "The web address stored with the capture", s => Meta(s, "url"));
            p.Add<string>("ocr.text", Untrusted, "The text found by OCR or a barcode", s =>
            {
                string text = s.Flow?.Payload?.ExtractedText;
                return string.IsNullOrEmpty(text) ? null : text;
            });
            p.Add<string>("video.file", Trusted, "The file of a video recording", s =>
                s.Flow?.Payload?.Metadata != null && s.Flow.Payload.Metadata.TryGetValue("VideoFilePath", out var file) ? file as string : null);
            return p;
        }

        private static VariableProvider Output()
        {
            var p = new VariableProvider("output");
            p.Add<string>("file", Trusted, "The full path of the file written by this export", s => Empty(s.OutputFile));
            p.Add<string>("folder", Trusted, "The folder of the file written by this export", s =>
                string.IsNullOrEmpty(s.OutputFile) ? null : Path.GetDirectoryName(s.OutputFile));
            p.Add<string>("name", Trusted, "The file name with extension of the file written by this export", s =>
                string.IsNullOrEmpty(s.OutputFile) ? null : Path.GetFileName(s.OutputFile));
            p.Add<string>("url", Trusted, "The link of the upload", s => Empty(s.OutputUrl));
            return p;
        }

        private static string Empty(string value) => string.IsNullOrEmpty(value) ? null : value;

        private static string Folder(Environment.SpecialFolder folder)
        {
            try
            {
                return Empty(Environment.GetFolderPath(folder));
            }
            catch (PlatformNotSupportedException)
            {
                return null;
            }
        }

        private static string Variable(string name, EnvironmentVariableTarget target)
        {
            try
            {
                return Empty(Environment.GetEnvironmentVariable(name, target));
            }
            catch (Exception ex) when (ex is System.Security.SecurityException || ex is ArgumentException || ex is NotSupportedException)
            {
                return null;
            }
        }

        private static VariableProvider User()
        {
            var p = new VariableProvider("user");
            p.Add<string>("name", Trusted, "The user name", s => Environment.UserName);
            p.Add<string>("domain", Trusted, "The domain of the user", s => Environment.UserDomainName);
            // Environment.GetFolderPath asks the shell, so moved folders (e.g. to OneDrive) are found
            p.Add<string>("profile", Trusted, "The user's profile folder", s => Folder(Environment.SpecialFolder.UserProfile));
            p.Add<string>("desktop", Trusted, "The desktop folder", s => Folder(Environment.SpecialFolder.Desktop));
            p.Add<string>("documents", Trusted, "The documents folder", s => Folder(Environment.SpecialFolder.MyDocuments));
            p.Add<string>("pictures", Trusted, "The pictures folder", s => Folder(Environment.SpecialFolder.MyPictures));
            p.Add<string>("videos", Trusted, "The videos folder", s => Folder(Environment.SpecialFolder.MyVideos));
            p.Add<string>("music", Trusted, "The music folder", s => Folder(Environment.SpecialFolder.MyMusic));
            p.Add<string>("appdata", Trusted, "The roaming application data folder", s => Folder(Environment.SpecialFolder.ApplicationData));
            p.Add<string>("localappdata", Trusted, "The local application data folder", s => Folder(Environment.SpecialFolder.LocalApplicationData));
            p.Add<string>("temp", Trusted, "The temporary folder", s => Empty(Path.GetTempPath()));
            p.AddWildcard<string>("env", Trusted, "An environment variable of the user (Windows only)", (name, s) => Variable(name, EnvironmentVariableTarget.User))
                .WindowsOnly = true;
            return p;
        }

        private static VariableProvider Machine()
        {
            var p = new VariableProvider("machine");
            p.Add<string>("name", Trusted, "The computer name", s => Environment.MachineName);
            p.Add<string>("os", Trusted, "The operating system: Win32NT, Unix, ...", s => Environment.OSVersion.Platform.ToString());
            p.Add<string>("osversion", Trusted, "The version of the operating system", s => Environment.OSVersion.VersionString);
            p.Add<int>("processors", Trusted, "The number of processors", s => Environment.ProcessorCount);
            p.Add<bool>("is64bit", Trusted, "Is the operating system 64 bit", s => Environment.Is64BitOperatingSystem);
            p.Add<string>("programfiles", Trusted, "The program files folder", s => Folder(Environment.SpecialFolder.ProgramFiles));
            p.Add<string>("commonappdata", Trusted, "The application data folder of all users", s => Folder(Environment.SpecialFolder.CommonApplicationData));
            p.Add<string>("systemdir", Trusted, "The system folder", s => Empty(Environment.SystemDirectory));
            p.AddWildcard<string>("env", Trusted, "An environment variable of the machine (Windows only)", (name, s) => Variable(name, EnvironmentVariableTarget.Machine))
                .WindowsOnly = true;
            return p;
        }

        private static VariableProvider Env()
        {
            var p = new VariableProvider("env");
            p.AddWildcard<string>(null, Trusted, "An environment variable of Greenshot's process (as %NAME% in Windows)", (name, s) => Variable(name, EnvironmentVariableTarget.Process));
            return p;
        }

        private static VariableProvider Option()
        {
            var p = new VariableProvider("option");
            p.AddWildcard<object>(null, Trusted, "A recipe option, as set in Settings > Recipes", (key, s) =>
            {
                var recipe = s.Flow?.Recipe;
                if (!RecipeOptionStore.TryResolve(recipe, key, out var owner, out var option)) return null;
                object value = RecipeOptionStore.GetValue(owner, option);
                // A template option is evaluated where it is used; it can't use options itself
                if (option.IsTemplate && value is string template && template.IndexOf("${", StringComparison.Ordinal) >= 0)
                {
                    return VariableResolver.Instance.RenderOptionTemplate(template, s);
                }
                return value;
            });
            return p;
        }

        private static VariableProvider Var()
        {
            var p = new VariableProvider("var");
            p.AddWildcard<object>(null, Untrusted, "A recipe variable: set by a trigger, a SetVariable step or the output of a step", (name, s) =>
                s.Flow?.Properties != null && s.Flow.Properties.TryGetValue(name, out var value) ? value : null);
            return p;
        }

        private static VariableProvider Recipe()
        {
            var p = new VariableProvider("recipe");
            p.Add<string>("id", Trusted, "The id of the running recipe", s => s.Flow?.Recipe?.Id);
            p.Add<string>("name", Trusted, "The name of the running recipe", s => s.Flow?.Recipe?.Name);
            p.Add<string>("executionId", Trusted, "The id of this run", s => s.Flow?.ExecutionId.ToString());
            p.Add<string>("state", Trusted, "The state of this run", s => s.Flow?.State.ToString());
            p.Add<string>("abortReason", Trusted, "Why this run was stopped", s => s.Flow?.AbortReason);
            return p;
        }
    }

    /// <summary>
    /// config.&lt;section&gt;.&lt;key&gt;: the settings of greenshot.ini, read only, read by name without reflection.
    /// Credentials, tokens and proxy settings are left out.
    /// </summary>
    internal sealed class ConfigVariables : IVariableProvider
    {
        private static readonly string[] HiddenKeyParts = { "password", "token", "secret", "credential", "proxy", "apikey" };
        private static readonly string[] HiddenSections = { "CustomUploader", "RecipeOptions" };

        public string Namespace => "config";

        public IEnumerable<VariableDescriptor> Variables
        {
            get
            {
                yield return new VariableDescriptor("config.*", typeof(string), Trusted,
                    "A setting of greenshot.ini as config.<section>.<key>, e.g. config.Core.OutputFileFormat");
            }
        }

        public static bool IsHidden(string section, string key) =>
            HiddenSections.Any(h => string.Equals(h, section, StringComparison.OrdinalIgnoreCase))
            || HiddenKeyParts.Any(part => key.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0);

        public bool TryGetValue(string name, VariableScope scope, out object value)
        {
            value = null;
            string rest = name.Substring(Namespace.Length + 1);
            int dot = rest.IndexOf('.');
            if (dot <= 0 || dot == rest.Length - 1)
            {
                return false;
            }
            string sectionName = rest.Substring(0, dot);
            string key = rest.Substring(dot + 1);
            if (IsHidden(sectionName, key))
            {
                return true;
            }
            if (!IniConfigRegistry.TryGet("greenshot.ini", out var iniConfig) || iniConfig == null)
            {
                return true;
            }
            var section = iniConfig.GetSections().FirstOrDefault(s => string.Equals(s.SectionName, sectionName, StringComparison.OrdinalIgnoreCase)) as IniSectionBase;
            if (section == null)
            {
                return true;
            }
            foreach (var pair in section.GetAllRawValues())
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = string.IsNullOrEmpty(pair.Value) ? null : pair.Value;
                    return true;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// The name of a value in one place, so a check can say what an old name is now.
    /// </summary>
    internal static class LegacyNames
    {
        private static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["title"] = "payload.title",
            ["capturetime"] = "payload.time",
            ["now"] = "time.now",
            ["utcnow"] = "time.utc",
            ["NUM"] = "counter.global",
            ["user"] = "user.name",
            ["domain"] = "user.domain",
            ["hostname"] = "machine.name",
            ["MyPictures"] = "user.pictures",
            ["MyDocuments"] = "user.documents",
            ["Desktop"] = "user.desktop",
            ["ocr_text"] = "payload.ocr.text",
            ["payload.text"] = "payload.ocr.text",
            ["payload.extractedtext"] = "payload.ocr.text",
            ["payload.filename"] = "payload.file",
            ["payload.w"] = "payload.width",
            ["payload.h"] = "payload.height",
            ["user.username"] = "user.name",
            ["user.userprofile"] = "user.profile",
            ["user.mydocuments"] = "user.documents",
            ["user.mypictures"] = "user.pictures",
            ["user.roamingappdata"] = "user.appdata",
            ["user.userdomain"] = "user.domain",
            ["machine.computername"] = "machine.name",
            ["machine.machinename"] = "machine.name",
            ["context.RecipeName"] = "recipe.name",
            ["context.RecipeId"] = "recipe.id",
            ["context.ExecutionId"] = "recipe.executionId",
            ["context.State"] = "recipe.state",
            ["context.AbortReason"] = "recipe.abortReason",
        };

        /// <summary>What to write instead of an unknown name, null when there's no hint</summary>
        public static string Hint(string name)
        {
            if (Replacements.TryGetValue(name, out var replacement)) return replacement;
            if (name.StartsWith("context.", StringComparison.OrdinalIgnoreCase)) return "var." + name.Substring(8);
            if (name.StartsWith("payload.", StringComparison.OrdinalIgnoreCase)) return null;
            if (name.IndexOf('.') < 0) return "var." + name + " (a recipe variable) or env." + name + " (an environment variable)";
            return null;
        }
    }
}
