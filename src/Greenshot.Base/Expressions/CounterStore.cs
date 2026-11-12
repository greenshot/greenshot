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
using System.Globalization;
using System.IO;
using System.Text;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// The stored counts of ${counter.scope(key)}: one line per key in greenshot-counters.txt next to greenshot.ini
    /// ("key TAB last number TAB last use (UTC ticks)"). Keys unused for 90 days are removed when the file is written.
    /// </summary>
    internal sealed class CounterStore
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CounterStore));
        internal static readonly TimeSpan KeepUnused = TimeSpan.FromDays(90);
        private const string FileName = "greenshot-counters.txt";
        private readonly object _lock = new object();
        private readonly Func<string> _pathProvider;

        public CounterStore(Func<string> pathProvider = null)
        {
            _pathProvider = pathProvider ?? DefaultPath;
        }

        private static string DefaultPath()
        {
            string iniDirectory = Path.GetDirectoryName(GreenshotEnvironment.ConfigLocation);
            if (string.IsNullOrEmpty(iniDirectory))
            {
                iniDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Greenshot");
            }
            return Path.Combine(iniDirectory, FileName);
        }

        /// <summary>A key is one line: tabs and line breaks become spaces</summary>
        public static string NormalizeKey(string key) =>
            (key ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim();

        /// <summary>The last number given out for the key, 0 when none</summary>
        public long Peek(string key)
        {
            key = NormalizeKey(key);
            lock (_lock)
            {
                return Read().TryGetValue(key, out var entry) ? entry.Value : 0;
            }
        }

        /// <summary>Store that <paramref name="value"/> was used for the key (only ever goes up)</summary>
        public void Commit(string key, long value)
        {
            key = NormalizeKey(key);
            lock (_lock)
            {
                var entries = Read();
                long current = entries.TryGetValue(key, out var entry) ? entry.Value : 0;
                entries[key] = (Math.Max(current, value), DateTime.UtcNow);
                Write(entries);
            }
        }

        private Dictionary<string, (long Value, DateTime LastUse)> Read()
        {
            var entries = new Dictionary<string, (long, DateTime)>(StringComparer.Ordinal);
            string path = _pathProvider();
            try
            {
                if (!File.Exists(path)) return entries;
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    var parts = line.Split('\t');
                    if (parts.Length < 3) continue;
                    if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long value)) continue;
                    if (!long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks)) continue;
                    entries[parts[0]] = (value, new DateTime(Math.Max(0, Math.Min(ticks, DateTime.MaxValue.Ticks)), DateTimeKind.Utc));
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Log.WarnFormat("Couldn't read the counters from {0}: {1}", path, ex.Message);
            }
            return entries;
        }

        private void Write(Dictionary<string, (long Value, DateTime LastUse)> entries)
        {
            string path = _pathProvider();
            var cutoff = DateTime.UtcNow - KeepUnused;
            var sb = new StringBuilder();
            foreach (var pair in entries)
            {
                if (pair.Value.LastUse < cutoff) continue;
                sb.Append(pair.Key).Append('\t')
                    .Append(pair.Value.Value.ToString(CultureInfo.InvariantCulture)).Append('\t')
                    .Append(pair.Value.LastUse.Ticks.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
            }
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                // Written to a temporary file first, so a crash never leaves half a file
                string temp = path + ".tmp";
                File.WriteAllText(temp, sb.ToString(), new UTF8Encoding(false));
                if (File.Exists(path))
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Log.WarnFormat("Couldn't write the counters to {0}: {1}", path, ex.Message);
            }
        }
    }
}
