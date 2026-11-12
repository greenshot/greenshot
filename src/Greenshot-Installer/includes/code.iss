[Code]
function FullInstall(Param : String) : String;
begin
	result := SetupMessage(msgFullInstallation);
end;

function CustomInstall(Param : String) : String;
begin
	result := SetupMessage(msgCustomInstallation);
end;

function CompactInstall(Param : String) : String;
begin
	result := SetupMessage(msgCompactInstallation);
end;

// Build a list of greenshot parameters from the supplied installer parameters
function GetParamsForGS(argument: String): String;
var
	i: Integer;
	parametersString: String;
	currentParameter: String;
	foundStart: Boolean;
	foundNoRun: Boolean;
	foundLanguage: Boolean;
begin
	foundNoRun := false;
	foundLanguage := false;
	foundStart := false;
	for i:= 0 to ParamCount() do begin
		currentParameter := ParamStr(i);

		// check if norun is supplied
		if Lowercase(currentParameter) = '--no-run' then begin
			foundNoRun := true;
			continue;
		end;

		if foundStart then begin
			parametersString := parametersString + ' ' + currentParameter;
			foundStart := false;
		end
		else begin
			if Lowercase(currentParameter) = '--language' then begin
				foundStart := true;
				foundLanguage := true;
				parametersString := parametersString + ' ' + currentParameter;
			end;
		end;
	end;
	if not foundLanguage then begin
		parametersString := parametersString + ' --language ' + ExpandConstant('{language}');
	end;
	if foundNoRun then begin
		parametersString := parametersString + ' --no-run';
	end;
	// For debugging comment out the following
	//MsgBox(parametersString, mbInformation, MB_OK);

	Result := parametersString;
end;

// Check if language group is installed
function hasLanguageGroup(argument: String): Boolean;
var
	keyValue: String;
	returnValue: Boolean;
begin
	returnValue := true;
	if (RegQueryStringValue( HKLM, 'SYSTEM\CurrentControlSet\Control\Nls\Language Groups', argument, keyValue)) then begin
		if Length(keyValue) = 0 then begin
			returnValue := false;
		end;
	end;
	Result := returnValue;
end;

var
	GreenshotWasRunning: Boolean;
	GreenshotCanBeRestartedByRM: Boolean;

const
  GreenshotMutexes = 'F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08,Global\F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08,Local\F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08';

function HasCommandLineSwitch(Switch: String): Boolean;
var
  I: Integer;
begin
  Result := False;
  for I := 1 to ParamCount do
    if CompareText(ParamStr(I), Switch) = 0 then
      Result := True;
end;

// Asks the running Greenshot to exit, like a click on Exit in its menu: it saves its settings and asks about unsaved editors.
// Greenshot up to 1.3 takes /exit, newer ones --exit. Returns True when no Greenshot runs anymore (waits up to 10 seconds).
function CloseRunningGreenshot(ExePath, Version: String): Boolean;
var
  ExitArgument: String;
  ResultCode, Waited: Integer;
begin
  Result := not CheckForMutexes(GreenshotMutexes);
  if Result then
    exit;
  if (Pos('0.', Version) = 1) or (Pos('1.0', Version) = 1) or (Pos('1.1', Version) = 1) or (Pos('1.2', Version) = 1) or (Pos('1.3', Version) = 1) then
    ExitArgument := '/exit'
  else
    ExitArgument := '--exit';
  if not FileExists(ExePath) then
  begin
    Log('Greenshot runs, but ' + ExePath + ' is missing, it can not be asked to exit.');
    exit;
  end;
  Log('Asking the running Greenshot to exit: ' + ExePath + ' ' + ExitArgument);
  ExecAsOriginalUser(ExePath, ExitArgument, '', SW_HIDE, ewNoWait, ResultCode);
  Waited := 0;
  while (Waited < 10000) and CheckForMutexes(GreenshotMutexes) do
  begin
    Sleep(250);
    Waited := Waited + 250;
  end;
  Result := not CheckForMutexes(GreenshotMutexes);
  if Result then
    Log('Greenshot exited.')
  else
    Log('Greenshot still runs after 10 seconds.');
end;

function InitializeSetup(): Boolean;
var
  PrevAppDir: String;
  VersionMS, VersionLS: Cardinal;
  Major, Minor, Revision: Word;
  IsOldVersion: Boolean;
