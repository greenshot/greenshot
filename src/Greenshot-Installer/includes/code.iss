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

function InitializeSetup(): Boolean;
begin
	// We must check if Greenshot is running at the very beginning of the setup.
	// If we check at the end (NotAlreadyRestarted), we hit a race condition:
	// (1) The Restart Manager might have already triggered Greenshot to restart, but 
	// (2) Greenshot takes a moment to create its Mutex. If the Mutex isn't created yet,
	// the installer thinks it's not running and launches a duplicate instance.
	GreenshotWasRunning := CheckForMutexes('F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08,Global\F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08,Local\F48E86D3-E34C-4DB7-8F8F-9A0EA55F0D08');

	// Check for .NET and install 4.8.0 if we don't have it
	Result := IsDotNetInstalled(net48, 0); //Returns True if .NET Framework version 4.6.2 is installed, or a compatible version such as 4.8.0
	if not Result then
		SuppressibleMsgBox(FmtMessage(SetupMessage(msgWinVersionTooLowError), ['.NET Framework', '4.8.0']), mbCriticalError, MB_OK, IDOK);
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
  // Skip the 'Start Greenshot' post-install option if it was running before installation.
  // The Windows Restart Manager handles auto-restarting the application.
  Result := not GreenshotWasRunning;
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
