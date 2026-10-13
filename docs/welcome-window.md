# Welcome window

The welcome window is the first thing a new user sees: a thank you for installing Greenshot, the basics with the user's own hotkeys,
a check of the setup and a tip. It's meant to make people feel good about the choice they just made.

## When it shows

- **Once per user**, at the start of Greenshot, as long as the core setting `WelcomeShown` in greenshot.ini is `False` (the default).
  Greenshot sets it to `True` when it shows the window. That includes users who update from a version before the welcome window:
  they don't have the setting yet, so they see it once too.
- **On request**: the named pipe command `FIRST_LAUNCH` (greenshot-cli) opens it again, without looking at `WelcomeShown`.
- To see it again while testing: set `WelcomeShown=False` in the `[Core]` section of greenshot.ini, or start without a greenshot.ini.
  An administrator can set `WelcomeShown=True` in greenshot-defaults.ini (or greenshot-fixed.ini) to never show it.

It doesn't touch the thank-you page on the website: that one opens on its own, independent of this window.

## What it shows

1. **Hello**: the animated Greenshot "G" (`Views/AnimatedLogo.xaml`, the same control the About window uses), the title and a thank-you line.
2. **The basics**: the hotkeys for region, window, full screen and last region, read from the user's configuration, each with what it does.
   A hotkey which isn't set is left out. Below are two lines about the tray menu and where captures go.
3. **Your setup**: only the problems which are there, each with a button to fix it, or "Everything looks good":
   - **Tray icon hidden** (Windows 11): Windows keeps a key per notification icon in `HKCU\Control Panel\NotifyIconSettings`,
     `IsPromoted=1` means it's shown next to the clock. When Greenshot has an icon there and none of its icons is promoted, the hint explains
     where to switch it on, and the button opens the taskbar settings (`ms-settings:taskbar`). The key is only read, never written:
     the choice is the user's. Windows 10 has no such key, there the check reports nothing.
   - **Print key opens the Snipping Tool** and **OneDrive takes the Print key**: the same checks and fixes as the Hotkeys section of the
     Self-Service window (`HotkeySectionViewModel`).

   The checks run again whenever the window is activated, so coming back from the Windows settings updates them.
4. **A tip**: one tip from the set, picked at random when the window opens; "Another tip" shows the next one.

## Changing the content

All texts are in the `[Welcome]` section of the language files, `src/Greenshot/Languages/greenshot.{language}.ini`.
English (`greenshot.en-US.ini`) is the base: a text missing in a translation is shown in English.
Each key has a property in `src/Greenshot.Base/Languages/IWelcomeLanguage.cs`, named after the key (`setup_tray_hidden` is `SetupTrayHidden`),
with the English text as its comment. The XAML binds them with `{wpf:Text Welcome.SetupTrayHidden}`.

- **Change a text**: edit it in greenshot.en-US.ini and update the comment in `IWelcomeLanguage`; translators pick the change up from there.
- **Add a text**: add the key to greenshot.en-US.ini and the property to `IWelcomeLanguage`. `LanguagePackTests` fails when a property has no
  English text or the XAML references one which doesn't exist.

### Tips

The tips are the keys `tip1` to `tip10`.

- **Change a tip**: edit its line, as above.
- **Add a tip**: add `tip11=...` to greenshot.en-US.ini, the property `string Tip11 { get; }` to `IWelcomeLanguage`,
  and `texts.Tip11` to the list in `WelcomeWindow.Tips()` (`src/Greenshot/Views/WelcomeWindow.xaml.cs`).
- **Remove a tip**: remove it from those three places. An empty tip, e.g. a translation which leaves one out on purpose, is skipped.

Keep tips short, one or two sentences, and only about what every edition has: Greenshot Light has no plugins, OCR or AI tools.

## Code

| What | Where |
|------|-------|
| Window | `src/Greenshot/Views/WelcomeWindow.xaml(.cs)` |
| A hotkey row | `src/Greenshot/ViewModels/WelcomeHotkeyViewModel.cs` |
| Animated "G" | `src/Greenshot/Views/AnimatedLogo.xaml(.cs)` |
| Shown at the start | `GreenshotShell` (checks `WelcomeShown`) calls `ApplicationStartupHelper.ShowWelcome` |
| Setting | `ICoreConfiguration.WelcomeShown` |
| Texts | `[Welcome]` in the language files, `IWelcomeLanguage` |
| Tests | `src/Greenshot.Tests/Forms/WelcomeWindowTests.cs` (tray icon check) |