begin
  IsOldVersion := False;

  // Try to find the previous installation path of Greenshot
  if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\Greenshot_is1', 'Inno Setup: App Path', PrevAppDir) or
     RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\Greenshot_is1', 'Inno Setup: App Path', PrevAppDir) or
     RegQueryStringValue(HKLM, 'Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Greenshot_is1', 'Inno Setup: App Path', PrevAppDir) then
  begin
    if PrevAppDir <> '' then
    begin
      // Read the version of the old executable
      if GetVersionNumbers(PrevAppDir + '\Greenshot.exe', VersionMS, VersionLS) then
      begin
        Major := VersionMS shr 16;
        Minor := VersionMS and $FFFF;
        Revision := VersionLS shr 16;

        if (Major = 1) and (Minor = 3) and (Revision <= 315) then
          IsOldVersion := True;
        if (Major = 1) and (Minor < 3) then
          IsOldVersion := True;
        if (Major = 0) then
          IsOldVersion := True;
      end;
    end;
  end;

  // If Greenshot is running AND it is an old version that doesn't support the Restart Manager, abort.
  // We use a loop with the built-in translated msgSetupAppRunningError so the user can close it and click OK.
  // Unattended nobody can close it: it's asked to exit. With /SUPPRESSMSGBOXES the message box answers Cancel, the setup ends instead of waiting.
  if IsOldVersion and (HasCommandLineSwitch('/SILENT') or HasCommandLineSwitch('/VERYSILENT')) then
    CloseRunningGreenshot(PrevAppDir + '\Greenshot.exe', '1.3');
  while IsOldVersion and CheckForMutexes(GreenshotMutexes) do
  begin
    if SuppressibleMsgBox(FmtMessage(SetupMessage(msgSetupAppRunningError), ['Greenshot']), mbError, MB_OKCANCEL, IDCANCEL) = IDCANCEL then
    begin
      Log('Greenshot runs and was not closed, the setup ends.');
      Result := False;
      Exit;
    end;
  end;

  // Check for .NET and install 4.8.0 if we don't have it
  Result := IsDotNetInstalled(net48, 0); //Returns True if .NET Framework version 4.6.2 is installed, or a compatible version such as 4.8.0
  if not Result then
    SuppressibleMsgBox(FmtMessage(SetupMessage(msgWinVersionTooLowError), ['.NET Framework', '4.8.0']), mbCriticalError, MB_OK, IDOK);
end;

/////////////////////////////////////////////////////////////////////
// greenshot-mcp.exe (a separate download, extracted next to Greenshot.exe) keeps running in the AI tool while it waits for
// Greenshot, and locks its file in the installation directory. Greenshot tells it to exit when the Restart Manager closes
// Greenshot; this is the fallback for an install, an update and an uninstall.
/////////////////////////////////////////////////////////////////////
procedure CloseMcpServer();
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM greenshot-mcp.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

// autostart.inf wrote the Run value, only this user's can be removed: Windows skips the ones of other users
// when the file is gone. Another Greenshot, e.g. a portable one, keeps its value.
procedure RemoveAutostart();
var
  RunValue: String;
