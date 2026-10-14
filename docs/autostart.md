# Starting Greenshot with Windows

## In short

What makes Greenshot start with Windows is one registry value per user: `Greenshot` in the Run key of that user,
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, with the quoted path of Greenshot.exe.
Windows reads it at every logon of that user and starts Greenshot. Nothing else starts Greenshot with Windows.

Everything below is only about **who writes that value, and when**:

| Who | Runs as | When | Does |
| --- | --- | --- | --- |
| **Installer** | the administrator who elevated it | install / update | asks *whether* and *for whom*. For every user: registers Greenshot in Active Setup (HKLM), so Windows will write the value for each user later |
| **Installer** | the user who is logged on (`runasoriginaluser`) | install / update, at the end | writes the value for that user right away, with the autostart helper |
| **Windows, Active Setup** | each user | the next logon of each user, **once** | runs the autostart helper for that user, which writes the value |
| **Windows, Run key** | each user | **every** logon | starts Greenshot.exe if the value is there |
| **Greenshot** | the user | when the checkbox "Launch Greenshot on startup" in the settings is changed | writes or removes the value of that user. The user's own decision, no administrator rights needed |
| **Uninstaller** | the administrator / the user | uninstall | removes the Active Setup registration and the value of the uninstalling user |

The **autostart helper** is `Greenshot.exe --enable-autostart`: it writes the value for the current user and exits.
It doesn't start Greenshot; the installer and Active Setup call it, nobody else.
(See [the alternatives](#alternatives-for---enable-autostart): it could be the small native `greenshot-proxy.exe` instead.)

### Example: an administrator installs Greenshot on a family member's computer

Anna is logged on, Bob types his administrator password when the setup asks for it, and chooses "for every user of this computer".

```mermaid
sequenceDiagram
    actor Bob as Bob (administrator)
    participant Setup as Installer
    participant Helper as autostart helper
    participant Reg as Registry
    participant Win as Windows
    participant GS as Greenshot

    rect rgba(128,128,128,0.1)
    Note over Bob,GS: Installation, while Anna is logged on
    Bob->>Setup: elevate, choose: start with Windows, for every user
    Setup->>Reg: as Bob (admin): HKLM Active Setup key, StubPath = helper
    Setup->>Helper: start as Anna (runasoriginaluser)
    Helper->>Reg: Anna's HKCU Run: Greenshot = …\Greenshot.exe
    Setup->>Reg: as Anna: HKCU Active Setup marker, Anna is done
    Setup->>GS: start Greenshot now (finish page)
    end

    rect rgba(128,128,128,0.1)
    Note over Bob,GS: Anna logs on the next time
    Win->>Reg: Active Setup: Anna has the marker, nothing to do
    Win->>Reg: read Anna's Run key
    Win->>GS: start Greenshot
    end

    rect rgba(128,128,128,0.1)
    Note over Bob,GS: Carl, another user of the computer, logs on for the first time after the installation
    Win->>Reg: Active Setup: Carl has no marker
    Win->>Helper: run the StubPath as Carl, wait for it
    Helper->>Reg: Carl's HKCU Run: Greenshot = …\Greenshot.exe
    Win->>Reg: HKCU Active Setup marker, Carl is done
    Win->>Reg: read Carl's Run key
    Win->>GS: start Greenshot
    end

    rect rgba(128,128,128,0.1)
    Note over Bob,GS: Carl doesn't want it
    GS->>Reg: settings, checkbox off: remove Carl's Run value
    Note over Win,Reg: next logons of Carl: marker is there, no Run value, Greenshot doesn't start
    end
```

With "only for me", the Active Setup part is left out: only Anna gets the value, Carl has to switch it on in the settings himself.

## The installer in detail

The task "Start Greenshot with Windows start" sets it up for the user who started the setup. An all-users install has two choices below it:

- **For every user of this computer** (default, so an update of an all-users install keeps starting Greenshot for everyone):
  every user gets the start with Windows at their next logon, and can switch it off in the settings.
- **Only for me**: only the user who started the setup.

A per-user install only has the task, for the current user.

The steps in the order the setup does them. The `[Registry]` steps run as the account of the setup (the administrator for an all-users install),
the `[Run]` steps marked "as the logged-on user" run as the user who started the setup:

```mermaid
flowchart TD
    A[Setup starts installing] --> B["[Registry] as admin: remove the old Greenshot values<br/>from the Run keys of HKCU and HKLM, both views"]
    B --> C["[Registry] as admin: remove the Active Setup key<br/>(all-users install)"]
    C --> D{"Task: start with Windows?"}
    D -- no --> Z[Done, nobody gets it]
    D -- yes --> E{"For every user?<br/>(all-users install only)"}
    E -- yes --> F["[Registry] as admin: create the Active Setup key:<br/>StubPath = …\Greenshot.exe --enable-autostart<br/>Version = 1,0"]
    F --> G
    E -- "only for me" --> G["[Run] as the logged-on user:<br/>autostart helper, writes the Run value"]
    G --> H{"For every user?"}
    H -- yes --> I["[Run] as the logged-on user: reg.exe add HKCU\…\Active Setup\…<br/>{17A6D9DC-…} Version = 1,0: this user is done already"]
    H -- no --> Z2[Done]
    I --> Z2
```

Notes:

- "As the user who started the setup" matters: when an administrator elevates the setup with their own account, for example on the computer of a family member,
  the HKCU of the setup is the administrator's. `runasoriginaluser` writes the Run value of the person who is logged on.
- The old values are removed first because older installers wrote the value for all users to HKLM, which a user without administrator rights can't switch off.
- Without the "every user" choice the Active Setup key is removed, so no new user gets the start with Windows.
- Unattended: `/TASKS="startup\allusers"` or `/TASKS="startup\currentuser"` for an all-users install, `/TASKS="startup"` for a per-user install,
  `/MERGETASKS="!startup"` for none.
- `AutostartComponentVersion` in `tasks-icons-reg.iss` is the Active Setup version. Raise it only to set up the start with Windows for every user
  once more, also for those who switched it off since.

### Uninstall

The uninstaller removes the Run value of the user who uninstalls, if it starts this Greenshot.exe, and that user's Active Setup marker;
the HKLM Active Setup key goes with the other registry entries of the setup.
The Run values of other users can't be reached: Windows skips a Run value whose file doesn't exist anymore.

## Active Setup at the logon

Active Setup is a Windows mechanism for setting something up once per user. For every key below
`HKLM\Software\Microsoft\Active Setup\Installed Components`, Windows compares the `Version` with the one in the same key below HKCU.
When the HKCU key is missing or older, it runs the `StubPath` and then writes the version to HKCU, so it doesn't run again.

```mermaid
sequenceDiagram
    participant W as Windows logon
    participant AS as Active Setup
    participant G as Greenshot.exe --enable-autostart
    participant R as HKCU\…\Run
    participant E as Explorer
    W->>AS: user logs on
    AS->>AS: HKLM Version 1,0 > HKCU Version (missing)?
    AS->>G: run StubPath, wait for it
    G->>R: Greenshot = C:\Program Files\Greenshot\Greenshot.exe
    G-->>AS: exit
    AS->>AS: HKCU Version = 1,0
    W->>E: show the desktop
    E->>R: start the Run entries
    R-->>E: Greenshot.exe starts
```

Windows waits for the `StubPath` before it shows the desktop, which is why `--enable-autostart` only writes the value and exits:
a command that keeps running delays the logon. Greenshot itself is started from the Run value it just wrote, in the same logon.
That Active Setup runs before the Run entries are started is what's expected; it still has to be confirmed on a real logon of a second user.

A user who switches the start off in the settings keeps it off: Active Setup only runs again when the `Version` in HKLM is raised.

## What `Greenshot.exe --enable-autostart` does

It's a startup option of Greenshot.exe, handled in `GreenshotMain.Main` before anything of the application starts:

```mermaid
flowchart TD
    A["Windows loads the .NET Framework runtime<br/>and Greenshot.exe (1.3 MB) with its references"] --> B["Main: TLS settings, cultures, thread name"]
    B --> C["log4net: read the configuration, open the log file"]
    C --> D["exception handlers"]
    D --> E["GreenshotCommandLine.Parse: loads System.CommandLine"]
    E --> F["StartupHelper.EnableAutostart"]
    F --> G["open HKCU and both views of HKLM"]
    G --> H{"does an all-users entry of an older installer<br/>start this Greenshot already?"}
    H -- yes --> X[exit]
    H -- no --> I["write HKCU\…\Run Greenshot = …\Greenshot.exe"]
    I --> J["remove a 'switched off' of Windows for it<br/>(StartupApproved)"]
    J --> X
```

No greenshot.ini, no plugins, no window, no single-instance check. Still, the whole runtime is loaded for writing one registry value:
on the development machine `Greenshot.exe --no-run`, which goes through the same start, takes about 220 ms warm and 430 ms cold.
At a first logon nothing is in the file cache yet, so it's likely more, and the user waits for it before seeing the desktop.
With .NET 10 the runtime and Greenshot get bigger, especially self-contained.

## Alternatives for `--enable-autostart`

The work itself is small: write one string value to HKCU. The only real logic, not adding a second entry when an all-users entry
from an older installer already starts Greenshot, isn't needed anymore once the installer has removed those entries,
and clearing a Windows "switched off" isn't needed for a user who never had the entry.

| Option | Size / start | Window | Notes |
| --- | --- | --- | --- |
| `Greenshot.exe --enable-autostart` (now) | 1.3 MB + .NET runtime, ~0.2–0.5 s or more | none | grows with .NET 10; logs to the Greenshot log |
| `greenshot-proxy.exe --enable-autostart` | 42 KB native, no C runtime, milliseconds | none (`/SUBSYSTEM:WINDOWS`) | already installed next to Greenshot.exe and already links advapi32; it's the "Windows shell integration" executable, so it fits there. The path is Greenshot.exe next to itself |
| `reg.exe add HKCU\…\Run /v Greenshot /d "\"…\Greenshot.exe\"" /f` as StubPath | part of Windows | console program: probably a short console window at the logon | no code of our own; quoting the path inside the StubPath is fiddly; the setup can use it hidden (`runhidden`), Active Setup can't |
| Write the HKCU value directly from the setup | – | – | only reaches the HKCU of the setup's account, not the original user, nor other users: doesn't replace Active Setup |
| Scheduled task at logon, Startup folder link for all users | – | – | other mechanisms with their own problems (users can't remove them without admin rights) |

**Proposal:** move `--enable-autostart` to `greenshot-proxy.exe`: a few lines of C with `RegSetValueExW`,
the setup and the Active Setup `StubPath` call `"…\greenshot-proxy.exe" --enable-autostart`, and Greenshot.exe loses the option.
The checks of `StartupHelper` stay in Greenshot, for the checkbox in the settings.

## Greenshot: the checkbox in the settings

This is the only part of Greenshot itself which takes part (apart from the autostart helper, which is just a mode of Greenshot.exe).
`StartupHelper` reads and writes the entries for the checkbox in the settings:

- **Is it on?** When the Run key of the user, or the one of the machine (64-bit and 32-bit view, from an older installer), starts this Greenshot.exe,
  and the entry isn't switched off in Windows (Task Manager > Startup apps, Settings > Apps > Startup). Windows keeps that switch in
  `…\Explorer\StartupApproved\Run` (`Run32` for the 32-bit Run key of the machine): the first byte of the value is odd when it's off.
  An entry for another Greenshot.exe, e.g. a portable one, doesn't count.
- **Switching it on** writes the Run value of the user, unless an entry of the machine already starts this Greenshot (it would start twice),
  and removes a "switched off" of Windows for it.
- **Switching it off** removes the entries which start this Greenshot.exe. The one of the machine needs administrator rights; without them it's only logged.
- The settings only change the entries when the checkbox was changed.

```mermaid
flowchart TD
    A[Settings open] --> B["checkbox = IsAutostartEnabled():<br/>an entry starts this Greenshot.exe<br/>and isn't switched off in Windows"]
    B --> C[Save]
    C --> D{"checkbox changed?"}
    D -- no --> E[leave the entries alone]
    D -- "switched on" --> F["EnableAutostart: HKCU Run value,<br/>clear StartupApproved 'off'"]
    D -- "switched off" --> G["DisableAutostart: remove the entries<br/>which start this Greenshot.exe"]
```

Not handled yet, for the Self-Service troubleshooter: links in the Startup folders, entries of other Greenshot installations and duplicates.
