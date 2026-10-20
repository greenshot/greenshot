; The greenshot: URL protocol, e.g. greenshot://log?search=HDR opens the log viewer filtered on HDR.
; greenshot-proxy.exe passes the URL to Greenshot (or starts it), which only accepts the commands a web page may run
; (see IpcSecurityDispatcher and docs/vNext/greenshot-proxy-and-integration.md).
; Registry keys are case-insensitive: the file type ProgIDs must never be called "greenshot"

[Registry]
Root: HKA; Subkey: Software\Classes\greenshot; ValueType: string; ValueName: ""; ValueData: "URL:Greenshot Protocol"; Flags: uninsdeletekey noerror
Root: HKA; Subkey: Software\Classes\greenshot; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Flags: noerror
Root: HKA; Subkey: Software\Classes\greenshot\DefaultIcon; ValueType: string; ValueName: ""; ValueData: """{app}\Greenshot.exe"",0"; Flags: noerror
Root: HKA; Subkey: Software\Classes\greenshot\shell\open\command; ValueType: string; ValueName: ""; ValueData: """{app}\greenshot-proxy.exe"" ""%1"""; Flags: noerror
Root: HKA; Subkey: Software\Greenshot\Capabilities\URLAssociations; ValueType: string; ValueName: "greenshot"; ValueData: "greenshot"; Flags: noerror
