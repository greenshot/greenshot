# External commands

An external command hands your capture to another program: an image editor, a converter, a script or an uploader. Each
command is a destination of its own ("External" + its name, like in Greenshot 1.3) and a recipe uses it by its name in the
`ExternalCommand` step ([capture-recipes.md](capture-recipes.md#external-command-step-in-depth)).

## The settings page

Settings > Plugins > External command lists the commands of the `Commands` folder ([the file](#the-file)) with the program
and its arguments.

| Button | What it does |
|---|---|
| New | Opens the editor for a new command |
| Import | Reads a command file (`.gscommand.json`), shows the program, its arguments and the text it gets, and adds it after you confirm. Cancel is the default |
| Edit | Opens the editor (also with a double click) |
| Export | Writes the command to a `.gscommand.json` file, to share it or to keep it |
| Approve | Only for a command file which was added or changed outside Greenshot: shows the program, its arguments and the text it gets, and uses the command after you confirm (Greenshot asks the same when it starts) |
| Delete | Removes the command |

A changed or approved command is used at once. A new or removed command shows up in the menus after Greenshot is restarted. A command
whose program isn't found when Greenshot starts (a USB stick, a network drive) stays in the settings and is only left out of
the menus until then.

### The editor

| Field | What it is | Recipe parameter |
|---|---|---|
| Name | The name in the menus and in recipes; unique | `Command` |
| Program | The program, e.g. `${user.localappdata}\Programs\tool.exe` ([variables.md](variables.md)) | `Program` |
| Arguments | Default `"${export.file}"`; nothing is quoted for you | `Arguments` |
| Working folder | Default the folder of the file | `WorkingDirectory` |
| File | A new file; the saved file (the file the capture was saved to, else a new one); no file | `File` (`New`, `LastExport`, `None`) |
| Image format | The format of the new file and of the image on standard input | `Format` |
| Standard input | Nothing, the image, or a text | `StandardInput`, `StandardInputText` |
| Output | Not read; text (a link in it is the link of the notification); a new image or the changed file (only in recipes) | `Output` |
| Clipboard | With a text output: the output or the link in it | `CopyToClipboard` |
| Success exit codes | e.g. `0,98,99` | `SuccessExitCodes` |
| Timeout | Seconds, 0: no limit | `Timeout` |
| Encoding | `utf-8`, `oem` or an encoding name | `Encoding` |
| Run in background | Greenshot doesn't wait for the command; its output isn't read | `RunInBackground` |

Save checks the command like an imported file and tells what doesn't fit, e.g. a command in the background which should give
a new image.

## Commands which come with Greenshot

MS Paint and Paint.NET are there when they are installed. They can be exported (e.g. to change a copy and import it under
another name) but not changed or removed. Deleting MS Paint or Paint.NET in Greenshot 1.3 (`DeletedBuildInCommands`) still
hides it.

## The commands of Greenshot 1.3

The first time, the commands of Greenshot 1.3 are taken over: program, arguments (`{0}` becomes `"${export.file}"`), image
format, background, and the output settings (reading the output for the clipboard or the link). They get the saved file
(`File` `LastExport`) like before. A command with the name and program of a built-in one isn't taken over; one with the name
of a built-in one but another program is kept as "name (1.3)". The old keys stay in greenshot.ini, so Greenshot 1.3 still
finds its commands. The setting "Show standard output in Greenshot log" is gone.

## The file

A command file is JSON with the fields of the editor; only `kind`, `name` and `program` are needed:

```json
{
  "kind": "externalCommand",
  "version": 1,
  "name": "Upload to my server",
  "program": "${machine.systemdir}\\curl.exe",
  "arguments": "-s -F \"file=@${export.file}\" https://example.com/upload",
  "format": "png",
  "output": "text",
  "copyToClipboard": "url"
}
```

Each command is a file of this format in the `Commands` folder next to greenshot.ini (in `%APPDATA%\Greenshot`, or next to
Greenshot.exe for a portable install), named after the command, plain JSON (a command holds no secrets). The folder is
read once, when Greenshot starts: a file copied into it, changed or removed there while Greenshot runs has no effect. Like
recipe files, Greenshot only uses the files it wrote itself (New, Import, Edit, the commands of 1.3): a file somebody put
into the folder is ignored, also after a restart, until you import it. A command file of Greenshot which was changed outside
Greenshot isn't used after the next start (no destination, recipes can't use it) until you approve it: Greenshot asks once
it has started, with the program, its arguments and the text it gets, and uses the command at once when you choose
*Approve*. With *Not now* it stays unused, is listed as "Not approved" in the settings (where you can approve it) and
Greenshot asks again at the next start. Each file is checked like an imported
file; one which doesn't pass is listed as "can't be read" and isn't used. Only import or approve commands from someone you
trust: the program runs with your rights.

A command which is switched off on the Destinations tab isn't used by recipes either: an `ExternalCommand` step which names
it is skipped, with a warning in the log of the flow.
