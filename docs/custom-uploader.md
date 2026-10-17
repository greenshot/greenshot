# Custom uploaders

Upload your captures to a service of your choice: a picture host, your own server, a Discord channel. The service
describes how to upload in an uploader file (`.sxcu`), the format of ShareX custom uploaders, and Greenshot reads the same
files. The plugin is `Greenshot.Plugin.CustomUploader`, a **beta** feature of the full Greenshot (not in Greenshot Light).

- [For users](#for-users): turn it on, import an uploader, upload, delete an upload
- [For developers](#for-developers): how it works, what is supported, the rules, tests

---

## For users

### Turn it on

The custom uploaders are only there for beta testers: in the settings, *Expert*, check *Enable to enable beta-test
features* (`BetaTester=True` in `[Core]` of greenshot.ini) and restart Greenshot. The installer installs the plugin with the full and
custom installation, it is listed as *Custom uploader plug-in (beta)*.

### Get an uploader file

An uploader file comes from the service you want to upload to:

- Many picture hosts and file hosts offer a *ShareX* download on their settings or API page, usually with your key in it.
- A self-hosted upload server (e.g. Zipline, XBackBone, Chibisafe) creates one for your account.
- For a Discord channel: create a webhook in the settings of the channel (*Integrations*), and make an uploader for its
  address in ShareX.
- ShareX exports one for each custom uploader you made in it (*Destinations*, *Custom uploader settings*, *Export*).

The file can hold the key to your account, or the webhook address of your channel: whoever has the file can upload as
you. Don't share it, and only use files of a service you trust.

### Import it

In the settings, *Plugins*, select *CustomUploader* and *Configure*, then *Import...* and choose the `.sxcu` file.

Greenshot first checks the file and refuses it when it can't upload safely with it (the message says why, see
[What Greenshot refuses](#what-greenshot-refuses)). Then it asks: it shows the name of the uploader, the server your
captures go to, and the names of the headers which go with them (often *Authorization*, the key). The values aren't shown.
The *Import* button reacts after a moment, Enter and Escape cancel. Nothing is stored before you click *Import*.

An uploader with the same name as one you already have replaces it, Greenshot says so in the question.

The uploader is stored in greenshot.ini, encrypted for your Windows account: another user, or the same greenshot.ini on
another computer (e.g. the portable Greenshot on a USB stick), can't read it. The list then shows *Can't be read on this
Windows account, import it again*: remove it and import the file again.

### Upload

Choose *Custom uploader* as destination: in the destination picker after a capture, in the editor, or as a default
destination in the settings. With more than one uploader, *Custom uploader* has a sub menu with each of them. The
destination is only there when you imported an uploader.

Greenshot uploads the capture in your output file format (PNG when that is the Greenshot format), shows the progress, and
puts the link to the upload on the clipboard. The notification shows the link too. When the upload fails, the notification
says why: the error of the service when the uploader reads it, otherwise the status (e.g. *403 Forbidden*).

In a recipe, use the step *Upload with a custom uploader* (`CustomUploader`) with the name of the uploader in `Uploader`
(empty when you have only one). The link is in the variable `CustomUploader.Url`. Like every step which sends data, the
recipe asks before it runs the first time: it names the uploader and its server. When you import the uploader again with
another server, the recipe asks again.

### Your uploads

The settings of the plugin list your last 100 uploads, newest first: the date, the uploader and the link.

- *Open* opens the upload in the browser.
- *Delete...* opens the delete link of the upload in the browser, after asking. Only uploaders whose service returns a
  delete link (`DeletionURL` in the file) have it. What happens then is up to the service, most show a page which deletes
  the upload.
- *Forget* removes the upload from the list, the upload stays on the service. Without the delete link Greenshot can't
  delete it later.

The list is stored encrypted like the uploaders, the delete links are secrets too.

### What Greenshot refuses

| The file... | Why |
|---|---|
| isn't an image or file uploader (a text uploader, URL shortener) | Greenshot uploads captures |
| sends without HTTPS (HTTP only to this computer, `localhost`) | Your capture and your key would be readable on the way |
| has a placeholder in the server part of the address (`https://{...}/upload`) | You approve the server, it must not change |
| has a user name or password in the address | Use the header the service names for the key |
| has a body other than *MultipartFormData* or *Binary* | Only these carry the file |
| uses `GET` or `DELETE` | Only `POST`, `PUT` and `PATCH` send a file |
| asks something or picks at random: `{prompt}`, `{inputbox}`, `{select}`, `{outputbox}`, `{random}` | You approve exactly what is sent where, without questions per upload |
| was made for ShareX before 13.7 (`$json:...$` placeholders) | Open it in a current ShareX and export it again |
| has a header with a line break or an invalid name | It could add headers you didn't see |
| is larger than 256 KB | Uploader files are a few KB |

---

## For developers

### The parts

Everything is in `src/Greenshot.Plugin.CustomUploader`:

| File | What it is for |
|---|---|
| `CustomUploaderPlugin.cs` | Registration (only for beta testers), the upload (`UploadAsync`: encode, progress, clipboard, keep the upload) and `SendAsync` without UI |
| `Sxcu/UploaderDefinition.cs` | The properties of an `.sxcu` file which Greenshot reads, with their ShareX names |
| `Sxcu/UploaderFile.cs` | Reads and checks a file: the import gate. `GetServer` / `DescribeServer` give what the user approves |
| `Sxcu/PlaceholderSyntax.cs` | The `{name:parameter\|parameter}` syntax of ShareX 13.7 and later |
| `Sxcu/UploaderRequest.cs` | The request: url with parameters, headers, multipart or binary body with `HttpContents` |
| `Sxcu/UploaderResponse.cs` | Link, thumbnail, deletion link and error from the response |
| `UploaderStore.cs`, `UserProtection.cs` | The uploaders and uploads in `[CustomUploader]`, each value encrypted with DPAPI for the Windows user |
| `Destinations/CustomUploaderDestination.cs` | *Custom uploader* with one dynamic destination per uploader |
| `Recipes/CustomUploaderStep.cs` | The recipe step, `Uploader` parameter, `CustomUploader.Url` output |
| `ViewModels/`, `Views/` | The settings page: import (with the confirmation), remove, the uploads |

HTTP goes through `HttpService.Current` like everywhere in Greenshot, see [http.md](http.md): the proxy, the certificate
rules and the logging rules apply.

### The flow

1. **Import** (settings, UI thread): `UploaderFile.Read` parses the JSON into `UploaderDefinition`, normalizes it (method,
   body of old files, name) and checks it. The view asks with `DescribeImport` and `ThemedMessageBox.ShowChoice` (Cancel is
   the default, the buttons are armed after a second). `UploaderStore.Add` stores the normalized JSON encrypted and saves
   greenshot.ini at once.
2. **Read**: `UploaderStore.GetUploaders` decrypts each uploader and runs it through `UploaderFile.Read` again, so a changed
   entry is checked like a file. One which can't be decrypted or doesn't pass has a null `Definition` and is only listed.
3. **Upload** (thread pool): `UploaderRequest.Create` fills the placeholders of the request and builds it, it refuses to send
   to another server than `GetServer`. `HttpService.SendAsync` sends it. `UploaderResponse.From` reads the result. Only
   http and https links are accepted, a link is opened and put on the clipboard.
4. **Keep**: the link and the deletion link go into the upload history (`UploaderStore.AddUpload`, the newest 100).

### Supported placeholders

| Placeholder | In the request (`RequestURL`, `Parameters`, `Headers`, `Arguments`) | In the results (`URL`, `ThumbnailURL`, `DeletionURL`, `ErrorMessage`) |
|---|---|---|
| `{filename}` | yes | yes |
| `{base64:text}` | yes | yes |
| `{response}` | | the response text |
| `{responseurl}` | | the url which answered, after redirects |
| `{header:name}` | | a response header |
| `{json:path}`, `{json:input\|path}` | | JSONPath, `$.` in front is optional (`data.link`, `[0].url`) |
| `{xml:xpath}`, `{xml:input\|xpath}` | | the text of the first node; no DTD, no external entities |
| `{regex:pattern}`, `{regex:pattern\|group}`, `{regex:input\|pattern\|group}` | | the match, or the group by number or name; 2 seconds at most |

A backslash escapes `{`, `}`, `|` and `\`; other backslashes stay (`\d` in a pattern). Values in `RequestURL` are url
encoded, the text around them isn't. Parameters are url encoded and added to the query. An empty `URL` means the response
is the link, like in ShareX.

Headers starting with `Content-`: a binary body takes `Content-Type` from them, a multipart body keeps its own (it carries
the boundary); `Content-Length` is always computed.

### Rules

- **Nothing of an uploader or a response is logged.** The file has keys, the webhook url is a secret, a response repeats
  them. Logged are the name of the uploader, the server and the status (`HttpService` logs method, scheme and host).
  A response which doesn't fit logs the exception type only.
- **The server is fixed and approved.** No placeholder in the server part, HTTPS (HTTP to loopback only), and
  `UploaderRequest.Create` checks it again. The recipe gate is `NetworkAccess` with `name (server)`.
- **No questions per upload.** Placeholders which ask the user or pick at random are refused at import.
- **Encrypted at rest.** Everything in `[CustomUploader]` is DPAPI (CurrentUser) with the plugin's own entropy, never clear
  text and never in an export. A value which can't be decrypted is shown as unreadable, never repaired.
- **Only web links leave the plugin.** A link from a response is opened in the browser and put on the clipboard only when
  it is an absolute http or https url (`file:`, `javascript:` and others are dropped).
- **Beta.** `CustomUploaderPlugin.IsEnabled` (`IsBetaTester`) decides at startup: without it there is no destination, no
  recipe step and no settings page.

### Adding a placeholder

1. Add the name to `UploaderFile.RequestPlaceholders` and/or `ResponsePlaceholders`. Never add one which asks the user,
   picks at random or reads the file system.
2. Implement it in `UploaderRequest.Call` and/or `UploaderResponse.Call`. Parameters arrive with their placeholders
   already replaced.
3. Add tests and a row to the table above.

### Adding a body type

`FormURLEncoded`, `JSON` and `XML` bodies don't carry the file, they are for text uploaders and URL shorteners, which
Greenshot doesn't offer. A new body goes into `UploaderFile.Check` (accept it), `UploaderRequest.Create` (build it with
`HttpContents`) and the table [What Greenshot refuses](#what-greenshot-refuses).

### Tests

`src/Greenshot.Tests/CustomUploader`: the import checks (`UploaderFileTests`), the syntax (`PlaceholderSyntaxTests`),
requests and responses with `FakeHttpHandler` (`UploaderRequestTests`, see [http.md](http.md#testing)), and the encrypted
store (`UploaderStoreTests`, DPAPI of the user who runs the tests). The tests never send anything over the network.
