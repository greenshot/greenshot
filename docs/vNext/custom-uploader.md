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
features* (`BetaTester=True` in `[Core]` of greenshot.ini) and restart Greenshot. The plugin is not delivered yet: only a Debug build puts it into the `Plugins` folder, the
installer and the portable version don't have it (`GreenshotDeliverPlugin` in the csproj, the commented out
`customuploader.iss` in `all-plugins.iss`). To deliver it, change both.

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
[What Greenshot refuses](#what-greenshot-refuses)). Then it shows the values of the uploader: the address, the address
parameters, the headers and the form fields. Shared files often have a text like `YOUR_API_KEY` or `<API KEY HERE>` where
your key belongs: put the key the service gave you there (no need to edit the file). Values whose name says key, token,
secret, password or auth (like *Authorization*) are masked, *Show* reveals them. When a value still looks like the text
of the file (`<…>`, `[…]`, `YOUR_…`, "insert", "here"), *Next* asks once whether to go on; *No* goes back to the values.
With *Next* Greenshot checks the values like the file. Then it asks: it shows the name of the uploader, the server your
captures go to, and the names of the headers which go with them (often *Authorization*, the key). The values aren't shown.
It also says that the uploader isn't part of Greenshot: whether uploads work depends on the service and the file, and when
they fail the service or whoever made the file can help, not Greenshot.
The *Import* button reacts after a moment, Enter and Escape cancel. Nothing is stored before you click *Import*.

An uploader with the same name as one you already have replaces it, Greenshot says so in the question.

After the import Greenshot gets the icon of the service once, from the server of the uploader (or the website it belongs
to, e.g. `example.com` for `api.example.com`), and shows it in the destination menus and in the lists of the plugin's settings. Only the icon is requested, nothing of
the uploader is sent. A service without an icon gets the generic upload icon, Greenshot tries again after the next start.

The uploader is stored encrypted in greenshot.ini. When a stored uploader can't be read (e.g. greenshot.ini was changed
by hand), the list shows *Can't be read, import it again*: remove it and import the file again.

### Upload

Choose *Custom uploader* as destination: in the destination picker after a capture, in the editor, or as a default
destination in the settings. With more than one uploader, *Custom uploader* has a sub menu with each of them. The
destination is only there when you imported an uploader.

Greenshot uploads the capture in your output file format (PNG when that is the Greenshot format), shows the progress, and
puts the link to the upload on the clipboard. The notification shows the link too. When the upload fails, the notification
says why: the error of the service when the uploader reads it, otherwise the status with the start of the answer when it is text (e.g.
*500 Internal Server Error: Error: file too large*; an HTML error page gives the status only). The log gets the same.
When you started the upload yourself, a window says it too, and that the service and the uploader file aren't part of
Greenshot. *Details* opens what helps the service or the author of the file: the uploader, the server, the names of the
address parameters, headers and form fields (not their values, they hold your key), the file name, type and size, the
answer, the time and the Greenshot version. *Copy details* puts them on the clipboard. A recipe run shows no window.

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
- *Remove from list* removes the upload from Greenshot's list, the upload stays online at the service. Without the delete
  link Greenshot can't delete it later.

Without a delete link only the service decides how long an upload stays online (Litterbox: the time in the file, catbox.moe:
for good). The import question says so, and the list says it when such an upload is selected. For such an uploader the
import also offers *Import and remind me*: before each upload Greenshot then shows the capture and asks, *Upload* or
*Cancel*. *Don't remind me again* in that question turns it off, importing the file again turns it on. A recipe running
without you doesn't ask, you approved the server for it.

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

An upload doesn't follow redirects: when the server answers with another address (a 3xx status), the upload fails with
that status, so your capture and the headers of the file (the key) never go to a server you didn't approve.

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
| `UploaderStore.cs` | The uploaders, their icons and the uploads in `[CustomUploader]`, uploaders and uploads encrypted with `StringExtensions.Encrypt` |
| `ServiceIcon.cs`, `Destinations/CustomUploaderIconProvider.cs` | The icon of the service, fetched at the import; the generic `Images/Upload.png` without one |
| `Destinations/CustomUploaderDestination.cs` | *Custom uploader* with one dynamic destination per uploader |
| `Recipes/CustomUploaderStep.cs` | The recipe step, `Uploader` parameter, `CustomUploader.Url` output |
| `ViewModels/`, `Views/` | The settings page: import (with the confirmation), remove, the uploads |

HTTP goes through `HttpService.Current` like everywhere in Greenshot, see [http.md](http.md): the proxy, the certificate
rules and the logging rules apply.

### The flow

1. **Import** (settings, UI thread): `UploaderFile.Read` parses the JSON into `UploaderDefinition`, normalizes it (method,
   body of old files, name) and checks it. `UploaderValuesWindow` shows `GetValues` (request url, parameters, headers,
   arguments) for the user to fill in (`UploaderValue.IsSecret` masks keys in a `PasswordBox`, `LooksLikePlaceholder`
   makes the view ask once), `WithValues` puts them into a copy and runs it through `UploaderFile.Read` again.
   The view asks with `DescribeImport` and `ThemedMessageBox.ShowChoice` (Cancel is
   the default, the buttons are armed after a second). `UploaderStore.Add` stores the normalized JSON encrypted and saves
   greenshot.ini at once.
2. **Read**: `UploaderStore.GetUploaders` decrypts each uploader and runs it through `UploaderFile.Read` again, so a changed
   entry is checked like a file. One which can't be decrypted or doesn't pass has a null `Definition` and is only listed.
