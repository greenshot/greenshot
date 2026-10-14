# Starting Greenshot with Windows

Greenshot starts with Windows through the value `Greenshot` in the Run key of the user:
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, with the quoted path of Greenshot.exe.
Every user decides for themselves, with the checkbox "Launch Greenshot on startup" in the settings; it needs no administrator rights.

## The installer

The task "Start Greenshot with Windows start" runs `Greenshot.exe --enable-autostart` as the user who started the setup,
which writes the Run value and exits without starting Greenshot.

An all-users install has two choices below the task:

- **For every user of this computer** (default): the setup also registers Greenshot in
  Active Setup,
  `HKLM\Software\Microsoft\Active Setup\Installed Components\{17A6D9DC-E818-4278-992A-167EF0FAC433}`.
  At the next logon of every user, Windows runs its `StubPath`, `"<app>\Greenshot.exe" --enable-autostart`, once for that user,
  before the desktop is shown. It remembers that in `HKCU\Software\Microsoft\Active Setup\Installed Components\{…}` and doesn't run it again
  as long as the `Version` doesn't change. A user who switches the start off in the settings keeps it off, also after updates.
  The user who ran the setup gets it right away and is marked as done.
- **Only for me**: only the user who started the setup.

A per-user install only has the task, for the current user.

Before that, the setup removes the old `Greenshot` values from the Run keys of HKCU and HKLM (both registry views):
older installers wrote the value for all users to HKLM, which a user without administrator rights can't switch off.
Without the "every user" choice, the Active Setup key is removed, so no new user gets the start with Windows.

`AutostartComponentVersion` in `tasks-icons-reg.iss` is the Active Setup version.
Raise it only to set up the start with Windows for every user once more, also for those who switched it off since.

Unattended: `/TASKS="startup\allusers"` or `/TASKS="startup\currentuser"` for an all-users install, `/TASKS="startup"` for a per-user install,
`/MERGETASKS="!startup"` for none.

The uninstaller removes the Run value of the user who uninstalls, when it starts this Greenshot.exe, and the Active Setup keys.
The Run values of other users can't be reached; Windows skips them when the file is gone.

## Greenshot

`StartupHelper` reads and writes the entries:

- **Is it on?** When the Run key of the user, or the one of the machine (64-bit and 32-bit view, from an older installer), starts this Greenshot.exe,
  and the entry isn't switched off in Windows (Task Manager > Startup apps, Settings > Apps > Startup). Windows keeps that switch in
  `…\Explorer\StartupApproved\Run` (`Run32` for the 32-bit Run key of the machine): the first byte of the value is odd when it's off.
  An entry for another Greenshot.exe, e.g. a portable one, doesn't count.
- **Switching it on** writes the Run value of the user, unless an entry of the machine already starts this Greenshot (it would start twice),
  and removes a "switched off" of Windows for it.
- **Switching it off** removes the entries which start this Greenshot.exe. The one of the machine needs administrator rights; without them it's only logged.
- The settings only change the entries when the checkbox was changed.

Not handled yet, for the Self-Service troubleshooter: links in the Startup folders, entries of other Greenshot installations and duplicates.
