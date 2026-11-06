# Variables and expressions: one engine (design)

Status: design for review (2026-11-04), not implemented yet.
Code references are to `main` at 6685bc5 (after #1691). Everything marked *(read, not run)* comes from reading the
code; nothing was built or run for this document.

---

## 1. What we have today

There are three variable syntaxes and five code paths that resolve variables.

| # | Syntax | Resolved by | Used for |
|---|---|---|---|
| 1 | `${name:params}`, a small parameter language (`d"format"`, `p<width>,<char>`, `r<old>,<new>`, `s<start>,<len>`) | `FilenameHelper.FillPattern` / `GetFilename*` (`src/Greenshot.Base/Core/FilenameHelper.cs`) | file name pattern, output folder, print footer, Outlook subject, editor Save as, Box/Dropbox/Jira/Confluence/custom uploader file names, recipe `SaveDirectory` + `FilenamePattern` |
| 2 | `${NAME}` = any environment variable, by bare name | `FilenameHelper.FillVariables` | `OutputFilePath` (`CoreConfigurationImpl.cs:227`), tray "open folder", Self-Service output check, recipe editor folder pickers, Jira `IssueKey`, Confluence `PageId`, clipboard custom text |
| 3 | `%NAME%` | `FilenameHelper.FillCmdVariables`, `Environment.ExpandEnvironmentVariables` | External command line, arguments and working folder (5 places in the ExternalCommand plugin, `WindowsAppHelper.cs:236`), Stamps `LibraryPath` (`StampsPlugin.cs:131`), RecordVideo `OutputDirectory` (`RecordVideoRecipeStep.cs:367`), internal icon paths |
| 4 | `${ns.name}`, `${expr}`, `${now:format}` | `ExpressionEvaluator` (`src/Greenshot.Base/Recipes/Expressions/ExpressionEvaluator.cs`) | every recipe parameter (engine resolves them before the step runs, `DagExecutionEngine.cs:319-322`), `when`/`enabled`, triggers, options |
| 5 | `${user.*}`/`${machine.*}` only | `ExpressionEvaluator.TryExpandFolder` (#1691) | recipe `DirectoryPath` options |
| – | `{0}`, `{capture}`, `{yyyyMMdd_HHmmss}` | string replace | ExternalCommand argument (file), Jira comment placeholder, RecordVideo file name |

The same idea has different names in each: the capture time is `${capturetime}` / `${YYYY}` in (1) and
`${now:...}` in (4) (which is *not* the capture time); the user name is `${user}` in (1) and `${user.username}` in (4);
Pictures is `${MyPictures}` in (1) and `${user.pictures}` in (4).

### Problems found on main

1. **Recipe file name patterns are erased before the step sees them** *(read, not run)*. The engine resolves every
   parameter with `ExpressionEvaluator` first. `${title}` and `${capturetime}` are not names it knows, so
   `ResolveSingleToken` returns null (`ExpressionEvaluator.cs:559-565`). `FilenamePattern: "${title}"` becomes
   null and the File step falls back to the settings pattern (`DestinationExportStep.cs:310`); `"Shot ${title}"`
   becomes `"Shot "`. Affects greenshot-recipes `cli_convert_format` and `cli_annotate_template`. No test covers it.
2. **`${NUM}` goes up on every evaluation, also previews.** `FilenameHelper.cs:426-427` increments the setting each
   time the pattern is filled: the Save as dialog proposal (`SaveImageFileDialog.cs:220`, `ImageEditorForm.cs:2834`),
   the Self-Service output check (`OutputPathCheck.cs:218`), the print document name (`PrintHelper.cs:67`), and the
   Confluence file name all use a number without saving a file. Two `${NUM}` in one pattern give two numbers.
3. **Values are evaluated twice.** Clipboard custom text: the engine has already replaced `${ocr_text}` (with nothing)
   before `DestinationExportStep.cs:434` replaces it, and the OCR text then goes through `FillVariables`, so text
   in the capture like `${USERNAME}` is expanded. Jira `IssueKey` from the context goes through `FillVariables` too
   (`JiraStep.cs:100`). This breaks the rule written in `IEvaluatesOwnParameters`.
4. **A capture can choose a folder.** Recipe `SaveDirectory` is a `DirectoryPath` parameter, but unlike a
   `DirectoryPath` option it isn't limited to `${user.*}`/`${machine.*}`: `"${payload.title}"` is resolved by the
   engine, and `FillPattern(customDir, captureDetails, false)` (`DestinationExportStep.cs:297`) fills `${title}`
   without making it file name safe. The approval window shows the raw text (`DestinationExportStep.cs:101`).
5. **Environment variables win over Greenshot names.** `FilenameHelper.cs:284` checks environment variables before
   `title`, `NUM`, `user`; an environment variable `TITLE` replaces the window title. Capture metadata
   (`FilenameHelper.cs:308`) is only made path safe, so it may contain `\`.
6. **No checking.** An unknown name becomes an empty text everywhere; the settings UI has no preview or error, and
   the help is one hand-written language text (`message_filenamepattern`).

---

## 2. Goals

- One syntax, `${...}`, everywhere a person types a variable. `%VAR%` goes away.
- One resolver in Greenshot.Base, the single source of truth: core, recipes, plugins and the UI ask it, nobody else
  replaces variables.
- Names are stable, namespaced and documented from one list (the same list drives help, completion and docs).
- Each place says what it is for (a folder, a file name, a command, a text for people, a condition), and that
  decides which variables are allowed, how values are escaped, and which culture formats dates.
- Values from a capture or from outside (window title, OCR, payload, detected work item) never choose a folder.
- Each text is evaluated exactly once; a value is never read as a template again.
- Previews never change state (counters).

---

## 3. The language

```
${ expression }                 value of the expression
${ expression : format }        formatted like .NET: ${payload.time:yyyy-MM-dd}, ${counter.global:000}, ${time.now:T}
${ expression : 'format' }      the format may be quoted, e.g. ${time.now:'HH:mm:ss'}
${ a ?? 'fallback' }            fallback when a value is missing (no session, no title)
${ (a > 1 ? 'x' : 'y') }        a ternary needs parentheses (like C# interpolation), the first top-level ':' is the format
${ left(payload.title, 40) }    functions, see below
$${                             a literal "${"
```

The expression part is today's `ExpressionEvaluator` grammar (arithmetic, comparisons, `&&`, `||`), moved from
`Greenshot.Base.Recipes.Expressions` to `Greenshot.Base.Expressions` and parsed once into a tree that is cached. The
format suffix replaces the `d"..."`/`p...` parameter language and the `now:` special case (now `${time.now:format}`).

### Operators, functions and formats

All of it is pure: an expression never changes anything (counters are the one controlled exception, section 5),
has no loops and no user-defined functions. Unknown functions and wrong argument counts are validation errors.

**Literals:** `'text'` or `"text"`, numbers (`1`, `2.5`, invariant), `true`, `false`, `null`.

**Operators** (highest first):

| Operators | Meaning |
|---|---|
| `( )` | grouping |
| `!`, unary `-` | not, negative |
| `*` `/` `%` | multiply, divide, remainder (divide by 0 is an error at validation when constant, else empty) |
| `+` `-` | add, subtract; `+` with a text joins texts |
| `<` `<=` `>` `>=` | compare (numbers as numbers, texts ordinal ignoring case, dates as dates) |
| `==` `!=` | equal, not equal |
| `&&` | and (short-circuit) |
| `\|\|` | or (short-circuit) |
| `??` | fallback when the left side is missing or empty |
| `c ? a : b` | choice; inside `${...}` it needs parentheses, because the first top-level `:` starts the format |

**Format** (`${value:format}`): the .NET format of the value's type, so nothing new to learn and documented by Microsoft.
- Date and time: `${payload.time:yyyy-MM-dd HH_mm_ss}`, `${time.now:dddd}`.
- Standard formats are preferred wherever a date or time is meant for people (footer, caption, e-mail subject):
  `d`/`D` (short/long date), `t`/`T` (short/long time), `g`/`G` (date and time), in the culture of the purpose, so a
  footer shows `21:07:53` in German and `9:07:53 PM` in English. The 1.3 footer default becomes
  `${payload.time:D} ${payload.time:T} - ${payload.title}`. Custom formats are for file names (fixed, sortable).
- The format may be quoted: `${time.now:'HH:mm:ss'}`. Docs and examples always quote a format which contains `:`;
  unquoted it works as well, because everything after the first top-level `:` is the format.
- Numbers: `${counter.global:000000}` (leading zeros), `${payload.width:N0}`, `${x:0.00}`.
- Text has no format; use the functions.
- Culture by purpose (section 4): file names and folders the Windows culture (as 1.3), texts for people the UI language,
  recipe values and conditions invariant.

**Functions:**

| Group | Functions |
|---|---|
| Text | `left(t, n)`, `right(t, n)`, `substring(t, start, length?)`, `replace(t, old, new)`, `lower(t)`, `upper(t)`, `trim(t)`, `padLeft(t, n, c?)`, `padRight(t, n, c?)`, `length(t)`, `contains(t, x)`, `startsWith(t, x)`, `endsWith(t, x)` |
| Text with patterns | `match(t, pattern, group?)` (first match or a named group, e.g. an issue key from a title), `replaceMatch(t, pattern, new)`; .NET regex with a match timeout (250 ms), so a bad pattern can't hang a capture |
| Paths | `fileName(p)`, `fileNameWithoutExtension(p)`, `extension(p)`, `folder(p)` (all work on a value, they never touch the disk) |
| Numbers | `round(n, digits?)`, `floor(n)`, `ceiling(n)`, `min(a, b)`, `max(a, b)`, `abs(n)` |
| Date and time | `addDays(d, n)`, `addHours(d, n)`, `addMinutes(d, n)`, `isoWeek(d)` (not in .NET formats), `date(text, format)` (read a date from a text) |
| Conversion | `number(x)`, `text(x)`, `bool(x)`, `empty(x)` |
| Random | `random(n)` (n letters and digits, replaces `${RRR}`), `guid()` |

The old parameter language maps to these (section 8): `p` → format or `padLeft`, `s` → `substring`, `r` → `replace`,
`d` → format.

Plugins can add functions the same way they add variables (`IFunctionProvider` with a descriptor: name, arguments,
result type, language key), under their own prefix (`jira.issueKey(t)`); they must be pure.


### Namespaces

No bare names any more (decided): every value is in a namespace, so a new name never clashes.

| Namespace | Values | Trust |
|---|---|---|
| `time.*` | `now` (evaluation time, local), `utc` (evaluation time, UTC); the payload's time is `payload.time` | trusted |
| `payload.*` | what is being worked on, a capture, an opened file or the clipboard (one namespace): `width`, `height` (current size), `time` (when taken, or the file's time; one value for every output), `source`, `hdr`, `title`, `process`, `file` (full path of an opened file), `ocr.text`, `video.file`, later `url` (browser extension) | per value: `width`, `height`, `time`, `source`, `hdr` trusted; `title`, `process`, `file`, `ocr.text`, `url` **untrusted** |
| `counter.global`, `counter.folder`, `counter.session` | see section 5 | trusted |
| `output.*` | the result of the export running now: `file` (full path of the file written), `folder`, `name` (file name with extension), later `url` (link after an upload). Today this is `{0}` of the external command, `ExternalCommand.TargetFile`, `Destination.Filename` and the Jira `{capture}` placeholder | trusted: Greenshot made the name safe; as a command argument it is quoted as one argument |
| `user.*` | fixed list, resolved per OS: `name`, `domain`, `profile`, `desktop`, `documents`, `pictures`, `videos`, `appdata`, `localappdata`, `temp` (`pictures` = the shell folder on Windows, also when moved, XDG on Linux, `~/Pictures` on macOS) | trusted |
| `machine.*` | fixed list: `name`, `os`, `osversion`, `programfiles`, `commonappdata`, ... | trusted |
| `env.<NAME>` | the process environment, on every OS; exactly what `%NAME%` gives today | trusted |
| `user.env.<NAME>` | Windows only: the user's variable (HKCU\Environment); empty on Linux/macOS, the validator warns | trusted |
| `machine.env.<NAME>` | Windows only: the machine's variable (HKLM ...\Session Manager\Environment); empty on Linux/macOS, the validator warns | trusted |
| `config.<section>.<key>` | settings of core and plugin sections, e.g. `config.Core.OutputFileFormat`, `config.Jira.Url`; read only, settings marked sensitive (proxy, uploader) left out, a setting that is a template comes back as plain text (not rendered again); looked up without reflection (check that Dapplo.Ini offers a by-name lookup, else generate the list) | trusted |
| `option.*` | recipe options (as today) | trust of the option type: a `String` option is untrusted for gated parameters (as today) |
| `var.*` | recipe flow variables: trigger parameters (`var.Window`), `SetVariable`, step outputs (`var.Destination.Filename`) | trust of the value that was stored |
| `recipe.*` | `id`, `name`, `executionId` (today `context.RecipeId`, ...) | trusted |
| `session.*` | `key`, `name`, `title`, `url`, `provider` of the active work session; missing when none | **untrusted** (typed or detected) |
| `profile.*` | reserved for profiles, if they come | trusted |
| `<plugin>.*` | registered by plugins, e.g. `jira.issue` (last chosen issue) | declared by the provider |

Why there is no `payload.meta.<key>`: today two metadata dictionaries exist:
- `CaptureDetails.MetaData` (strings), filled only by the Source step: `source` (`Screen`, `Window`, `Clipboard`, `file`)
  for every capture, and for an opened or watched file also `file`, `dirname`, `filename`, `basename`, `extension`
  (`FileCaptureSource.cs:127-132`, `181-186`). The 1.3 file name pattern reaches these as bare `${dirname}` etc.
  For an opened image file `ImageMetadataReader.ReadInto` (`FileCaptureSource.cs:193`) also reads back what Greenshot
  writes into its files: the capture time (XMP, else EXIF `DateTimeOriginal` + `OffsetTimeOriginal`) into the capture
  time and `capturetime`, the title (XMP `dc:title`, else EXIF `XPTitle`) into the title and `windowtitle`, and the web
  address into `url`. Camera details (location, serial numbers) are deliberately not read.
- `CapturePayload.Metadata` (objects) in recipes: today only RecordVideo writes `VideoFilePath` and `VideoRecordingResult`
  (`RecordVideoRecipeStep.cs:197-198`).

With the names above most of it gets a proper name: `payload.source`, `payload.file` (full path of the opened file),
`payload.video.file`, and for an opened file `payload.time`, `payload.title` and `payload.url` are the values read from
its EXIF/XMP (else the file's time and name). Other EXIF fields stay unread: if one is wanted later, it gets a named
value from a fixed list (e.g. `payload.author`, `payload.description`), never a generic `payload.exif.<tag>`, so
location or serial numbers can't end up in a file name or an upload. So the known keys become named values, and anything else a step stores goes to `var.*`. The image file metadata (EXIF/XMP fields of
the `SetMetadata` step) is something else and is not a variable.

Decided: `env.X` is the generic environment variable; `user.env.X` and `machine.env.X` are the
Windows-only scopes. `user.*` and `machine.*` otherwise only hold the named values, so #1691's fallback
`${user.<anything>}` → environment variable goes (`${user.OneDrive}` becomes `${env.OneDrive}` or `${user.env.OneDrive}`).

---

## 4. The API (Greenshot.Base.Expressions)

```csharp
public enum TemplatePurpose { Folder, FileName, Command, Argument, Text, Condition, Value }

public interface IVariableResolver                       // the single entry point, from SimpleServiceProvider
{
    Template Parse(string text);                          // cached; holds errors with positions
    IReadOnlyList<TemplateError> Validate(string text, TemplatePurpose purpose);
    string Render(Template template, TemplatePurpose purpose, VariableScope scope);
    object Evaluate(Template template, VariableScope scope); // Condition / Value
    IReadOnlyList<VariableDescriptor> Describe(TemplatePurpose purpose); // help, completion, docs
}

public sealed class VariableScope                        // what is known where the text is used
{
    public ICaptureDetails Capture { get; init; }
    public CaptureFlowContext Flow { get; init; }         // recipes
    public string TargetFolder { get; init; }             // for counter.folder
    public bool Preview { get; init; }                    // never changes state
}

public interface IVariableProvider                        // core namespaces are providers too
{
    string Namespace { get; }                             // "capture", "session", "jira"
    IEnumerable<VariableDescriptor> Variables { get; }    // name, type, trust, language key of the description
    bool TryGetValue(string name, VariableScope scope, out object value);
}

public interface IVariableRegistry { void Register(IVariableProvider provider); }  // plugins, at start
```

- **Purpose** decides: allowed trust, escaping and culture.
  - `Folder`: trusted values only, else a validation error (generalizes `TryExpandFolder`); the result must be a full
    local or network path.
  - `FileName`: the template's own `\` makes sub folders (as in 1.3); every value is made one safe path segment (no
    `\ / : ..`, trimmed, length capped); the final path must stay inside the folder (`Path.GetFullPath` check).
  - `Command`: trusted values only. `Argument`: trusted values plus `${output.file}` (replaces `{0}`), quoted as one
    argument.
  - `Text` (footer, e-mail subject, caption, clipboard text): all values, formatted in the UI language (today's
    `DateCultureMode.UILanguage`). Paths and file names keep the current culture, so migrated patterns give the same
    file names as 1.3.
- **Evaluate once**: `Render` returns plain text; nothing renders its own output again. The recipe engine only
  pre-resolves parameters that are not templates (see section 7), so steps get templates raw and render them when
  they know the target folder.
- **Errors**: unknown names and wrong use are found by `Validate` (settings UI, recipe validator, plugin settings).
  At run time a missing value is empty (or the `??` fallback) and logged once at debug level.
- `FilenameHelper` keeps only the safe-name helpers (`MakeFilenameSafe`, `MakePathSafe`, `IsFilenameValid`); its
  pattern methods go, and plugins call `IVariableResolver` (plugin API may change with the core, no adapter).

---

## 5. Counters

| Name | Scope | Stored |
|---|---|---|
| `${counter.global}` | global, replaces `${NUM}` | new setting `FileCounter` (Core), value taken from `OutputFileIncrementingNumber` once |
| `${counter.folder}` | the folder the file goes to: next free number there | not stored: the highest number in that folder's files made from the same template, plus one |
| `${counter.session}` | the active work session | in the session (when sessions exist) |

- One render gives one value for a counter, even when the template uses it twice.
- In `Preview` the next value is shown and nothing changes. The global counter is only increased when the export
  really wrote the file (the File destination reports success), not when a name is proposed.
- `counter.folder` needs no state, works on a shared network folder for several people, and fills the gap when
  files are deleted. The template is turned into a match pattern (literal parts escaped, `${counter.folder}` = the
  number, other values = anything); the file is created with `FileMode.CreateNew`, and when that fails because
  someone took the number, the next number is tried.
  - Other values match anything, so there is one sequence per folder whatever the title: with
    `${payload.title} ${counter.folder:000}.png` and `Notepad 001.png`, `Chrome 002.png` the next is `Paint 003.png`.
  - Only numbers with exactly the format's width (`000` = 3 digits) and files with an image extension Greenshot writes
    count, so an unrelated `Report 2024.png` is unlikely to match.
  - A count per title puts the title into the folder part: `${payload.title}\Shot ${counter.folder:000}.png`.
- Default width comes from the format: `${counter.global:000000}` is today's `${NUM}` (6 digits).
- Named counters (`${counter.named('bugs')}`) only if someone asks.

---

## 6. Capture sessions and context

Planned work sessions: the session is explicit and sticky, detection only
suggests. The resolver gives it a home now: `session.*` is a provider that returns nothing until sessions exist, so
templates like `Pictures\Greenshot\${session.key ?? 'unsorted'}\${payload.time:yyyy-MM-dd HH_mm_ss}` can be
validated today and work when sessions land. Session values are untrusted (typed or taken from window titles), so
they can name a sub folder below the configured folder (purpose `FileName`) but never be the folder itself.

Context rules' `output` (`"key": "${repo}#${id}"`) use the same resolver with a provider for the regex groups.

---

## 7. Recipes (beta: breaking is fine)

- New contract data type `Template` (with a purpose: `FileName`, `Text`), e.g. `FilenamePattern`,
  `ClipboardCustomText`, annotation texts. The engine validates these at load and passes them raw; the step renders
  them once with its scope. This fixes problem 1 and 3. Other parameters are still resolved by the engine as today.
- `DirectoryPath` parameters get purpose `Folder`: same rule as `DirectoryPath` options (fixes problem 4); the
  approval window shows the rendered folder.
- Renames: `payload.width/height/title/hdr` stay, `payload.filename` → `payload.file`, `payload.text`/`payload.extractedtext`/`${ocr_text}` → `payload.ocr.text`,
  `context.X` and bare `${X}` → `var.X` (`${Window}`, `${Ocr}`, `${SelectedMode}` in `AiToolRecipes.cs:62,139` and the
  docs), `now:fmt` / `utcnow:fmt` → `time.now:fmt` / `time.utc:fmt`, `config.X` → `config.Core.X`, ternaries need parentheses. RecordVideo `{yyyyMMdd_HHmmss}` →
  `${payload.time:yyyyMMdd_HHmmss}`.
- Recipe format 3.0; a 2.x file is rejected with a clear error like 1.x is today, and the validator names the
  replacement (`${title} is now ${payload.title}`). Built-in recipes, docs (`capture-recipes.md`) and the
  greenshot-recipes repository are updated in the same change.

---

## 8. Settings and migration

Released 1.3 settings change meaning, so each gets a new key (never reuse a key). At first start of 1.4, when the new
key is absent, the old value is converted once and the result logged; the old key stays untouched for a downgrade to
1.3 and is no longer written.

| Old key (section) | New key | Conversion |
|---|---|---|
| `OutputFilePath` (Core) | `OutputDirectory` | purpose `Folder`; `${MyPictures}` → `${user.pictures}`, `${X}` → `${env.X}` |
| `OutputFileFilenamePattern` (Core) | `OutputFileNameTemplate` | purpose `FileName`; mapping below |
| `OutputFileIncrementingNumber` (Core) | `FileCounter` | value copied |
| `OutputPrintFooterPattern` (Core) | `OutputPrintFooterTemplate` | purpose `Text` |
| `EmailSubjectPattern` (Office) | `EmailSubjectTemplate` | purpose `Text` |
| `Commandline`, `Argument`, `WorkingDirectory` (ExternalCommand) | `CommandlineTemplate`, `ArgumentTemplate`, `WorkingDirectoryTemplate` | `%X%` → `${env.X}`, `{0}` → `${output.file}` |

Not released, changed in place (no migration): Stamps `LibraryPath` (`%X%` → `${...}`, purpose `Folder`), recipe
parameters and options.

Old pattern → new:

| 1.3 | 1.4 |
|---|---|
| `${capturetime}` / `${capturetime:d"fmt"}` | `${payload.time:yyyy-MM-dd HH-mm-ss}` / `${payload.time:fmt}` |
| `${now:d"fmt"}` | `${time.now:fmt}` |
| `${YYYY}` `${MM}` `${DD}` `${hh}` `${mm}` `${ss}` | `${payload.time:yyyy}` ... `${payload.time:ss}` |
| `${NUM}` / `${NUM:p-2,0}` | `${counter.global:000000}` / `${counter.global:00}` |
| `${RRR}` | `${random(3)}` |
| `${title}` / `${title:s0,10}` / `${title:ra,b}` | `${payload.title}` / `${substring(payload.title,0,10)}` / `${replace(payload.title,'a','b')}` |
| `${user}` `${domain}` `${hostname}` | `${user.name}` `${user.domain}` `${machine.name}` |
| `${MyPictures}` `${MyDocuments}` `${Desktop}` `${ApplicationData}` `${LocalApplicationData}` `${MyMusic}` | `${user.pictures}` ... (`MyMusic` → `user.music`) |
| `${source}` `${file}` | `${payload.source}` `${payload.file}` |
| `${dirname}` `${filename}` / `${basename}` `${extension}` | `${folder(payload.file)}` `${fileNameWithoutExtension(payload.file)}` `${extension(payload.file)}` |
| `${<other>}` | `${env.<other>}` |

The converter is a separate class with tests: the 1.3 defaults (`${capturetime:d"yyyy-MM-dd HH_mm_ss"}-${title}`,
the footer default) must give the same text as 1.3 for the same capture.

---

## 9. UI

One WPF `TemplateBox` control in `Greenshot.Base.Wpf` (shared style), with the purpose as a property:

- live preview below the box (rendered with `Preview`, sample capture), red message from `Validate`;
- an "Insert variable" menu built from `Describe(purpose)`, so a `Folder` box only offers trusted values;
- the help window lists the same descriptors (one language key per variable description), replacing the
  hand-written `message_filenamepattern` text (new keys, the old one is removed).

Used in Settings > Output (folder, file name), print footer, Outlook subject, ExternalCommand settings, Stamps folder,
recipe editor template parameters and recipe options with format `template`. Keyboard and screen reader rules from
the accessibility guide apply (the preview is a live region).

---

## 10. Work and order

Planned as one PR for the whole change. Suggested order inside it:

1. `Greenshot.Base.Expressions`: parser (format suffix, `??`, functions, `$${`), providers, purposes, trust, tests.
2. Counters (store, preview, commit after write, `counter.folder` with `CreateNew`), tests on a temp folder.
3. Migration converter + new setting keys, tests with the 1.3 defaults.
4. Switch every caller in section 1 (core, destinations, plugins), remove the `FilenameHelper` pattern methods and
   `ExpandEnvironmentVariables` for user input.
5. Recipes: `Template` type, `Folder` rule for `DirectoryPath` parameters, renames, format 3.0, validator messages,
   built-ins, docs, greenshot-recipes.
6. `TemplateBox` + help from descriptors, language keys.
7. Docs: `docs/vNext/variables.md` (generated table of names), `capture-recipes.md`.

It is big. If it should be split, 1-5 first and the UI (6) after works, since the settings UI keeps working with
plain text boxes plus validation.

Sessions and profiles only get their namespace now; they fill it when they are built.

---

## 11. Open decisions

1. ~~Format syntax~~ decided: format suffix `${x:fmt}` (may be quoted), a choice `? :` needs parentheses, standard
   formats (`D`, `T`, ...) preferred for texts meant for people. Compared with `if()`, pipes and a `format()` function.
2. ~~Environment variables~~ decided: `env.X` generic, `user.env.X` / `machine.env.X` on Windows, no fallback in `user.*`.
3. `counter.folder` from the folder's files, no state (recommended) vs. a stored counter per folder.
4. Global counter increases only after a successful write (recommended) vs. on every name, as in 1.3.
5. One PR vs. engine first, UI second.
