[Files]
Source: ..\..\bin\Release\*; DestDir: {app}; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion
Source: ..\additional_files\*; DestDir: {app}; Flags: overwritereadonly recursesubdirs ignoreversion replacesameversion
[Setup]
OutputDir=..\
OutputBaseFilename=Greenshot-INSTALLER-585
DefaultDirName={pf}\Greenshot
DefaultGroupName=Greenshot
AppName=Greenshot
AppVerName=Greenshot
AppMutex=F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08,Global\F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08
PrivilegesRequired=none
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Greenshot"; ValueData: "{app}\Greenshot.exe"; Permissions: users-modify; Flags: uninsdeletevalue
[Icons]
Name: {group}\Greenshot; Filename: {app}\Greenshot.exe; WorkingDir: {app}
Name: {group}\Uninstall Greenshot; Filename: {app}\unins000.exe; WorkingDir: {app}
Name: {group}\Readme.txt; Filename: {app}\readme.txt; WorkingDir: {app}
Name: {group}\License.txt; Filename: {app}\license.txt; WorkingDir: {app}
[UninstallRun]
Filename: {app}\Greenshot.exe; Parameters: uninstall; WorkingDir: {app}; Languages: 
[Languages]
Name: en; MessagesFile: compiler:Default.isl
Name: de; MessagesFile: compiler:Languages\German.isl
[CustomMessages]
en.dotnetmissing=This setup requires the .NET Framework v2.0.%nDo you want to download the framework now?
de.dotnetmissing=Dieses Programm benötigt Microsoft .NET Framework v2.0.%nWollen Sie das Framework jetzt downloaden?
[Code]
function InitializeSetup(): Boolean;
var
	ErrorCode : Integer;
	NetFrameWorkInstalled : Boolean;
	MsgBoxResult : Boolean;
begin

	NetFrameWorkInstalled := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\.NETFramework\policy\v2.0');
	if NetFrameWorkInstalled = true then
	begin
		Result := true;
	end;

	if NetFrameWorkInstalled = false then
	begin
		MsgBoxResult := MsgBox(ExpandConstant('{cm:dotnetmissing}'), mbConfirmation, MB_YESNO) = idYes;
		Result := false;
		if MsgBoxResult = true then
		begin
			ShellExec('open',
				'http://download.microsoft.com/download/5/6/7/567758a3-759e-473e-bf8f-52154438565a/dotnetfx.exe',
				'','',SW_SHOWNORMAL,ewNoWait,ErrorCode);
		end;
	end;

end;

[Run]
Filename: {app}\Greenshot.exe; WorkingDir: {app}; Flags: nowait postinstall
[InstallDelete]
Name: {app}; Type: filesandordirs; Languages: 
Name: {userstartup}\Greenshot.lnk; Type: files; Languages:
Name: {commonstartup}\Greenshot.lnk; Type: files; Languages:

