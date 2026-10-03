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
using System.Linq;
using System.Reflection;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The edition of the running Greenshot: Full or Light (more may follow). The build writes it into Greenshot.exe as
    /// [AssemblyMetadata("GreenshotEdition", ...)], see GreenshotEdition in Directory.Build.props, so the libraries, which are
    /// the same for every edition, can show it too.
    /// </summary>
    public static class EditionInfo
    {
        /// <summary>
        /// The edition every build is unless it says otherwise
        /// </summary>
        public const string FullEdition = "Full";

        private const string MetadataKey = "GreenshotEdition";

        private static readonly Lazy<string> EditionName = new(ReadEditionName);

        /// <summary>
        /// The name of the edition, e.g. "Full" or "Light"
        /// </summary>
        public static string Name => EditionName.Value;

        /// <summary>
        /// True for the full edition
        /// </summary>
        public static bool IsFull => string.Equals(Name, FullEdition, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The product name with the edition, e.g. "Greenshot" or "Greenshot Light"
        /// </summary>
        public static string ProductName => IsFull ? "Greenshot" : $"Greenshot {Name}";

        /// <summary>
        /// The edition for the version text in logs and reports: empty for the full edition, otherwise e.g. " Light"
        /// </summary>
        public static string Suffix => IsFull ? string.Empty : $" {Name}";

        private static string ReadEditionName()
        {
            try
            {
                // Greenshot.exe, also when it isn't the entry assembly (e.g. in the tests)
                var greenshotAssembly = Assembly.GetEntryAssembly();
                if (greenshotAssembly?.GetName().Name != "Greenshot")
                {
                    greenshotAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Greenshot");
                }

                var edition = greenshotAssembly?.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(a => a.Key == MetadataKey)?.Value;
                return string.IsNullOrWhiteSpace(edition) ? FullEdition : edition;
            }
            catch (Exception)
            {
                return FullEdition;
            }
        }
    }
}
