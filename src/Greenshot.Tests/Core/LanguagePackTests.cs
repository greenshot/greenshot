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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapplo.Ini.Internationalization.Attributes;
using Dapplo.Ini.Internationalization.Configuration;
using Greenshot.Base.Languages;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The language packs (greenshot.{ietf}.ini, greenshot.{module}.{ietf}.ini) and the typed language sections
    /// </summary>
    public class LanguagePackTests
    {
        private static readonly string LanguageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages");

        public LanguagePackTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// All language section interfaces: the core ones and those of the plugins
        /// </summary>
        public static IEnumerable<object[]> LanguageSections()
        {
            var assemblies = new[]
            {
                typeof(ICoreLanguage).Assembly,
                typeof(Greenshot.Plugin.Box.IBoxLanguage).Assembly,
                typeof(Greenshot.Plugin.Confluence.IConfluenceLanguage).Assembly,
                typeof(Greenshot.Plugin.Dropbox.IDropboxLanguage).Assembly,
                typeof(Greenshot.Plugin.ExternalCommand.IExternalCommandLanguage).Assembly,
                typeof(Greenshot.Plugin.Imgur.IImgurLanguage).Assembly,
                typeof(Greenshot.Plugin.Jira.IJiraLanguage).Assembly,
                typeof(Greenshot.Plugin.Office.IOfficeLanguage).Assembly
            };
            return assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsInterface && t.GetCustomAttribute<IniLanguageSectionAttribute>() != null)
                .Select(t => new object[] { t });
        }

        [Theory]
        [MemberData(nameof(LanguageSections))]
        public void EveryText_HasAnEnglishTranslation(Type sectionType)
        {
            // Texts.Get<T>() for the section type
            object section = typeof(Texts).GetMethod(nameof(Texts.Get)).MakeGenericMethod(sectionType).Invoke(null, null);
            var missing = sectionType.GetProperties()
                .Where(p => p.PropertyType == typeof(string))
                .Select(p => (Name: p.Name, Value: (string)p.GetValue(section)))
                // Empty is allowed: about_translation names the translator, English has none
                .Where(p => p.Value == null || p.Value.StartsWith("###", StringComparison.Ordinal))
                .Select(p => p.Name)
                .ToList();
            Assert.True(missing.Count == 0, $"{sectionType.Name}: no English text for {string.Join(", ", missing)}");
        }

        [Fact]
        public void XamlTexts_ReferenceExistingProperties()
        {
            var sections = LanguageSections()
                .Select(o => (Type)o[0])
                .ToDictionary(t => t.GetCustomAttribute<IniLanguageSectionAttribute>().SectionName, StringComparer.Ordinal);
            var textReference = new Regex(@"\{wpf:Text\s+(\w+)\.(\w+)\s*\}");
            var problems = new List<string>();
            int count = 0;
            foreach (string xamlFile in Directory.EnumerateFiles(FindSourceDirectory(), "*.xaml", SearchOption.AllDirectories)
                         .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\")))
            {
                foreach (Match match in textReference.Matches(File.ReadAllText(xamlFile)))
                {
                    count++;
                    string section = match.Groups[1].Value, property = match.Groups[2].Value;
                    if (!sections.TryGetValue(section, out var sectionType))
                    {
                        problems.Add($"{Path.GetFileName(xamlFile)}: unknown section {section} in {match.Value}");
                    }
                    else if (sectionType.GetProperty(property) == null)
                    {
                        problems.Add($"{Path.GetFileName(xamlFile)}: {sectionType.Name} has no {property}");
                    }
                }
            }

            Assert.True(count > 300, $"Only {count} text references found, wrong source directory?");
            Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        }

        [Fact]
        public void LanguageSwitch_ChangesTheTexts_AndNotifiesTheChangedOnes()
        {
            // An own configuration: switching the shared one would change the texts of tests running in parallel
            var core = new CoreLanguageImpl();
            using var config = LanguageConfigBuilder.ForBasename("greenshot")
                .AddSearchPath(LanguageDirectory)
                .WithBaseLanguage("en-US")
                .ResolveLanguages()
                .RegisterSection<ICoreLanguage>(core)
                .Build();
            Assert.Equal("Cancel", core.Cancel);
            Assert.Equal("Alpha", core.ColorpickerAlpha);
            var changed = new List<string>();
            ((INotifyPropertyChanged)core).PropertyChanged += (s, e) => changed.Add(e.PropertyName);

            config.SetLanguage("de");

            Assert.Equal("de-DE", config.CurrentLanguage);
            Assert.Equal("Abbrechen", core.Cancel);
            Assert.Contains(nameof(ICoreLanguage.Cancel), changed);
            // Same text in both languages: no notification
            Assert.DoesNotContain(nameof(ICoreLanguage.ColorpickerAlpha), changed);
        }

        [Fact]
        public void LanguagePacks_HaveOnlyKnownSections()
        {
            var known = new HashSet<string>(LanguageSections().Select(o => ((Type)o[0]).GetCustomAttribute<IniLanguageSectionAttribute>().SectionName))
            {
                "__language__"
            };
            var files = Directory.GetFiles(LanguageDirectory, "greenshot.*.ini");
            Assert.True(files.Length > 100, $"Only {files.Length} language packs in {LanguageDirectory}");
            foreach (string file in files)
            {
                string section = null;
                foreach (string line in File.ReadAllLines(file))
                {
                    if (line.StartsWith("[", StringComparison.Ordinal))
                    {
                        section = line.Trim('[', ']');
                        Assert.True(known.Contains(section), $"{Path.GetFileName(file)}: unknown section [{section}]");
                    }
                    else if (line.Length > 0 && !line.StartsWith(";", StringComparison.Ordinal) && !line.StartsWith("#", StringComparison.Ordinal))
                    {
                        Assert.True(section != null, $"{Path.GetFileName(file)}: '{line}' is outside a section");
                        Assert.True(line.IndexOf('=') > 0, $"{Path.GetFileName(file)}: '{line}' is not key=value");
                    }
                }
            }
        }

        [Fact]
        public void RuntimeKeys_AreFound()
        {
            Assert.Equal("Automatically", Texts.Config.GetTranslation("WindowCaptureMode.Auto"));
            Assert.Equal("Windows Bitmap", Texts.Translate(Greenshot.Base.Core.Enums.ClipboardFormat.BITMAP));
            Assert.Equal("Border", Greenshot.Base.Recipes.RecipeText.Translate("Recipe.extension_border"));
            Assert.Equal("Recipe Import", Texts.Recipe.Import);
            // A plain word of a recipe file is not a key
            Assert.Equal("Title", Greenshot.Base.Recipes.RecipeText.Translate("Title"));
        }

        private static string FindSourceDirectory()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Greenshot.sln")))
                {
                    return directory.FullName;
                }

                string candidate = Path.Combine(directory.FullName, "src");
                if (File.Exists(Path.Combine(candidate, "Greenshot.sln")))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not find the source directory from " + AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}
