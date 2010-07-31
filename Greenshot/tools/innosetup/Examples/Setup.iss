; Inno Setup
; Copyright (C) 1997-2009 Jordan Russell. All rights reserved.
; Portions by Martijn Laan
; For conditions of distribution and use, see LICENSE.TXT.
;
; Inno Setup QuickStart Pack Setup script by Martijn Laan
;
; $jrsoftware: ispack/setup.iss,v 1.92 2010/06/11 11:45:27 mlaan Exp $

[Setup]
AppName=Inno Setup QuickStart Pack
AppId=Inno Setup 5
AppVersion=5.3.10
AppPublisher=Martijn Laan
AppPublisherURL=http://www.innosetup.com/
AppSupportURL=http://www.innosetup.com/
AppUpdatesURL=http://www.innosetup.com/
AppMutex=InnoSetupCompilerAppMutex,Global\InnoSetupCompilerAppMutex
MinVersion=4.1,
DefaultDirName={pf}\Inno Setup 5
DefaultGroupName=Inno Setup 5
AllowNoIcons=yes
Compression=lzma2/ultra
InternalCompressLevel=ultra
SolidCompression=yes
UninstallDisplayIcon={app}\Compil32.exe
LicenseFile=isfiles\license.txt
AppModifyPath="{app}\Ispack-setup.exe" /modify=1
WizardImageFile=compiler:WizModernImage-IS.bmp
WizardSmallImageFile=compiler:WizModernSmallImage-IS.bmp
SetupIconFile=Setup.ico
SignTool=ispacksigntool
SignedUninstaller=yes

[Messages]
AboutSetupNote=Inno Setup Preprocessor home page:%nhttp://ispp.sourceforge.net/

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"
;Name: fileassoc; Description: "{cm:AssocFileExtension,Inno Setup,.iss}"

[Files]
;first the files used by [Code] so these can be quickly decompressed despite solid compression
Source: "otherfiles\ISTool.ico"; Flags: dontcopy
Source: "otherfiles\ISPP.ico"; Flags: dontcopy
Source: "otherfiles\ISCrypt.ico"; Flags: dontcopy
Source: "isxdlfiles\isxdl.dll"; Flags: dontcopy
Source: "isfiles\WizModernSmallImage-IS.bmp"; Flags: dontcopy
;other files
Source: "isfiles\license.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\ISetup.chm"; DestDir: "{app}"; Flags: ignoreversion
Source: "isppfiles\ISPP.chm"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\Compil32.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\ISCC.exe"; DestDir: "{app}"; Flags: ignoreversion; Check: not ISPPCheck
Source: "isppfiles\ISPPCC.exe"; DestDir: "{app}"; DestName: "ISCC.exe"; Flags: ignoreversion; Check: ISPPCheck
Source: "isfiles\ISCmplr.dll"; DestDir: "{app}"; Flags: ignoreversion; Check: not ISPPCheck
Source: "isppfiles\ISCmplr.dll"; DestDir: "{app}"; Flags: ignoreversion; Check: ISPPCheck
Source: "isfiles\ISCmplr.dll"; DestDir: "{app}"; DestName: "ISCmplr.dls"; Flags: ignoreversion; Check: ISPPCheck
Source: "isppfiles\Builtins.iss"; DestDir: "{app}"; Flags: ignoreversion; Check: ISPPCheck
Source: "isfiles\Setup.e32"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\SetupLdr.e32"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\Default.isl"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\Languages\Basque.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\BrazilianPortuguese.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Catalan.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Czech.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Danish.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Dutch.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Finnish.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\French.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\German.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Hebrew.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Hungarian.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Italian.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Japanese.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Norwegian.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Polish.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Portuguese.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Russian.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Slovak.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Slovenian.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\Languages\Spanish.isl"; DestDir: "{app}\Languages"; Flags: ignoreversion
Source: "isfiles\WizModernImage.bmp"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\WizModernImage-IS.bmp"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\WizModernSmallImage.bmp"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\WizModernSmallImage-IS.bmp"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\iszlib.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\isunzlib.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\isbzip.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\isbunzip.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\islzma.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\islzma32.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\islzma64.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\whatsnew.htm"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\isfaq.htm"; DestDir: "{app}"; Flags: ignoreversion
Source: "isfiles\Examples\Example1.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\Example2.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\Example3.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\64Bit.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\64BitThreeArch.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\64BitTwoArch.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\Components.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\Languages.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\MyProg.exe"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\MyProg-x64.exe"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\MyProg-IA64.exe"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\MyProg.chm"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\Readme.txt"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\Readme-Dutch.txt"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\Readme-German.txt"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\CodeExample1.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\CodeDlg.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\CodeClasses.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\CodeDll.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\CodeAutomation.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\CodeAutomation2.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\CodePrepareToInstall.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\UninstallCodeExample1.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\MyDll.dll"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\MyDll\C\MyDll.c"; DestDir: "{app}\Examples\MyDll\C"; Flags: ignoreversion
Source: "isfiles\Examples\MyDll\C\MyDll.def"; DestDir: "{app}\Examples\MyDll\C"; Flags: ignoreversion
Source: "isfiles\Examples\MyDll\C\MyDll.dsp"; DestDir: "{app}\Examples\MyDll\C"; Flags: ignoreversion
Source: "isfiles\Examples\MyDll\Delphi\MyDll.dpr"; DestDir: "{app}\Examples\MyDll\Delphi"; Flags: ignoreversion
Source: "isfiles\Examples\ISPPExample1.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "isfiles\Examples\ISPPExample1License.txt"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "Setup.iss"; DestDir: "{app}\Examples"; Flags: ignoreversion
Source: "Setup.ico"; DestDir: "{app}\Examples"; Flags: ignoreversion
;external files
Source: "{tmp}\ISCrypt.dll"; DestDir: "{app}"; Flags: external ignoreversion; Check: ISCryptCheck
Source: "{srcexe}"; DestDir: "{app}"; DestName: "Ispack-setup.exe"; Flags: external ignoreversion; Check: not ModifyingCheck

