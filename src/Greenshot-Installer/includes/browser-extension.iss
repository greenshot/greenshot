; Browser integration: the Native Messaging host for the Greenshot browser extension and the greenshot: URL protocol.
; Not part of the installer yet. To build an installer that includes it, uncomment the #include of this file in
; setup.iss (and setup-light.iss). greenshot-proxy.exe itself is always installed, it also opens files (see tasks-icons-reg.iss).

[Files]
Source: additional_files\org.greenshot.proxy.json; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}
Source: additional_files\org.greenshot.proxy-firefox.json; DestDir: {app}; Components: greenshot; Flags: {#DefaultInstallFlags}

[Registry]
; Custom URL protocol greenshot: (registry keys are case-insensitive: the file type ProgIDs must never be called "greenshot")
Root: HKA; Subkey: Software\Classes\greenshot; ValueType: string; ValueName: ""; ValueData: "URL:Greenshot Protocol"; Flags: uninsdeletekey noerror
Root: HKA; Subkey: Software\Classes\greenshot; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\greenshot\DefaultIcon; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.exe"",0"; Flags: noerror
Root: HKA; Subkey: Software\Classes\greenshot\shell\open\command; ValueType: string; ValueName: ""; ValueData: """{app}\greenshot-proxy.exe"" ""%1"""; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\URLAssociations; ValueType: string; ValueName: "greenshot"; ValueData: "greenshot"; Flags: noerror

; Native Messaging hosts, in HKLM (Strictly machine-level install per ADR 001)
Root: HKLM; Subkey: Software\Google\Chrome\NativeMessagingHosts\org.greenshot.proxy; ValueType: string; ValueName: ""; ValueData: "{app}\org.greenshot.proxy.json"; Flags: uninsdeletekey noerror; Check: IsAdminInstallMode
Root: HKLM; Subkey: Software\Microsoft\Edge\NativeMessagingHosts\org.greenshot.proxy; ValueType: string; ValueName: ""; ValueData: "{app}\org.greenshot.proxy.json"; Flags: uninsdeletekey noerror; Check: IsAdminInstallMode
Root: HKLM; Subkey: Software\Mozilla\NativeMessagingHosts\org.greenshot.proxy; ValueType: string; ValueName: ""; ValueData: "{app}\org.greenshot.proxy-firefox.json"; Flags: uninsdeletekey noerror; Check: IsAdminInstallMode

