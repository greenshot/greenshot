# Portable versions

Greenshot comes in two portable versions. Neither one needs an installation, and both keep their settings in their own folder.

What each download has compared to the installers is in the feature matrix in [editions.md](editions.md).

| | Portable .zip | PortableApps.com package |
|---|---|---|
| Download | `Greenshot-PORTABLE-<version>.zip`, `Greenshot-Light-PORTABLE-<version>.zip` | `Greenshot-Light-PORTABLEAPPS-<version>.paf.exe` |
| Contains | Greenshot, or Greenshot Light | Greenshot Light |
| Start | `Greenshot.exe` | `GreenshotPortable.exe`, or from the PortableApps.com Platform menu |
| greenshot.ini | next to `Greenshot.exe` | `Data\Settings` |
| Log | `Greenshot.log` next to `Greenshot.exe` | `Data\Greenshot\Greenshot.log` |
| Default folder for captures | the Desktop | `Documents\Pictures\Greenshots` of the PortableApps.com drive |

Like Greenshot Light, both versions use the balloons of the tray icon instead of toasts, and they register nothing (see
[notifications.md](notifications.md)).

## Where greenshot.ini is read from

Greenshot uses the first of these that applies:

1. `--ini-directory <folder>`: greenshot.ini is read from and saved to that folder only.
2. The PortableApps.com package: `Data\Settings`, the same way.
3. The folder of `Greenshot.exe`, when greenshot.ini is there. The .zip ships an empty greenshot.ini, so it keeps its
   settings next to `Greenshot.exe` even when an installed Greenshot has its own in `%APPDATA%\Greenshot`.
4. `%APPDATA%\Greenshot`. When greenshot.ini isn't found anywhere, it is created here.

`greenshot-fixed.ini` is never read from the folders in 1 and 2, so a user can't replace what an administrator fixed.

The approvals of recipe files (`recipe_trust.dat`) belong to the greenshot.ini which lists the files: next to it in 1 to 3,
in `%LOCALAPPDATA%\Greenshot` for 4. At the start Greenshot removes the approvals of the files its greenshot.ini doesn't use,
so a shared store would make a portable Greenshot remove the approvals of the installed one. The store is encrypted for the
Windows user (DPAPI), on another computer the approvals are asked again.

## The PortableApps.com package

The package follows the [PortableApps.com Format](https://portableapps.com/development/portableapps.com_format) 3.9:

```text
GreenshotPortable\
  GreenshotPortable.exe   PortableApps.com Launcher, starts App\Greenshot\Greenshot.exe
  help.html
  App\AppInfo\            appinfo.ini, installer.ini, Launcher\GreenshotPortable.ini, icons
  App\Greenshot\          Greenshot Light
  Data\Settings\          greenshot.ini (created by Greenshot)
  Data\Greenshot\         Greenshot.log (created by Greenshot)
```

Greenshot handles the portable part itself. It knows it runs from the package when `App\AppInfo\appinfo.ini` is two folders up
from `Greenshot.exe` (`GreenshotEnvironment.PortableAppsDirectory`). So the launcher only starts it and needs no settings of its own.

`GreenshotEnvironment` has one property for each decision, so none of them hides an assumption about the others:

| Property | True or set when | Used for |
|---|---|---|
| `PortableAppsDirectory`, `IsPortableApps` | `App\AppInfo\appinfo.ini` is two folders up from `Greenshot.exe` | the basis of the PortableApps.com properties |
| `PortableAppsSettingsDirectory` | the package's `Data\Settings` | where greenshot.ini is kept, unless `--ini-directory` is given |
| `DefaultOutputDirectory` | the package's drive: `Documents\Pictures\Greenshots`; otherwise the desktop | the default folder for saved captures |
| `UsesSettingsNextToExecutable` | greenshot.ini was loaded from the folder of `Greenshot.exe` (the .zip ships one), known after loading | `LeavesNoTraces` and `PortableSuffix` |
| `LeavesNoTraces` | `IsPortableApps` or `UsesSettingsNextToExecutable` | no toast registration, no JIT profile, the *Portable* badge in About |
| `PortableSuffix` | " PortableApps", " Portable" or empty | the version in the log and the bug report |

An update keeps `Data`. An update of a Greenshot 1.x package removes the old `Greenshot.exe` from the main folder
(`installer.ini`) and goes on using `Data\Settings\greenshot.ini`.

### Building it

`prepare-portableapps.ps1` builds the package on Windows from the Greenshot Light build. The release workflow (job
`portableapps`) and `build-and-deploy.ps1` both use it. The script:

1. copies the template `installer\portable` and runs `prepare-portable.ps1 -Light -PortableApps` for `App\Greenshot`,
2. fills in the version of `Greenshot.exe` in `appinfo.ini`,
3. downloads the PortableApps.com Launcher and Installer once (the versions and SHA-256 are in the script) and extracts them
   with 7-Zip (installed with winget when it is missing),
4. creates `GreenshotPortable.exe` with the Launcher Generator, then `GreenshotPortable_<version>.paf.exe` with the
   PortableApps.com Installer. With the environment variable `CertumThumbprint`, which also signs the release build and
   the installers, both are signed with `sign-files.ps1`. The continuous builds on GitHub have no certificate, so their
   package is unsigned, like the rest of their files.

PortableApps.com asks that every release uses the current PortableApps.com Installer. When a new version comes out, update its
version and SHA-256 in the script.
