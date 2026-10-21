# Building UI in Greenshot

How to add a window, a dialog or a settings page to Greenshot 1.4: where the files go, how texts and settings are bound,
and which shared pieces to reuse so every window looks and behaves the same. Where code goes in general is described in
[code-structure.md](code-structure.md), translations in detail in [translation/README.md](translation/README.md).

- [Where the files go](#where-the-files-go)
- [A new window](#a-new-window)
- [Reuse the shared styles](#reuse-the-shared-styles)
- [Texts and languages](#texts-and-languages)
- [Accessibility](#accessibility)
- [Settings and greenshot.ini](#settings-and-greenshotini)
- [The settings window](#the-settings-window)
- [Settings of a plugin](#settings-of-a-plugin)
- [Images](#images)
- [Threads](#threads)
- [Checklist](#checklist)

## Where the files go

- **New UI is WPF.** WinForms stays where it is (the editor, older dialogs), but new windows, dialogs and pages are WPF.
- **`Forms` is only for WinForms.** A WPF window, page or control goes into a `Views` folder of its feature and is named
  `…Window`, `…Page` or `…View`. The class the view binds to is a `…ViewModel` in the `ViewModels` folder next to it, one class per file.
- **Shared WPF parts are in `Greenshot.Base/Wpf`**: styles, the theme, the title bar, the message box, converters, the hotkey editor,
  the destination picker. A plugin gets them through its reference to Greenshot.Base.
- **Optional parts stay out of Greenshot Light.** UI for the AI tools, the browser extension or plugin loading goes into their
  folders (`Greenshot/Ai`, `Greenshot/Ipc/BrowserExtension`, `Greenshot/Plugins`) or behind `#if !GREENSHOT_LIGHT`.

## A new window

Every Greenshot window uses the same frame: the theme brushes of `ThemeManager`, the shared styles and `ThemedTitleBar` in its first row.
The Jira upload window (`Greenshot.Plugin.Atlassian/Jira/Views/JiraUploadWindow.xaml`) is a small, complete example:

```xaml
<Window x:Class="Greenshot.Plugin.Atlassian.Jira.Views.JiraUploadWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:wpf="clr-namespace:Greenshot.Base.Wpf;assembly=Greenshot.Base"
        Title="{wpf:Text Jira.UploadMenuItem}"
        Background="{Binding Source={x:Static wpf:ThemeManager.Instance}, Path=BackgroundBrush}"
        Foreground="{Binding Source={x:Static wpf:ThemeManager.Instance}, Path=ForegroundBrush}"
        WindowStyle="None">
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/Greenshot.Base;component/Wpf/Styles/DialogStyles.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>
    <Border BorderBrush="{Binding Source={x:Static wpf:ThemeManager.Instance}, Path=BorderBrush}" BorderThickness="1">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <wpf:ThemedTitleBar Grid.Row="0"/>
            <!-- the content in Grid.Row="1" -->
        </Grid>
    </Border>
</Window>
```

- **`ThemedTitleBar`** decides the title bar: with the theme "Same as Windows" (and with high contrast) the window keeps the title bar
  of Windows, with the light or dark theme it draws its own in the theme colors. It follows theme changes while the window is open.
  Extra buttons go inside it as content, `ShowThemeToggle="True"` adds the light/dark switch.
- **`ThemeManager.Instance`** has the brushes of the current theme (`BackgroundBrush`, `ForegroundBrush`, `MutedBrush`, `BorderBrush`,
  `ControlBackgroundBrush`, `TextBoxBackgroundBrush`, `AccentBrush`, `WarningBrush`, `ErrorBrush`, ...) and raises `PropertyChanged` when the
  theme changes. Bind to them, never write a color into the XAML, or the window breaks in the dark theme.
- **`WindowFrameTheme`** colors the frame of Windows to match the theme. WPF windows get it on their own, a WinForms form calls
  `WindowFrameTheme.Attach(handle)` when its handle is created.
- **Message boxes:** use `ThemedMessageBox.Show` (or `ShowChoice` with own button texts) instead of `MessageBox.Show`, so the message box has the theme too.

## Reuse the shared styles

Don't write local styles for buttons, check boxes or lists: take the ones in `Greenshot.Base/Wpf/Styles`, so every window
looks the same and a change of the design is made in one place. Merge the dictionary you need with its pack URI
(`/Greenshot.Base;component/Wpf/Styles/<file>.xaml`); these URIs are used by plugins too, don't move or rename the files.

| File | What it gives you |
|---|---|
| `DialogStyles.xaml` | The usual start for a window: merges the shared button, check box, expander and list styles and themes `Label`, `TextBox`, `ComboBox` and the list views by default. |
| `ButtonStyles.xaml` | `PrimaryButtonStyle` (the main action, e.g. OK), `SecondaryButtonStyle` (the other actions, e.g. Cancel), `WarnButtonStyle`, `ErrorButtonStyle` (strong red, destructive), `LightErrorButtonStyle` (a lighter delete), text-only buttons `PrimaryTextButtonStyle`, `SecondaryTextButtonStyle`, `LinkTextButtonStyle`, `ToolboxButtonStyle`, `PaletteSwatchStyle`, `DefaultRadioButtonStyle`, `FilterChipRadioButtonStyle`, and the caption buttons `TitleBarButtonStyle`, `TitleCloseButtonStyle`. |
| `CheckBoxStyles.xaml` | `GreenshotCheckBoxStyle`, also the default style of every `CheckBox` in a window which merges it. |
| `ExpanderStyles.xaml` | `ExpanderStyle`: a header with an arrow which turns when the content is shown (settings groups, error details). |
| `ListViewStyles.xaml` | `ThemeListView`, `ThemeListViewItem`, `ThemeGridViewColumnHeader`. |
| `PillStyles.xaml` | `PillStyle` (an entry shown as a pill), `RemoveEntryButtonStyle` (the small x in it), `AddPillButtonStyle` (the + at the end of the list). |
| `MenuStyles.xaml` | `GreenshotContextMenuStyle`, `GreenshotMenuItemStyle`, `GreenshotMenuSeparatorStyle`; `ThemedMenu` uses them for the tray menu and the other menus. |

```xaml
<Button Content="{wpf:Text Core.Ok}" Style="{StaticResource PrimaryButtonStyle}" IsDefault="True"/>
<Button Content="{wpf:Text Core.Cancel}" Style="{StaticResource SecondaryButtonStyle}" IsCancel="True"/>
```

For an error with details for whoever has to fix it, a destination or step calls `ShowDialogAsync` with an
`ErrorDetailsRequest` (title, message, note, details): Greenshot shows the message, the note and an expandable *Details*
with *Copy details*.

Other parts to reuse instead of building your own: `HotkeyEditorView` and `HotkeyDisplayView` (enter and show a hotkey),
`KeyCapBadgeView` (a key as a key cap), `DestinationPicker`, `ProgressWindow`, `QualityWindow`, `ThumbnailWindow`,
`RelayCommand`, `EnumDisplayer` (translated enum values for a combo box) and `ComboBoxHelper`.

When a style is missing, add it to the shared file it belongs to, not to your window.

## Texts and languages

Every text a user sees comes from a language pack, never from a string in the code or the XAML.

- The language packs are INI files: `src/Greenshot/Languages/greenshot.<ietf>.ini` for the core, `Languages/greenshot.<module>.<ietf>.ini`
  in a plugin. English (`en-US`) is the base, other languages fall back to it until they are translated.
- Each `[Section]` of a pack is a typed interface, loaded with Dapplo.Ini: `ICoreLanguage` `[Core]`, `IEditorLanguage` `[Editor]`,
  `ISettingsLanguage` `[Settings]`, `IRecipeLanguage` `[Recipe]`, `ISelfServiceLanguage` `[SelfService]`, `IWelcomeLanguage` `[Welcome]`,
  all in `Greenshot.Base/Languages`, and one per plugin (e.g. `IJiraLanguage`). The property is the key without `_` and `-` in PascalCase:
  `btn_open_folder` is `BtnOpenFolder`.
- `Texts` (`Greenshot.Base/Languages/Texts.cs`) is the entry point: `Texts.Settings.Title` in code, `Texts.Get<T>()` for a plugin section,
  `Texts.Translate(enumValue)` for an enum (key `Type.Value`), `Texts.Config.GetTranslation(key)` for keys only known at runtime.
- **In XAML** use `{wpf:Text Section.Property}`, e.g. `Content="{wpf:Text SelfService.BtnOpenFolder}"`. It is a one-way binding to the
  section, which raises `PropertyChanged` for the texts which changed when the language is switched, so an open window updates by itself.
- **Adding a text:** add `key=value` to the right section of the en-US pack and a `string Key { get; }` property, with the English text as
  its documentation, to the language interface. Values stay on one line. Other languages are translated later (see the
  [translation workflow](translation/TRANSLATION_WORKFLOW.md)).
- **A plugin's texts:** an interface with `[IniLanguageSection("Jira")]`; the source generator of Dapplo.Ini writes the implementation,
  and the plugin registers it in `ConfigureServices` with `Texts.Register<IJiraLanguage>(new JiraLanguageImpl())`.
- **.resx files contain only strings** (the build fails otherwise) and are not used for translations.
- `LanguagePackTests` checks that every property has an English text, that every `{wpf:Text ...}` in the XAML names an existing property,
  that translations keep the placeholders (`{0}`) of English, and that a language switch notifies the changed texts.

## Accessibility

Every window works with a screen reader, the keyboard alone, high contrast and a larger Windows text size. The rules and how to check
them are in [accessibility.md](accessibility.md); the short version for a new view:

- Font sizes as `{wpf:FontSize 12}`, never a plain number.
- Never `FocusVisualStyle="{x:Null}"`; the shared styles already show a focus frame.
- A button with only an icon or glyph gets `AutomationProperties.Name="{wpf:Text …}"`; an input gets `Label Target` or `AutomationProperties.LabeledBy`.
- List item view models override `ToString()` with the visible text.
- Status and error text gets `wpf:LiveRegion.Mode="Polite"`.

`AccessibilityXamlTests` checks the first three and that no text in the XAML is literal.

## Settings and greenshot.ini

Settings are stored in greenshot.ini with Dapplo.Ini. Each `[Section]` is an interface:

```csharp
[IniSection("Jira")]
[Description("Greenshot Jira Plugin configuration")]
public interface IJiraConfiguration : IIniSection
{
    [Description("Base url to Jira system, without anything else")]
    [DefaultValue("https://jira")]
    [Required(ErrorMessage = "Jira URL is required.")]
    string Url { get; set; }
}
```

- `[Description]` is written into greenshot.ini above the value, `[DefaultValue]` is used when the file has none.
  `[Required]` and `[Range]` (System.ComponentModel.DataAnnotations) validate the value.
- The core sections are in `Greenshot.Base/Core` (`ICoreConfiguration` `[Core]`, `IEditorConfiguration`, ...). Get one with
  `IniConfigRegistry.GetSection<ICoreConfiguration>()`; the sections implement `INotifyPropertyChanged`, so a view can bind to them directly.
- greenshot.ini is in `%APPDATA%\Greenshot` (the portable Greenshot keeps it next to Greenshot.exe). An administrator can add
  `greenshot-defaults.ini` (other defaults) and `greenshot-fixed.ini` (values the user can't change) next to Greenshot.exe.
  A fixed value must show as disabled in the UI, see [The settings window](#the-settings-window).
- `IniConfigRegistry.Get()?.Save()` writes all sections.
- **Don't add a setting to the UI for every small option.** A value only a few people change can live in greenshot.ini only.
- A new feature which is still tested can be gated with `ICoreConfiguration.IsBetaTester` (Expert tab, *beta-test features*):
  when it is off, the feature isn't registered at all.

## The settings window

The settings window is `Greenshot/Settings/Views/SettingsWindow.xaml` with `SettingsViewModel` (split into partial files per page, e.g.
`SettingsViewModel.Notifications.cs`). The view model exposes `CoreConfiguration`, the controls bind to it:

```xaml
<CheckBox Content="{wpf:Text Settings.CaptureMousepointer}"
          IsChecked="{Binding CoreConfiguration.CaptureMousepointer, Mode=TwoWay}"/>

<!-- disabled when greenshot-fixed.ini sets the value -->
<ComboBox IsEnabled="{Binding CoreConfiguration, Converter={StaticResource FixedToEnabledConverter}, ConverterParameter=Language}"/>
```

- `FixedToEnabledConverter` (in `IniValueConverter.cs`) disables a control whose value is fixed; the parameter is the property name.
- The expert settings on the Expert tab are only enabled with `ExpertModeEnabled` (the check box above them). `ExpertVisibilityConverter` hides a single control
  outside the expert mode, `InverseBooleanConverter` inverts a boolean.
- OK saves (`SaveSettings` copies lists like the destinations into the configuration, then saves all sections), Cancel closes without saving.
- Adding a setting: the property in the configuration interface, the text in `[Settings]` and `ISettingsLanguage`, the control on the right tab,
  with `FixedToEnabledConverter` when it can be fixed.

## Settings of a plugin

A plugin with settings implements `IConfigurablePlugin` and registers its view for its view model in `ConfigureServices`:

```csharp
public void ConfigureServices(IPluginServices services)
{
    Texts.Register<IJiraLanguage>(new JiraLanguageImpl());
    var section = new JiraConfigurationImpl();
    services.AddConfiguration(section);
    services.AddSettingsView<IJiraConfiguration>(config => new JiraConfigurationView(config));
}
```

The settings window shows the view (a `UserControl`, e.g. `JiraConfigurationView`) on the plugins page, bound to the view model
`CreateSettingsViewModel` returns, here the configuration section itself.

## Images

- WPF images are `Resource` items, referenced with pack URIs.
- WinForms images, icons and sounds are embedded files loaded with `EmbeddedResources`, never put into a .resx with the designer
  (see "Adding images" in the README).
- Icons are drawn by the Greenshot team, don't replace or redraw them in a UI change.

## Threads

- The UI runs on one thread. Background code talks to it only through `IUiDispatcher` (`Greenshot.Base/Threading`).
- No blocking waits on the UI thread (`Thread.Sleep`, `Task.Wait`, `WaitHandle.WaitOne` and `Application.DoEvents` are banned, see `src/BannedSymbols.txt`), make the work async.
- Every awaitable is observed (VSTHRD110 is an error): `await` it, or for fire-and-forget e.g. `Dispatcher.InvokeAsync(...).Task.FireAndLog("what it does")`.
- The rules and the reasons are in [async-threading-roadmap.md](async-threading-roadmap.md).

## Checklist

- [ ] WPF, in a `Views` folder, with a `…ViewModel` in `ViewModels`
- [ ] `ThemedTitleBar`, theme brushes from `ThemeManager`, no colors in the XAML
- [ ] Shared styles from `Greenshot.Base/Wpf/Styles`, no local copies
- [ ] Every text from a language pack with `{wpf:Text ...}` or `Texts`, the English text added to the en-US pack and the interface
- [ ] Accessible: keyboard, names, live regions, `{wpf:FontSize}`, checked with Narrator and high contrast (see [accessibility.md](accessibility.md))
- [ ] Settings in a Dapplo.Ini section with description and default, fixed values disabled in the UI
- [ ] Nothing blocks the UI thread
- [ ] Checked in the light and the dark theme, and with another language
