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
using System.IO;
using System.Linq;
using System.Reflection;
using Dapplo.Ini.Configuration;
using Dapplo.Ini.Interfaces;
using Dapplo.Ini.WindowsPolicies;
using Dapplo.Ini.WindowsPolicies.Admx;
using Dapplo.Ini.WindowsPolicies.Export;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Policies
{
    /// <summary>
    /// The Windows policies of Greenshot: the registry location (HKLM/HKCU\SOFTWARE\Policies\Greenshot\Greenshot) and the
    /// administrative templates (ADMX/ADML) for Group Policy and Intune, generated from the configuration sections.
    /// See docs/vNext/policies.md
    /// </summary>
    internal static class PolicyTemplates
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PolicyTemplates));

        /// <summary>SOFTWARE\Policies\Greenshot\...</summary>
        public const string Vendor = "Greenshot";

        /// <summary>SOFTWARE\Policies\Greenshot\Greenshot</summary>
        public const string Product = "Greenshot";

        /// <summary>
        /// The options for the template: file greenshot.admx, en-US\greenshot.adml.
        /// The revision is raised with every version, so a newer template replaces an older one in the central store.
        /// </summary>
        public static AdmxOptions CreateOptions()
        {
            var version = typeof(PolicyTemplates).Assembly.GetName().Version ?? new Version(1, 4);
            return new AdmxOptions(Vendor, Product)
            {
                Prefix = "greenshot",
                Namespace = "Greenshot.Policies",
                SupportedOn = "Greenshot 1.4 or later",
                Revision = $"{version.Major * 100 + version.Minor}.{Math.Max(0, version.Build)}"
            };
        }

        /// <summary>
        /// Writes the templates for the given sections into the directory
        /// </summary>
        /// <param name="directory">The directory, created when missing</param>
        /// <param name="sections">The configuration sections, e.g. those of the running Greenshot</param>
        /// <returns>The number of policies in the template</returns>
        public static int Write(string directory, IEnumerable<IIniSection> sections)
        {
            Directory.CreateDirectory(directory);
            var template = AdmxGenerator.Generate(sections, CreateOptions());
            template.WriteTo(directory);
            Log.InfoFormat("Wrote the policy templates ({0} policies) to {1}", template.PolicyCount, directory);
            return template.PolicyCount;
        }

        /// <summary>
        /// A .reg file which sets the [Core] lines of greenshot-fixed.ini (as the administrator settings window builds them) as
        /// mandatory machine policies: the same restrictions, for a deployment through the registry, Group Policy preferences or Intune.
        /// </summary>
        /// <param name="fixedIniLines">Lines like <c>BlockedDestinations=Printer,Box</c> below <c>[Core]</c></param>
        public static string CreateRegFile(string fixedIniLines)
        {
            // A section of its own with only these values: the export doesn't touch the running configuration
            var section = new CoreConfigurationImpl();
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in fixedIniLines.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = line.IndexOf('=');
                if (line.StartsWith("[") || line.StartsWith(";") || separator <= 0)
                {
                    continue;
                }
                var key = line.Substring(0, separator).Trim();
                section.SetRawValue(key, line.Substring(separator + 1).Trim());
                keys.Add(key);
            }

            return PolicyRegFile.Create(new IIniSection[] { section }, Vendor, Product, new PolicyRegFileOptions
            {
                Kind = PolicyKind.Mandatory,
                Include = info => keys.Contains(info.Key)
            });
        }

        /// <summary>
        /// Writes the templates for every configuration section of this installation (Greenshot and the plugins in the Plugins
        /// folder) into the directory, for <c>Greenshot.exe --policy-templates &lt;directory&gt;</c> in the release build.
        /// Nothing is started and greenshot.ini isn't read: the sections are created with their defaults.
        /// </summary>
        /// <param name="directory">The directory for greenshot.admx and the language folders</param>
        /// <returns>True when the templates were written</returns>
        public static bool WriteForInstallation(string directory)
        {
            try
            {
                var applicationPath = AppContext.BaseDirectory;
                var assemblyFiles = Directory.GetFiles(applicationPath, "Greenshot*.dll", SearchOption.TopDirectoryOnly).ToList();
                assemblyFiles.Add(typeof(PolicyTemplates).Assembly.Location);
                var pluginsPath = Path.Combine(applicationPath, "Plugins");
                if (Directory.Exists(pluginsPath))
                {
                    assemblyFiles.AddRange(Directory.GetFiles(pluginsPath, "Greenshot.Plugin.*.dll", SearchOption.AllDirectories));
                }

                var sections = FindSections(assemblyFiles);
                Write(directory, sections);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Couldn't write the policy templates to {directory}", ex);
                return false;
            }
        }

        /// <summary>
        /// Creates every generated configuration section (a public, non-abstract IniSectionBase with a parameterless constructor)
        /// of the assemblies, one per section name: no list of sections to keep up to date.
        /// </summary>
        private static IReadOnlyList<IIniSection> FindSections(IEnumerable<string> assemblyFiles)
        {
            var sections = new Dictionary<string, IIniSection>(StringComparer.OrdinalIgnoreCase);
            foreach (var assemblyFile in assemblyFiles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                Type[] types;
                try
                {
                    types = Assembly.LoadFrom(assemblyFile).GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch (Exception ex)
                {
                    Log.WarnFormat("Skipping {0} for the policy templates: {1}", assemblyFile, ex.Message);
                    continue;
                }

                foreach (var type in types.Where(IsSectionType))
                {
                    IIniSection section;
                    try
                    {
                        section = (IIniSection)Activator.CreateInstance(type);
                    }
                    catch (Exception ex)
                    {
                        Log.WarnFormat("Skipping {0} for the policy templates: {1}", type.FullName, ex.Message);
                        continue;
                    }
                    if (!sections.ContainsKey(section.SectionName))
                    {
                        sections.Add(section.SectionName, section);
                    }
                }
            }
            return sections.Values.OrderBy(s => s.SectionName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// A generated configuration section which can be created; a type which can't be loaded (a missing dependency) isn't one
        /// </summary>
        private static bool IsSectionType(Type type)
        {
            try
            {
                return type.IsPublic && !type.IsAbstract && typeof(IniSectionBase).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
