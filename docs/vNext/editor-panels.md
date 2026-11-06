# The panel area of the editor

The editor has an area at the right of the capture for panels. One panel is shown at a time. The first one is the
*Selection* panel: it shows what can be changed of the selected elements, or of the tool which is picked, in sections.
Plugins can add panels of their own, or sections to the Selection panel.

- [For users](#for-users)
- [For developers](#for-developers)

---

## For users

### Show and hide the panel area

| What | How |
|---|---|
| Show | *Properties* in the context menu of any element, or a double-click on an emoji. The area also opens by itself when a choice is needed: picking the emoji tool or selecting an emoji shows the Selection panel, without taking the keyboard away from the capture. |
| Hide | The **x** at the top of the area. It never closes by itself: once open, it shows what fits the selection. Closed with the **x**, it opens again the next time a choice is needed (e.g. the emoji tool). |
| Size | Drag the bar between the capture and the area. The width is remembered. The editor window can't get so small that the capture has less than 300 by 200 pixels (at 100 % scaling), plus the panel area when it's shown; a smaller window grows when the area opens. For a small window with a capture, use the [viewer](viewer.md). |
| Another panel | When there is more than one panel, the list at the top of the area chooses which one is shown. |
| Keyboard | F6 goes from the menu, the tools, the properties, the destinations and the capture to the panel area (and on to the tabs). Esc in the area goes back to the capture. |

### The Selection panel

The Selection panel shows the sections which fit what is selected: *Size and position* on top, *Style and size* below it,
then the sections of the kind of element, e.g. *Emoji*. A click on the heading of a section closes or opens it; this is remembered.
With nothing selected and no tool which needs it, the panel says so.

| Section | What it does |
|---|---|
| Size and position | For one selected element: left, top, width and height in pixels of the capture. Type a number and press Enter or leave the field. When the element keeps its aspect ratio (e.g. an emoji or an image), changing the width also changes the height and the other way round. Ctrl+Z undoes it. |
| Style and size | For every selection: *Copy style* and *Copy size* take them from one selected element, *Apply style* and *Apply size* give them to all selected elements, in one step for Ctrl+Z. The same commands are in the context menu of the elements; Ctrl+Shift+C copies the style, Ctrl+Shift+V applies it. See the rules below. |
| Emoji | Takes the rest of the height of the panel. Picks the emoji: for the emoji tool the next one to place, for selected emojis the emoji of all of them, in one step for Ctrl+Z. *Search* finds emojis by their English name, by group (e.g. "animal") or by the emoji itself; every word must fit. Enter places the first one found, Down goes to the emojis. The tabs show the groups, the first one the emojis used last. An emoji with skin tones shows them in a small list after the click. |

### Settings

In `[Editor]` of greenshot.ini:

| Setting | Meaning |
|---|---|
| `EditorPanelWidth` | The width of the panel area, in pixels at 100 % scaling (200 to 1200, default 320) |
| `SelectedEditorPanel` | The panel which is shown when the area opens (default `Greenshot.Selection`) |
| `CollapsedEditorSections` | The sections of the Selection panel which are closed, separated by `|` |

---

## For developers

### Panels and sections

| Part | What it is |
|---|---|
| Panel (`IEditorPanel`) | A view in the panel area. One is shown at a time; the user chooses it, or the editor shows it. Examples: Selection (in the editor), Stamps (plugin), later Barcode, Text (OCR). |
| Section (`IEditorSection`) | A part of the Selection panel. The panel shows all sections which apply to the selection or tool, sorted by `Order`. Examples: Size and position, Emoji, later a barcode section for the barcode element of the Zxing plugin. |
| Context (`IEditorPanelContext`) | What a panel or section sees of its editor window: the window, the surface which is shown, events when the surface, the selection, the drawing mode or the elements change, and `ShowPanel` / `FocusCanvas`. |

The contract is in `Greenshot.Base.Interfaces.Editor`. It has no WinForms types and no WPF types: the editor itself is
WinForms today, its panels are WPF, and the contract should stay usable when the editor becomes WPF, or Avalonia.

### Rules

These rules decide when the panel area shows and what the Selection panel shows. New panels and sections follow them.
*Built* is in the editor now, *planned* comes with later sections.

**When the panel area shows**

| Rule | Status |
|---|---|
| It opens by itself only when a tool needs a choice before it can be used (`ShowsPanel = true`): the emoji tool and selected emojis, later stamps. It doesn't take the keyboard from the capture. | Built |
| It never closes by itself, so it doesn't jump while the user clicks around. Closed by the user, it opens again the next time a choice is needed (such a section starts to apply again). | Built |
| Otherwise it opens only on request: *Properties* in the context menu of every element, a double-click on an element whose double-click isn't used otherwise (text keeps it for editing). Opened like this it gets the keyboard. F6 goes into an open panel area. | *Properties* built for every element, double-click built for emojis |
| Once open, it stays open and follows the selection and the tool, until the user closes it. When a choice is needed while another panel is shown (e.g. the emoji tool while Stamps is shown), it switches to Selection, and back to that panel once the choice is done (e.g. the emoji is placed and the selection is gone). Choosing a panel or closing the area in between keeps what the user chose. | Built |
| A selection of several elements never opens it by itself, except when all of them are emojis. | Built |

**What the Selection panel shows**

| Rule | Status |
|---|---|
| Sections go by property, not by kind of element: a section shows when every selected element has its properties. Rectangle and arrow: Line, no Fill (an arrow has none). Rectangle and ellipse: Line and Fill. Text and rectangle: no Text. The same rule as the properties tool bar today (`FieldAggregator`, the fields all bound elements have). | Built for Emoji, planned for the others |
| Order: Size and position on top, Style and size below it, then the sections of the elements, then the rest (`EditorSectionOrder`). One section may fill the height which is left (`FillsHeight`), e.g. the emoji. | Built |
| A value which differs between the selected elements is shown empty (not the value of one of them). A change sets it on all selected elements, as one undo step. | Planned |
| Size and position: one element shows X, Y, width and height. Several elements show X and Y of the box around them, a change moves them together; width and height are only for one element (scaling a group would also have to decide about line thickness and text). | Built for one element, planned for several |
| Nothing in common (e.g. an emoji and an arrow): only Size and position, so the group can still be moved. | Planned |
| Nothing selected: the sections of the tool which is picked (e.g. the emoji to place), otherwise the text that nothing is selected. | Built |

**Sections per element** (planned, each moves its settings out of the properties tool bar)

| Element | Sections |
|---|---|
| Rectangle, ellipse | Size and position, Line and fill |
| Line, arrow, freehand | Size and position, Line (arrow heads) |
| Text, speech bubble | Size and position, Line and fill, Text |
| Step label | Size and position, Line and fill, Number |
| Highlight, obfuscate | Size and position, Effect |
| Image | Size and position |
| Emoji | Size and position, Emoji (built) |

**Copy and apply style or size** (built)

- *Copy style* keeps all settings of the selected element (line, fill, shadow, font, alignment, arrow heads, the strength of
  blur or pixelate...), not what kind of element it is. *Apply style* sets them on all selected elements, each only the
  settings it has (a line gets no fill), as one undo step.
- *Copy size* keeps width and height. *Apply size* gives them to all selected elements as one undo step: every element keeps
  its top left corner, a line or arrow its direction; an element which keeps its aspect ratio (an emoji, an image) takes
  the width and its own ratio.
- What was copied is kept for all editor windows until Greenshot ends, not in greenshot.ini.
- *Style and size* section of the panel, context menu, Ctrl+Shift+C (copy style) and Ctrl+Shift+V (apply style). Copy needs
  exactly one selected element. The core is `ElementStyleClipboard` (no UI), undo with `ChangeFieldsMemento`.

### The contract

```csharp
public interface IEditorPanel
{
    string Id { get; }                               // stored, e.g. "Greenshot.Selection"
    string Title { get; }                            // translated
    int Order { get; }                               // place in the list of panels
    string MenuText { get; }                         // menu item in the Plugins menu, null for none
    string Shortcut { get; }                         // e.g. "Ctrl+Shift+K", null for none
    bool IsAvailable(IEditorPanelContext context);
    object CreateViewModel(IEditorPanelContext context);
    object CreateView(object viewModel);             // a WPF FrameworkElement today
}

public interface IEditorSection
{
    string Id { get; }                               // stored for the collapsed state, e.g. "Greenshot.Emoji"
    string Title { get; }                            // translated heading
    int Order { get; }                               // EditorSectionOrder.SizeAndPosition / StyleAndSize / Element / Other
    bool AppliesTo(IEditorPanelContext context);     // for the current selection and drawing mode
    bool ShowsPanel { get; }                         // true: shows the Selection panel by itself
    bool FillsHeight { get; }                        // true: gets the height the other sections leave
    object CreateViewModel(IEditorPanelContext context);
    object CreateView(object viewModel);
}
```

- **View model and view are separate.** `CreateViewModel` gets the context and returns a plain class with
  `INotifyPropertyChanged`, without types of a UI toolkit; this is the part which moves to another toolkit unchanged.
  `CreateView` builds the view for it; today it must return a WPF `FrameworkElement` and set its `DataContext`.
  The same split is used for settings pages (`IPluginServices.AddSettingsView`).
- **Once per editor window.** View model and view are created the first time a panel is shown, or a section applies, and
  kept while the window is open. A view model which implements `IDisposable` is disposed when the window closes;
  unsubscribe from the context events there.
- **Follow the context, not the form.** Read `context.CurrentSurface` (it changes with the tabs, it can be null) and
  listen to `SelectionChanged`, `DrawingModeChanged` and `ElementsChanged` (after a move, resize, undo or redo).
  `AppliesTo` and `IsAvailable` are called often: keep them cheap.
- **Changes are undoable.** Change elements through the surface and record the old state first, e.g.
  `element.MakeBoundsChangeUndoable(false)` or `surface.MakeUndoable(memento, false)`, so Ctrl+Z works. Several
  elements changed by one action are one memento. Set `surface.Modified` or call `Invalidate` on the elements as the tools do.
- **Keyboard.** While the panel area has the keyboard, the editor only handles its keys which also work while typing,
  so a text box in a panel gets letters, Delete and the arrow keys. Esc goes back to the capture. After an action done with
  the mouse which ends the work in the panel (e.g. picking an emoji), call `context.FocusCanvas()`, so the keys of the
  editor work again; after the keyboard, leave the focus where it is.
- **Showing a panel.** `context.ShowPanel(id, activate)` shows a panel; `activate: false` doesn't take the keyboard from
  the capture. Only do that on a user action: the editor itself shows the Selection panel for sections with
  `ShowsPanel = true`, also over another panel, because a choice is needed.
- **Menu and keys.** `MenuText` and `Shortcut` (e.g. "Ctrl+Shift+K") put the panel into the Plugins menu of the editor;
  the editor adds the item, the plugin needs no WinForms types. null for none (the Selection panel has none).
- **Height.** A section is as high as its content, the panel scrolls when they don't fit. A section with `FillsHeight = true`
  (e.g. a list to pick from) gets the height the other sections leave, at least 160 pixels; its view must scroll its
  content itself and gets a fixed height, so a virtualized list stays virtualized.

### Register

A plugin registers its panels and sections as services, in `Initialize`:

```csharp
services.AddServices<IEditorSection>(() => new IEditorSection[] { new BarcodeSection() });
services.AddServices<IEditorPanel>(() => IsEnabled ? new IEditorPanel[] { _stampsPanel } : Array.Empty<IEditorPanel>());
```

Ids are unique; start them with the name of the plugin. The sections of the editor are `Greenshot.SizeAndPosition`
and `Greenshot.Emoji`, the panel is `Greenshot.Selection`.

### Look

Use the shared styles of `Greenshot.Base.Wpf.Styles` (see [ui-how-to.md](ui-how-to.md)) and, for colors, the keys of
`Greenshot.Base.Wpf.EditorPanelResources` with `DynamicResource`, e.g.
`Background="{DynamicResource Greenshot.EditorPanel.SectionBackground}"`. The editor fills them from its own theme
(`EditorTheme`), which can differ from the theme of the other windows. Keys: `Background`, `Foreground`, `Muted`,
`Border`, `SectionBackground`, `ControlBackground`, `ControlBorder`, `Hover`, `Accent`, all with the prefix
`Greenshot.EditorPanel.`. A view in a popup or window of its own calls `EditorPanelResources.Apply` on its resources.
The shared styles follow the editor theme in the panel area too: the area sets the editor palette as `ThemeScope.Theme`
(see [ui-how-to.md](ui-how-to.md)), so a view which merges e.g. `DialogStyles.xaml` looks like the editor. In your own
bindings use `{Binding Path=(wpf:ThemeScope.Theme).MutedBrush, RelativeSource={RelativeSource Self}}` instead of
`ThemeManager.Instance`.

Texts come from the language files, as everywhere ([ui-how-to.md](ui-how-to.md)); give every input an
`AutomationProperties.Name` ([accessibility.md](accessibility.md)).

### In the editor

| Class | Job |
|---|---|
| `Panels/EditorPanelHost` | Which panel is shown, opened by the user or by itself; the rules for showing and hiding. No UI, tested in `EditorPanelTests`. |
| `Panels/EditorPanelContext` | The `IEditorPanelContext` of one editor window. |
| `Panels/EditorPanelDock` | The WinForms control right of the canvas: one `ElementHost` with the WPF `PanelDockView`. The only WinForms part. |
| `Panels/SelectionPanel`, `ViewModels/SelectionPanelViewModel` | The Selection panel: the sections which apply, their collapsed state. |
| `Panels/Sections/*` | The sections of the editor. |
| `Forms/ImageEditorForm.Panels.cs` | Connects form, surface and panels: tabs, selection, drawing mode, DPI, theme, F6. |

The emoji picker (`Controls/Emoji/EmojiPicker`, view model `EmojiPickerViewModel`) is also used outside the panel, in a
drop down button (`EmojiPickerButton`), e.g. in the recipe editor. The English names for the search come from the Unicode
`emoji-test.txt` file, which the build turns into `emojis.xml` (attribute `N`).

### Later

Planned with the same contract: a barcode
section for the Zxing plugin, the effects as sections instead of dialogs, a Text panel for OCR and an overview of the
capture at the bottom of the area. Then the other WinForms parts of the editor API (`IEditorPlugin` with its menu items
and form) go away.
