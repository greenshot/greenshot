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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Dapplo.Ini;
using Dapplo.Ini.Interfaces;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The sections a settings window edits, as Dapplo.Ini drafts: the window binds to the drafts, the rest of Greenshot keeps the
    /// live sections until OK applies the changed keys. Cancel (or closing the window) drops them, nothing was changed or saved.
    /// One draft per live section, the same for the window and the pages of the plugins.
    /// </summary>
    public sealed class SettingsDrafts
    {
        private readonly Dictionary<IIniSection, IIniSection> _drafts = new Dictionary<IIniSection, IIniSection>(ReferenceComparer.Instance);

        /// <summary>
        /// The draft of the live section, made the first time it is asked for. A draft (or null) is returned as it is.
        /// </summary>
        public T Get<T>(T live) where T : class, IIniSection
        {
            if (live == null || live.IsDraft())
            {
                return live;
            }

            if (!_drafts.TryGetValue(live, out var draft))
            {
                draft = live.CreateDraft();
                _drafts.Add(live, draft);
            }
            return (T)draft;
        }

        /// <summary>
        /// The drafts made so far
        /// </summary>
        public IReadOnlyList<IIniSection> Drafts => _drafts.Values.ToList();

        /// <summary>
        /// A draft has a value its validation doesn't accept
        /// </summary>
        public bool HasErrors => _drafts.Values.OfType<INotifyDataErrorInfo>().Any(draft => draft.HasErrors);

        /// <summary>
        /// OK: writes the keys changed in the drafts to the live sections, all at once, and saves once.
        /// A key which changed live meanwhile (e.g. the counter of a capture) and wasn't changed here stays as it is.
        /// </summary>
        /// <returns>true when a key was written, false when the drafts had no change (nothing is written or saved then)</returns>
        public bool Apply()
        {
            // Nothing changed: no write, no save (IniConfig.Apply would save anyway)
            if (_drafts.Values.All(draft => draft.GetDraftChanges().Count == 0))
            {
                return false;
            }

            var config = IniConfigRegistry.Get();
            if (config != null && _drafts.Keys.All(live => config.GetSections().Contains(live)))
            {
                return config.Apply(_drafts.Values.ToArray()).Values.Any(keys => keys.Count > 0);
            }

            // Sections which aren't registered (e.g. in tests): applied one by one, saved by whoever owns them
            bool applied = false;
            foreach (var draft in _drafts.Values)
            {
                applied |= draft.ApplyDraft().Count > 0;
            }
            return applied;
        }

        private sealed class ReferenceComparer : IEqualityComparer<IIniSection>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(IIniSection x, IIniSection y) => ReferenceEquals(x, y);

            public int GetHashCode(IIniSection obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    /// For the settings pages of plugins: the section to bind to, the draft while the settings window is open
    /// </summary>
    public static class SettingsDraftsExtensions
    {
        /// <summary>
        /// The draft of the section when <paramref name="services"/> come from the settings window, else the section itself.
        /// Use it in IConfigurablePlugin.CreateSettingsViewModel for every section the view model binds to.
        /// </summary>
        public static T SettingsSection<T>(this IServiceProvider services, T section) where T : class, IIniSection =>
            (services?.GetService(typeof(SettingsDrafts)) as SettingsDrafts)?.Get(section) ?? section;
    }
}
