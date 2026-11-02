using System;
using System.Collections.Generic;
using System.Windows.Input;
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;
using Greenshot.Base.Recipes.Triggers;
using Greenshot.Base.Wpf;

namespace Greenshot.Plugin.RecipeEditor.ViewModels
{
    /// <summary>
    /// ViewModel representing a single trigger attached to a recipe (Hotkey, ContextMenu, Clipboard, Manual).
    /// </summary>
    public class TriggerItemViewModel : ViewModelBase
    {
        public TriggerConfig Config { get; }
        private readonly Action _onChanged;
        private readonly Func<CaptureRecipe> _getRecipe;

        public string TriggerType
        {
            get => Config.TriggerType;
            set
            {
                if (Config.TriggerType != value)
                {
                    Config.TriggerType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsHotkey));
                    OnPropertyChanged(nameof(IsHotkeyReference));
                    OnPropertyChanged(nameof(IsFixedHotkey));
                    OnPropertyChanged(nameof(IsContextMenu));
                    OnPropertyChanged(nameof(IsEditor));
                    OnPropertyChanged(nameof(IsClipboard));
                    OnPropertyChanged(nameof(IsManual));
                    OnPropertyChanged(nameof(IsCommandline));
                    OnPropertyChanged(nameof(IsOpenFile));
                    OnPropertyChanged(nameof(IsExtension));
                    OnPropertyChanged(nameof(IsAiTool));
                    OnPropertyChanged(nameof(DisplayTitle));
                    _onChanged?.Invoke();
                }
            }
        }

        public string Name
        {
            get => Config.Name ?? TriggerType;
            set
            {
                if (Config.Name != value)
                {
                    Config.Name = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayTitle));
                    _onChanged?.Invoke();
                }
            }
        }

        public bool Enabled
        {
            get => Config.Enabled;
            set
            {
                if (Config.Enabled != value)
                {
                    Config.Enabled = value;
                    OnPropertyChanged();
                    _onChanged?.Invoke();
                }
            }
        }

        public string Hotkey
        {
            get => GetParam("Hotkey", "Ctrl + Alt + R");
            set
            {
                SetParam("Hotkey", value);
                OnPropertyChanged(nameof(DisplayTitle));
                OnPropertyChanged(nameof(IsHotkeyReference));
                OnPropertyChanged(nameof(IsFixedHotkey));
                OnPropertyChanged(nameof(HotkeySourceText));
            }
        }

        /// <summary>
        /// The hotkey comes from an option ("${option.key}") or the general settings ("${config.Name}"):
        /// the hotkey window can't show it, recording a hotkey would replace the reference
        /// </summary>
        public bool IsHotkeyReference => IsHotkey && (RecipeOptionStore.TryParseReference(Hotkey, out _) || TriggerConfig.TryParseConfigReference(Hotkey, out _));

        public bool IsFixedHotkey => IsHotkey && !IsHotkeyReference;

        /// <summary>
        /// The hotkey which is used now and where it comes from
        /// </summary>
        public string HotkeySourceText
        {
            get
            {
                if (!IsHotkeyReference) return null;
                string current = ResolvedHotkey;
                if (string.IsNullOrEmpty(current)) current = "None";
                if (RecipeOptionStore.TryParseReference(Hotkey, out var optionKey))
                {
                    string label = _getRecipe?.Invoke()?.FindOption(optionKey)?.DisplayLabel ?? optionKey;
                    return string.Format(Texts.Get<IRecipeEditorLanguage>().TriggerHotkeyFromOption, current, label);
                }
                return string.Format(Texts.Get<IRecipeEditorLanguage>().TriggerHotkeyFromSettings, current);
            }
        }

        private string ResolvedHotkey
        {
            get
            {
                var recipe = _getRecipe?.Invoke();
                return recipe != null ? Config.GetHotkey(recipe) : string.Empty;
            }
        }

        /// <summary>
        /// Replaces the reference with the hotkey used now, which can then be changed in the hotkey window
        /// </summary>
        public ICommand UseFixedHotkeyCommand { get; }

        public string MenuItemText
        {
            get => GetParam("MenuItemText", Config.Name ?? Texts.Get<IRecipeEditorLanguage>().TriggerDefaultMenuItemText);
            set { SetParam("MenuItemText", value); OnPropertyChanged(nameof(DisplayTitle)); }
        }

        public string Group
        {
            get => GetParam("Group", "Recipes");
            set => SetParam("Group", value);
        }

        public int Order
        {
            get => Config.GetParameter<int>("Order", 0);
            set
            {
                if (Config.GetParameter<int>("Order", 0) != value)
                {
                    SetParam("Order", value);
                }
            }
        }

        public string FormatFilter
        {
            get => GetParam("FormatFilter", "");
            set => SetParam("FormatFilter", value);
        }

        public string Command
        {
            get => GetParam("Command", Config.Name ?? "custom");
            set { SetParam("Command", value); OnPropertyChanged(nameof(DisplayTitle)); }
        }

        public string Description
        {
            get => GetParam("Description", "");
            set => SetParam("Description", value);
        }

        public string Filter
        {
            get => GetParam("Filter", "");
            set { SetParam("Filter", value); OnPropertyChanged(nameof(DisplayTitle)); }
        }

        public bool FireAndForget
        {
            get
            {
                if (Config.Parameters != null && Config.Parameters.TryGetValue("FireAndForget", out var val))
                {
                    if (val is bool b) return b;
                    if (bool.TryParse(val?.ToString(), out var parsed)) return parsed;
                }
                return false;
            }
            set
            {
                SetParam("FireAndForget", value);
            }
        }

        public bool IsHotkey => string.Equals(TriggerType, TriggerConfig.TypeHotkey, StringComparison.OrdinalIgnoreCase);
        public bool IsContextMenu => string.Equals(TriggerType, TriggerConfig.TypeContextMenu, StringComparison.OrdinalIgnoreCase);
        public bool IsEditor => string.Equals(TriggerType, TriggerConfig.TypeEditor, StringComparison.OrdinalIgnoreCase);
        public bool IsClipboard => string.Equals(TriggerType, TriggerConfig.TypeClipboard, StringComparison.OrdinalIgnoreCase);
        public bool IsManual => string.Equals(TriggerType, TriggerConfig.TypeManual, StringComparison.OrdinalIgnoreCase);
        public bool IsCommandline => string.Equals(TriggerType, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase);
        public bool IsOpenFile => string.Equals(TriggerType, TriggerConfig.TypeOpenFile, StringComparison.OrdinalIgnoreCase);
        public bool IsExtension => string.Equals(TriggerType, TriggerConfig.TypeExtension, StringComparison.OrdinalIgnoreCase);
        public bool IsAiTool => string.Equals(TriggerType, TriggerConfig.TypeAiTool, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// AI tool trigger: the tool name the AI uses
        /// </summary>
        public string ToolName
        {
            get => GetParam("ToolName", "");
            set { SetParam("ToolName", value); OnPropertyChanged(nameof(DisplayTitle)); }
        }

        /// <summary>
        /// AI tool trigger: the title of the tool for people
        /// </summary>
        public string ToolTitle
        {
            get => GetParam("Title", "");
            set => SetParam("Title", value);
        }

        /// <summary>
        /// AI tool trigger: the tool doesn't change anything
        /// </summary>
        public bool ReadOnly
        {
            get => Config.GetParameter("ReadOnly", true);
            set => SetParam("ReadOnly", value);
        }

        public string Browser
        {
            get => Config.GetParameter<string>("Browser");
            set
            {
                if (Config.GetParameter<string>("Browser") != value)
                {
                    Config.SetParameter("Browser", value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayTitle));
                    _onChanged?.Invoke();
                }
            }
        }

        public string DisplayTitle
        {
            get
            {
                if (IsHotkey) return "⌨ " + string.Format(Texts.Get<IRecipeEditorLanguage>().TriggerTitleHotkey, IsHotkeyReference ? ResolvedHotkey : Hotkey);
                if (IsContextMenu) return "📋 " + string.Format(Texts.Get<IRecipeEditorLanguage>().TriggerTitleContextMenu, MenuItemText);
                if (IsEditor) return "🎨 " + string.Format(Texts.Get<IRecipeEditorLanguage>().TriggerTitleEditor, MenuItemText);
                if (IsClipboard) return string.IsNullOrEmpty(FormatFilter) ? "📋 " + Texts.Get<IRecipeEditorLanguage>().TriggerClipboardMonitor : "📋 " + string.Format(Texts.Get<IRecipeEditorLanguage>().TriggerTitleClipboard, FormatFilter);
                if (IsCommandline) return "💻 " + string.Format(Texts.Get<IRecipeEditorLanguage>().TriggerTitleCli, Command);
                if (IsOpenFile) return string.IsNullOrEmpty(Filter) ? "📂 " + Texts.Get<IRecipeEditorLanguage>().TriggerTitleOpenFileAll : "📂 " + string.Format(Texts.Get<IRecipeEditorLanguage>().TriggerTitleOpenFile, Filter);
                if (IsExtension) return string.IsNullOrEmpty(Browser) ? "🌐 " + Texts.Get<IRecipeEditorLanguage>().TriggerTitleBrowserAll : "🌐 " + string.Format(Texts.Get<IRecipeEditorLanguage>().TriggerTitleBrowser, Browser);
                if (IsAiTool) return "🤖 " + string.Format(Texts.Get<IRecipeEditorLanguage>().TriggerTitleAiTool, ToolName);
                return "⚡ " + string.Format(Texts.Get<IRecipeEditorLanguage>().TriggerTitleOther, TriggerType);
            }
        }

        public ICommand RemoveCommand { get; }

        public TriggerItemViewModel(TriggerConfig config, Action onChanged = null, Action<TriggerItemViewModel> onRemove = null, Func<CaptureRecipe> getRecipe = null)
        {
            Config = config ?? new TriggerConfig(TriggerConfig.TypeHotkey);
            _onChanged = onChanged;
            _getRecipe = getRecipe;
            RemoveCommand = new RelayCommand(() => onRemove?.Invoke(this));
            UseFixedHotkeyCommand = new RelayCommand(() =>
            {
                string current = ResolvedHotkey;
                Hotkey = string.IsNullOrEmpty(current) ? "Ctrl + Alt + R" : current;
            });
        }

        private string GetParam(string key, string fallback = "")
        {
            if (Config.Parameters != null)
            {
                foreach (var kvp in Config.Parameters)
                {
                    if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
                    {
                        return kvp.Value.ToString();
                    }
                }
            }
            return fallback;
        }

        private void SetParam(string key, object value)
        {
            if (Config.Parameters == null)
            {
                Config.Parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }
            Config.Parameters[key] = value;
            OnPropertyChanged(key);
            _onChanged?.Invoke();
        }
    }
}
