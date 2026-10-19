# Accessibility

Greenshot has to work for people who use a screen reader, only a keyboard, high contrast, larger text or no animations.
This page lists the rules every window, page and control follows, how to meet each one, and how to check the result.
It applies to people and to AI coding assistants alike: new UI is not done until it meets these rules.

- [The rules in short](#the-rules-in-short)
- [WPF](#wpf)
- [WinForms (the editor)](#winforms-the-editor)
- [Keyboard in the editor](#keyboard-in-the-editor)
- [The capture window](#the-capture-window)
- [Menus and lists to choose from](#menus-and-lists-to-choose-from)
- [What the tests check](#what-the-tests-check)
- [Checking by hand](#checking-by-hand)
- [Review checklist](#review-checklist)

## The rules in short

1. **Everything works with the keyboard.** Every action you can do with the mouse has a key or a focusable control, in a sensible
   Tab order. A drag (pipette, drawing, tab tear-off) needs another way, for example a text field or a menu entry.
2. **You can see where the keyboard is.** The focus frame is never switched off.
3. **Everything has a name a screen reader can say**, in the user's language: buttons, fields, list items, images that mean something.
4. **Changes are announced.** Status, errors, progress and results that appear without a focus change are a live region.
5. **Colors come from the theme**, so dark, light and high contrast all work, and text keeps a contrast of at least 4.5:1.
6. **Text follows the Windows text size** (Settings > Accessibility > Text size).
7. **Animations stop when Windows says so** (Settings > Accessibility > Visual effects > Animation effects).
8. **Every text comes from a language pack.** This includes accessible names and tooltips.

## WPF

### Keyboard and focus

- Use real controls (`Button`, `ToggleButton`, `CheckBox`, `ListBox`...). A `Border` or `Grid` with a `MouseDown` handler can't be reached
  with Tab and has no name: make it a `Button` with a template instead (see the thumbnail in `DynamicDestinationWindow.xaml`).
- Never set `FocusVisualStyle="{x:Null}"`. The shared styles in `Greenshot.Base/Wpf/Styles` use `GreenshotFocusVisual` (`FocusStyles.xaml`),
  a 2px frame in the text color of the theme. A style of your own sets
  `<Setter Property="FocusVisualStyle" Value="{StaticResource GreenshotFocusVisual}"/>` and merges `FocusStyles.xaml`.
- `IsDefault="True"` on the main button and `IsCancel="True"` on Cancel, so Enter and Esc work.
- A grid of many small items (colors, emoji) is one Tab stop: `KeyboardNavigation.TabNavigation="Once"` and
  `KeyboardNavigation.DirectionalNavigation="Contained"` on the panel, the arrow keys move inside (see `ColorPickerWindow.xaml`).
- When a custom control takes keys (like `HotkeyEditorView`), Tab still moves on and Esc still cancels.

### Names

- A button with text in it is named by that text. A button with only an icon or a glyph (✕, ▲, ➕, …) gets
  `AutomationProperties.Name="{wpf:Text Section.Key}"`. A tooltip is not a name.
- An input gets its name from its label: `<Label Content="{wpf:Text …}" Target="{Binding ElementName=WidthBox}"/>`, or for a `TextBlock`
  label `AutomationProperties.LabeledBy="{Binding ElementName=WidthLabel}"` on the input. Without one Narrator says only "edit".
- List items are read by `ToString()` of their view model unless the item container sets `AutomationProperties.Name`. Override `ToString()`
  to return the visible text, otherwise Narrator reads the class name.
- A state shown only by color (selected step, a status dot) goes into the name or `AutomationProperties.ItemStatus`.
- A control with a value says the value the user sees, with its unit. A plain `Slider` is read as a percentage of its range
  (Narrator says "32 %" next to a label "7 px"): use `wpf:ValueSlider` with `ValueFormat="{}{0:0} px"` and bind the text next to it to
  its `ValueText`, so screen and screen reader say the same. The same goes for a custom control with a value: give its automation peer
  the value pattern (see `HotkeyEditorView.CaptureBox`).
- The shared parts are named already: `ThemedTitleBar` (minimize, maximize, close, theme), `ThemedMessageBox`, `DestinationPicker`,
  the pill and title close styles. Reuse them.

### Announcements

- Bind an `AutomationProperties` text with `TargetNullValue=''`: these properties refuse null, a binding to a null value fails
  with a binding error in the log.
- Status lines, errors, search results and progress use the `LiveRegion` attached property (`Greenshot.Base/Wpf/LiveRegion.cs`):
  `wpf:LiveRegion.Mode="Polite"` (or `Assertive` for an error that blocks the user). It sets `AutomationProperties.LiveSetting` and raises
  the event a screen reader listens for whenever the text changes.
- For text that is not on screen, `LiveRegion.Notify(element, text, activityId)` raises a UI Automation notification (the window
  returns a `NotifyingWindowAutomationPeer`). It is not said in every window (not in the frozen capture window on Windows 11), so
  check it with Narrator; moving the focus to an element with the text is what a screen reader always says.
- When a page or a wizard step changes, move the focus to a hidden `TextBlock` with the text of the new content (see `FocusStep` in
  `WelcomeWindow.xaml.cs`): Narrator says only the window title and the focused button, not text that appears. Focusing a panel
  says nothing, a panel has no automation peer. Keep the main button `IsDefault`, so Enter still goes on.
- Don't announce what changes many times a second (a live log tail, a drag). Announce the result.

### Colors and contrast

- Brushes come from `ThemeManager.Instance` (`ForegroundBrush`, `MutedBrush`, `AccentBrush`, …) and the palette roles of
  `ThemeManager.Instance.CurrentPalette`: `SuccessBrush`, `InfoBrush`, `ErrorBrush`/`ErrorBackground`, `WarningBrush` with
  `WarningForegroundBrush` on it, `DangerBrush` with `DangerForegroundBrush` on it. No `#RRGGBB`, `White` or `Gray` in a view, no
  `Opacity` to make text lighter (use `MutedBrush`).
- In high contrast every role maps to a system color the user picked (`ThemePalette.CreateHighContrast`). Colors can't carry meaning
  there, so never show a state by color alone: add a word or an icon.
- A text color on a colored background is computed with `ThemePalette.ReadableForeground(background)`.
- `ThemePaletteTests` checks that every status color has a contrast of 4.5:1 or more. When you add a role, add it there.

### Text size

- Font sizes are written `FontSize="{wpf:FontSize 12}"`, in setters too. It is the size at 100 % and grows with the Windows text size
  (`ThemeManager.TextScale`, live). In code: `FontSizeExtension.Scale(12)`, or `FontSizeExtension.CreateBinding(12)` for a binding.
- Leave room: use `Auto` sizes and `MinWidth` instead of fixed widths and heights for anything with text, and check the window at 150 %.
- The size of an annotation drawn into an image is not UI text and does not scale.

### Animations

- Check `SystemParameters.ClientAreaAnimation` (and a remote session) before you animate, and show the end state at once when it is off
  (see `CaptureWindow` and `AnimatedLogo`). Nothing moves endlessly.

## WinForms (the editor)

The editor is WinForms. Until it is reworked:

- A control without visible text sets `AccessibleName` (translated) and, where it helps, `AccessibleDescription`.
- An owner-drawn control (like the tab strip) needs an `AccessibleObject` (`CreateAccessibilityInstance`) that exposes its parts.
- Shortcuts keep working when a toolbar box has the focus, and F6 moves between toolbar, canvas and tabs.
- Colors come from `SystemColors` or the theme, not fixed values.

The editor still misses some of this (the canvas needs the mouse to draw). New editor code doesn't add to that list.

## Keyboard in the editor

| Key | Where | What it does |
|---|---|---|
| F6, Shift+F6 | anywhere | Next or previous part: menu, tools, properties, destinations, canvas, tabs. In a tool bar the arrow keys move and Enter uses the button |
| Esc | tool bar, tabs | Back to the canvas |
| Enter | number field of a tool bar | Takes the value and goes back to the canvas |
| Ctrl and Alt combinations | number field or font list | Keep working (Ctrl+Tab, Ctrl+W, menu and destination shortcuts); the single keys and the editing keys of the field stay in the field |
| Ctrl+Tab, Ctrl+Page Down | anywhere | Next tab (with Shift, or Ctrl+Page Up: previous tab) |
| Ctrl+Shift+Page Up, Ctrl+Shift+Page Down | anywhere | Move the tab to the left or to the right |
| Ctrl+W | anywhere | Close the tab |
| Left, Right, Home, End | tabs | Select a tab |
| Delete | tabs | Close the selected tab |
| Apps key, Shift+F10 | tabs | Context menu: new image, move the tab left, right or to a new window, close the tab |
| Enter | canvas | With a tool chosen: draw it in the middle of the view (crop: draw the crop area, Enter again applies it). With one element selected: edit it, like a double click |
| Tab, Shift+Tab | canvas | Select the next or previous element (after the last one the focus moves on) |
| Esc | canvas | Clear the selection |
| Ctrl+arrows | canvas | Make the selection narrower, wider, lower or higher (with Shift: 10 px) |
| Tab, arrows, Enter, Esc | emoji picker | Opens after an emoji is placed, or with Enter on a selected emoji. Tab goes between the categories and the emoji, the arrow keys move, Enter takes the emoji (one with a mark opens its variations), Esc closes the picker |

## The capture window

The capture window is drawn, not made of controls, so it speaks for itself: it announces the mode, the window under the selection and
the selection size (see `Announce` in `CaptureWindow.xaml.cs`). On the frozen screen the text goes into one of two hidden elements and
the focus moves to it, because a screen reader always says what gets the focus; Narrator said neither a live region nor a
`LiveRegion.Notify` notification there. On the live screen the application below keeps the focus, and Narrator says nothing there either, so while the screen reader flag
is on the selection starts on the frozen screen (`UseLiveSelection` in `SourceAcquisitionStep.cs`, L still switches).
When you add a tool or a mode:

- Register its keys with `RegisterToolKey`, so they work and show up in the F1 key panel, and add them to
  [capture-user-guide.md](capture-user-guide.md).
- Announce what the user would otherwise only see (`Announce(...)` in `CaptureWindow.xaml.cs`).
- Keys a screen reader needs (Insert, Caps Lock and keys held with them) are never swallowed.
- Use the high contrast colors when `SystemParameters.HighContrast` is on.
- Window capture selects the window under the cursor as soon as it starts, so a screen reader says its title without a mouse move.
- While the screen reader flag of Windows is on (`User32Api.IsScreenReaderRunning()` of Dapplo.Windows), a region capture starts as
  window capture. Applications can leave that flag on, so use it only for a default the user changes with one key (Space), never to
  turn something off.

## Menus and lists to choose from

- A choice that people make often gets a fixed letter, the same whatever else is in the list, so it can be learned. Numbers or letters
  given by position change when the list changes.
- The destination picker takes the letters from `destination_picker_keys` in the `[Core]` language section (`designation:letter;…`).
  Each letter is used once in a language; a new destination gets its letter there. A letter that isn't in the name is shown after it,
  "Name (K)".
- A list of check boxes (the destinations in the settings) gives the rows `Focusable="False"` and the list
  `KeyboardNavigation.TabNavigation="Continue"`: Tab goes from check box to check box and Space switches one. A focusable row is one
  more stop which can't be switched.
- A text next to a control which only shows its value (the percentage next to a slider) is a `TextBlock`, not a read-only `TextBox`:
  a read-only box is a tab stop that can't be changed.

## What the tests check

`Greenshot.Tests/Core/AccessibilityXamlTests.cs` reads every `.xaml` file of the source tree and fails on:

| Test | Fails when |
|---|---|
| `FontSizes_FollowTheWindowsTextSize` | a `FontSize` is a number instead of `{wpf:FontSize N}` |
| `KeyboardFocus_IsNeverHidden` | `FocusVisualStyle` is set to `{x:Null}` |
| `Sliders_SayTheValueTheyShow` | a plain `Slider` is used instead of `wpf:ValueSlider` |
| `AutomationTexts_AreNeverNull` | an `AutomationProperties` text (Name, HelpText, ItemStatus, ...) is bound without `TargetNullValue=''` |
| `ButtonsWithoutText_HaveAnAccessibleName` | a `Button`, `ToggleButton` or `RepeatButton` shows no text and has no `AutomationProperties.Name` or `LabeledBy` |
| `Texts_ComeFromTheLanguagePacks` | `Text`, `Content`, `Header`, `ToolTip`, `Title` or an accessible name is literal text instead of `{wpf:Text …}` (links, "Greenshot" and glyphs are fine) |

`LanguagePackTests` checks that every `{wpf:Text …}` exists and has an English text, and `ThemePaletteTests` checks the contrast of the
palette. Don't silence a failing test: fix the view.

## Checking by hand

The tests can't hear or see. Before a UI change is done, check it once with:

- **Keyboard only:** unplug the mouse in your head. Tab through the window, use every action, close it with Esc.
- **Narrator** (Ctrl+Win+Enter): Tab through the window; every stop says what it is and what it does. Or NVDA.
- **Accessibility Insights for Windows**, FastPass: finds unnamed and unreachable elements.
- **High contrast** (Left Alt+Left Shift+Print Screen): everything is readable, nothing disappears.
- **Text size 150 %**: nothing is cut off or overlaps.
- **Animation effects off**: nothing moves.
- **Dark and light theme**, and another language than English.

## Review checklist

- [ ] Every action works with the keyboard, the Tab order follows the layout, Enter and Esc do what they should
- [ ] The focus frame is visible everywhere
- [ ] Every icon button, input and list item has a translated name
- [ ] Sliders and other value controls are read with the value and unit shown on screen
- [ ] Status and errors are live regions
- [ ] Only theme brushes and palette roles; no state shown by color alone
- [ ] `{wpf:FontSize N}` for every font size, layout survives 150 %
- [ ] Animations respect "Animation effects"
- [ ] `AccessibilityXamlTests`, `LanguagePackTests` and `ThemePaletteTests` pass
- [ ] Checked once with Narrator and high contrast
