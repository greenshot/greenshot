# Destination definitions

Some destinations are one piece of code which can be used in many ways: the custom uploader sends a capture to any
service an uploader file describes. The code is the same for every service, what differs is data: the
**definition**. Each definition shows up as its own destination ("Custom uploader" > "My image host") and as a name a recipe
step can pick.

The parts all such plugins need are in `src/Greenshot.Base/Definitions`, so a plugin only writes what is its own: what a
definition holds, how it is checked and how it runs.

## The parts

| Type | What it does |
|---|---|
| `IDefinitionKind<T>` | Implemented by the plugin, once per kind: the designation (e.g. `CustomUploader`), the name of a definition, `Read` (parse and check JSON, null when it doesn't pass), `Write` (JSON, ASCII only), `Describe` (a few words for the list, e.g. the server) and `CreateGatedAction` (what the user approves for a recipe) |
| `DefinitionStore<T>` | The definitions in an ini section, one encrypted JSON value per definition (`StringExtensions.Encrypt`), and an icon per definition (base64 PNG). `GetAll`, `GetReadable`, `Find`, `FindOrOnly`, `Add` (replaces the one with the same name, drops its icon), `Remove`, `GetIcon`, `SetIcon`. A plugin can derive from it to keep more next to the definitions under the same lock |
| `DefinitionDestination<T>` | The destination of the kind: one sub destination per definition, only shown when there is one, all their approvals for a recipe. An export with one definition uses it, with more the user picks one; without the user it is declined |
| `DefinitionItemDestination<T>` | The destination of one definition: designation `Kind:name`, icon key `kind:name`, the approval of the definition. The plugin writes `ExportAsync` |
| `DefinitionIconProvider<T>`, `DefinitionIcons` | The icon of a definition from the store, else the generic icon of the kind. `kind:` alone is the icon of the only definition, else the generic one. `OnIconMissing` lets the plugin fetch one |
| `DefinitionListItem` | A row of the settings list: icon, name, `Describe`, read by a screen reader as "name, description" |
| `DefinitionListView` (`Greenshot.Base.Wpf.Views`) | The list on the plugin's own settings page: `Title` (also the list's name for a screen reader), `Items`, `SelectedItem`, `EmptyText` when there is none. The plugin adds its own buttons next to it |
| `DefinitionImport` | The steps of an import, used by each plugin on its own page: `PickFile`, `ReadFile` (size limit, UTF-8, the plugin's parser; the reason when it fails), `Confirm` (what it does, Cancel is the default, the buttons react after a second) |
| `DefinitionFileException` | Thrown by a plugin's parser with the reason (translated) a file can't be imported |

## Rules

- **Read is the check.** A stored definition goes through `IDefinitionKind<T>.Read` each time it is read, like an imported
  file. Someone who changes greenshot.ini by hand can't get past the checks of the import.
- **What the user approved is what runs.** `CreateGatedAction` names what matters (the server, the program). A definition
  imported again with another server gives another target, so a recipe which uses it needs a new approval.
- **The name is the key.** Names are unique ignoring case; recipes and designations use them. An import with a known name
  replaces that definition and keeps its id.
- **Nothing is stored before the user confirmed.** An import reads and checks the file, shows what the definition does
  (`DefinitionImport.Confirm`) and only then adds it. Each plugin imports on its own settings page, with its own file type.
- **Nothing is sent or started from the store.** The store only keeps data; the plugin decides when and how a definition
  runs.

## Using it in a plugin

The custom uploader is the example (`src/Greenshot.Plugin.CustomUploader`):

1. `UploaderKind` implements `IDefinitionKind<UploaderDefinition>` with the checks of `UploaderFile`.
2. `UploaderStore` derives from `DefinitionStore<UploaderDefinition>` and keeps the reminders and the upload history next
   to the uploaders.
3. `CustomUploaderDestination` and `UploaderDestination` derive from the destination classes; only the menu text and the
   upload are their own.
4. `CustomUploaderIconProvider` derives from `DefinitionIconProvider<T>` and fetches the icon of the service when one is
   missing.
5. The settings page shows the uploaders with `DefinitionListView` and imports with `DefinitionImport`, with the
   uploader's own step in between: the values window for the keys.
6. The recipe step finds its uploader with `FindOrOnly(name)`: by name, or the only one when the step names none.

## Tests

`src/Greenshot.Tests/Definitions/DefinitionTests.cs` covers the store, the destinations and reading a file with a small test kind;
`src/Greenshot.Tests/CustomUploader` covers the uploader on top of it.
