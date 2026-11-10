# Windows policies: configuring Greenshot with Group Policy or Intune

Every setting of `greenshot.ini` can also be set by a Windows policy: with Group Policy (gpedit.msc, the Group Policy
Management Console), with Intune or with a `.reg` file. Nothing new has to be learned: a policy sets the same key, in the
same section, with the same value as an ini file would. The full Greenshot reads policies, Greenshot Light doesn't.

## Where Greenshot reads them

| Kind | Registry key | Effect |
| --- | --- | --- |
| Mandatory | `HKLM\SOFTWARE\Policies\Greenshot\Greenshot\<Section>\<Key>` (computer) or `HKCU\...` (user) | The user can't change the setting, its control in the settings is disabled |
| Recommended | `HKLM\SOFTWARE\Policies\Greenshot\Greenshot\Recommended\<Section>\<Key>` or `HKCU\...` | A default: the user can still choose another value |

`<Section>` and `<Key>` are the names in `greenshot.ini`, e.g. `Core\OutputFileFormat` or `Jira\Url`. A computer policy wins
over a user policy. Only administrators (and Windows itself, for Group Policy and Intune) can write below
`SOFTWARE\Policies`, also in HKCU, so a user can't remove a policy.

Changes are picked up while Greenshot runs (after `gpupdate` or an Intune sync), there is no need to restart Greenshot or to
call `--reload`. A changed plugin or signature setting applies at the next start, like in the settings.

### Values

| Setting type | Registry value |
| --- | --- |
| On / off | `REG_DWORD` 1 or 0 |
| Number | `REG_DWORD` |
| Choice (e.g. `OutputFileFormat`) | `REG_SZ` with the name, as in greenshot.ini |
| Text, folder, template | `REG_SZ` (a `REG_EXPAND_SZ` is not expanded: use `${user.pictures}` and the other [variables](variables.md), not `%USERPROFILE%`) |
| List (e.g. `BlockedDestinations`) | a sub key named like the setting with the values `1`, `2`, … (or one `REG_MULTI_SZ`) |
| Other | `REG_SZ` with the text greenshot.ini would hold |

A value Greenshot can't use is written to the log and skipped; Greenshot always starts.

## Order

From low to high, the later one wins:

1. the default of Greenshot
2. `greenshot-defaults.ini` next to Greenshot.exe
3. recommended policies (user, then computer)
4. the user's own value in `greenshot.ini`
5. `greenshot-fixed.ini` next to Greenshot.exe
6. mandatory policies (user, then computer)

So a mandatory policy also wins over `greenshot-fixed.ini`; the log has a line for each value it replaced.

## What greenshot.ini keeps

`greenshot.ini` only keeps what the user (or Greenshot itself) set. Every other setting is written as a commented-out line
with its current value, e.g.

```ini
; Default file type for writing screenshots.
; OutputFileFormat=png
```

so the file still lists every setting and can be edited by hand: removing the `; ` makes it the user's own value. Because of
this, a changed `greenshot-defaults.ini`, a recommended policy or a new default of a later Greenshot reaches every user who
didn't choose the setting. A value from `greenshot-fixed.ini` or a mandatory policy is never written to `greenshot.ini`.

A `greenshot.ini` written by an earlier Greenshot holds every key. At the first start of this version, Greenshot once turns the
values which are the same as the default back into defaults; after that, only what a user changes is theirs.

## The administrative templates (ADMX)

The templates show every Greenshot setting in the Group Policy editor and in Intune: `greenshot.admx` and
`en-US\greenshot.adml`. They are generated from the settings of Greenshot and its plugins, so they always match.

- **Download:** `Greenshot-PolicyTemplates-<version>.zip` with every release on GitHub. A newer template works for older
  Greenshot versions too, settings only get added.
- **From Greenshot:** Self-Service, section *Windows policies*, **Save policy templates…** writes them for this Greenshot with
  its plugins.
- **In a build:** `Greenshot.exe --policy-templates <folder>` writes them and exits (the release build uses this).

In the editor they are below *Administrative Templates > Greenshot*, with a folder per section and the folder
*Common settings* with the ones used most (plugins, destinations, access to the settings, updates, AI tools, output). The
recommended policies are in *Greenshot > Greenshot - Default Settings (users can override)*, with the same folders. Each policy exists for
*Computer Configuration* and *User Configuration*.

Left out are settings Greenshot keeps for itself (window positions, recent colors, counters, versions, sign-in tokens).

### Group Policy

1. Copy `greenshot.admx` to `\\<domain>\SYSVOL\<domain>\Policies\PolicyDefinitions` and `en-US\greenshot.adml` to
   `...\PolicyDefinitions\en-US` (for a single computer: `C:\Windows\PolicyDefinitions`).
2. Open the Group Policy Management Console, edit a policy object, set the Greenshot policies and link the object.
3. Test on a computer with `gpupdate /force`; Self-Service shows what arrived.

### Intune

1. *Devices > Configuration > Import ADMX*: import `greenshot.admx` with `greenshot.adml`.
2. Create a profile *Templates > Imported Administrative templates* and set the Greenshot policies.

### Without templates: .reg

In the settings, **Block plugins…** / **Block destinations…** opens the window for `greenshot-fixed.ini`; **Save as .reg…**
writes the same lines as mandatory computer policies. The file can be imported with `reg import`, deployed with Group Policy
preferences, a script or any management tool.

## Checking what applies

Self-Service, section *Windows policies*, lists every setting a policy sets, its value and whether it is mandatory or
recommended, for the computer or the user. **Reload** reads them again, **Copy** copies the list for support. The settings
window shows a note when a mandatory policy applies.

## Limits

- Policies bind the Greenshot which reads them. Another copy of Greenshot (an older version, Greenshot Light, a build without
  this) doesn't. To make sure only the managed Greenshot runs, use App Control for Business or AppLocker.
- `--ini-directory` and the portable version don't avoid policies: they are read wherever `greenshot.ini` is.
- Don't put passwords or tokens into policies: every user of the computer can read them.
