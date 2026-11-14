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
using System.Linq;
using Dapplo.Ini;
using Dapplo.Ini.Interfaces;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Contracts;
using Greenshot.Helpers;
using Greenshot.Recipes;
using log4net;

namespace Greenshot.Settings
{
    /// <summary>
    /// The quick settings: the settings which are changed often, stored right away. The tray menu shows them as a sub menu,
    /// the quick settings window (OpenWindow step, Window QuickSettings) as check boxes and lists. A value fixed in
    /// greenshot-fixed.ini is left out.
    /// </summary>
    public static class QuickSettings
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(QuickSettings));

        /// <summary>
        /// At most this many recipe options in the "Automatic steps" group, the others are in Settings > Recipes ("More…")
        /// </summary>
        public const int MaxRecipeOptions = 6;

        /// <summary>
        /// The groups of the quick settings, read now: build them again to show the current values of a new menu or window
        /// </summary>
        public static IReadOnlyList<QuickSettingGroup> Create()
        {
            var conf = IniConfigRegistry.GetSection<ICoreConfiguration>();
            var groups = new List<QuickSettingGroup>();
            if (conf == null)
            {
                return groups;
            }

            if (!conf.IsConstant("CaptureMousepointer"))
            {
                groups.Add(new QuickSettingGroup(null, new[]
                {
                    QuickSetting.Switch(Texts.Settings.CaptureMousepointer, () => conf.CaptureMousepointer, value => conf.CaptureMousepointer = value)
                }));
            }

            if (!conf.IsConstant("Destinations"))
            {
                var destinations = DestinationHelper.GetAllDestinations()
                    .Select(destination => QuickSetting.Switch(destination.Descriptor?.DisplayName ?? destination.Designation,
                        () => conf.OutputDestinations.Contains(destination.Designation),
                        isChecked => DestinationChanged(conf, destination, isChecked)))
                    .ToList();
                AddGroup(groups, Texts.Settings.Destination, destinations);
            }

            var printOptions = new List<QuickSetting>();
            AddSwitch(printOptions, conf, "OutputPrintPromptOptions", Texts.Settings.Alwaysshowprintoptionsdialog, () => conf.OutputPrintPromptOptions, v => conf.OutputPrintPromptOptions = v);
            AddSwitch(printOptions, conf, "OutputPrintAllowRotate", Texts.Core.PrintoptionsAllowrotate, () => conf.OutputPrintAllowRotate, v => conf.OutputPrintAllowRotate = v);
            AddSwitch(printOptions, conf, "OutputPrintAllowEnlarge", Texts.Core.PrintoptionsAllowenlarge, () => conf.OutputPrintAllowEnlarge, v => conf.OutputPrintAllowEnlarge = v);
            AddSwitch(printOptions, conf, "OutputPrintAllowShrink", Texts.Core.PrintoptionsAllowshrink, () => conf.OutputPrintAllowShrink, v => conf.OutputPrintAllowShrink = v);
            AddSwitch(printOptions, conf, "OutputPrintCenter", Texts.Core.PrintoptionsAllowcenter, () => conf.OutputPrintCenter, v => conf.OutputPrintCenter = v);
            AddSwitch(printOptions, conf, "OutputPrintInverted", Texts.Core.PrintoptionsInverted, () => conf.OutputPrintInverted, v => conf.OutputPrintInverted = v);
            AddSwitch(printOptions, conf, "OutputPrintGrayscale", Texts.Core.PrintoptionsPrintgrayscale, () => conf.OutputPrintGrayscale, v => conf.OutputPrintGrayscale = v);
            AddSwitch(printOptions, conf, "OutputPrintMonochrome", Texts.Core.PrintoptionsPrintmonochrome, () => conf.OutputPrintMonochrome, v => conf.OutputPrintMonochrome = v);
            AddSwitch(printOptions, conf, "OutputPrintFooter", Texts.Core.PrintoptionsTimestamp, () => conf.OutputPrintFooter, v => conf.OutputPrintFooter = v);
            AddGroup(groups, Texts.Settings.Printoptions, printOptions);

            var effects = new List<QuickSetting>();
            AddSwitch(effects, conf, "PlayCameraSound", Texts.Settings.Playsound, () => conf.PlayCameraSound, v => conf.PlayCameraSound = v);
            AddSwitch(effects, conf, "ShowTrayNotification", Texts.Settings.Shownotify, () => conf.ShowTrayNotification, v => conf.ShowTrayNotification = v);
            AddGroup(groups, Texts.Settings.Visualization, effects);

            var recipeOptions = CreateRecipeOptions();
            if (recipeOptions.Count > 0)
            {
                groups.Add(new QuickSettingGroup(Texts.Core.QuicksettingsAutomaticsteps, recipeOptions, isInline: true, moreSettingsTab: "recipes"));
            }

            return groups;
        }

        private static void AddSwitch(List<QuickSetting> settings, IIniSection section, string propertyName, string label, Func<bool> isOn, Action<bool> setOn)
        {
            if (!section.IsConstant(propertyName))
            {
                settings.Add(QuickSetting.Switch(label, isOn, setOn));
            }
        }

        private static void AddGroup(List<QuickSettingGroup> groups, string title, IReadOnlyList<QuickSetting> settings)
        {
            if (settings.Count > 0)
            {
                groups.Add(new QuickSettingGroup(title, settings));
            }
        }

        /// <summary>
        /// The options recipes offer in the quick settings ("quickSettings": true): a switch, or a choice with one entry per value,
        /// at most <see cref="MaxRecipeOptions"/>. The extensions (border, drop shadow, caption, ...) first, then the recipes.
        /// </summary>
        private static List<QuickSetting> CreateRecipeOptions()
        {
            List<(FlowDefinition Recipe, RecipeOption Option)> options;
            try
            {
                var manager = RecipeManager.Instance;
                options = manager.GetAllExtensions()
                    .OrderBy(e => e.Extends?.Order ?? 0).ThenBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                    .Cast<FlowDefinition>()
                    .Concat(manager.GetAllRecipes().OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase))
                    .Where(r => r?.Options != null)
                    .SelectMany(r => r.Options
                        .Where(o => o != null && o.QuickSettings && (o.Type == ContractDataType.Boolean || (o.Type == ContractDataType.Enum && o.Choices != null)))
                        .Select(o => (Recipe: r, Option: o)))
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't add the options of the recipes to the quick settings.", ex);
                return new List<QuickSetting>();
            }

            if (options.Count > MaxRecipeOptions)
            {
                Log.DebugFormat("{0} recipe options are marked for the quick settings, showing {1}.", options.Count, MaxRecipeOptions);
            }

            // The same label of two recipes gets the recipe name in front
            var duplicateLabels = new HashSet<string>(options.GroupBy(o => o.Option.DisplayLabel, StringComparer.CurrentCultureIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key), StringComparer.CurrentCultureIgnoreCase);
            var settings = new List<QuickSetting>();
            foreach (var (recipe, option) in options.Take(MaxRecipeOptions))
            {
                string label = duplicateLabels.Contains(option.DisplayLabel) ? $"{recipe.DisplayName}: {option.DisplayLabel}" : option.DisplayLabel;
                string description = string.IsNullOrEmpty(option.DisplayDescription) ? null : option.DisplayDescription;
                if (option.Type == ContractDataType.Boolean)
                {
                    settings.Add(QuickSetting.Switch(label, () => RecipeOptionStore.GetValue(recipe, option) is true, isChecked => RecipeOptionStore.SetValue(recipe.Id, option, isChecked), description));
                    continue;
                }

                var choices = option.Choices.Where(c => c != null)
                    .Select(choice => new QuickSettingChoice(choice.DisplayLabel,
                        () => string.Equals(choice.Value, RecipeOptionStore.GetValue(recipe, option) as string, StringComparison.OrdinalIgnoreCase),
                        () => RecipeOptionStore.SetValue(recipe.Id, option, choice.Value)))
                    .ToList();
                settings.Add(QuickSetting.Choice(label, choices, description));
            }
            return settings;
        }

        /// <summary>
        /// A destination was switched on or off: the destination picker and the other destinations exclude each other,
        /// with none left the picker is used
        /// </summary>
        internal static void DestinationChanged(ICoreConfiguration conf, IDestination selectedDestination, bool isChecked)
        {
            if (isChecked)
            {
                if (selectedDestination.Designation.Equals(nameof(WellKnownDestinations.Picker)))
                {
                    // If the item is the destination picker, remove all others
                    conf.OutputDestinations.Clear();
                }
                else
                {
                    // If the item is not the destination picker, remove the picker
                    conf.OutputDestinations.Remove(nameof(WellKnownDestinations.Picker));
                }

                // Checked an item, add if the destination is not yet selected
                if (!conf.OutputDestinations.Contains(selectedDestination.Designation))
                {
                    conf.OutputDestinations.Add(selectedDestination.Designation);
                }
            }
            else if (conf.OutputDestinations.Contains(selectedDestination.Designation))
            {
                // deselected a destination, only remove if it was selected
                conf.OutputDestinations.Remove(selectedDestination.Designation);
            }

            // Check if something was selected, if not make the picker the default
            if (conf.OutputDestinations.Count == 0)
            {
                conf.OutputDestinations.Add(nameof(WellKnownDestinations.Picker));
            }
        }
    }

    /// <summary>
    /// A group of the quick settings: a sub menu in the tray menu, a section in the window
    /// </summary>
    public sealed class QuickSettingGroup
    {
        public QuickSettingGroup(string title, IReadOnlyList<QuickSetting> settings, bool isInline = false, string moreSettingsTab = null)
        {
            Title = title;
            Settings = settings ?? Array.Empty<QuickSetting>();
            IsInline = isInline;
            MoreSettingsTab = moreSettingsTab;
        }

        /// <summary>
        /// The title, null for the settings without a group (shown first)
        /// </summary>
        public string Title { get; }

        public IReadOnlyList<QuickSetting> Settings { get; }

        /// <summary>
        /// In the tray menu the settings are shown below a header instead of in a sub menu
        /// </summary>
        public bool IsInline { get; }

        /// <summary>
        /// The page of the settings with the rest ("More…"), null when the group is complete
        /// </summary>
        public string MoreSettingsTab { get; }
    }

    /// <summary>
    /// One quick setting: a switch, or a choice of one of several values
    /// </summary>
    public sealed class QuickSetting
    {
        private readonly Func<bool> _isOn;
        private readonly Action<bool> _setOn;

        private QuickSetting(string label, string description, Func<bool> isOn, Action<bool> setOn, IReadOnlyList<QuickSettingChoice> choices)
        {
            Label = label ?? string.Empty;
            Description = description;
            _isOn = isOn;
            _setOn = setOn;
            Choices = choices;
        }

        public static QuickSetting Switch(string label, Func<bool> isOn, Action<bool> setOn, string description = null) =>
            new QuickSetting(label, description, isOn ?? throw new ArgumentNullException(nameof(isOn)), setOn ?? throw new ArgumentNullException(nameof(setOn)), null);

        public static QuickSetting Choice(string label, IReadOnlyList<QuickSettingChoice> choices, string description = null) =>
            new QuickSetting(label, description, null, null, choices ?? throw new ArgumentNullException(nameof(choices)));

        public string Label { get; }

        /// <summary>
        /// Shown as tool tip, null when there is none
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The values of a choice, null for a switch
        /// </summary>
        public IReadOnlyList<QuickSettingChoice> Choices { get; }

        public bool IsChoice => Choices != null;

        /// <summary>
        /// The current value of a switch
        /// </summary>
        public bool IsOn => _isOn?.Invoke() == true;

        /// <summary>
        /// Switch on or off, stored right away
        /// </summary>
        public void SetOn(bool value) => _setOn?.Invoke(value);
    }

    /// <summary>
    /// A value of a <see cref="QuickSetting.Choice"/>
    /// </summary>
    public sealed class QuickSettingChoice
    {
        private readonly Func<bool> _isSelected;
        private readonly Action _select;

        public QuickSettingChoice(string label, Func<bool> isSelected, Action select)
        {
            Label = label ?? string.Empty;
            _isSelected = isSelected ?? throw new ArgumentNullException(nameof(isSelected));
            _select = select ?? throw new ArgumentNullException(nameof(select));
        }

        public string Label { get; }

        public bool IsSelected => _isSelected();

        /// <summary>
        /// Make this the value, stored right away
        /// </summary>
        public void Select() => _select();

        public override string ToString() => Label;
    }
}
