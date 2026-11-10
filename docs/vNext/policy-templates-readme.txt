Greenshot administrative templates (Group Policy, Intune)

greenshot.admx and en-US\greenshot.adml show every Greenshot setting in the Group Policy editor and in Intune.

Group Policy:
  Copy greenshot.admx to \\<domain>\SYSVOL\<domain>\Policies\PolicyDefinitions
  and en-US\greenshot.adml to ...\PolicyDefinitions\en-US
  (for one computer: C:\Windows\PolicyDefinitions and C:\Windows\PolicyDefinitions\en-US).
  The settings are below Administrative Templates > Greenshot.

Intune:
  Devices > Configuration > Import ADMX, import greenshot.admx with greenshot.adml,
  then create a profile "Templates > Imported Administrative templates".

Greenshot reads the policies from HKLM/HKCU\SOFTWARE\Policies\Greenshot\Greenshot\<Section>\<Key>
(mandatory) and ...\Greenshot\Recommended\<Section>\<Key> (recommended).

More: https://github.com/greenshot/greenshot/blob/main/docs/vNext/policies.md
