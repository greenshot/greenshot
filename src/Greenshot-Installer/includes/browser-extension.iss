; Browser integration: the Native Messaging host for the Greenshot browser extension.
; Not part of the installer yet. To build an installer that includes it, uncomment the #include of this file in
; setup.iss (and setup-light.iss). greenshot-proxy.exe itself is always installed, it also opens files (see tasks-icons-reg.iss)
; and greenshot: URLs (see url-protocol.iss).

[Files]
Source: additional_files\org.greenshot.proxy.json; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: additional_files\org.greenshot.proxy-firefox.json; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}

[Registry]
; Native Messaging hosts, in HKLM (Strictly machine-level install per ADR 001)
Root: HKLM; Subkey: Software\Google\Chrome\NativeMessagingHosts\org.greenshot.proxy; ValueType: string; ValueName: ""; ValueData: "{app}\org.greenshot.proxy.json"; Flags: uninsdeletekey noerror; Check: IsAdminInstallMode
Root: HKLM; Subkey: Software\Microsoft\Edge\NativeMessagingHosts\org.greenshot.proxy; ValueType: string; ValueName: ""; ValueData: "{app}\org.greenshot.proxy.json"; Flags: uninsdeletekey noerror; Check: IsAdminInstallMode
Root: HKLM; Subkey: Software\Mozilla\NativeMessagingHosts\org.greenshot.proxy; ValueType: string; ValueName: ""; ValueData: "{app}\org.greenshot.proxy-firefox.json"; Flags: uninsdeletekey noerror; Check: IsAdminInstallMode

