using System;
#nullable disable

using Greenshot.Linux.Core.Languages;
using System.Windows.Input;
using System.Collections.Generic;
using Greenshot.Linux.Recipes.Triggers;

namespace Greenshot.Linux.RecipeEditor.ViewModels
{
    /// <summary>
    /// ViewModel representing a single trigger attached to a recipe (Hotkey, ContextMenu, Clipboard, Manual).
    /// </summary>
    public class TriggerItemViewModel : ViewModelBase
    {
        public TriggerConfig Config { get; }
        private readonly Action _onChanged;

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
            set { SetParam("Hotkey", value); OnPropertyChanged(nameof(DisplayTitle)); }
        }

        public string MenuItemText
        {
            get => GetParam("MenuItemText", Config.Name ?? Texts.Get("RecipeEditor", "trigger_default_menu_item_text"));
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
                if (IsHotkey) return "⌨ " + string.Format(Texts.Get("RecipeEditor", "trigger_title_hotkey"), Hotkey);
                if (IsContextMenu) return "📋 " + string.Format(Texts.Get("RecipeEditor", "trigger_title_context_menu"), MenuItemText);
                if (IsEditor) return "🎨 " + string.Format(Texts.Get("RecipeEditor", "trigger_title_editor"), MenuItemText);
                if (IsClipboard) return string.IsNullOrEmpty(FormatFilter) ? "📋 " + Texts.Get("RecipeEditor", "trigger_clipboard_monitor") : "📋 " + string.Format(Texts.Get("RecipeEditor", "trigger_title_clipboard"), FormatFilter);
                if (IsCommandline) return "💻 " + string.Format(Texts.Get("RecipeEditor", "trigger_title_cli"), Command);
                if (IsOpenFile) return string.IsNullOrEmpty(Filter) ? "📂 " + Texts.Get("RecipeEditor", "trigger_title_open_file_all") : "📂 " + string.Format(Texts.Get("RecipeEditor", "trigger_title_open_file"), Filter);
                if (IsExtension) return string.IsNullOrEmpty(Browser) ? "🌐 " + Texts.Get("RecipeEditor", "trigger_title_browser_all") : "🌐 " + string.Format(Texts.Get("RecipeEditor", "trigger_title_browser"), Browser);
                if (IsAiTool) return "🤖 " + string.Format(Texts.Get("RecipeEditor", "trigger_title_ai_tool"), ToolName);
                return "⚡ " + string.Format(Texts.Get("RecipeEditor", "trigger_title_other"), TriggerType);
            }
        }

        public ICommand RemoveCommand { get; }

        public TriggerItemViewModel(TriggerConfig config, Action onChanged = null, Action<TriggerItemViewModel> onRemove = null)
        {
            Config = config ?? new TriggerConfig(TriggerConfig.TypeHotkey);
            _onChanged = onChanged;
            RemoveCommand = new RelayCommand(() => onRemove?.Invoke(this));
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
