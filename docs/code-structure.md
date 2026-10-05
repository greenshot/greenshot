# Code structure

Where code goes in Greenshot, and the names it gets. Folders are grouped by feature first (Capturing, Recipes, Settings, Ipc, Ai, Plugins),
and inside a feature by kind (Views, ViewModels, Tools, Steps, ...). The namespace follows the folder.

## Rules

- **`Forms` is only for WinForms.** A WPF window, page or control goes into a `Views` folder and is named `…Window`, `…Page` or `…View`.
- **A class which only exists to be bound by a view is a view model**: it is named `…ViewModel` and lives in a `ViewModels` folder next to the `Views` folder,
  one class per file.
- **Not everything with a UI is a view.** Capture tools and overlays draw into the capture window but are no XAML; they live in `Capturing/Tools` and `Capturing/Overlays`.
- **Recipes are the umbrella for the capture pipeline.** Execution, steps, triggers, approval and the recipe windows are under `Greenshot/Recipes`,
  the shared model under `Greenshot.Base/Recipes`. A plugin's recipe steps live in its own `Recipes` folder (e.g. `Greenshot.Plugin.Jira/Recipes/JiraStep.cs`).
- **Optional parts live in their own folder and register themselves.** The browser extension (`Greenshot/Ipc/BrowserExtension`), the AI tools (`Greenshot/Ai`)
  and plugin loading (`Greenshot/Plugins`) are left out of Greenshot Light by excluding their folders in `Greenshot.csproj`.
  They plug in through `[assembly: GreenshotModule(typeof(...))]` in their folder, implementing `IIpcCommandExtension` (IPC commands) or `IBuiltInRecipeProvider` (built-in recipes).
  `CheckLightAssembly` fails the Light build when one of their type names is still in Greenshot.exe.
- A namespace must not shadow a type it uses: the capture folder is `Capturing`, because a `Greenshot.Capture` namespace would hide `Greenshot.Base.Core.Capture`.

## Frozen: do not move or rename

The `.greenshot` file stores the drawing surface with the BinaryFormatter, which writes full type names (see `BinaryFormatterHelper`).
Moving these types breaks opening existing files:

- `Greenshot.Editor/Drawing/**` (containers, fields, filters, adorners, emoji, `Surface`) and `EmojiPicker`
- `Greenshot.Base/Interfaces/Drawing/**`, `ISurface` and the surface event types
- `Greenshot.Editor/Helpers/BinaryFormatterHelper.cs` and the editor file format handlers
- the assembly names

The Zxing `BarcodeContainer` is not in the `BinaryFormatterHelper` allow-list, so barcodes are not part of `.greenshot` files today;
once it is added there, its type names (`Greenshot.Plugin.Zxing.Drawing`) are frozen too.

Also kept as they are: the plugin classes (`…Plugin`, recipe requirements match their full type name, embedded resources are named after them),
`Greenshot.Base.Interfaces.Capture` (capture tool plugin API) and the pack URIs of `Greenshot.Base/Wpf/Styles`.

## Layout

| Folder | What is in it |
|---|---|
| `Greenshot/Capturing` | Capture window (`Views`), tools, overlays, info panel view model, key registry, placement |
| `Greenshot/Recipes` | `RecipeManager`, `Pipeline` (execution), `Steps`, `Triggers`, `Approval`, `Views`, `ViewModels` |
| `Greenshot/Settings` | Settings window (`Views`) and its view models |
| `Greenshot/Ipc` | Named pipe server and client, dispatcher, `Cli`, `BrowserExtension` |
| `Greenshot/Ai` | Everything for AI tools (greenshot-mcp), with its settings page |
| `Greenshot/Plugins` | Plugin loading and the plugins settings page |
| `Greenshot/SelfService`, `Greenshot/Views`, `Greenshot/ViewModels` | Self-service, about, bug report and the other app windows |
| `Greenshot/Forms` | The remaining WinForms (`MainForm`, `SharingForm`) |
| `Greenshot.Base/Recipes` | `Pipeline`, `Contracts`, `Sources`, `Triggers`, `Expressions` |
| `Greenshot.Base/Wpf` | Shared WPF helpers, styles, `Views`, `ViewModels` |
| `Greenshot.Editor/Views` | WPF windows of the editor (the editor itself is still WinForms in `Forms`) |
| `Greenshot.Plugin.*` | Root: the plugin class, its configuration and language interfaces. `Destinations`, `Recipes` (steps), `Views`, `Api` (the web service client and its entities), `Images`; Jira also `Monitoring`, Zxing `Drawing` (barcode container) and `Processing` (detection, generation) |
