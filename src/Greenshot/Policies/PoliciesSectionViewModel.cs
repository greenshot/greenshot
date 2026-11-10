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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Dapplo.Ini;
using Dapplo.Ini.Configuration;
using Dapplo.Ini.Interfaces;
using Dapplo.Ini.WindowsPolicies;
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;
using Greenshot.SelfService.ViewModels;
using Microsoft.Win32;

namespace Greenshot.Policies
{
    /// <summary>
    /// One setting a Windows policy sets
    /// </summary>
    public sealed class PolicyItemViewModel
    {
        public string Setting { get; set; }
        public string Value { get; set; }
        public string Source { get; set; }
    }

    /// <summary>
    /// The Self-Service section "Windows policies": every setting a policy sets, from which hive and whether it is mandatory or
    /// recommended, read from the origin Dapplo.Ini gives each value. See docs/vNext/policies.md
    /// </summary>
    public sealed class PoliciesSectionViewModel : SelfServiceSectionViewModel
    {
        public override string Id => "policies";
        public override string Title => Texts.SelfService.PoliciesTitle;
        public override string Subtitle => Texts.SelfService.PoliciesSub;
        public override string Icon => "🏢";

        public ObservableCollection<PolicyItemViewModel> Policies { get; } = new ObservableCollection<PolicyItemViewModel>();

        public bool HasPolicies => Policies.Count > 0;

        public bool HasNoPolicies => !HasPolicies;

        public PoliciesSectionViewModel()
        {
            Refresh();
        }

        public override void OnNavigatedTo() => Refresh();

        public override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            Refresh();
        }

        public override void Refresh()
        {
            Policies.Clear();
            foreach (var item in FindPolicies(IniConfigRegistry.Get().GetSections()))
            {
                Policies.Add(item);
            }
            OnPropertyChanged(nameof(HasPolicies));
            OnPropertyChanged(nameof(HasNoPolicies));
            BadgeText = HasPolicies ? string.Format(Texts.SelfService.PoliciesCount, Policies.Count) : null;
            BadgeBrush = HasPolicies ? WpfThemeHelper.Accent : null;
        }

        /// <summary>
        /// The policies as text, for support
        /// </summary>
        public string ToText()
        {
            var text = new StringBuilder();
            foreach (var item in Policies)
            {
                text.AppendLine($"{item.Setting}={item.Value} ({item.Source})");
            }
            return text.ToString();
        }

        /// <summary>
        /// True when a mandatory Windows policy sets at least one key
        /// </summary>
        internal static bool HasMandatoryPolicies(IEnumerable<IIniSection> sections) =>
            sections.Any(section => section.GetKeys().Any(key => section.GetValueOrigin(key).Source is WindowsPolicySource { Kind: PolicyKind.Mandatory }));

        /// <summary>
        /// Every key whose value comes from a Windows policy source
        /// </summary>
        internal static IEnumerable<PolicyItemViewModel> FindPolicies(IEnumerable<IIniSection> sections)
        {
            foreach (var section in sections.OrderBy(s => s.SectionName))
            {
                foreach (var key in section.GetKeys().OrderBy(k => k))
                {
                    if (section.GetValueOrigin(key).Source is not WindowsPolicySource source)
                    {
                        continue;
                    }

                    var info = section is IniSectionBase sectionBase ? PolicyMetadata.GetKey(sectionBase, key) : null;
                    string where = source.Hive == RegistryHive.LocalMachine ? Texts.SelfService.PoliciesComputer : Texts.SelfService.PoliciesUser;
                    yield return new PolicyItemViewModel
                    {
                        Setting = $"[{section.SectionName}] {key}",
                        // Secrets are readable for every user in the registry, still not shown or copied here
                        Value = info?.IsSecret == true ? "••••" : section.GetRawValue(key),
                        Source = string.Format(source.Kind == PolicyKind.Mandatory ? Texts.SelfService.PoliciesMandatory : Texts.SelfService.PoliciesRecommended, where)
                    };
                }
            }
        }
    }
}
