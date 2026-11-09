# External commands

An external command hands your capture to another program: an image editor, a converter, a script or an uploader. Each
command is a destination of its own ("External" + its name, like in Greenshot 1.3) and a recipe uses it by its name in the
`ExternalCommand` step ([capture-recipes.md](capture-recipes.md#external-command-step-in-depth)).

## The settings page

Settings > Plugins > External command lists the commands of the `Commands` folder ([the file](#the-file)) with the program
and its arguments. The check box of a row ("Offer ... as a destination") turns a command off and on, the built-in ones too: a
command which is off has no destination, it isn't in the menus or in the destination settings, and a recipe step which
names it fails. This applies at once and is kept in `DisabledCommands` of greenshot.ini.

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

The main part is what most commands need:

| Field | What it is | Recipe parameter |
|---|---|---|
| Name | The name in the menus and in recipes; unique | `Command` |
| Program | The program, e.g. `${user.localappdata}\Programs\tool.exe` ([variables.md](variables.md)). *Choose app...* lists the apps which open the image format ("Open with"), the programs Windows knows by name (App Paths) and the command-line names of Windows Apps (App Execution Aliases), with a search; *Browse...* picks any program. A Windows App (Store/MSIX) which opens the image format is listed by its app id, see [Windows Apps](#windows-apps) | `Program` |
| Arguments | Default `"${export.file}"`; nothing is quoted for you. A picked app gets the arguments of its open command. Greenshot saves the capture to a file only when the arguments (or the working folder, or the text on standard input) use `${export.file}` | `Arguments`, `File` (`New` or `None`) |
| Image format | The format of the file and of the image on standard input | `Format` |
| Run in background | Greenshot doesn't wait for the program to close, for an app which stays open like an image editor; what it prints isn't read. A picked app gets it | `RunInBackground` |
| Afterwards | Nothing; show a link it prints in the notification; copy what it prints; copy the link it prints | `Output` (`None`, `Text`), `CopyToClipboard` |

*For command-line tools*, open when one of them isn't the default:

| Field | What it is | Recipe parameter |
|---|---|---|
| Use the file you saved last | The file the capture was saved to, instead of a new one (when there is one) | `File` `LastExport` |
| Send to the program's input | Nothing; the capture, for a tool which reads it when it gets `-` instead of a file name; a text, with variables, e.g. `${payload.title}` (nothing is quoted or split, so values with spaces or quotes are safe). `${payload.ocr.text}` only has the text in a recipe, after its OCR step | `StandardInput`, `StandardInputText` |
| Working folder | Empty: the folder of the capture file. Only needed for a program which looks for its own files in the current folder | `WorkingDirectory` |
| Exit codes which mean success | Empty means `0`; several are separated by commas, e.g. `0,1` | `SuccessExitCodes` |
| Timeout | Seconds, 0: no limit | `Timeout` |
| Encoding of what it prints | Empty means `utf-8`; `oem` for old console tools, or an encoding name | `Encoding` |

A new image or the changed file (`Output` `Image`, `File`) only means something in a recipe, which sets it in its step. The
editor doesn't offer it; an imported command which has it keeps it and the editor says so.

Save checks the command like an imported file and tells what doesn't fit, e.g. a command in the background which should give
a new image.

## Windows Apps

A Windows App (from the Microsoft Store or an MSIX package) often has no program path which stays the same: its folder
changes with every update. Such an app is written as `shell:AppsFolder\<app id>`, e.g.
`shell:AppsFolder\dotPDNLLC.paint.net_h55e3w7q8jbva!dotPDNLLC.paint.net`, the same as a shortcut to the app. Greenshot
then asks Windows to open the file with that app, as "Open with" does; a packaged desktop app (like Paint.NET), which
doesn't take that, is started with the file as its argument. The app only gets the file: Windows starts it, so Greenshot
has no process to wait for or read from. The command always runs in the background (the editor shows the check box set
and locked), without standard input, output or exit code, and the arguments aren't used. A command file which asks for
more can't be imported.

## Commands which come with Greenshot

MS Paint and Paint.NET are there when they are installed. Paint.NET from the Microsoft Store is found too. As in Greenshot
1.3 they get the file the capture was saved to, so what is changed in the editor stays; a capture which wasn't saved is
handed over as a copy, which Greenshot removes later. They can be
exported (e.g. to change a copy and import it under another name) or turned off, but not changed or removed. Deleting MS Paint or Paint.NET in Greenshot 1.3 (`DeletedBuildInCommands`) still
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
