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

In the recipe editor, the toolbar button "${ } Variables" (or F1) lists everything the open recipe can use: its
options, its variables, the values below and the functions, with a search; Enter copies the selected line.

`%NAME%` is gone, use `${env.NAME}`. Settings of 1.3 are converted once (see below).

## Names

| Namespace | Values |
|---|---|
| `time.*` | `now` (local time now), `utc` (UTC now) |
| `payload.*` | what is being worked on (a capture, an opened file, the clipboard): `width`, `height` (pixels, now), `time` (when it was taken, for an opened file the time stored in it, else the file's time), `source` (Screen, Window, Clipboard, file), `hdr` (Available, Lost, None), `title`, `file`, `folder` (the folder of an opened file, e.g. to save the result next to it), `url`, `ocr.text`, `video.file` |
| `counter.*` | `global`, `folder`, `name`, `scope(key)`, see Counters |
| `output.*` | the export running now: `file` (full path of the written file), `folder`, `name`, `url` (link of an upload) |
| `user.*` | `name`, `domain`, `profile`, `desktop`, `documents`, `pictures`, `videos`, `music`, `appdata`, `localappdata`, `temp`; the folders are found also when they were moved (e.g. to OneDrive) |
| `machine.*` | `name`, `os`, `osversion`, `processors`, `is64bit`, `programfiles`, `commonappdata`, `systemdir` |
| `env.NAME` | an environment variable of Greenshot's process (what `%NAME%` was) |
| `user.env.NAME`, `machine.env.NAME` | Windows only: the user's or the machine's environment variable |
| `config.<section>.<key>` | a setting, e.g. `${config.Core.OutputFileFormat}`; read only, settings with secrets are left out |
| `option.*` | recipe options |
| `var.*` | recipe variables: trigger values (`var.Window`), `SetVariable`, step outputs (`var.Destination.Filename`) |
| `recipe.*` | `id`, `name`, `executionId`, `state`, `abortReason` of the running recipe |

`session.*` and `profile.*` are reserved. Plugins add their own namespace.

## Functions

| Group | Functions |
|---|---|
| Text | `left(t, n)`, `right(t, n)`, `substring(t, start, length?)`, `replace(t, old, new)`, `lower(t)`, `upper(t)`, `trim(t)`, `padLeft(t, n, c?)`, `padRight(t, n, c?)`, `length(t)`, `contains(t, x)`, `startsWith(t, x)`, `endsWith(t, x)` |
| Patterns | `match(t, pattern, group?)`, `replaceMatch(t, pattern, new)` (.NET regular expressions, 250 ms limit) |
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
| Argument | the arguments of an external command | only trusted values and `${output.file}`; nothing is quoted for you, write `"${output.file}"` |
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
| `"${output.file}"` | the argument of an external command |

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
| `{0}` in command arguments | `${output.file}` |

## Recipes of an earlier 1.4 beta

Recipes are not converted: the recipe format stays 2.0, while recipes are experimental. A name of an earlier beta is an
error in the validator and the recipe editor, so change it by hand: bare names become `var.*`
(`${Barcode.Text}` → `${var.Barcode.Text}`), `context.X` becomes `var.X`, `${now:...}` becomes `${time.now:...}`,
`${ocr_text}` and `payload.extractedtext` become `${payload.ocr.text}`, `${config.X}` becomes `${config.Core.X}`, a
choice needs parentheses, and file name patterns use the names of the table above.
