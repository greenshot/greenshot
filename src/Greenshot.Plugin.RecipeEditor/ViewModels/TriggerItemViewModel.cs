using System;
using System.Collections.Generic;
using System.Windows.Input;
using Greenshot.Base.Triggers;
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
            get => GetParam("MenuItemText", Config.Name ?? "Run Recipe");
            set { SetParam("MenuItemText", value); OnPropertyChanged(nameof(DisplayTitle)); }
        }

        public string Group
        {
            get => GetParam("Group", "Recipes");
            set => SetParam("Group", value);
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
        public bool IsContextMenu => string.Equals(TriggerType, TriggerConfig.TypeContextMenu, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(TriggerType, TriggerConfig.TypeSystray, StringComparison.OrdinalIgnoreCase);
        public bool IsEditor => string.Equals(TriggerType, TriggerConfig.TypeEditor, StringComparison.OrdinalIgnoreCase);
        public bool IsClipboard => string.Equals(TriggerType, TriggerConfig.TypeClipboard, StringComparison.OrdinalIgnoreCase);
        public bool IsManual => string.Equals(TriggerType, TriggerConfig.TypeManual, StringComparison.OrdinalIgnoreCase);
        public bool IsCommandline => string.Equals(TriggerType, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase);
        public bool IsOpenFile => string.Equals(TriggerType, TriggerConfig.TypeOpenFile, StringComparison.OrdinalIgnoreCase);
        public bool IsExtension => string.Equals(TriggerType, TriggerConfig.TypeExtension, StringComparison.OrdinalIgnoreCase);

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
                if (IsHotkey) return $"⌨ Hotkey: {Hotkey}";
                if (IsContextMenu) return $"📋 Context Menu: {MenuItemText}";
                if (IsEditor) return $"🎨 Editor Menu: {MenuItemText}";
                if (IsClipboard) return string.IsNullOrEmpty(FormatFilter) ? "📋 Clipboard Monitor" : $"📋 Clipboard: {FormatFilter}";
                if (IsCommandline) return $"💻 CLI: {Command}";
                if (IsOpenFile) return string.IsNullOrEmpty(Filter) ? "📂 Open With File (All)" : $"📂 Open With: {Filter}";
                if (IsExtension) return string.IsNullOrEmpty(Browser) ? "🌐 Browser Extension (All)" : $"🌐 Browser: {Browser}";
                return $"⚡ Trigger: {TriggerType}";
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