[InstallDelete]
;optional ISPP files
Type: files; Name: {app}\Iscmplr.dls
Type: files; Name: {app}\Builtins.iss
;optional ISCrypt files
Type: files; Name: {app}\IsCrypt.dll
;optional desktop icon files
Type: files; Name: {commondesktop}\Inno Setup Compiler.lnk
;older versions created the desktop icon under {userdesktop}
Type: files; Name: "{userdesktop}\Inno Setup Compiler.lnk"

[UninstallDelete]
Type: files; Name: "{app}\Examples\Output\setup.exe"
Type: files; Name: "{app}\Examples\Output\setup-*.bin"
Type: dirifempty; Name: "{app}\Examples\Output"
Type: dirifempty; Name: "{app}\Examples\MyDll\Delphi"
Type: dirifempty; Name: "{app}\Examples\MyDll\C"
Type: dirifempty; Name: "{app}\Examples\MyDll"
Type: dirifempty; Name: "{app}\Examples"

[Icons]
Name: "{group}\Inno Setup Compiler"; Filename: "{app}\Compil32.exe"; WorkingDir: "{app}"; AppUserModelID: "JR.InnoSetup.IDE.5"
Name: "{group}\Inno Setup Documentation"; Filename: "{app}\ISetup.chm";
Name: "{group}\Inno Setup Example Scripts"; Filename: "{app}\Examples\";
Name: "{group}\Inno Setup Preprocessor Documentation"; Filename: "{app}\ISPP.chm";
Name: "{group}\Inno Setup FAQ"; Filename: "{app}\isfaq.htm";
Name: "{group}\Inno Setup Revision History"; Filename: "{app}\whatsnew.htm";
Name: "{commondesktop}\Inno Setup Compiler"; Filename: "{app}\Compil32.exe"; WorkingDir: "{app}"; AppUserModelID: "JR.InnoSetup.IDE.5"; Tasks: desktopicon; Check: not ISToolCheck

[Dirs]
Name: "{localappdata}\ISTool"

[INI]
Filename: "{localappdata}\ISTool\ISTool.ini"; Section: "prefs"; Key: "InnoFolder"; String: "{app}"; Flags: createkeyifdoesntexist

