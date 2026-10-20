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
- **Notifications go through `INotificationService`, created by an `INotificationServiceProvider`** (both in `Greenshot.Base/Interfaces`).
  The provider checks whether its platform is available and initializes it (the toasts register Greenshot with Windows),
  `NotificationServiceFactory` (`Greenshot/Shell`) takes the first available one: the toasts, else the balloons of the tray icon.
  See [notifications.md](notifications.md).
- **HTTP goes through `HttpService.Current`** (`Greenshot.Base/Http`): one HttpClient with the proxy setting, the certificate exceptions
  of the configuration and the user agent. It logs method, host and status only; headers, path, query and content can hold tokens and are not logged.
  Upload content with progress comes from `HttpContents`. Jira uses Dapplo.Jira, which gets the certificate exceptions through `CertificateValidator.RegisterGlobally`.
  See [http.md](http.md).
- **Greenshot.exe of the full version is x64, Greenshot Light is AnyCPU.** COM doesn't give a toast click to a Greenshot it starts for it
  when the exe is AnyCPU (its PE header says 32-bit, although it runs as a 64-bit process), see [notifications.md](notifications.md).
  Light has no toasts (the tray icon shows its notifications), no `greenshot-cli.exe` and no `greenshot-proxy.exe` (its installer
  opens files with `Greenshot.exe --file`), so it also runs on 32-bit Windows and natively on ARM64. The libraries and plugins stay
  AnyCPU, Full and Light load them; the solution platform stays "Any CPU" (the output paths are `bin\<Configuration>\net480`).
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
| `Greenshot/Shell` | Start (`GreenshotApplication`), the shell which composes the services and owns the shutdown (`GreenshotShell`), the tray icon and its WPF menu |
| `Greenshot.Base/Recipes` | `Pipeline`, `Contracts`, `Sources`, `Triggers`, `Expressions` |
| `Greenshot.Base/Wpf` | Shared WPF helpers, styles, `Views`, `ViewModels`, the themed menus (`ThemedMenu`) and the destination picker |
| `Greenshot.Editor/Views` | WPF windows of the editor (the editor itself is still WinForms in `Forms`) |
| `Greenshot.Editor/Tabs` | The tabs of the editor window: `EditorTab` (one capture, the `IImageEditor`), the tab strip and dragging tabs between windows |
| `Greenshot.Document` | Experimental document model (`GreenshotDocument`: canvas, layers, elements, resources), platform-neutral (netstandard2.0, no System.Drawing, WinForms or Dapplo.Windows). Runs in parallel with `Surface`/`DrawableContainer`, only the tests use it for now, see [document-model.md](document-model.md) |
| `Greenshot.Document.Legacy` | Reads the .greenshot / .gst files of Greenshot 1.2 - 1.4 into a `GreenshotDocument` without BinaryFormatter (System.Formats.Nrbf, netstandard2.0, experimental) |
| `Greenshot.Document.Editor` | Maps the editor `Surface` to a `GreenshotDocument` (experimental, only the tests use it for now) |
| `Greenshot.Document.Rendering` | Draws a `GreenshotDocument` without the editor, with ImageSharp.Drawing (netstandard2.0, experimental, only the tests use it for now) |
| `Greenshot.Plugin.*` | Root: the plugin class, its configuration and language interfaces. `Destinations`, `Recipes` (steps), `Views`, `Api` (the web service client and its entities), `Images`; Jira also `Monitoring`, Zxing `Drawing` (barcode container) and `Processing` (detection, generation) |
