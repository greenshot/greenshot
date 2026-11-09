# Variables in file names, folders, commands and recipes

Everywhere Greenshot takes a text with values in it (the screenshot folder, the file name, the print footer, the
Outlook subject, external commands, the Stamps library folder, recipe parameters) the same syntax and the same names
work. The design and the reasons are in [expression-engine.md](expression-engine.md).

## Syntax

| Text | Gives |
|---|---|
| `${payload.title}` | a value |
| `${payload.time:yyyy-MM-dd}` | a value with a .NET format (dates, numbers), everything after the first `:` |
| `${time.now:'HH:mm:ss'}` | a format may be quoted, always quote one with `:` in it |
| `${payload.title ?? 'no title'}` | a fallback when the value is missing or empty |
| `${payload.width > 1000 && option.big}` | arithmetic, comparisons, `&&`, `\|\|`, `!` |
| `${(payload.width > 1000 ? 'big' : 'small')}` | a choice needs parentheses, because `:` starts the format |
| `${left(payload.title, 40)}` | a function |
| `$${` | a literal `${` |

A value which isn't known is missing: it gives an empty text. A name which doesn't exist, a wrong function or a wrong
number of arguments is an error, shown where the text is typed (settings, recipe editor) and in the log.

The "?" next to the file name in the settings lists the values and functions a file name can use, with a search; Enter
copies the selected line. In the recipe editor, the toolbar button "${ } Variables" (or F1) shows the same list with the
options and variables of the open recipe. Under the folder, file name and footer boxes the settings show what the text
gives now (with the title of the settings window as the sample window title), or what is wrong with it. A folder picked
with "..." in a known folder is written with its value, e.g. `${user.pictures}\Greenshots`.

`%NAME%` is gone, use `${env.NAME}`. Settings of 1.3 are converted once (see below).

## Names