[Run]
Filename: "{tmp}\istool-setup.exe"; StatusMsg: "Installing ISTool..."; Parameters: "/verysilent /group=""{groupname}\ISTool"" desktopicon"; Flags: skipifdoesntexist; Check: ISToolCheck; Tasks: desktopicon
Filename: "{tmp}\istool-setup.exe"; StatusMsg: "Installing ISTool..."; Parameters: "/verysilent /group=""{groupname}\ISTool"""; Flags: skipifdoesntexist; Check: ISToolCheck; Tasks: not desktopicon
Filename: "{app}\Compil32.exe"; Parameters: "/ASSOC"; StatusMsg: "{cm:AssocingFileExtension,Inno Setup,.iss}"; Check: not ISToolCheck
Filename: "{app}\Compil32.exe"; WorkingDir: "{app}"; Description: "{cm:LaunchProgram,Inno Setup}"; Flags: nowait postinstall skipifsilent; Check: not ISToolCheck and not ModifyingCheck
Filename: "{code:GetISToolPath}\ISTool.exe"; WorkingDir: "{code:GetISToolPath}"; Description: "{cm:LaunchProgram,ISTool}"; Flags: nowait postinstall skipifsilent skipifdoesntexist; Check: ISToolCheck and not ModifyingCheck

[UninstallRun]
Filename: "{app}\Compil32.exe"; Parameters: "/UNASSOC"; RunOnceId: "RemoveISSAssoc"

[Code]
var
  Modifying: Boolean;

  ISToolPage, ISPPPage, ISCryptPage: TWizardPage;
  ISToolCheckBox, ISPPCheckBox, ISCryptCheckBox: TCheckBox;
  ISToolOrg: Boolean;

  FilesDownloaded: Boolean;
  
  ISToolPath: String;
  ISToolPathRead: Boolean;

procedure isxdl_AddFile(URL, Filename: AnsiString);
external 'isxdl_AddFile@files:isxdl.dll stdcall';
function isxdl_DownloadFiles(hWnd: Integer): Integer;
external 'isxdl_DownloadFiles@files:isxdl.dll stdcall';
function isxdl_SetOption(Option, Value: AnsiString): Integer;
external 'isxdl_SetOption@files:isxdl.dll stdcall';

function GetModuleHandle(lpModuleName: LongInt): LongInt;
external 'GetModuleHandleA@kernel32.dll stdcall';
function ExtractIcon(hInst: LongInt; lpszExeFileName: AnsiString; nIconIndex: LongInt): LongInt;
external 'ExtractIconA@shell32.dll stdcall';
function DrawIconEx(hdc: LongInt; xLeft, yTop: Integer; hIcon: LongInt; cxWidth, cyWidth: Integer; istepIfAniCur: LongInt; hbrFlickerFreeDraw, diFlags: LongInt): LongInt;
external 'DrawIconEx@user32.dll stdcall';
function DestroyIcon(hIcon: LongInt): LongInt;
external 'DestroyIcon@user32.dll stdcall';

const
  DI_NORMAL = 3;

function InitializeSetup(): Boolean;
begin
  Modifying := ExpandConstant('{param:modify|0}') = '1';
  FilesDownloaded := False;
  ISToolPathRead := False;
    
  Result := True;
end;

function CreateCustomOptionPage(AAfterId: Integer; ACaption, ASubCaption, AIconFileName, ALabel1Caption, ALabel2Caption,
  ACheckCaption: String; var CheckBox: TCheckBox): TWizardPage;
var
  Page: TWizardPage;
  Rect: TRect;
  hIcon: LongInt;
  Label1, Label2: TNewStaticText;
