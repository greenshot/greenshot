# WPF Settings Window Implementation

This document describes the WPF-based settings window that replaces the legacy Windows Forms implementation.

## Overview

The WPF settings window provides a modern, theme-aware user interface for managing Greenshot settings. It includes:

- **Modern UI**: Clean, organized interface with proper spacing and grouping
- **Dark/Light Theme Support**: Automatically detects and follows Windows system theme
- **Translation Support**: All text updates dynamically when language changes
- **INI Binding**: Two-way binding to configuration properties with change notifications
- **Expert Mode**: Settings can be marked as expert-only and hidden from regular users
- **Grouped Settings**: Organized into logical groups matching the original form

## Architecture

### WPF Infrastructure (`Greenshot.Base/Wpf/`)

#### TranslateExtension.cs
XAML markup extension that provides translation binding:
```xaml
<Label Content="{wpf:Translate settings_language}"/>
```

#### TranslationData.cs
Implements `INotifyPropertyChanged` to update translations when language changes. Subscribes to `Language.LanguageChanged` event.

#### ThemeManager.cs
Singleton that:
- Detects Windows light/dark theme from registry
- Provides theme-aware brushes (Background, Foreground, Border, GroupBox)
- Listens for Windows theme changes via `SystemEvents.UserPreferenceChanged`
- Updates UI automatically when theme changes

#### IniValueConverter.cs
Converters for INI values:
- `ExpertVisibilityConverter`: Shows/hides controls based on expert mode
- `FixedToEnabledConverter`: Disables controls when INI value is fixed
- `InverseBooleanConverter`: Inverts boolean values for binding

### Settings Window (`Greenshot/Forms/Wpf/`)

#### SettingsViewModel.cs
ViewModel that:
- Exposes `CoreConfiguration` for binding
- Manages `ExpertModeEnabled` property
- Implements `INotifyPropertyChanged` for UI updates

#### SettingsWindow.xaml
XAML window with:
- TabControl for organizing settings into categories
- Theme-aware styles
- Translation bindings for all text
- Two-way binding to configuration properties

#### SettingsWindow.xaml.cs
Code-behind that:
- Initializes ViewModel
- Applies theme resources
- Handles OK/Cancel buttons
- Saves configuration on OK

## Usage

The WPF settings window is opened from MainForm:

```csharp
_settingsWindow = new SettingsWindow();
if (_settingsWindow.ShowDialog() == true)
{
    InitializeQuickSettingsMenu();
}
```

## INI Configuration Binding

Settings are bound directly to the CoreConfiguration properties:

```xaml
<CheckBox Content="{wpf:Translate settings_playsound}"
          IsChecked="{Binding CoreConfiguration.PlayCameraSound, Mode=TwoWay}"/>
```

The binding supports:
- **OnPropertyChanged**: Updates UI when configuration changes
- **Fixed values**: Controls are disabled when configuration is marked as fixed
- **Expert values**: Controls are hidden when not in expert mode
- **Visibility**: Respects the visibility attribute of INI values

## Theme Support

Themes are applied through resource dictionaries:

```csharp
Resources.MergedDictionaries.Add(ThemeManager.Instance.GetThemeResources());
```

Theme changes are detected automatically and the UI updates in real-time.

## Translation Support

Translations update automatically when language changes through the `TranslationData` class:

1. XAML uses `{wpf:Translate key}` markup extension
2. Extension creates `TranslationData` instance for the key
3. `TranslationData` subscribes to `Language.LanguageChanged`
4. When language changes, `PropertyChanged` is raised
5. WPF binding updates the UI text

## Extending the Settings Window

### Adding a New Setting

1. Ensure the property exists in `CoreConfiguration`
2. Add the control to the appropriate tab in `SettingsWindow.xaml`:
```xaml
<CheckBox Content="{wpf:Translate your_setting_key}"
          IsChecked="{Binding CoreConfiguration.YourSetting, Mode=TwoWay}"/>
```

### Adding a New Tab

1. Add a new `TabItem` to the `TabControl`:
```xaml
<TabItem Header="{wpf:Translate your_tab_title}">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel>
            <!-- Your settings groups here -->
        </StackPanel>
    </ScrollViewer>
</TabItem>
```

### Adding Expert-Only Settings

Use the `ExpertVisibilityConverter`:
```xaml
<CheckBox Visibility="{Binding ExpertModeEnabled, Converter={StaticResource ExpertVisibilityConverter}}"
          Content="{wpf:Translate expert_setting}"
          IsChecked="{Binding CoreConfiguration.ExpertSetting, Mode=TwoWay}"/>
```

## Migration Notes

The WPF settings window is designed to coexist with the original Windows Forms `SettingsForm`. This allows for:
- Gradual migration
- Testing and validation
- Fallback if issues are discovered

To switch back to Windows Forms, simply change `MainForm.ShowSetting()` to use `SettingsForm` instead of `SettingsWindow`.

## Requirements

- .NET Framework 4.7.2
- Windows 10 or later (for theme detection)
- WPF enabled in project (already configured in `Directory.Build.props`)

## Known Limitations

- Hotkey controls need custom WPF implementation (currently using TextBox placeholders)
- Destinations list view needs proper ListView binding
- Some complex controls from WinForms need WPF equivalents
- Theme detection requires Windows 10+ registry keys

## Future Enhancements

- [ ] Custom hotkey input control
- [ ] Destinations list with drag-and-drop reordering
- [ ] Plugin settings integration
- [ ] Printer settings page
- [ ] Search/filter functionality
- [ ] Settings import/export
- [ ] Validation with visual feedback