| Namespace | Values |
|---|---|
| `time.*` | `now` (local time now), `utc` (UTC now) |
| `payload.*` | what is being worked on (a capture, an opened file, the clipboard): `width`, `height` (pixels, now), `time` (when it was taken, for an opened file the time stored in it, else the file's time), `source` (Screen, Window, Clipboard, file), `hdr` (Available, Lost, None), `title`, `file`, `folder` (the folder of an opened file, e.g. to save the result next to it), `url`, `ocr.text`, `video.file` |
| `counter.*` | `global`, `folder`, `name`, `scope(key)`, see Counters |
| `export.*` | the export running now: `file` (full path of the written file), `folder`, `name`, `url` (link of an upload) |
| `user.*` | `name`, `domain`, `profile`, `desktop`, `documents`, `pictures`, `videos`, `music`, `appdata`, `localappdata`, `temp`; the folders are found also when they were moved (e.g. to OneDrive) |
| `machine.*` | `name`, `os`, `osversion`, `processors`, `is64bit`, `programfiles`, `commonappdata`, `systemdir` |
| `env.NAME` | an environment variable of Greenshot's process (what `%NAME%` was) |
| `user.env.NAME`, `machine.env.NAME` | Windows only: the user's or the machine's environment variable |
| `config.<section>.<key>` | a setting, e.g. `${config.Core.OutputFileFormat}`; read only, settings with secrets are left out |
| `option.*` | recipe options |
| `var.*` | recipe variables: trigger values (`var.Window`), `SetVariable`, step outputs (`var.Destination.Filename`) |
| `recipe.*` | `id`, `name`, `executionId`, `state`, `abortReason` of the running recipe |

`session.*` and `profile.*` are reserved. Plugins add their own namespace.

### All values

Every value of Greenshot itself (plugins add their own). *There* says when it has a value, otherwise it is missing (an
empty text). *Trusted* values may choose a folder or a program (see Where a text is used below).

<!-- values:start (generated from the code by VariableDocumentation.ValueTable, a test compares it) -->
| Value | Type | There | Trusted | Description |
|---|---|---|---|---|
| `config.<section>.<key>` | text | always; settings with secrets are left out | yes | A setting of greenshot.ini as config.&lt;section&gt;.&lt;key&gt;, e.g. config.Core.OutputFileFormat |
| `counter.folder` | number | file names of a file written to a folder (not uploads) | yes | The next free number in the folder the file goes to, read from its files |
| `counter.global` | number | file names | yes | One number for all files, stored in greenshot.ini; goes up when a file was written or uploaded |
| `counter.name` | number | file names of a file written to a folder (not uploads) | yes | The next free number among the files of the folder with the same name apart from the number |
| `env.<name>` | text | when the variable is set | yes | An environment variable of Greenshot's process (as %NAME% in Windows) |
| `export.file` | text | while an export writes a file | yes | The full path of the file written by this export |
| `export.folder` | text | while an export writes a file | yes | The folder of the file written by this export |
| `export.name` | text | while an export writes a file | yes | The file name with extension of the file written by this export |
| `export.url` | text | after an upload | yes | The link of the upload |
| `machine.commonappdata` | text | always | yes | The application data folder of all users |
| `machine.env.<name>` | text | Windows, when the variable is set | yes | An environment variable of the machine (Windows only) |
| `machine.is64bit` | true/false | always | yes | Is the operating system 64 bit |
| `machine.name` | text | always | yes | The computer name |
| `machine.os` | text | always | yes | The operating system: Win32NT, Unix, ... |
| `machine.osversion` | text | always | yes | The version of the operating system |
| `machine.processors` | number | always | yes | The number of processors |
| `machine.programfiles` | text | always | yes | The program files folder |
| `machine.systemdir` | text | always | yes | The system folder |
| `option.<key>` | any | in a recipe with that option | yes | A recipe option, as set in Settings &gt; Recipes |
| `payload.file` | text | an opened file | no | The full path of an opened file |
| `payload.folder` | text | an opened file | yes | The folder of an opened file, e.g. to save the result next to it |
| `payload.hdr` | text | in a recipe | yes | Available (can be saved as HDR), Lost (an edit removed it) or None |
| `payload.height` | number | a capture or an opened file | yes | The height of the image in pixels (now) |
| `payload.ocr.text` | text | after an OCR or barcode step | no | The text found by OCR or a barcode |
| `payload.source` | text | a capture or an opened file | yes | Where it came from: Screen, Window, Clipboard or file |
| `payload.time` | date and time | always (the time now without a capture) | yes | When it was taken; for an opened file the time stored in the file, else the file's time |
| `payload.title` | text | a window capture, or an opened file with a title | no | The window title, or the title stored in an opened file |
| `payload.url` | text | a capture with a web address (browser extension, opened file) | no | The web address stored with the capture |
| `payload.video.file` | text | after a video recording | yes | The file of a video recording |
| `payload.width` | number | a capture or an opened file | yes | The width of the image in pixels (now, after cropping or resizing) |
| `recipe.abortReason` | text | in a recipe | yes | Why this run was stopped |
| `recipe.executionId` | text | in a recipe | yes | The id of this run |
| `recipe.id` | text | in a recipe | yes | The id of the running recipe |
| `recipe.name` | text | in a recipe | yes | The name of the running recipe |
| `recipe.state` | text | in a recipe | yes | The state of this run |
| `time.now` | date and time | always | yes | The time now (local time) |
| `time.utc` | date and time | always | yes | The time now in UTC |
| `user.appdata` | text | always | yes | The roaming application data folder |
| `user.desktop` | text | always | yes | The desktop folder |
| `user.documents` | text | always | yes | The documents folder |
| `user.domain` | text | always | yes | The domain of the user |
| `user.env.<name>` | text | Windows, when the variable is set | yes | An environment variable of the user (Windows only) |
| `user.localappdata` | text | always | yes | The local application data folder |
| `user.music` | text | always | yes | The music folder |
| `user.name` | text | always | yes | The user name |
| `user.pictures` | text | always | yes | The pictures folder |
| `user.profile` | text | always | yes | The user's profile folder |
| `user.temp` | text | always | yes | The temporary folder |
| `user.videos` | text | always | yes | The videos folder |
| `var.<name>` | any | in a recipe, once set | no | A recipe variable: set by a trigger, a SetVariable step or the output of a step |
<!-- values:end -->

## Functions

| Group | Functions |
|---|---|
| Text | `left(t, n)`, `right(t, n)`, `substring(t, start, length?)`, `replace(t, old, new)`, `lower(t)`, `upper(t)`, `trim(t)`, `padLeft(t, n, c?)`, `padRight(t, n, c?)`, `length(t)`, `contains(t, x)`, `startsWith(t, x)`, `endsWith(t, x)` |
| Patterns | `match(t, pattern, group?)`, `replaceMatch(t, pattern, new)` (.NET regular expressions, 250 ms limit) |
| Web addresses | `urlEncode(t)` (for a part of a web address, e.g. `https://crm.example.com/customers/${urlEncode(var.Found)}`) |
| Paths | `fileName(p)`, `fileNameWithoutExtension(p)`, `extension(p)`, `folder(p)`; they never touch the disk |
| Numbers | `round(n, digits?)`, `floor(n)`, `ceiling(n)`, `min(a, b)`, `max(a, b)`, `abs(n)` |
| Date and time | `addDays(d, n)`, `addHours(d, n)`, `addMinutes(d, n)`, `isoWeek(d)`, `date(text, format)` |
| Conversion | `number(x)`, `text(x)`, `bool(x)`, `empty(x)`, `format(x, format)` |
| Random | `random(n)` (n letters and digits), `guid()` |

## Where a text is used

Each place says what the text is for. That decides what it may use and how values are written:

| Used as | Examples | Rules |
|---|---|---|
| Folder | screenshot folder, `SaveDirectory`, Stamps library, working folder of a command | only trusted values: nothing of the capture (title, OCR, file, url) and no recipe variable can choose a folder |
| File name | file name template, `FilenamePattern` | values are made file name safe (`\ / : * ? " < > \|` become `_`, at most 80 characters); a `\` typed in the template makes a sub folder |
| Command | the program of an external command | only trusted values |
| Argument | the arguments of an external command | only trusted values and `${export.file}`; nothing is quoted for you, write `"${export.file}"` |
| Text | print footer, Outlook subject, clipboard text, captions | everything; dates in the language of Greenshot |
| Condition, Value | recipe `when`, parameters | everything; a single `${...}` keeps its type (number, true/false) |

## Counters

| Name | Counts |
|---|---|
| `${counter.global:000000}` | one number for all files (what `${NUM}` was), stored in greenshot.ini |
| `${counter.folder:000}` | the next number in the folder the file goes to: the highest number of the files there made from the same template, plus one; nothing is stored, so it works on a shared folder and fills gaps |
| `${counter.name:000}` | like `counter.folder`, but per name: `Notepad 001`, `Notepad 002`, `Chrome 001` |
| `${counter.scope('bugs')}` | a stored count per key, the key is an expression; stored in `greenshot-counters.txt` next to greenshot.ini, keys unused for 90 days are removed |

- A counter only goes up when the file was written or uploaded. A preview (the settings, the Save as proposal) shows
  the next number and changes nothing.
- One file name gives one value per counter, also when it is used twice.
- `counter.folder` and `counter.name` need fixed text next to them (` `, `-`, `_`), and a folder: uploads (Jira, Box,
  Dropbox, ...) use `counter.global` or `counter.scope(...)`.
- There is no lock: two files saved at the same moment into the same folder (by two Greenshots, e.g. on a shared
  folder) can get the same `counter.folder` number. Use `counter.scope(...)` when that must never happen.
- Counters can only be used in file names.

## Examples

| Template | Gives |
|---|---|
| `${payload.time:yyyy-MM-dd HH_mm_ss}-${payload.title}` | `2026-11-06 19_28_03-Notepad.png` (the default) |
| `${payload.time:yyyy-MM-dd}\Shot ${counter.folder:000}` | a folder per day, numbered from 001 in each |
| `${user.pictures}\Greenshot` | the screenshot folder in Pictures, also when Pictures is in OneDrive |
| `${payload.time:D} ${payload.time:T} - ${payload.title}` | the print footer (the default), in the language of Greenshot |
| `"${export.file}"` | the argument of an external command |

## Settings of 1.3

The 1.3 settings stay as they are, so going back to 1.3 keeps working. When a new setting is empty, its 1.3 setting is
converted once:

| 1.3 setting | New setting |
|---|---|
| `OutputFilePath` | `OutputDirectory` |
| `OutputFileFilenamePattern` | `OutputFileNameTemplate` |
| `OutputPrintFooterPattern` | `OutputPrintFooterTemplate` |
| ExternalCommand `Commandline`, `Argument` | `CommandlineTemplate`, `ArgumentTemplate` |
| Office `EmailSubjectPattern` | `EmailSubjectTemplate` |

A 1.3 setting fixed by an administrator in greenshot-fixed.ini is converted on every start.

| 1.3 | Now |
|---|---|
| `${title}` | `${payload.title}` |
| `${capturetime}`, `${capturetime:d"yyyy-MM-dd"}` | `${payload.time:yyyy-MM-dd HH-mm-ss}`, `${payload.time:yyyy-MM-dd}` |
| `${YYYY}`, `${MM}`, `${DD}`, `${hh}`, `${mm}`, `${ss}` | `${payload.time:yyyy}`, ... |
| `${NUM}` | `${counter.global:000000}` |
| `${RRR}` | `${random(3)}` |
| `${user}`, `${domain}`, `${hostname}` | `${user.name}`, `${user.domain}`, `${machine.name}` |
| `${MyPictures}`, `${MyDocuments}`, `${Desktop}` | `${user.pictures}`, `${user.documents}`, `${user.desktop}` |
| `${NAME:p5,0}`, `${NAME:s0,10}`, `${NAME:rold,new}` | `padLeft(...)`, `substring(...)`, `replace(...)` |
| `${dirname}`, `${filename}`, `${extension}` (an opened file) | `${payload.folder}`, `${fileNameWithoutExtension(payload.file)}`, `${extension(payload.file)}` |
| any other `${NAME}`, `%NAME%` (commands) | `${env.NAME}` |
| `{0}` in command arguments | `${export.file}` |

## Recipes of an earlier 1.4 beta

Recipes are not converted: the recipe format stays 2.0, while recipes are experimental. A name of an earlier beta is an
error in the validator and the recipe editor, so change it by hand: bare names become `var.*`
(`${Barcode.Text}` → `${var.Barcode.Text}`), `context.X` becomes `var.X`, `${now:...}` becomes `${time.now:...}`,
`${ocr_text}` and `payload.extractedtext` become `${payload.ocr.text}`, `${config.X}` becomes `${config.Core.X}`, a
choice needs parentheses, and file name patterns use the names of the table above.