3. **Upload** (thread pool): with the reminder of the import (`UploaderStore.RemindsBeforeUpload`) and an interactive
   export, `ConfirmUploadAsync` first asks with a `PreviewPromptRequest` (`IUserInteraction.ShowDialogAsync`), the generic
   question with a preview of the capture which Greenshot shows in the window of the recipe step *User prompt*.
   `UploaderRequest.Create` fills the placeholders of the request and builds it, it refuses to send
   to another server than `GetServer`. `HttpService.SendAsync` sends it. `UploaderResponse.From` reads the result. Only
   http and https links are accepted, a link is opened and put on the clipboard.
4. **Keep**: the link and the deletion link go into the upload history (`UploaderStore.AddUpload`, the newest 100).

**Icon** (after the import, thread pool): `ServiceIcon.FetchAsync` asks `SiteIcon` (Greenshot.Base, see [http.md](http.md)) for the server of the uploader, then its parent domain
(one label less, not for IP addresses or `localhost`): `/favicon.ico`, else the first `<link rel="icon">` of the start page
which stays on that host. A plain GET with no header or value of the uploader, 45 seconds for all and 256 KB per file at most; the outcome is logged (host and status). Only ICO, PNG,
GIF and JPEG (by their first bytes) are read, through the file format handlers, and stored as a 32x32 PNG
(`UploaderStore.SetIcon`). `CustomUploaderIconProvider` gives `customuploader:name` the stored icon, else the generic one.
A re-import fetches it again. Without an icon (the fetch failed, an uploader imported before icons existed) the first menu which shows the uploader after a start of Greenshot tries once more in the background.

### Supported placeholders

| Placeholder | In the request (`RequestURL`, `Parameters`, `Headers`, `Arguments`) | In the results (`URL`, `ThumbnailURL`, `DeletionURL`, `ErrorMessage`) |
|---|---|---|
| `{filename}` | yes | yes |
| `{base64:text}` | yes | yes |
| `{response}` | | the response text |
| `{responseurl}` | | the url which answered (the request url, redirects are not followed) |
| `{header:name}` | | a response header |
| `{json:path}`, `{json:input\|path}` | | JSONPath, `$.` in front is optional (`data.link`, `[0].url`, `['a b']`); names and array indexes only, no wildcards or filters |
| `{xml:xpath}`, `{xml:input\|xpath}` | | the text of the first node; no DTD, no external entities |
| `{regex:pattern}`, `{regex:pattern\|group}`, `{regex:input\|pattern\|group}` | | the match, or the group by number or name; 2 seconds at most |

A backslash escapes `{`, `}`, `|` and `\`; other backslashes stay (`\d` in a pattern). Values in `RequestURL` are url
encoded, the text around them isn't. Parameters are url encoded and added to the query. An empty `URL` means the response
is the link, like in ShareX.

Headers starting with `Content-`: a binary body takes `Content-Type` from them, a multipart body keeps its own (it carries
the boundary); `Content-Length` is always computed.

### Differences from ShareX

ShareX reads `.sxcu` files and responses with Json.NET, as Greenshot did until it moved to System.Text.Json. Files are read
as leniently as before (comments, trailing commas, property names in any casing, unknown properties), but a few corner
cases give a different result:

| What | ShareX (Json.NET) | Greenshot |
|---|---|---|
| `{json:path}` with a wildcard (`files[*].url`), recursive descent (`$..url`) or a filter (`files[?(@.type=='image')].url`) | full JSONPath | not supported: the upload fails with "Only names and array indexes are supported in a JSON path", it never gives a wrong link. Names (`data.link`, `['direct link']`) and array indexes (`files[0]`) work as before |
| `{json:path}` pointing at a number or boolean in the response | the value converted: `1.0` gives `1`, `true` gives `True` | the JSON text: `1.0` gives `1.0`, `true` gives `true`. Strings and whole numbers (ids) are the same |
| A response with the same property twice | the last one counts | the response doesn't fit, the upload fails |
| A number or boolean without quotes in `Parameters`, `Headers` or `Arguments` of the file (`"expiry": 1.0`) | the value converted: `1.0` is sent as `1`, `true` as `True` | sent as written in the file: `1.0`, `true`. Quoted values (`"expiry": "1.0"`, what ShareX exports) are the same |

System.Text.Json has no JSONPath, `UploaderResponse.Json` walks the supported part itself. Wildcards and recursive descent
could be added there when a real uploader needs them; filters would need an expression parser. The conversions are in
`LenientStringConverter` (Greenshot.Base/Serialization). `TestData/Sxcu` and `JsonNetCompatibilityTests` cover the
supported cases.

### Rules

- **Nothing of an uploader or a response is logged.** The file has keys, the webhook url is a secret, a response repeats
  them. Logged are the name of the uploader, the server and the status (`HttpService` logs method, scheme and host).
  A response which doesn't fit logs the exception type only.
- **The server is fixed and approved.** No placeholder in the server part, HTTPS (HTTP to loopback only), and
  `UploaderRequest.Create` checks it again. The recipe gate is `NetworkAccess` with `name (server)`.
- **No redirects.** Uploads go through `HttpService.WithoutRedirects`: a redirect would carry the headers of the file to
  another host. A 3xx is a failed upload with its status (`{header:Location}` is readable in `ErrorMessage`).
- **No questions per upload.** Placeholders which ask the user or pick at random are refused at import.
- **Encrypted at rest.** Uploaders and uploads in `[CustomUploader]` go through `StringExtensions.Encrypt`/`Decrypt`, the
  encryption of all Greenshot secrets (the icons are the public favicons, plain base64), never in an export. That
  encryption keeps ASCII only, so the stored JSON escapes everything else (`AsciiJson`). `Decrypt` gives back what it
  can't decrypt, the JSON then can't be read: such a value is shown as unreadable, never repaired.
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
store (`UploaderStoreTests`). The tests never send anything over the network.
