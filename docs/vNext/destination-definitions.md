# Destination definitions

Some destinations are one piece of code which can be used in many ways: the custom uploader sends a capture to any
service an uploader file describes, the external command runs any program a command describes. The code is the same for every service, what differs is data: the
**definition**. Each definition shows up as its own destination ("Custom uploader" > "My image host") and as a name a recipe
step can pick.

The parts all such plugins need are in `src/Greenshot.Base/Definitions`, so a plugin only writes what is its own: what a
definition holds, how it is checked and how it runs.

## The parts

| Type | What it does |
|---|---|
| `IDefinitionKind<T>` | Implemented by the plugin, once per kind: the designation (e.g. `CustomUploader`), the name of a definition, `Read` (parse and check JSON, null when it doesn't pass), `Write` (JSON, ASCII only), `Describe` (a few words for the list, e.g. the server) and `CreateGatedAction` (what the user approves for a recipe) |
| `DefinitionStore<T>` | The definitions, one JSON value per definition in a `DefinitionFolder` (or any dictionary, e.g. in tests), protected as the plugin says (default `StringExtensions.Encrypt`), and an icon per definition (base64 PNG, in the ini section). `GetAll`, `GetReadable`, `Find`, `FindOrOnly`, `Add` (replaces the one with the same name, drops its icon), `Remove`, `GetIcon`, `SetIcon`. A plugin can derive from it to keep more next to the definitions under the same lock. Built-in definitions (given to the constructor) are listed with the stored ones (`IsBuiltIn`, id `builtin:name`), aren't stored and can't be replaced; a stored definition with the same name takes the place of a built-in one |
| `DefinitionDestination<T>` | The destination of the kind: one sub destination per definition, only shown when there is one, all their approvals for a recipe. An export with one definition uses it, with more the user picks one; without the user it is declined |
| `DefinitionItemDestination<T>` | The destination of one definition: designation `Kind:name`, icon key `kind:name`, the approval of the definition. The plugin writes `ExportAsync` |
| `DefinitionIconProvider<T>`, `DefinitionIcons` | The icon of a definition from the store, else the generic icon of the kind. `kind:` alone is the icon of the only definition, else the generic one. `OnIconMissing` lets the plugin fetch one |
| `DefinitionFolder` | A folder as the dictionary of a store: one file per definition (the id is the file name, made of the name: `NewId`), read once (`Open` shares one per folder), written to a new file first. `NextToConfiguration(name)` is the folder next to greenshot.ini (portable: next to the executable). Implements `IDefinitionApprovals` |
| `DefinitionApprovalAtStartup` | The question at the start for the files which wait for approval: `NotApproved`, `Ask` (approve the confirmed ones), `AskWhenIdle` (posted from `StartAsync`, shown once Greenshot runs) |
| `IDefinitionApprovals`, `DefinitionStore.Approve` | Whether the user approved a file as it was read; `StoredDefinition.IsApproved`, `IsUsable` (read and approved: `GetReadable` and `Find` only give these) |
| `FileApprovals`, `ProtectedRecordFile<T>`, `ContentHash` (`Greenshot.Base.Trust`) | The approvals by full path and SHA-256 in `%LocalAppData%\Greenshot\definition_trust.dat`, protected with DPAPI and an entropy of their own; the recipe approvals (`RecipeTrustStore`) use the same file code and hash |
| `DefinitionListItem` | A row of the settings list: icon, name, `Describe`, read by a screen reader as "name, description"; `IsBuiltIn`, `IsEditable`; `WithOnOff(isOn, checkBoxName, turned)` gives the row a check box to turn the definition off and on (the external commands use it) |
| `DefinitionListView` (`Greenshot.Base.Wpf.Views`) | The list on the plugin's own settings page: `Title` (also the list's name for a screen reader), `Items`, `SelectedItem`, `EmptyText` when there is none. The plugin adds its own buttons next to it |
| `DefinitionImport` | The steps of an import, used by each plugin on its own page: `PickFile`, `ReadFile` (size limit, UTF-8, the plugin's parser; the reason when it fails), `Confirm` (what it does, Cancel is the default, the buttons react after a second), `PickExportFile` and `FileNameFor` for an export |
| `DefinitionFileException` | Thrown by a plugin's parser with the reason (translated) a file can't be imported |

## Rules

- **Read is the check.** A stored definition goes through `IDefinitionKind<T>.Read` each time it is read, like an imported
  file. Someone who changes a file of the folder by hand can't get past the checks of the import.
- <a id="approval"></a>**Nothing is picked up behind the user's back.** The folder is read once, when Greenshot starts; a
  file copied in or changed later has no effect on the running Greenshot. A file Greenshot writes (import, editor, the
  conversion of the 1.3 settings) is approved with the SHA-256 of its content. Like the recipe files (only the paths an
  import added to `RecipeFiles`), only those files are read: a file without an approval record (`IFileApprovals.IsKnown`) is
  ignored, the user imports it. One which changed since isn't approved: it has no destination and recipes can't use it until
  the user approves it. The plugin asks once Greenshot has started (`DefinitionApprovalAtStartup.AskWhenIdle` in `StartAsync`, the same
  confirmation as an import, *Approve* or *Not now*); one left for later is listed as *Not approved* on the plugin's page,
  which can approve it too, and is asked about again at the next start. Removing a definition removes its approval.
- **Switched off is off everywhere.** A recipe step which uses a destination of its own says so (`IUsesDestinations`); the
  flow skips it when `ConfigurationRestrictions.IsDestinationAllowed` says no, like the Destinations step.
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
2. `UploaderStore` derives from `DefinitionStore<UploaderDefinition>`: the uploaders are `.sxcu` files in the `Uploaders`
   folder with only their keys encrypted, the reminders and the upload history stay in `[CustomUploader]`.
3. `CustomUploaderDestination` and `UploaderDestination` derive from the destination classes; only the menu text and the
   upload are their own.
4. `CustomUploaderIconProvider` derives from `DefinitionIconProvider<T>` and fetches the icon of the service when one is
   missing.
5. The settings page shows the uploaders with `DefinitionListView` and imports with `DefinitionImport`, with the
   uploader's own step in between: the values window for the keys.
6. The recipe step finds its uploader with `FindOrOnly(name)`: by name, or the only one when the step names none.

The external command (`src/Greenshot.Plugin.ExternalCommand`, [external-commands.md](external-commands.md)) is the second
user, with a few differences:

- `CommandKind` reads and checks `.gscommand.json` files (`CommandKind.Parse`); `CommandStore` has the built-in commands
  (MS Paint, Paint.NET when installed); the commands are plain `.gscommand.json` files in the `Commands` folder (a command
  holds no secrets). The commands of 1.3 are converted once (`CommandDefinitions.ConvertOnce`).
- Each command stays a destination of its own, `External name` like in 1.3, so `OutputDestinations` and recipes keep
  working: `ExternalCommandDestination` derives from `DefinitionItemDestination<T>` and overrides the designation and the icon
  key (`extcmd:name`, the icon of the program). There is no destination of the kind.
- The settings page adds and changes commands in an editor and exports them; the export writes the definition as it is
  (a command holds no secrets).
- The recipe step takes the command as the defaults of its parameters (`ExternalCommandStep.ResolveCommand`); a recipe can
  also carry the whole command in its parameters. Recipes don't install commands.

## Tests

`src/Greenshot.Tests/Definitions/DefinitionTests.cs` covers the store, the destinations and reading a file with a small test kind;
`src/Greenshot.Tests/CustomUploader` covers the uploader on top of it, `src/Greenshot.Tests/ExternalCommand` the commands.
