# Notifications

How Greenshot informs the user (a file was saved, a capture went to the clipboard, an upload finished, an error), and how
a click on a notification gets back to Greenshot, also when Greenshot isn't running anymore.

## Overview

| Edition / variant | Notifications | Kept capture and thumbnail | Click while Greenshot is closed |
| :--- | :--- | :--- | :--- |
| Full, installed (x64) | Windows toasts | yes | starts Greenshot, shows the destination flyout |
| Full, toasts not available | balloons of the tray icon | no | — |
| Full, notifications turned off by the user or a policy | none | no | — |
| Portable (.zip, PortableApp) | balloons of the tray icon | no | — |
| Greenshot Light (AnyCPU) | balloons of the tray icon | no | — |

## Architecture: initialization and usage

Notifications are behind two interfaces in `Greenshot.Base/Interfaces`, one implementation per platform:

* **`INotificationServiceProvider`** (initialization): `Priority` and `TryCreate()`. `TryCreate` checks whether its platform is
  available here and initializes it, or returns `null`.
* **`INotificationService`** (usage): `ShowNotification`, `ShowInfoMessage` / `ShowWarningMessage` / `ShowErrorMessage`, and
  * `IsEnabled`: whether notifications are shown at the moment (see [Turned off](#turned-off-by-the-user-or-a-policy)),
  * `ShowsCaptures`: whether a notification can show a kept capture and be clicked after Greenshot exited,
  * `GetShownCaptureIds()`: the capture ids of the notifications which can still be clicked,
  * `CaptureActivated`: the click on a notification with a capture, on the UI thread, also when the click started Greenshot.

`NotificationServiceFactory` (`Greenshot/Shell`) is the one place which picks the implementation, once at startup:
`NotificationServiceProviders.CreateService` tries the providers by their priority and takes the first one which returns a
service (a provider which throws is skipped). The service is registered as `INotificationService` in the
`SimpleServiceProvider`; everything else only uses the interface.

| Provider | Priority | Where | Implementation |
| :--- | :--- | :--- | :--- |
| `ToastNotificationServiceProvider` | 100 | `Greenshot/Native/ToastNotificationService.cs` (not in Light) | `ToastNotificationService` |
| `TrayNotificationServiceProvider` | 0 | `Greenshot/Shell/TrayNotificationService.cs` | `TrayNotificationService` |

Another platform (macOS, Linux) is another provider in the list of `NotificationServiceFactory`.

## Windows toasts

`ToastNotificationServiceProvider.TryCreate` initializes the toasts; it returns no service, so the balloons of the tray icon are
used, when

* Greenshot runs as PortableApp or from the .zip (`GreenshotEnvironment.LeavesNoTraces`): they leave nothing on the system,
  and the toasts need a registration,
* Windows has no toast API (`ToastNotificationActionTrigger` is missing),
* the registration fails, or
* Windows can't show Greenshot's toasts at all (`NotificationSetting.DisabledByManifest`).

Otherwise it writes the icon of the toasts (`%LocalAppData%\Greenshot\greenshot.png`) and registers Greenshot
(`DesktopToasts.Register` in `Greenshot.Base/Native/Toasts`), each time Greenshot starts:

| Registry (HKCU) | Value |
| :--- | :--- |
| `Software\Classes\AppUserModelId\Greenshot` | `DisplayName`, `IconUri`, `CustomActivator` = `{DD8B2638-A274-4988-9CC7-23A6D40BACEB}` |
| `Software\Classes\CLSID\{DD8B2638-A274-4988-9CC7-23A6D40BACEB}\LocalServer32` | `"…\Greenshot.exe"` |

The process gets the app id `Greenshot` (`SetCurrentProcessExplicitAppUserModelID`, the same as the start menu shortcut, see
`tasks-icons-reg.iss`) and registers the COM class `ToastActivator` (`INotificationActivationCallback`), which receives the
clicks. The toast XML is built by `ToastContent` (template `ToastGeneric`, `launch` = the tag, a text, rows and an optional image).

### What a toast shows

Every export of a flow is added to its export log (`ExportLog` in `Greenshot.Base/Recipes/Pipeline`, an `ExportOutcome` per export:
the destination, the `ExportResult`, the capture details and the export source which was exported): by the `DestinationDispatcher`
(destination steps, the destination picker, also the copies with the extensions of a destination) and by the plugin steps which
export (Box, Confluence, Dropbox, External command, Jira, Office, PDF), with the same result as their destination. The log is in `CaptureFlowContext.Properties` from the start, so the
contexts of the nodes and of parallel branches add to the same log. The Notification step (`NotificationStep`) shows one notification
for the exports since the previous Notification step (`ExportLog.TakeUnnotified`), so a recipe with Destination A, Destination B and a
Notification step shows both, and two Notification steps don't show an export twice:

* the title: what was captured (`CaptureDetails.Title`, e.g. the title of the window), else "Greenshot",
* one row per export (`Notification.Lines`, `ToastContent.AddRow`: an adaptive group), e.g. "Exported to: Clipboard"
  (`ExportResultHandler.GetMessage`, the same text the editor shows in its status bar). The destination of a row is the one which
  exported: with the destination picker (and the document choice of the Office destinations) the picked one
  (`ExportResult.ExportedBy`), not the picker; its icon is `DestinationDescriptor.IconKey`,
* the kind: an error when an export failed (for the notification level),
* the icons: with one destination its icon is the big image left of the texts (`ToastContent.SetAppLogo`, `appLogoOverride`), which
  unlike the rows and the thumbnail stays visible when the toast is collapsed in the notification center, and the row is the text
  only; with several destinations the big image stays Greenshot's icon and every row has the icon of its destination in a small
  column beside the text. Greenshot's name and icon are always in the header,
* the thumbnail of the kept capture below (see [Kept captures](#kept-captures-and-the-destination-flyout)), unless
  `ShowNotificationThumbnail` is off; the capture is kept anyway, a click still offers it.

### Buttons

Below the content a toast of an export has buttons (`Notification.Buttons`, `ToastContent.AddButton`, built by
`NotificationButtons.Create` in `Greenshot/Notifications`, not in Light). Some act on the capture, one on the exports:

| Button | When | Does |
| :--- | :--- | :--- |
| Edit | a capture is kept | opens the kept capture in the editor |
| Show in folder | saved to a file | opens Explorer with the (last) saved file selected |
| Copy link | an export has a link (an upload), or a barcode is a link | copies the link; with more than one link a drop-down above the buttons chooses which (`NotificationButton.Choices`, `ToastContent.AddSelection`, up to 5, e.g. "Box: https://…") |

Nothing opens a file or a link: opening a file only asks Windows for the app, and a link of a barcode is someone else's content.

A click on a button works like a click on the toast, also when Windows has to start Greenshot for it: the arguments of a button
are a query string (`ToastArguments`: `button=<action>&capture=<id>&value=<file or link>&input=<drop-down>`), the launch argument
of the toast stays its tag. The chosen value of a drop-down comes with the click as user input (`ToastActivator.Activated`), the
service puts it into the value of the button. The service raises `ButtonActivated`, `NotificationCaptures.Initialize` subscribes
`NotificationButtons.Run`. Since a local process can call the COM class with any arguments, `Run` checks them: a file must exist, a
link must be http(s), and the capture id must be a UUID. After the click the kept capture is removed, the toast is gone. The balloons
of the tray icon show no buttons.

### Barcodes

The barcodes found in the capture (`IBarcodeFeature` in `CaptureDetails.Features`, by the barcode scanner step, the ZXing processor
or the QR destination) get a row each, up to 3, also without an export (a recipe which scans and notifies): "Barcode: …". A barcode
is someone else's content: a link (only http and https) is shown completely, with its host as the DNS sees it (`Uri.IdnHost`, a
look-alike letter of another alphabet shows as `xn--…`), and can only be copied, the copied text is the shown one. Other text
(e.g. `WIFI:…`, `javascript:…`) is shortened and is no link.

A toast only shows image files, so `NotificationIcons` (`Greenshot/Notifications`, not in Light) writes the icon of each destination
once as a png to `%LocalAppData%\Greenshot\Notifications\Icons` (named by a hash of the icon key; dynamic destinations like a
Word document have their own key), before the step switches to the UI thread. The balloons of the tray icon show `Notification.Message`,
the text of all rows.

Before the registration, the provider removes what earlier versions left with the Windows Community Toolkit
(`DesktopToasts.RemoveOldRegistrations`), otherwise Windows lists Greenshot several times in its notification settings. It
looks at every app id under `AppUserModelId` whose `CustomActivator` is another COM class, whose `LocalServer32` starts an exe
named like ours (`Greenshot.exe`, compared by file name):

* the COM class (`CLSID\{…}`) and the Toolkit's icon folder (`%LocalAppData%\ToastNotificationManagerCompat\Apps\{…}`) are removed,
* from the app id the Toolkit's values (`CustomActivator`, `IconBackgroundColor`, `Has7.0.1Fix`) are removed; the key of an app id
  the Toolkit created (one per exe path of Greenshot up to 1.4, e.g. `C:/…/Greenshot.exe`) goes when nothing else is left in it,
* the own app id `Greenshot` is kept (the Toolkit of Greenshot 1.4 builds used it too, with a COM class of its own), and so are the
  values of another app: the Toolkit sometimes wrote its `CustomActivator` into the app id of another app (e.g. a Windows package).

When an older Greenshot with the Toolkit starts again, it registers itself again; the next start of this version removes it again.

The uninstaller removes both keys, the icon and the kept captures (`cleanup.iss`); the Light installer removes them when it is
installed over the full version.

### A click on a toast

* **Greenshot runs:** Windows calls `ToastActivator.Activate` with the `launch` argument of the toast. A toast without a capture
  runs its click action (e.g. open the saved file or the upload); a toast with a capture raises `CaptureActivated`.
* **Greenshot isn't running:** COM starts `Greenshot.exe -Embedding`. Windows appends `-Embedding` to every program it starts for a
  COM call (an old OLE name); Greenshot ignores it and only accepts it, so the start isn't rejected (see
  [greenshot-proxy-and-integration.md](greenshot-proxy-and-integration.md)) and calls `Activate` as soon as Greenshot registered
  the class. The click arrives a fraction of a second after the start, before the startup is finished: the provider subscribes
  the click handler before the registration, and the handler runs on the UI thread, after the startup subscribed to
  `CaptureActivated`. The click action of a toast lives only in memory, so after a restart only a toast with a capture does
  something.

**Greenshot.exe must be a 64-bit (PE32+) exe for this.** COM never hands the pending click to the registration of a process it
started when the exe is AnyCPU: its PE header says 32-bit (machine x86), although it runs as a 64-bit process. Greenshot then
starts, registers its class without an error, waits, and is ended by COM after the activation timeout (DCOM event 10010 "did not
register with DCOM within the required timeout" in the System log). The same .NET Framework COM server compiled as x64 works at
once, as do native and .NET 10 servers; an AppID with `PreferredServerBitness` doesn't help. That's why the full version of
Greenshot.exe is built with `PlatformTarget` x64 (see `Greenshot.csproj`); the process was 64-bit before as well. COM starts only
one server per class at a time, so a click which hangs blocks the next one until its timeout.

### Kept captures and the destination flyout

For a service which `ShowsCaptures` (the toasts), the Notification step of a recipe keeps what went to the destinations
(`NotificationCaptures`, `Greenshot/Notifications`, not in Light): a `.greenshot` file and a thumbnail (364×180) in
`%LocalAppData%\Greenshot\Notifications`, named by a UUID v7, the `CaptureId` of the notification and the tag of the toast.
`NotificationCaptures.Initialize` subscribes to `CaptureActivated`: a click offers the capture with the built-in recipe
`recipe_notification` ("Capture of a notification": a file source which passes the capture on, and the dynamic destination
flyout).

Nothing of this delays the toast: the Notification step writes only the thumbnail before the toast is shown, and the `.greenshot`
file after it. Both come from the export source of the last export (`ExportOutcome.Source`, what went to that destination, also the
copy with its extensions): the thumbnail from its render (`IExportSource.RenderAsync`, cached and shared with the destinations), the
file from its encoding in the greenshot format (`EncodeAsync`, the elements are serialized on the UI thread, like every export to the
greenshot format); the files are written on the pool thread of the flow.

Windows doesn't tell an app which isn't packaged when the user removes a toast from the notification center (only a packaged app
gets a background task for that, `ToastNotificationHistoryChangedTrigger`), and the `Dismissed` event of a toast only fires while
Greenshot runs. So the files are removed

* when the toast or one of its buttons is clicked,
* by `RemoveUnused`: after a capture was kept (on the pool thread of the flow) and at startup (next to it, a "PARALLEL" branch),
  for every toast which is gone (`GetShownCaptureIds`) and older than an hour, the lifetime of the toasts: a clicked toast leaves
  the notification center before Greenshot handles the click, so a younger capture is kept,
* by the uninstaller.

## Balloons of the tray icon

`TrayNotificationService` shows the balloons of the tray icon (`ITrayIcon.ShowBalloon`): only text, and a click only works while
Greenshot runs (`ShowsCaptures` is false, `CaptureActivated` is never raised). It needs no registration. The tray icon is created
after the services; a notification before it isn't shown.

## Settings

| greenshot.ini | Settings window (Capture tab) | |
| :--- | :--- | :--- |
| `ShowTrayNotification` | "Notifications": None turns it off | the switch, also in the tray menu |
| `NotificationLevel` | "Notifications": All, Warnings and errors, Errors only | which `NotificationKind`s are shown (`NotificationLevel.Shows`); None turns all off |
| `ShowNotificationThumbnail` | "Show a thumbnail of the capture in the notification", only offered where the service `ShowsCaptures` | the thumbnail of a toast |

Both implementations filter by the level in their `Show…` calls; the Notification step of a recipe checks it before it keeps a
capture. Choosing None in the settings window keeps the level, so the tray menu turns the notifications on again with it.

## Turned off by the user or a policy

`IsEnabled` is false while notifications are turned off, and every `Show…` call does nothing then; the balloons don't take over:

* in Greenshot: `ShowTrayNotification` off or `NotificationLevel` None in `greenshot.ini` (both implementations),
* in Windows, for the toasts: Greenshot's notifications turned off (`DisabledForApplication`), all notifications turned off
  (`DisabledForUser`) or a group policy (`DisabledByGroupPolicy`). The toast service asks Windows each time
  (`DesktopToasts.GetSetting`), so turning them on again works without a restart. No capture is kept while they are off.

Windows itself doesn't show the balloons while the user turned the notifications off.

## Greenshot Light and the portable version

* **Greenshot Light** has no toasts: `ToastNotificationService.cs` and `Notifications\**` are not compiled
  (`CheckLightAssembly` fails the build when `ToastNotificationService`, `ToastActivator` or `DesktopToasts` is in its
  Greenshot.exe). It writes no registration, stays AnyCPU (32-bit Windows, native on ARM64) and ships without
  `greenshot-cli.exe` and `greenshot-proxy.exe`.
* **The portable version** (.zip, PortableApp) is the full Greenshot.exe, but writes no registration and uses the balloons.

Every Greenshot process gets the app id `Greenshot` at the start (`AppUserModelId.SetForCurrentProcess` in
`GreenshotApplication.Start`), which writes nothing. Without an app id Windows registers one of its own for the tray icon
(`AppUserModelId\NotifyIconGeneratedAumid_…` with an icon in Temp). What Windows keeps for every app which notifies,
`Notifications\Settings\Greenshot` (the user's settings for its notifications), is written by Windows in all variants.
