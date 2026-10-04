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
using System.Globalization;
using System.IO;
using System.Linq;
using Dapplo.Ini;
using Dapplo.Ini.Internationalization;
using Dapplo.Ini.Internationalization.Configuration;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Base.Languages
{
    /// <summary>
    /// Greenshot's texts: the language packs greenshot.{ietf}.ini (core) and greenshot.{module}.{ietf}.ini (plugins)
    /// in the Languages folders, loaded with Dapplo.Ini. The core sections are properties here, a plugin registers
    /// its section with <see cref="Register{T}"/> and gets it with <see cref="Get{T}"/>.
    /// Keys which are only known at runtime (enum values, labels of built-in extensions) are looked up with
    /// <see cref="LanguageConfig.GetTranslation"/> on <see cref="Config"/>.
    /// </summary>
    public static class Texts
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Texts));
        private static readonly object InitializeLock = new object();
        private const string BaseName = "greenshot";
        private const string BaseLanguage = "en-US";
        private static LanguageConfig _config;
        private static bool _initializing;
        private static IReadOnlyList<string> _searchPaths = Array.Empty<string>();

        /// <summary>
        /// The language configuration, loaded on first use
        /// </summary>
        public static LanguageConfig Config => _config ?? Initialize();

        /// <summary>
        /// The language of Windows, set at startup before the threads' cultures are changed; used when no language is configured
        /// </summary>
        public static string SystemLanguage { get; set; }

        /// <summary>
        /// The folders searched for language packs and help files, highest priority first
        /// </summary>
        public static IReadOnlyList<string> SearchPaths
        {
            get
            {
                _ = Config;
                return _searchPaths;
            }
        }

        public static ICoreLanguage Core => Get<ICoreLanguage>();

        public static IEditorLanguage Editor => Get<IEditorLanguage>();

        public static ISettingsLanguage Settings => Get<ISettingsLanguage>();

        public static ISelfServiceLanguage SelfService => Get<ISelfServiceLanguage>();

        public static IRecipeLanguage Recipe => Get<IRecipeLanguage>();

        /// <summary>
        /// The language section of type T, a core section or one a plugin registered
        /// </summary>
        public static T Get<T>() where T : class => Config.GetSection<T>();

        /// <summary>
        /// Register the language section of a plugin, its texts are loaded right away
        /// </summary>
        public static T Register<T>(T section) where T : class => Config.RegisterSection(section);

        /// <summary>
        /// Load the language packs. The language comes from the configuration, else from Windows; the closest
        /// available language is used and stored in the configuration. Called at startup, otherwise on first use.
        /// </summary>
        /// <param name="additionalSearchPaths">Folders searched before the default ones (e.g. for tests)</param>
        public static LanguageConfig Initialize(params string[] additionalSearchPaths)
        {
            lock (InitializeLock)
            {
                if (_config != null)
                {
                    return _config;
                }

                // The lock lets the same thread in again: a text used while loading would recurse until the stack overflows
                if (_initializing)
                {
                    throw new InvalidOperationException("A text was used while the language packs are loaded");
                }

                _initializing = true;
                try
                {
                    return _config = Load(additionalSearchPaths);
                }
                finally
                {
                    _initializing = false;
                }
            }
        }

        private static LanguageConfig Load(string[] additionalSearchPaths)
        {
            // The language setting only counts once greenshot.ini was read; before that (texts used very early) Windows decides
            ICoreConfiguration coreConfig = null;
            if (IniConfigRegistry.TryGet("greenshot.ini", out var iniConfig))
            {
                coreConfig = iniConfig.IsLoaded ? iniConfig.GetSection<ICoreConfiguration>() : null;
            }
            else
            {
                coreConfig = IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());
            }

            string requested = coreConfig?.Language;
            if (string.IsNullOrEmpty(requested))
            {
                requested = SystemLanguage ?? CultureInfo.CurrentUICulture.Name;
            }

            _searchPaths = FindSearchPaths(additionalSearchPaths);
            var builder = LanguageConfigRegistry.ForFile(BaseName)
                .WithBaseLanguage(BaseLanguage)
                .WithCurrentLanguage(string.IsNullOrEmpty(requested) ? BaseLanguage : requested)
                .ResolveLanguages()
                .MergeSearchPaths()
                .AllowLateSectionRegistration()
                .AddListener(new LanguageLogListener())
                .RegisterSection<ICoreLanguage>(new CoreLanguageImpl())
                .RegisterSection<IEditorLanguage>(new EditorLanguageImpl())
                .RegisterSection<ISettingsLanguage>(new SettingsLanguageImpl())
                .RegisterSection<ISelfServiceLanguage>(new SelfServiceLanguageImpl())
                .RegisterSection<IRecipeLanguage>(new RecipeLanguageImpl());
            foreach (var searchPath in _searchPaths)
            {
                builder.AddSearchPath(searchPath);
            }

            LanguageConfig config;
            try
            {
                config = builder.Build();
            }
            catch (Exception ex)
            {
                // Without language packs every text shows its key, Greenshot still works
                Log.Error("Couldn't load the language packs", ex);
                LanguageConfigRegistry.Unregister(BaseName);
                config = LanguageConfigRegistry.ForFile(BaseName)
                    .WithBaseLanguage(BaseLanguage)
                    .AllowLateSectionRegistration()
                    .AddSearchPath(AppDomain.CurrentDomain.BaseDirectory)
                    .RegisterSection<ICoreLanguage>(new CoreLanguageImpl())
                    .RegisterSection<IEditorLanguage>(new EditorLanguageImpl())
                    .RegisterSection<ISettingsLanguage>(new SettingsLanguageImpl())
                    .RegisterSection<ISelfServiceLanguage>(new SelfServiceLanguageImpl())
                    .RegisterSection<IRecipeLanguage>(new RecipeLanguageImpl())
                    .Create();
            }

            Log.InfoFormat("Language {0} (requested {1}), language packs from {2}", config.CurrentLanguage, requested, string.Join(", ", _searchPaths));
            if (coreConfig != null && coreConfig.Language != config.CurrentLanguage)
            {
                coreConfig.Language = config.CurrentLanguage;
            }

            return config;
        }

        /// <summary>
        /// Switch the language: the closest available one is used and stored in the configuration
        /// </summary>
        /// <returns>The language which is used</returns>
        public static string SetLanguage(string ietf)
        {
            if (string.IsNullOrEmpty(ietf))
            {
                return Config.CurrentLanguage;
            }

            if (!string.Equals(ietf, Config.CurrentLanguage, StringComparison.OrdinalIgnoreCase))
            {
                Config.SetLanguage(ietf);
            }

            var coreConfig = IniConfigHelper.EnsureSection<ICoreConfiguration>(() => new CoreConfigurationImpl());
            if (coreConfig != null && coreConfig.Language != Config.CurrentLanguage)
            {
                coreConfig.Language = Config.CurrentLanguage;
            }

            return Config.CurrentLanguage;
        }

        /// <summary>
        /// The text of an enum value: the key Type.Value (e.g. ClipboardFormat.PNG), or the value's name when there is none
        /// </summary>
        public static string Translate(Enum value)
        {
            if (value == null)
            {
                return null;
            }

            return Config.TryGetTranslation($"{value.GetType().Name}.{value}", out var text) ? text : value.ToString();
        }

        /// <summary>
        /// The culture of the current language, null for a tag Windows doesn't know (e.g. de-x-franconia)
        /// </summary>
        public static CultureInfo CurrentCulture
        {
            get
            {
                try
                {
                    var culture = CultureInfo.GetCultureInfo(Config.CurrentLanguage);
                    return string.Equals(culture.Name, Config.CurrentLanguage, StringComparison.OrdinalIgnoreCase) ? culture : null;
                }
                catch (CultureNotFoundException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The folders with language packs, highest priority first: the portable app folder,
        /// the user's own translations in %APPDATA%\Greenshot\Languages, the installation.
        /// </summary>
        private static IReadOnlyList<string> FindSearchPaths(IEnumerable<string> additionalSearchPaths)
        {
            var candidates = new List<string>();
            candidates.AddRange(additionalSearchPaths ?? Enumerable.Empty<string>());
            string applicationFolder = EnvironmentInfo.GetApplicationFolder();
            if (applicationFolder != null)
            {
                // PortableApps.com layout
                candidates.Add(Path.Combine(applicationFolder, @"App\Greenshot\Languages"));
            }

            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Greenshot\Languages"));
            if (applicationFolder != null)
            {
                candidates.Add(Path.Combine(applicationFolder, "Languages"));
            }

            candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages"));

            var result = new List<string>();
            foreach (var candidate in candidates.Where(c => !string.IsNullOrEmpty(c)))
            {
                string fullPath;
                try
                {
                    fullPath = Path.GetFullPath(candidate);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Ignoring language path {candidate}", ex);
                    continue;
                }

                if (Directory.Exists(fullPath) && !result.Contains(fullPath, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(fullPath);
                }
            }

            return result;
        }
    }
}