begin
  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', '{#ExeName}', RunValue) then
    if Pos(Lowercase(ExpandConstant('{app}\{#ExeName}.exe')), Lowercase(RunValue)) > 0 then
      RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', '{#ExeName}');
  RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Microsoft\Active Setup\Installed Components\{{#AutostartComponentGuid}}');
end;

/////////////////////////////////////////////////////////////////////
// Files in use are set aside (issue #732). A deployment tool often runs the setup in another session (as SYSTEM, from a service),
// there it can't see or close the Greenshot of the user (the Restart Manager reports "Session Mismatch"). A running program or a
// loaded library can't be replaced or deleted, but it can be renamed: it's renamed to *.old, the new file is installed under its name.
// The running Greenshot keeps its files and notices the new version itself (InstalledUpdate.cs). The *.old files are deleted at the
// next restart of Windows, by the next setup and by the uninstaller. See docs/vNext/autostart.md.
/////////////////////////////////////////////////////////////////////
var
  // The files in use and the names they were renamed to, at the same index
  SetAsideFiles, SetAsideNames: TStringList;
  InstallationDone: Boolean;

// A file which can't be opened exclusively for writing is in use: a running program or a loaded library
function IsFileInUse(FileName: String): Boolean;
var
  Stream: TFileStream;
begin
  Result := False;
  try
    Stream := TFileStream.Create(FileName, fmOpenReadWrite or fmShareExclusive);
    Stream.Free;
  except
    Result := True;
  end;
end;

// The name a file in use gets: FileName.old, or FileName.1.old when an earlier one is still in use
function GetSetAsideName(FileName: String): String;
var
  Number: Integer;
begin
  Result := FileName + '.old';
  Number := 0;
  while FileExists(Result) and not DeleteFile(Result) do
  begin
    Number := Number + 1;
    Result := FileName + '.' + IntToStr(Number) + '.old';
  end;
end;

// Finds the files in use in the directory and its sub directories. With Rename they are renamed to *.old (remembered in SetAsideFiles
// and SetAsideNames), else only tried: renamed and back. Returns False when a file in use can't be renamed.
function SetAsideFilesInUse(Directory: String; Rename: Boolean): Boolean;
var
  FindRec: TFindRec;
  FileName, SetAsideName: String;
begin
  Result := True;
  if not FindFirst(AddBackslash(Directory) + '*', FindRec) then
    exit;
  try
    repeat
      if (FindRec.Name = '.') or (FindRec.Name = '..') then
        continue;
      FileName := AddBackslash(Directory) + FindRec.Name;
      if FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0 then
      begin
        if not SetAsideFilesInUse(FileName, Rename) then
          Result := False;
        continue;
      end;
      if CompareText(ExtractFileExt(FileName), '.old') = 0 then
        continue;
      if not IsFileInUse(FileName) then
        continue;
      SetAsideName := GetSetAsideName(FileName);
      if not RenameFile(FileName, SetAsideName) then
      begin
        Log('In use and can not be renamed: ' + FileName);
        Result := False;
      end
      else if Rename then
      begin
        Log('In use, renamed to ' + SetAsideName);
        SetAsideFiles.Add(FileName);
        SetAsideNames.Add(SetAsideName);
      end
      else if not RenameFile(SetAsideName, FileName) then
      begin
        // Can't happen for a file we could rename a moment ago, but then it stays set aside
        Log('In use, renamed to ' + SetAsideName + ', it could not be renamed back');
        SetAsideFiles.Add(FileName);
        SetAsideNames.Add(SetAsideName);
      end;
    until not FindNext(FindRec);
  finally
    FindClose(FindRec);
  end;
end;

// Deletes the *.old files which aren't in use anymore. With AtRestart the others are deleted at the next restart of Windows
// (needs administrator rights), else they stay for the next setup.
procedure DeleteSetAsideFiles(Directory: String; AtRestart: Boolean);
var
  FindRec: TFindRec;
  FileName: String;
begin
  if not FindFirst(AddBackslash(Directory) + '*', FindRec) then
    exit;
  try
    repeat
      if (FindRec.Name = '.') or (FindRec.Name = '..') then
        continue;
      FileName := AddBackslash(Directory) + FindRec.Name;
      if FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0 then
      begin
        DeleteSetAsideFiles(FileName, AtRestart);
        continue;
      end;
      if CompareText(ExtractFileExt(FileName), '.old') <> 0 then
        continue;
      if DeleteFile(FileName) or not AtRestart then
        continue;
      try
        RestartReplace(FileName, '');
        Log('Still in use, deleted at the next restart of Windows: ' + FileName);
      except
        Log('Still in use, deleted by the next setup: ' + FileName);
      end;
    until not FindNext(FindRec);
  finally
    FindClose(FindRec);
  end;
end;

// The setup ended without installing (cancelled, or an error rolled it back): the files set aside get their names back
procedure RestoreSetAsideFiles();
var
  I: Integer;
begin
  for I := 0 to SetAsideFiles.Count - 1 do
  begin
    if FileExists(SetAsideFiles[I]) then
      Log('Not renamed back, ' + SetAsideFiles[I] + ' exists')
    else if not RenameFile(SetAsideNames[I], SetAsideFiles[I]) then
      Log('Could not rename ' + SetAsideNames[I] + ' back');
  end;
end;

procedure DeinitializeSetup();
begin
  if (SetAsideFiles <> nil) and not InstallationDone then
    RestoreSetAsideFiles();
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    CloseMcpServer();
    RemoveAutostart();
  end;
  if CurUninstallStep = usPostUninstall then
    DeleteSetAsideFiles(ExpandConstant('{app}'), False);
end;

// Greenshot 1.3 installed for the current user by default (in the profile). An installation for all users doesn't replace it, Inno Setup
// only finds the previous installation in the registry of the install mode it runs in. Both would stay, and uninstalling the old one later
// removes the Run value and the Print key setting of this one. So it's removed first, when the user agrees: this installation writes both
// values again afterwards (task "startup", component "disablesnippingtool"). See docs/vNext/autostart.md.
procedure RemoveOlderPerUserInstallation();
var
  UninstallKey, Location, Version, Uninstaller, Parameters: String;
  ResultCode: Integer;
begin
  if not IsAdminInstallMode then
    exit;
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\Greenshot_is1';
  if not RegQueryStringValue(HKCU, UninstallKey, 'InstallLocation', Location) then
    exit;
  if not RegQueryStringValue(HKCU, UninstallKey, 'UninstallString', Uninstaller) then
    exit;
  Uninstaller := RemoveQuotes(Uninstaller);
  if not FileExists(Uninstaller) then
  begin
    Log('The older installation for the current user in ' + Location + ' has no uninstaller, it is left alone.');
    exit;
  end;
  if not RegQueryStringValue(HKCU, UninstallKey, 'DisplayVersion', Version) then
    Version := '';

  // With /SUPPRESSMSGBOXES the answer is Yes
  if SuppressibleMsgBox(FmtMessage(CustomMessage('removeolderperuser'), [Version, Location, ExpandConstant('{app}')]), mbConfirmation, MB_YESNO, IDYES) <> IDYES then
  begin
    Log('The user keeps the older installation in ' + Location);
    exit;
  end;

  // The user wants it gone: a Greenshot which still runs is asked to exit, its uninstaller refuses to run otherwise (AppMutex).
  // If it doesn't exit, the old uninstaller asks the user to close it (not when very silent).
  CloseRunningGreenshot(AddBackslash(Location) + 'Greenshot.exe', Version);

  // Not very silent while this setup shows something: the old uninstaller can ask to close a Greenshot which still runs
  if HasCommandLineSwitch('/VERYSILENT') then
    Parameters := '/VERYSILENT /NORESTART'
  else
    Parameters := '/SILENT /NORESTART';
  if HasCommandLineSwitch('/SUPPRESSMSGBOXES') then
    Parameters := Parameters + ' /SUPPRESSMSGBOXES';
  Log('Removing the older installation for the current user: ' + Uninstaller + ' ' + Parameters);
  if not Exec(Uninstaller, Parameters, '', SW_SHOW, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0)
     or RegKeyExists(HKCU, UninstallKey) then
  begin
    Log('The older installation was not removed, exit code ' + IntToStr(ResultCode));
    SuppressibleMsgBox(FmtMessage(CustomMessage('removeolderperuserfailed'), [Location]), mbInformation, MB_OK, IDOK);
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
	CloseMcpServer();
	RemoveOlderPerUserInstallation();
	// We must check if Greenshot is running HERE at the handoff point before installation.
	// This prevents a stale snapshot if the user closes/starts Greenshot while sitting on the wizard pages.
	// PrepareToInstall fires right before the Restart Manager checks for locked files.
	GreenshotWasRunning := CheckForMutexes(GreenshotMutexes);
	Result := '';

	// The *.old files of an earlier update which aren't in use anymore
	DeleteSetAsideFiles(ExpandConstant('{app}'), False);
	// Only tried here: the files are renamed when the installation starts (ssInstall), after the Restart Manager closed what it could.
	// When a file in use can't be renamed (e.g. a security program prevents it), the setup ends before anything changed (exit code 7).
	SetAsideFiles := TStringList.Create;
	SetAsideNames := TStringList.Create;
	if not SetAsideFilesInUse(ExpandConstant('{app}'), False) then
		Result := FmtMessage(SetupMessage(msgSetupAppRunningError), ['{#ExeName}']);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  VersionMS, VersionLS: Cardinal;
  Major, Minor, Revision: Word;
begin
  if CurStep = ssInstall then
  begin
    GreenshotCanBeRestartedByRM := False;

    // At this point (right before installation starts), {app} is fully initialized
    // and we can check the version of the OLD Greenshot.exe before it gets overwritten.
    if GetVersionNumbers(ExpandConstant('{app}\Greenshot.exe'), VersionMS, VersionLS) then
    begin
      Major := VersionMS shr 16;
      Minor := VersionMS and $FFFF;
      Revision := VersionLS shr 16;

      // Restart Manager restart is only correctly supported in 1.4.143 and newer.
      if (Major > 1) then
        GreenshotCanBeRestartedByRM := True
      else if (Major = 1) then
      begin
        if (Minor > 4) then
          GreenshotCanBeRestartedByRM := True
        else if (Minor = 4) and (Revision >= 143) then
          GreenshotCanBeRestartedByRM := True;
      end;
    end;

    // What is still in use now runs where the Restart Manager can't close it (another session), or the user kept it running
    SetAsideFilesInUse(ExpandConstant('{app}'), True);
  end;

  if CurStep = ssPostInstall then
  begin
    InstallationDone := True;
    // Usually still in use: then deleted at the next restart of Windows, or by the next setup
    DeleteSetAsideFiles(ExpandConstant('{app}'), True);
  end;
end;

function ShouldDisableSnippingTool: Boolean;
begin
  Result := WizardIsComponentSelected('disablesnippingtool');
end;

/////////////////////////////////////////////////////////////////////
// Restart manager support. This is needed to avoid launching a duplicate
// Greenshot instance when the Restart Manager already handles restart.
// When CloseApplications=yes and RestartApplications=yes (setup-header.iss),
// the RM will close Greenshot before installation and restart it afterwards.
// In that case, the "Start Greenshot" [Run] entry must be skipped.
/////////////////////////////////////////////////////////////////////
function NotAlreadyRestarted: Boolean;
begin
  // Skip the 'Start Greenshot' post-install option if it was running before installation
  // AND the installed version is new enough to support the Windows Restart Manager.
  Result := not (GreenshotWasRunning and GreenshotCanBeRestartedByRM);
end;

function GetCurrentProcessId(): Cardinal;
  external 'GetCurrentProcessId@kernel32.dll stdcall';
function ProcessIdToSessionId(ProcessId: Cardinal; var SessionId: Cardinal): BOOL;
  external 'ProcessIdToSessionId@kernel32.dll stdcall';

// False when the setup runs in session 0, where services run: started as SYSTEM or by a service (e.g. a deployment tool).
// Nobody sees a program there, and a Greenshot started there would keep its files in use at the next update.
function IsUserSession: Boolean;
var
  SessionId: Cardinal;
begin
  Result := not ProcessIdToSessionId(GetCurrentProcessId(), SessionId) or (SessionId <> 0);
  if not Result then
    Log('The setup runs in session 0, Greenshot and the web page are not started.');
end;

function ShouldStartGreenshot: Boolean;
begin
  Result := NotAlreadyRestarted and IsUserSession;
end;

var
    LanguagePreselected: Boolean;

function GetUserDefaultUILanguage(): Word;
    external 'GetUserDefaultUILanguage@kernel32.dll stdcall';

procedure CurPageChanged(CurPageID: Integer);
var
    LangID: Word;
    PrimaryLangID: Word;
    CompName: String;
begin
    if (CurPageID = wpSelectComponents) and (not LanguagePreselected) then
    begin
        LanguagePreselected := True;
        LangID := GetUserDefaultUILanguage();
        PrimaryLangID := LangID and $03FF;
        
        // Exact matches for specific regions
        case LangID of
            $0416: CompName := 'languages\ptBR';
            $0816: CompName := 'languages\ptPT';
            $0C0C: CompName := 'languages\frQC';
            $0404, $0C04, $1404: CompName := 'languages\zhTW';
            $0804, $1004: CompName := 'languages\zhCN';
        else
            // Fallback to primary language
            case PrimaryLangID of
                $01: CompName := 'languages\arSY';
                $03: CompName := 'languages\caCA';
                $05: CompName := 'languages\csCZ';
                $06: CompName := 'languages\daDK';
                $08: CompName := 'languages\elGR';
                $0A: CompName := 'languages\esES';
                $25: CompName := 'languages\etEE';
                $29: CompName := 'languages\faIR';
                $0B: CompName := 'languages\fiFI';
                $0C: CompName := 'languages\frFR';
                $0D: CompName := 'languages\heIL';
                $0E: CompName := 'languages\huHU';
                $21: CompName := 'languages\idID';
                $10: CompName := 'languages\itIT';
                $11: CompName := 'languages\jaJP';
                $12: CompName := 'languages\koKR';
                $27: CompName := 'languages\ltLT';
                $26: CompName := 'languages\lvLV';
                $14: CompName := 'languages\nnNO';
                $15: CompName := 'languages\plPL';
                $16: CompName := 'languages\ptPT'; // Fallback
                $18: CompName := 'languages\roRO';
                $19: CompName := 'languages\ruRU';
                $1B: CompName := 'languages\skSK';
                $24: CompName := 'languages\slSI';
                $1A: CompName := 'languages\srRS';
                $1D: CompName := 'languages\svSE';
                $1F: CompName := 'languages\trTR';
                $22: CompName := 'languages\ukUA';
                $2A: CompName := 'languages\viVN';
                $04: CompName := 'languages\zhCN';
            end;
        end;

        if CompName <> '' then
        begin
            if WizardSelectedComponents(False) <> '' then
            begin
                WizardSelectComponents(WizardSelectedComponents(False) + ',' + CompName);
            end
            else
            begin
                WizardSelectComponents(CompName);
            end;
        end;
    end;
end;