begin
  Page := CreateCustomPage(AAfterID, ACaption, ASubCaption);
  
  try
    AIconFileName := ExpandConstant('{tmp}\' + AIconFileName);
    if not FileExists(AIconFileName) then
      ExtractTemporaryFile(ExtractFileName(AIconFileName));

    Rect.Left := 0;
    Rect.Top := 0;
    Rect.Right := 32;
    Rect.Bottom := 32;

    hIcon := ExtractIcon(GetModuleHandle(0), AIconFileName, 0);
    try
      with TBitmapImage.Create(Page) do begin
        with Bitmap do begin
          Width := 32;
          Height := 32;
          Canvas.Brush.Color := WizardForm.Color;
          Canvas.FillRect(Rect);
          DrawIconEx(Canvas.Handle, 0, 0, hIcon, 32, 32, 0, 0, DI_NORMAL);
        end;
        Parent := Page.Surface;
      end;
    finally
      DestroyIcon(hIcon);
    end;
  except
  end;

  Label1 := TNewStaticText.Create(Page);
  with Label1 do begin
    AutoSize := False;
    Left := WizardForm.SelectDirLabel.Left;
    Width := Page.SurfaceWidth - Left;
    WordWrap := True;
    Caption := ALabel1Caption;
    Parent := Page.Surface;
  end;
  WizardForm.AdjustLabelHeight(Label1);

  Label2 := TNewStaticText.Create(Page);
  with Label2 do begin
    Top := Label1.Top + Label1.Height + ScaleY(12);
    Caption := ALabel2Caption;
    Parent := Page.Surface;
  end;
  WizardForm.AdjustLabelHeight(Label2);

  CheckBox := TCheckBox.Create(Page);
  with CheckBox do begin
    Top := Label2.Top + Label2.Height + ScaleY(12);
    Width := Page.SurfaceWidth;
    Caption := ACheckCaption;
    Parent := Page.Surface;
  end;
  
  Result := Page;
end;

procedure CreateCustomPages;
var
  Caption, SubCaption1, IconFileName, Label1Caption, Label2Caption, CheckCaption: String;
begin
  Caption := 'ISTool';
  SubCaption1 := 'Would you like to download and install ISTool?';
  IconFileName := 'ISTool.ico';
  Label1Caption :=
    'ISTool is an easy to use Inno Setup Script editor by Bjørnar Henden and meant as a replacement of the standard' +
    ' Compiler IDE that comes with Inno Setup. See http://www.istool.org/ for more information.' + #13#10#13#10 +
    'Using ISTool is especially recommended for new users.' + #13#10#13#10 +
    'Note: the ISTool editor does NOT support Unicode text at the moment.';
  Label2Caption := 'Select whether you would like to download and install ISTool, then click Next.';
  CheckCaption := '&Download and install ISTool';

  ISToolPage := CreateCustomOptionPage(wpSelectProgramGroup, Caption, SubCaption1, IconFileName, Label1Caption, Label2Caption, CheckCaption, ISToolCheckBox);

  Caption := 'Inno Setup Preprocessor';
  SubCaption1 := 'Would you like to install Inno Setup Preprocessor?';
  IconFileName := 'ISPP.ico';
  Label1Caption :=
    'Inno Setup Preprocessor (ISPP) is an add-on by Alex Yackimoff for Inno Setup. ISPP allows' +
    ' you to conditionally compile parts of scripts, to use compile time variables in your scripts and to use built-in' +
    ' functions which for example can read from the registry or INI files at compile time.' + #13#10#13#10 +
    'ISPP also contains a special version of the ISCC command line compiler which can take variable definitions as command' +
    ' line parameters and use them during compilation.' + #13#10#13#10 +
    'ISPP is compatible with ISTool.';
  Label2Caption := 'Select whether you would like to install ISPP, then click Next.';
  CheckCaption := '&Install Inno Setup Preprocessor';

  ISPPPage := CreateCustomOptionPage(ISToolPage.ID, Caption, SubCaption1, IconFileName, Label1Caption, Label2Caption, CheckCaption, ISPPCheckBox);

  Caption := 'Encryption Support';
  SubCaption1 := 'Would you like to download encryption support?';
  IconFileName := 'ISCrypt.ico';
  Label1Caption :=
    'Inno Setup supports encryption. However, because of encryption import/export laws in some countries, encryption support is not included in the main' +
    ' Inno Setup installer. Instead, it can be downloaded from a server located in the Netherlands now.';
  Label2Caption := 'Select whether you would like to download encryption support, then click Next.';
  CheckCaption := '&Download and install encryption support';

  ISCryptPage := CreateCustomOptionPage(ISPPPage.ID, Caption, SubCaption1, IconFileName, Label1Caption, Label2Caption, CheckCaption, ISCryptCheckBox);
end;

procedure InitializeWizard;
begin
  CreateCustomPages;
  
  ISToolCheckBox.Checked := GetPreviousData('ISTool', '1') = '1';
  ISPPCheckBox.Checked := GetPreviousData('ISPP', '1') = '1';
  ISCryptCheckBox.Checked := GetPreviousData('ISCrypt', '1') = '1';

  ISToolOrg := ISToolCheckBox.Checked;
end;

procedure RegisterPreviousData(PreviousDataKey: Integer);
begin
  SetPreviousData(PreviousDataKey, 'ISTool', IntToStr(Ord(ISToolCheckBox.Checked)));
  SetPreviousData(PreviousDataKey, 'ISPP', IntToStr(Ord(ISPPCheckBox.Checked)));
  SetPreviousData(PreviousDataKey, 'ISCrypt', IntToStr(Ord(ISCryptCheckBox.Checked)));
end;

procedure DownloadFiles(ISTool, ISCrypt: Boolean);
var
  hWnd: Integer;
  URL, FileName: String;
begin
  isxdl_SetOption('label', 'Downloading extra files');
  isxdl_SetOption('description', 'Please wait while Setup is downloading extra files to your computer.');

  try
    FileName := ExpandConstant('{tmp}\WizModernSmallImage-IS.bmp');
    if not FileExists(FileName) then
      ExtractTemporaryFile(ExtractFileName(FileName));
    isxdl_SetOption('smallwizardimage', FileName);
  except
  end;

  //turn off isxdl resume so it won't leave partially downloaded files behind
  //resuming wouldn't help anyway since we're going to download to {tmp}
  isxdl_SetOption('resume', 'false');

  hWnd := StrToInt(ExpandConstant('{wizardhwnd}'));
  
  if ISTool then begin
    //URL := 'http://www.istool.org/getistool.aspx?version=5';
    URL := 'http://downloads.sourceforge.net/istool/istool-5.3.0.1.exe';
    FileName := ExpandConstant('{tmp}\istool-setup.exe');
    isxdl_AddFile(URL, FileName);
  end;
  
  if ISCrypt then begin
    URL := 'http://www.xs4all.nl/~mlaan2/iscrypt/ISCrypt.dll';
    FileName := ExpandConstant('{tmp}\ISCrypt.dll');
    isxdl_AddFile(URL, FileName);
  end;

  if isxdl_DownloadFiles(hWnd) <> 0 then
    FilesDownloaded := True
  else
    SuppressibleMsgBox('Setup could not download the extra files. Try again later or download and install the extra files manually.' + #13#13 + 'Setup will now continue installing normally.', mbError, mb_Ok, idOk);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  if ISToolCheckBox.Checked or ISCryptCheckBox.Checked then
    DownloadFiles(ISToolCheckBox.Checked, ISCryptCheckBox.Checked);
  Result := '';
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := Modifying and ((PageID = wpSelectDir) or (PageID = wpSelectProgramGroup) or ((PageID = ISToolPage.ID) and ISToolOrg));
end;

function ModifyingCheck: Boolean;
begin
  Result := Modifying;
end;

function ISToolCheck: Boolean;
begin
  Result := ISToolCheckBox.Checked and FilesDownloaded;
end;

function ISPPCheck: Boolean;
begin
  Result := ISPPCheckBox.Checked;
end;

function ISCryptCheck: Boolean;
begin
  Result := ISCryptCheckBox.Checked and FilesDownloaded;
end;

function GetISToolPath(S: String): String;
var
  ISToolPathKeyName, ISToolPathValueName: String;
begin
  if not ISToolPathRead then begin
    ISToolPathKeyName := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{A9E12684-DD23-4D11-ACAF-6041954BCA00}_is1';
    ISToolPathValueName := 'Inno Setup: App Path';

    if not RegQueryStringValue(HKLM, ISToolPathKeyName, ISToolPathValueName, ISToolPath) then begin
      if not RegQueryStringValue(HKCU, ISToolPathKeyName, ISToolPathValueName, ISToolPath) then begin
        SuppressibleMsgBox('Error launching ISTool:'#13'Could not read ISTool path from registry.', mbError, mb_Ok, idOk);
        ISToolPath := '';
      end;
    end;

    ISToolPathRead := True;
  end;

  Result := ISToolPath;
end;
