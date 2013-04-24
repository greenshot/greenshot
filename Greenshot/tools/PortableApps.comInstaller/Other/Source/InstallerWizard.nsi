;Copyright (C) 2006-2012 John T. Haller

;Website: http://PortableApps.com/Installer

;This software is OSI Certified Open Source Software.
;OSI Certified is a certification mark of the Open Source Initiative.

;This program is free software; you can redistribute it and/or
;modify it under the terms of the GNU General Public License
;as published by the Free Software Foundation; either version 2
;of the License, or (at your option) any later version.

;This program is distributed in the hope that it will be useful,
;but WITHOUT ANY WARRANTY; without even the implied warranty of
;MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;GNU General Public License for more details.

;You should have received a copy of the GNU General Public License
;along with this program; if not, write to the Free Software
;Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

!define APPNAME "PortableApps.com Installer"
!define VER "3.0.5.0"
!define WEBSITE "PortableApps.com/Installer"
!define FRIENDLYVER "3.0.5"
!define PORTABLEAPPS.COMFORMATVERSION "3.0"

;=== Program Details
Name "${APPNAME}"
OutFile "..\..\PortableApps.comInstaller.exe"
Caption "${APPNAME}"
VIProductVersion "${VER}"
VIAddVersionKey ProductName "${APPNAME}"
VIAddVersionKey Comments "For additional details, visit ${WEBSITE}"
VIAddVersionKey CompanyName "PortableApps.com"
VIAddVersionKey LegalCopyright "John T. Haller"
VIAddVersionKey FileDescription "${APPNAME}"
VIAddVersionKey FileVersion "${VER}"
VIAddVersionKey ProductVersion "${VER}"
VIAddVersionKey InternalName "${APPNAME}"
VIAddVersionKey LegalTrademarks "PortableApps.com is a trademark of Rare Ideas, LLC."
VIAddVersionKey OriginalFilename "PortableApps.comInstaller.exe"

;=== Runtime Switches
CRCCheck On
RequestExecutionLevel user

; Best Compression
SetCompress Auto
SetCompressor /SOLID lzma
SetCompressorDictSize 32
SetDatablockOptimize On

;=== Include
;(Standard)
!include WordFunc.nsh
!insertmacro WordReplace
!include FileFunc.nsh
!insertmacro GetFileName
!insertmacro GetParameters
!insertmacro GetParent
!insertmacro GetSize
!include LogicLib.nsh
!include MUI.nsh

;(Addons)
!include dialogs.nsh

;(Custom)
!include MoveFiles.nsh
!include ReadINIStrWithDefault.nsh
!include TBProgress.nsh

;=== Icon & Stye ===
!define MUI_ICON "..\..\App\AppInfo\appicon.ico"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_RIGHT
!define MUI_HEADERIMAGE_BITMAP header.bmp
!define MUI_HEADERIMAGE_BITMAP_RTL header_rtl.bmp

BrandingText "PortableApps.com®"
InstallButtonText "Go >"
ShowInstDetails show
SubCaption 3 " | Processing Files"

;=== Variables
Var FINISHTEXT
Var FINISHTITLE
Var INSTALLAPPDIRECTORY
Var SKIPWELCOMEPAGE
Var AUTOMATICCOMPILE

Var INCLUDESOURCE
Var PORTABLEAPPNAME
Var PORTABLEAPPNAMEDOUBLEDAMPERSANDS
Var PLUGINNAME
Var APPID
Var SHORTNAME
Var APPLANGUAGE
Var ALLLANGUAGES
Var INSTALLERFILENAME
Var OPTIONALCOMPONENTS
Var DISPLAYVERSION
Var COMMONFILESPLUGIN
Var USEEXTRACTEDICON
Var INTERACTIVEMODE
Var EULAVERSION

Var ERROROCCURED

Var AppInfoINIFile
Var InstallerINIFile
Var PluginInstaller
Var OptionalSectionSelectedInstallType

;=== Pages
!define MUI_WELCOMEFINISHPAGE_BITMAP welcomefinish.bmp
!define MUI_WELCOMEPAGE_TITLE "PortableApps.com Installer ${FRIENDLYVER}"
!define MUI_WELCOMEPAGE_TEXT "Welcome to the PortableApps.com Installer.\r\n\r\nThis utility allows you to create a PortableApps.com Installer package for an app in PortableApps.com Format.  Just click next and select the application to package.\r\n\r\nLICENSE: The PortableApps.com Installer can be used with open source and freeware apps provided the installer is unmodified and the app adheres to the current PortableApps.com Format Specification as published at PortableApps.com/development. It may also be used with commercial software by contacting PortableApps.com."
!define MUI_PAGE_CUSTOMFUNCTION_PRE ShowWelcomeWindow
!insertmacro MUI_PAGE_WELCOME
Page custom ShowOptionsWindow LeaveOptionsWindow " | Portable App Folder Selection"
!insertmacro MUI_PAGE_INSTFILES
!define MUI_PAGE_CUSTOMFUNCTION_PRE ShowFinishPage
!define MUI_FINISHPAGE_TITLE "$FINISHTITLE"
!define MUI_FINISHPAGE_TEXT "$FINISHTEXT"
!define MUI_FINISHPAGE_RUN
!define MUI_FINISHPAGE_RUN_NOTCHECKED
!define MUI_FINISHPAGE_RUN_TEXT "Test Installer"
!define MUI_FINISHPAGE_RUN_FUNCTION "RunOnFinish"
!define MUI_FINISHPAGE_SHOWREADME "$EXEDIR\Data\PortableApps.comInstallerLog.txt"
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
!define MUI_FINISHPAGE_SHOWREADME_TEXT "View log file"
!define MUI_FINISHPAGE_CANCEL_ENABLED
!insertmacro MUI_PAGE_FINISH

;=== Languages
!insertmacro MUI_LANGUAGE "English"

Function .onInit
	!insertmacro MUI_INSTALLOPTIONS_EXTRACT "InstallerWizardForm.ini"

	;=== Check for settings.ini
	${IfNot} ${FileExists} $EXEDIR\Data\settings.ini
		CreateDirectory $EXEDIR\Data
		CopyFiles /SILENT $EXEDIR\App\DefaultData\settings.ini $EXEDIR\Data
	${EndIf}

	; Get settings
	ReadINIStr $SKIPWELCOMEPAGE "$EXEDIR\Data\settings.ini" "InstallerWizard" "SkipWelcomePage"
	ReadINIStr $INSTALLAPPDIRECTORY "$EXEDIR\Data\settings.ini" "InstallerWizard" "INSTALLAPPDIRECTORY"

	${GetParameters} $R0
	${If} $R0 != ""
		StrCpy $INSTALLAPPDIRECTORY $R0
		StrCpy $SKIPWELCOMEPAGE "true"
		StrCpy $AUTOMATICCOMPILE "true"
		;Strip quotes from $INSTALLAPPDIRECTORY
		StrCpy $R0 $INSTALLAPPDIRECTORY 1
		${If} $R0 == `"`
			StrCpy $INSTALLAPPDIRECTORY $INSTALLAPPDIRECTORY "" 1
			StrCpy $INSTALLAPPDIRECTORY $INSTALLAPPDIRECTORY -1
		${EndIf}
	${EndIf}

	;=== Pre-Fill Path with Directory
	WriteINIStr $PLUGINSDIR\InstallerWizardForm.ini "Field 2" "State" "$INSTALLAPPDIRECTORY"
FunctionEnd

Function ShowWelcomeWindow
	${If} $SKIPWELCOMEPAGE == "true"
		Abort
	${EndIf}
FunctionEnd

Function ShowOptionsWindow
	!insertmacro MUI_HEADER_TEXT "PortableApps.com Installer ${FRIENDLYVER}" "the open portable software standard"
	${If} $AUTOMATICCOMPILE == "true"
		${If} ${FileExists} "$INSTALLAPPDIRECTORY\App\AppInfo\appinfo.ini"
			StrCpy $AppInfoINIFile "$INSTALLAPPDIRECTORY\App\AppInfo\appinfo.ini"
			StrCpy $InstallerINIFile "$INSTALLAPPDIRECTORY\App\AppInfo\installer.ini"
			StrCpy $PluginInstaller "false"
			Abort
		${ElseIf} ${FileExists} "$INSTALLAPPDIRECTORY\App\AppInfo\plugininstaller.ini"
			StrCpy $AppInfoINIFile "$INSTALLAPPDIRECTORY\App\AppInfo\plugininstaller.ini"
			StrCpy $InstallerINIFile "$INSTALLAPPDIRECTORY\App\AppInfo\plugininstaller.ini"
			StrCpy $PluginInstaller "true"
			Abort
		${ElseIf} ${FileExists} "$INSTALLAPPDIRECTORY\Other\Source\plugininstaller.ini"
			CreateDirectory "$INSTALLAPPDIRECTORY\App"
			CreateDirectory "$INSTALLAPPDIRECTORY\App\AppInfo"
			Rename "$INSTALLAPPDIRECTORY\Other\Source\plugininstaller.ini" "$INSTALLAPPDIRECTORY\App\AppInfo\plugininstaller.ini"
			StrCpy $AppInfoINIFile "$INSTALLAPPDIRECTORY\App\AppInfo\plugininstaller.ini"
			StrCpy $InstallerINIFile "$INSTALLAPPDIRECTORY\App\AppInfo\plugininstaller.ini"
			StrCpy $PluginInstaller "true"
			Abort
		${EndIf}
	${EndIf}
	${ReadINIStrWithDefault} $INTERACTIVEMODE "$EXEDIR\Data\settings.ini" "InstallerWizard" "InteractiveMode" "1"
	WriteINIStr "$PLUGINSDIR\InstallerWizardForm.ini" "Field 3" "State" "$INTERACTIVEMODE"
	InstallOptions::InitDialog /NOUNLOAD "$PLUGINSDIR\InstallerWizardForm.ini"
    Pop $0
    InstallOptions::Show
FunctionEnd

Function LeaveOptionsWindow
	;=== Blank
	ReadINIStr $INSTALLAPPDIRECTORY $PLUGINSDIR\InstallerWizardForm.ini "Field 2" "State"
	ReadINIStr $INTERACTIVEMODE "$PLUGINSDIR\InstallerWizardForm.ini" "Field 3" "State"

	StrCmp $INSTALLAPPDIRECTORY "" NoInstallAppDirectoryError
	${If} ${FileExists} "$INSTALLAPPDIRECTORY\App\AppInfo\appinfo.ini"
		StrCpy $AppInfoINIFile "$INSTALLAPPDIRECTORY\App\AppInfo\appinfo.ini"
		StrCpy $InstallerINIFile "$INSTALLAPPDIRECTORY\App\AppInfo\installer.ini"
		StrCpy $PluginInstaller "false"
	${ElseIf} ${FileExists} "$INSTALLAPPDIRECTORY\App\AppInfo\plugininstaller.ini"
		StrCpy $AppInfoINIFile "$INSTALLAPPDIRECTORY\App\AppInfo\plugininstaller.ini"
		StrCpy $InstallerINIFile "$INSTALLAPPDIRECTORY\App\AppInfo\plugininstaller.ini"
		StrCpy $PluginInstaller "true"
	${ElseIf} ${FileExists} "$INSTALLAPPDIRECTORY\Other\Source\plugininstaller.ini"
		CreateDirectory "$INSTALLAPPDIRECTORY\App"
		CreateDirectory "$INSTALLAPPDIRECTORY\App\AppInfo"
		Rename "$INSTALLAPPDIRECTORY\Other\Source\plugininstaller.ini" "$INSTALLAPPDIRECTORY\App\AppInfo\plugininstaller.ini"
		StrCpy $AppInfoINIFile "$INSTALLAPPDIRECTORY\App\AppInfo\plugininstaller.ini"
		StrCpy $InstallerINIFile "$INSTALLAPPDIRECTORY\App\AppInfo\plugininstaller.ini"
		StrCpy $PluginInstaller "true"
	${ElseIf} $INTERACTIVEMODE = 1
		; No AppInfo found
		${IfNot} ${FileExists} "$INSTALLAPPDIRECTORY\*.exe"
		${AndIf} $PluginInstaller != "true"
			Goto NoInstallAppDirectoryError
		${EndIf}

		MessageBox MB_ICONQUESTION|MB_YESNO "The app does not appear to have the necessary files within the App\AppInfo directory required by PortableApps.com Format.  Would you like to create the settings interactively and use a set of default icons for now for testing?" IDNO NoInstallAppDirectoryError

			;Find EXE file
			FindFirst $2 $3 "$INSTALLAPPDIRECTORY\*.exe"
			StrCpy $4 0

			${DoWhile} $3 != ""
				StrCpy $5 $3
				IntOp $4 $4 + 1
				FindNext $2 $3
			${Loop}
			FindClose $2

			${If} $4 > 1
				MessageBox MB_OK|MB_ICONEXCLAMATION `Multiple EXEs were found in the directory you selected.  The PortableApps.com Installer can only generate default information for applications with a single EXE.  Please review the information at PortableApps.com/development for details on creating the configuration files.`
				Abort
			${EndIf}

			CreateDirectory "$INSTALLAPPDIRECTORY\App\AppInfo"
			CopyFiles /SILENT "$EXEDIR\App\default_bits\appicon_16.png" "$INSTALLAPPDIRECTORY\App\AppInfo"
			CopyFiles /SILENT "$EXEDIR\App\default_bits\appicon_32.png" "$INSTALLAPPDIRECTORY\App\AppInfo"
			CopyFiles /SILENT "$EXEDIR\App\default_bits\appicon.ico" "$INSTALLAPPDIRECTORY\App\AppInfo"
			CopyFiles /SILENT "$EXEDIR\App\default_bits\appinfo.ini" "$INSTALLAPPDIRECTORY\App\AppInfo"
			WriteINIStr "$INSTALLAPPDIRECTORY\App\AppInfo\appinfo.ini" "Format" "Version" "${PORTABLEAPPS.COMFORMATVERSION}"
			WriteINIStr "$INSTALLAPPDIRECTORY\App\AppInfo\appinfo.ini" "Control" "Start" "$5"

			MessageBox MB_ICONINFORMATION "Before releasing this application, please be sure to create a set of proper icons in App\AppInfo."
	${Else}
		Goto NoInstallAppDirectoryError
	${EndIf}

	; Store settings
	WriteINIStr "$EXEDIR\Data\settings.ini" "InstallerWizard" "INSTALLAPPDIRECTORY" $INSTALLAPPDIRECTORY
	WriteINIStr "$EXEDIR\Data\settings.ini" "InstallerWizard" "InteractiveMode" $INTERACTIVEMODE
	Goto EndLeaveOptionsWindow

	NoInstallAppDirectoryError:
		MessageBox MB_OK|MB_ICONEXCLAMATION `Please select a valid portable app's base directory to create an installer for.`
		Abort

	EndLeaveOptionsWindow:
FunctionEnd

!define SetIndividualLanguage "!insertmacro SetIndividualLanguage"

!define WriteConfig "!insertmacro WriteConfig"

!macro WriteConfig Variable Value
	FileWriteUTF16LE $0 `!define ${Variable} "${Value}"$\r$\n`
!macroend

!macro SetIndividualLanguage IndividualLanguage
	StrCpy $2 "${IndividualLanguage}"
	${ReadINIStrWithDefault} $1 $InstallerINIFile "Languages" "$2" "false"
	${If} $1 == "true"
	${OrIf} $ALLLANGUAGES == "true"
		${WriteConfig} USES_$2 "true"
	${EndIf}
!macroend

!define WriteErrorToLog "!insertmacro WriteErrorToLog"

!macro WriteErrorToLog ErrorToWrite
	FileOpen $9 "$EXEDIR\Data\PortableApps.comInstallerLog.txt" a
	FileSeek $9 0 END
	FileWriteUTF16LE $9 `ERROR: ${ErrorToWrite}$\r$\n`
	FileClose $9
	StrCpy $ERROROCCURED "true"
!macroend

!define TransferInstallerINIToConfig "!insertmacro TransferInstallerINIToConfig"

!macro TransferInstallerINIToConfig Section Key Required
	${ReadINIStrWithDefault} $1 $InstallerINIFile ${Section} ${Key} ""
	${If} $1 != ""
		${WriteConfig} ${Key} "$1"
	!if ${Required} == required
	${Else}
		${WriteErrorToLog} "Installer.ini - ${Section} - ${Key} is missing."
	!endif
	${EndIf}
!macroend

Section Main
	!insertmacro MUI_HEADER_TEXT "PortableApps.com Installer ${FRIENDLYVER}" "the open portable software standard"
	${TBProgress} 33
	SetDetailsPrint ListOnly
	DetailPrint "App: $INSTALLAPPDIRECTORY"
	DetailPrint " "
	;FindWindow $0 "#32770" "" $HWNDPARENT
	;FindWindow $1 "msctls_progress32" "" $0
	
	;DetailPrint "Hanlde: $1"
	RealProgress::SetProgress /NOUNLOAD 1
	RealProgress::GradualProgress /NOUNLOAD 2 1 90 "Processing complete."
	DetailPrint "Generating installer code..."
	SetDetailsPrint none

	;Ensure the source directory exists
	CreateDirectory "$INSTALLAPPDIRECTORY\Other\Source"

	;Remove any existing installer files (leaving custom intact)
	RMDir /r "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerLanguages"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstaller.bmp"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstaller.ico"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstaller.nsi"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerConfig-EXAMPLE.nsh"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerConfig.nsh"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerDumpLogToFile.nsh"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerHeader.bmp"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerMoveFiles.nsh"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerStrRep.nsh"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerHeaderRTL.bmp"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerPlugin.nsi"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerPluginConfig.nsh"
	Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerTBProgress.nsh"

	;Copy the current PortableApps.com Installer in
	CopyFiles /SILENT "$EXEDIR\App\installer\*.*" "$INSTALLAPPDIRECTORY\Other\Source"
	${If} $PluginInstaller == "true"
		Rename "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstaller.nsi" "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerPlugin.nsi"
		Rename "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerConfig.nsh" "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerPluginConfig.nsh"
	${EndIf}

	;Generate the configuration file
	Delete "$EXEDIR\Data\PortableApps.comInstallerLog.txt"

	;Determine icon type
	${ReadINIStrWithDefault} $1 $AppInfoINIFile "Control" "ExtractIcon" ""
	${If} $1 != ""
		StrCpy $USEEXTRACTEDICON "true"
	${EndIf}

	;Check for content
	${IfNot} ${FileExists} "$INSTALLAPPDIRECTORY\*.exe"
	${AndIf} $PluginInstaller != "true"
		${WriteErrorToLog} "No EXE in $INSTALLAPPDIRECTORY."
	${EndIf}

	${IfNot} ${FileExists} "$INSTALLAPPDIRECTORY\help.html"
	${AndIf} $PluginInstaller != "true"
		${WriteErrorToLog} "No help.html in $INSTALLAPPDIRECTORY."
	${EndIf}

	!macro AppInfoFileMissingAskInsertDefault FileName FileDescription
	${IfNot} ${FileExists} "$INSTALLAPPDIRECTORY\App\AppInfo\${FileName}"
	${AndIf} $PluginInstaller != "true"
		${If} $USEEXTRACTEDICON == "true"
			!if ${FileName} == appicon.ico
			;Copy the default icon in (appicon_*.png don't get included)
			CopyFiles /SILENT "$EXEDIR\App\default_bits\${FileName}" "$INSTALLAPPDIRECTORY\App\AppInfo"
			!endif
		${ElseIf} $INTERACTIVEMODE = 1
		${AndIf} ${Cmd} ${|} MessageBox MB_ICONQUESTION|MB_YESNO "The app does not have ${FileDescription} (${FileName}) in the App\AppInfo directory.  Would you like to use a default icon for test purposes for now?" IDYES ${|}
			CopyFiles /SILENT "$EXEDIR\App\default_bits\${FileName}" "$INSTALLAPPDIRECTORY\App\AppInfo"
			MessageBox MB_ICONINFORMATION "Before releasing this application, please be sure to create a proper ${FileName} app icon in App\AppInfo."
		${Else}
			${WriteErrorToLog} "No ${FileName} in $INSTALLAPPDIRECTORY\App\AppInfo."
		${EndIf}
	${EndIf}
	!macroend

	!insertmacro AppInfoFileMissingAskInsertDefault appicon_16.png "a 16x16 PNG icon"
	!insertmacro AppInfoFileMissingAskInsertDefault appicon_32.png "a 32x32 PNG icon"
	!insertmacro AppInfoFileMissingAskInsertDefault appicon.ico    "an icon"

	${If} $PluginInstaller == "true"
		FileOpen $0 "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerPluginConfig.nsh" a
	${Else}
		FileOpen $0 "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerConfig.nsh" a
	${EndIf}
	FileSeek $0 0 END
	FileWriteUTF16LE $0 `;Code generated by PortableApps.com Installer ${FRIENDLYVER}.  DO NOT EDIT.$\r$\n$\r$\n`

	;PortableApps.comFormat Version
	${ReadINIStrWithDefault} $1 $AppInfoINIFile "Format" "Version" ""
	${If} $1 == "0.9.8"
		;Preserve old installer config in case it's needed
		Rename "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerConfig.nsh" "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerConfigOld.nsh"

		;Autogenerate App ID is handled normally when interactive

		;Language selection is handled normally when in interactive

		;This brings it up to 0.90
		StrCpy $1 "0.90"
	${EndIf}
	${If} $1 == "0.90"
		;0.90 to 0.91 needs no changes, so it brings it to 0.91
		StrCpy $1 "0.91"
	${EndIf}
	${If} $1 == "0.91"
		;0.91 to 1.0 needs no changes, so it brings it to 1.0
		StrCpy $1 "1.0"
	${EndIf}
	${If} $1 == "1.0"
	${OrIf} $1 == "2.0"
		;1.0 to 2.0 needs no changes, so it brings it to 2.0
		WriteINIStr $AppInfoINIFile "Format" "Version" "${PORTABLEAPPS.COMFORMATVERSION}"
	${EndIf}

	!macro GetValueFromAppInfo Section Key Prompt DefaultValue Variable Required
		ReadINIStr ${Variable} $AppInfoINIFile ${Section} ${Key}
		${If} ${Variable} == ""
			${If} $INTERACTIVEMODE = 1
				${InputTextBox} "${APPNAME}" "${Prompt}" "${DefaultValue}" "255" "OK" "Cancel" 9
				${If} $9 != ""
					StrCpy ${Variable} $9
					WriteINIStr $AppInfoINIFile ${Section} ${Key} $9
				!if ${Required} == required
				${Else}
					${WriteErrorToLog} "AppInfo.ini - ${Section} - ${Key} is missing."
				!endif
				${EndIf}
			!if ${Required} == required
			${Else}
				${WriteErrorToLog} "AppInfo.ini - ${Section} - ${Key} is missing."
			!endif
			${EndIf}
		${EndIf}
	!macroend

	;App Name
	!insertmacro GetValueFromAppInfo \
		Details \
		Name \
		"Enter the portable app's name (e.g. FileZilla Portable):" \
		"AppName Portable" \
		$PORTABLEAPPNAME \
		required

	${If} $PluginInstaller != "true"
		${WriteConfig} PORTABLEAPPNAME "$PORTABLEAPPNAME"
		${WordReplace} $PORTABLEAPPNAME "&" "~~~@@@~~~" + $PORTABLEAPPNAMEDOUBLEDAMPERSANDS
		${WordReplace} $PORTABLEAPPNAMEDOUBLEDAMPERSANDS "~~~@@@~~~" "&&" + $PORTABLEAPPNAMEDOUBLEDAMPERSANDS
		${WriteConfig} PORTABLEAPPNAMEDOUBLEDAMPERSANDS "$PORTABLEAPPNAMEDOUBLEDAMPERSANDS"
	${EndIf}

	;Plugin Name
	${If} $PluginInstaller == "true"
		!insertmacro GetValueFromAppInfo \
			Details \
			PluginName \
			"Enter the plugin's name (e.g. Acme Plugin):" \
			"Plugin" \
			$PLUGINNAME \
			required

		${WriteConfig} PLUGINNAME "$PLUGINNAME"
		${WriteConfig} PORTABLEAPPNAME "$PLUGINNAME"
		${WordReplace} $PLUGINNAME "&" "~~~@@@~~~" + $PORTABLEAPPNAMEDOUBLEDAMPERSANDS
		${WordReplace} $PORTABLEAPPNAMEDOUBLEDAMPERSANDS "~~~@@@~~~" "&&" + $PORTABLEAPPNAMEDOUBLEDAMPERSANDS
		${WriteConfig} PORTABLEAPPNAMEDOUBLEDAMPERSANDS "$PORTABLEAPPNAMEDOUBLEDAMPERSANDS"
		${ReadINIStrWithDefault} $1 $AppInfoINIFile "Details" "PluginType" "App"
		${If} $1 == "CommonFiles"
			StrCpy $COMMONFILESPLUGIN "true"
			${WriteConfig} COMMONFILESPLUGIN "true"
		${EndIf}
	${EndIf}


	;App ID
	${WordReplace} $PORTABLEAPPNAME " "   ""  + $8
	${WordReplace} $8               " "   "_" + $8
	${WordReplace} $8               "("   ""  + $8
	${WordReplace} $8               ")"   ""  + $8
	${WordReplace} $8               "["   ""  + $8
	${WordReplace} $8               "]"   ""  + $8
	${WordReplace} $8               "~"   "-" + $8
	${WordReplace} $8               "&"   "+" + $8
	${WordReplace} $8               "#"   "+" + $8
	${WordReplace} $8               "$\"" "-" + $8
	${WordReplace} $8               "*"   "+" + $8
	${WordReplace} $8               "/"   "_" + $8
	${WordReplace} $8               "\"   "_" + $8
	${WordReplace} $8               ":"   "." + $8
	${WordReplace} $8               "<"   "-" + $8
	${WordReplace} $8               ">"   "-" + $8
	${WordReplace} $8               "?"   ""  + $8
	${WordReplace} $8               "|"   "-" + $8
	${WordReplace} $8               "="   "-" + $8
	${WordReplace} $8               ","   "." + $8
	${WordReplace} $8               ";"   "." + $8
	!insertmacro GetValueFromAppInfo \
		Details \
		AppID \
		"Enter the portable app's App ID (usually the name with no spaces or symbols):" \
		$8 \
		$APPID \
		required

	${WriteConfig} APPID "$APPID"
	StrCpy $SHORTNAME $APPID

	;Publisher
	!insertmacro GetValueFromAppInfo \
		Details \
		Publisher \
		"Enter the publisher ('App Developer && PortableApps.com' for our apps):" \
		"No Publisher Specified" \
		$1 \
		optional

	;Homepage
	!insertmacro GetValueFromAppInfo \
		Details \
		Homepage \
		"Enter the app's homepage (e.g. portableapps.com):" \
		"example.com" \
		$1 \
		optional

	;Category
	!insertmacro GetValueFromAppInfo \
		Details \
		Category \
		"Enter the app's category *exactly* (Accessibility, Development, Education, Games, Graphics && Pictures, Internet, Music && Video, Office, Operating Systems, Utilities):" \
		"" \
		$1 \
		optional

	;Description
	!insertmacro GetValueFromAppInfo \
		Details \
		Description \
		"Enter the app's description (e.g. Simple FTP program.):" \
		"" \
		$1 \
		optional

	;Language
	!insertmacro GetValueFromAppInfo \
		Details \
		Language \
		"Enter the portable app's language as expected by NSIS (e.g. English or Multilingual):" \
		"English" \
		$APPLANGUAGE \
		optional
	${If} $APPLANGUAGE == ""
		StrCpy $APPLANGUAGE "English"
	${EndIf}

	!macro GetLicenseValueFromAppInfo Key Prompt
		ReadINIStr $1 $AppInfoINIFile License ${Key}
		${If} $1 == ""
			${If} $INTERACTIVEMODE = 1
				${If} ${Cmd} ${|} MessageBox MB_ICONQUESTION|MB_YESNO "License Question: ${Prompt}" IDYES ${|}
					StrCpy $1 "true"
				${Else}
					StrCpy $1 "false"
				${EndIf}
				WriteINIStr $AppInfoINIFile License ${Key} $1
			${EndIf}
		${EndIf}
	!macroend

	;License
	!insertmacro GetLicenseValueFromAppInfo Shareable     "Can this application be legally shared from one user to another?"
	!insertmacro GetLicenseValueFromAppInfo OpenSource    "Is this application 100% open source under an OSI-approved license?"
	!insertmacro GetLicenseValueFromAppInfo Freeware      "Is this application freeware (it can be used without payment)?"
	!insertmacro GetLicenseValueFromAppInfo CommercialUse "Can this app be used in a commercial environment?"

	;EULA Version
	${ReadINIStrWithDefault} $EULAVERSION $AppInfoINIFile "License" "EULAVersion" ""

	;Display Version
	!insertmacro GetValueFromAppInfo \
		Version \
		DisplayVersion \
		"Enter the portable app's display version (e.g. 1.0 or 2.2 Beta 1):" \
		"0.1" \
		$DISPLAYVERSION \
		required

	;Package Version
	!insertmacro GetValueFromAppInfo \
		Version \
		PackageVersion \
		"Enter the portable app's package version as all numbers in the form X.X.X.X (e.g. 1.0.0.0 or 2.2.0.1):" \
		"0.1.0.0" \
		$1 \
		required

	${WriteConfig} VERSION "$1"

	;Filename should only be alpha, numbers as well as:  + . - _
	${If} $PluginInstaller == "true"
		StrCpy $INSTALLERFILENAME "$PLUGINNAME_$DISPLAYVERSION"
	${Else}
		StrCpy $INSTALLERFILENAME "$APPID_$DISPLAYVERSION"
	${EndIf}

	${If} $APPLANGUAGE != "Multilingual"
		StrCpy $INSTALLERFILENAME "$INSTALLERFILENAME_$APPLANGUAGE"
	${EndIf}

	${WordReplace} $INSTALLERFILENAME " "   "_"    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "("   ""     + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME ")"   ""     + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "["   ""     + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "]"   ""     + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "~"   "-"    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "&"   "-"    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "#"   "-"    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "$\"" "-"    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "*"   "-"    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "/"   "_"    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "\"   "_"    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME ":"   "."    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "<"   "-"    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME ">"   "-"    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "?"   ""     + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "|"   "-"    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "="   "-"    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME ","   "."    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME ";"   "."    + $INSTALLERFILENAME
	${WordReplace} $INSTALLERFILENAME "+"   "Plus" + $INSTALLERFILENAME

	${ReadINIStrWithDefault} $1 $InstallerINIFile "DownloadFiles" "DownloadURL" ""
	${If} $1 != ""
		StrCpy $INSTALLERFILENAME "$INSTALLERFILENAME_online"
	${EndIf}

	${WriteConfig} FILENAME "$INSTALLERFILENAME"

	
	${ReadINIStrWithDefault} $1 $AppInfoINIFile "Control" "Start" ""
		${If} $1 == ""
		${WriteErrorToLog} "AppInfo.ini - Control - Start is missing."
	${Else}
		${WriteConfig} FINISHPAGERUN "$1"
	${EndIf}
	${IfNot} ${FileExists} "$INSTALLAPPDIRECTORY\$1"
	${AndIf} $PluginInstaller != "true"
		${WriteErrorToLog} "AppInfo.ini - Control - Start=$1, file is missing."
	${EndIf}

	${ReadINIStrWithDefault} $2 $InstallerINIFile "CheckRunning" "CloseEXE" "$1"
	${WriteConfig} CHECKRUNNING "$2"
	${ReadINIStrWithDefault} $1 $InstallerINIFile "CheckRunning" "CloseName" "$PORTABLEAPPNAME"
	${WriteConfig} CLOSENAME "$1"
	${ReadINIStrWithDefault} $1 $AppInfoINIFile "SpecialPaths" "Plugins" "NONE"
	${WriteConfig} ADDONSDIRECTORYPRESERVE "$1"
	${WriteConfig} INSTALLERCOMMENTS "For additional details, visit PortableApps.com"
	${ReadINIStrWithDefault} $1 $AppInfoINIFile "Details" "Trademarks" ""
	${If} $1 != ""
		StrCpy $1 "$1. "
	${EndIf}
	${WriteConfig} INSTALLERADDITIONALTRADEMARKS "$1"

	;Source Code
	${ReadINIStrWithDefault} $INCLUDESOURCE $InstallerINIFile "Source" "IncludeInstallerSource" "false"
	${If} $INCLUDESOURCE == "true"
		${WriteConfig} INCLUDEINSTALLERSOURCE "true"
	${EndIf}

	;Languages
	${If} $APPLANGUAGE != "Multilingual"
		${WriteConfig} INSTALLERLANGUAGE "$APPLANGUAGE"
	${Else}
		${WriteConfig} INSTALLERMULTILINGUAL "true"

		${ReadINIStrWithDefault} $1 $InstallerINIFile "Languages" "English" ""
		${If} $1 == ""
			StrCpy $ALLLANGUAGES "true"
		${EndIf}

		${SetIndividualLanguage} "ENGLISH"
		${SetIndividualLanguage} "ENGLISHGB"
		${SetIndividualLanguage} "AFRIKAANS"
		${SetIndividualLanguage} "ALBANIAN"
		${SetIndividualLanguage} "ARABIC"
		${SetIndividualLanguage} "ARMENIAN"
		${SetIndividualLanguage} "BASQUE"
		${SetIndividualLanguage} "BELARUSIAN"
		${SetIndividualLanguage} "BOSNIAN"
		${SetIndividualLanguage} "BRETON"
		${SetIndividualLanguage} "BULGARIAN"
		${SetIndividualLanguage} "CATALAN"
		${SetIndividualLanguage} "CROATIAN"
		${SetIndividualLanguage} "CZECH"
		${SetIndividualLanguage} "DANISH"
		${SetIndividualLanguage} "DUTCH"
		${SetIndividualLanguage} "ESPERANTO"
		${SetIndividualLanguage} "ESTONIAN"
		${SetIndividualLanguage} "FARSI"
		${SetIndividualLanguage} "FINNISH"
		${SetIndividualLanguage} "FRENCH"
		${SetIndividualLanguage} "GALICIAN"
		${SetIndividualLanguage} "GERMAN"
		${SetIndividualLanguage} "GREEK"
		${SetIndividualLanguage} "HEBREW"
		${SetIndividualLanguage} "HUNGARIAN"
		${SetIndividualLanguage} "ICELANDIC"
		${SetIndividualLanguage} "INDONESIAN"
		${SetIndividualLanguage} "IRISH"
		${SetIndividualLanguage} "ITALIAN"
		${SetIndividualLanguage} "JAPANESE"
		${SetIndividualLanguage} "KOREAN"
		${SetIndividualLanguage} "KURDISH"
		${SetIndividualLanguage} "LATVIAN"
		${SetIndividualLanguage} "LITHUANIAN"
		${SetIndividualLanguage} "LUXEMBOURGISH"
		${SetIndividualLanguage} "MACEDONIAN"
		${SetIndividualLanguage} "MALAY"
		${SetIndividualLanguage} "MONGOLIAN"
		${SetIndividualLanguage} "NORWEGIAN"
		${SetIndividualLanguage} "NORWEGIANNYNORSK"
		${SetIndividualLanguage} "POLISH"
		${SetIndividualLanguage} "PORTUGUESE"
		${SetIndividualLanguage} "PORTUGUESEBR"
		${SetIndividualLanguage} "ROMANIAN"
		${SetIndividualLanguage} "RUSSIAN"
		${SetIndividualLanguage} "SERBIAN"
		${SetIndividualLanguage} "SERBIANLATIN"
		${SetIndividualLanguage} "SIMPCHINESE"
		${SetIndividualLanguage} "SLOVAK"
		${SetIndividualLanguage} "SLOVENIAN"
		${SetIndividualLanguage} "SPANISH"
		${SetIndividualLanguage} "SPANISHINTERNATIONAL"
		${SetIndividualLanguage} "SWEDISH"
		${SetIndividualLanguage} "THAI"
		${SetIndividualLanguage} "TRADCHINESE"
		${SetIndividualLanguage} "TURKISH"
		${SetIndividualLanguage} "UKRAINIAN"
		${SetIndividualLanguage} "UZBEK"
		${SetIndividualLanguage} "WELSH"
	${EndIf}

	;EULA
	${If} $PluginInstaller == "true"
		${If} ${FileExists} "$INSTALLAPPDIRECTORY\App\AppInfo\PluginEULA.txt"
			${WriteConfig} LICENSEAGREEMENT "PluginEULA.txt"
		${ElseIf} ${FileExists} "$INSTALLAPPDIRECTORY\Other\Source\PluginEULA.txt"
			Rename "$INSTALLAPPDIRECTORY\Other\Source\PluginEULA.txt" "$INSTALLAPPDIRECTORY\App\AppInfo\PluginEULA.txt"
			${WriteConfig} LICENSEAGREEMENT "PluginEULA.txt"
		${ElseIf} ${FileExists} "$INSTALLAPPDIRECTORY\Other\Source\PluginEULA.rtf"
			${WriteErrorToLog} "EULA - Other\Source\PluginEULA.rtf is no longer supported.  Use App\AppInfo\PluginEULA.txt instead."
		${ElseIf} ${FileExists} "$INSTALLAPPDIRECTORY\App\AppInfo\PluginEULA.rtf"
			${WriteErrorToLog} "EULA - App\AppInfo\PluginEULA.rtf is not supported.  Use App\AppInfo\PluginEULA.txt instead."
		${EndIf}
	${Else}
		${If} ${FileExists} "$INSTALLAPPDIRECTORY\App\AppInfo\EULA.txt"
			${WriteConfig} LICENSEAGREEMENT "eula.txt"
		${ElseIf} ${FileExists} "$INSTALLAPPDIRECTORY\Other\Source\EULA.txt"
			Rename "$INSTALLAPPDIRECTORY\Other\Source\EULA.txt" "$INSTALLAPPDIRECTORY\App\AppInfo\EULA.txt"
			${WriteConfig} LICENSEAGREEMENT "eula.txt"
		${ElseIf} ${FileExists} "$INSTALLAPPDIRECTORY\Other\Source\EULA.rtf"
			${WriteErrorToLog} "EULA - Other\Source\EULA.rtf is no longer supported.  Use App\AppInfo\EULA.txt instead."
		${ElseIf} ${FileExists} "$INSTALLAPPDIRECTORY\App\AppInfo\EULA.rtf"
			${WriteErrorToLog} "EULA - App\AppInfo\EULA.rtf is not supported.  Use App\AppInfo\EULA.txt instead."
		${EndIf}
	${EndIf}

	${If} $EULAVERSION != ""
		${WriteConfig} EULAVERSION "$EULAVERSION"
	${EndIf}

	;OptionalComponents
	${ReadINIStrWithDefault} $OPTIONALCOMPONENTS $InstallerINIFile "OptionalComponents" "OptionalComponents" "false"
	${If} $OPTIONALCOMPONENTS == "true"
		${ReadINIStrWithDefault} $1 $InstallerINIFile "OptionalComponents" "MainSectionTitle" "$PORTABLEAPPNAME (English) [Required]"
		${WriteConfig} MAINSECTIONTITLE "$1"
		${ReadINIStrWithDefault} $1 $InstallerINIFile "OptionalComponents" "MainSectionDescription" "Install the portable app"
		${WriteConfig} MAINSECTIONDESCRIPTION "$1"
		${ReadINIStrWithDefault} $1 $InstallerINIFile "OptionalComponents" "OptionalSectionTitle" "Additional Languages"
		${WriteConfig} OPTIONALSECTIONTITLE "$1"
		${ReadINIStrWithDefault} $1 $InstallerINIFile "OptionalComponents" "OptionalSectionDescription" "Add multilingual support for this app"
		${WriteConfig} OPTIONALSECTIONDESCRIPTION "$1"
		${ReadINIStrWithDefault} $OptionalSectionSelectedInstallType $InstallerINIFile "OptionalComponents" "OptionalSectionSelectedInstallType" "Multilingual"
		${WriteConfig} OPTIONALSECTIONSELECTEDINSTALLTYPE "$OptionalSectionSelectedInstallType"
		${ReadINIStrWithDefault} $1 $InstallerINIFile "OptionalComponents" "OptionalSectionNotSelectedInstallType" "English"
		${WriteConfig} OPTIONALSECTIONNOTSELECTEDINSTALLTYPE "$1"
		${ReadINIStrWithDefault} $1 $InstallerINIFile "OptionalComponents" "OptionalSectionPreSelectedIfNonEnglishInstall" "true"
		${If} $1 == "true"
			${WriteConfig} OPTIONALSECTIONPRESELECTEDIFNONENGLISHINSTALL "$1"
		${EndIf}

		${If} $OptionalSectionSelectedInstallType == "Multilingual"
			${ReadINIStrWithDefault} $1 $InstallerINIFile "OptionalComponents" "OptionalSectionInstalledWhenSilent" "false"
		${Else}
			${ReadINIStrWithDefault} $1 $InstallerINIFile "OptionalComponents" "OptionalSectionInstalledWhenSilent" "true"
		${EndIf}

		${If} $1 == "true"
			${WriteConfig} OPTIONALSECTIONINSTALLEDWHENSILENT "$1"
		${EndIf}
	${EndIf}

	;Main directories
	${If} $PluginInstaller == "true"
	${AndIf} $COMMONFILESPLUGIN != "true"
		${ReadINIStrWithDefault} $1 $InstallerINIFile "MainDirectories" "RemoveAppDirectory" "false"
	${Else}
		${ReadINIStrWithDefault} $1 $InstallerINIFile "MainDirectories" "RemoveAppDirectory" "true"
	${EndIf}
	${If} $1 == "true"
		${WriteConfig} REMOVEAPPDIRECTORY "true"
	${EndIf}
	${ReadINIStrWithDefault} $1 $InstallerINIFile "MainDirectories" "RemoveDataDirectory" "false"
	${If} $1 == "true"
		${WriteConfig} REMOVEDATADIRECTORY "true"
	${EndIf}
	${If} $PluginInstaller == "true"
		${ReadINIStrWithDefault} $1 $InstallerINIFile "MainDirectories" "RemoveOtherDirectory" "false"
	${Else}
		${ReadINIStrWithDefault} $1 $InstallerINIFile "MainDirectories" "RemoveOtherDirectory" "true"
	${EndIf}
	${If} $1 == "true"
		${WriteConfig} REMOVEOTHERDIRECTORY "true"
	${EndIf}

	;Preserve directories
	StrCpy $R1 1
	${Do}
		${ReadINIStrWithDefault} $1 $InstallerINIFile "DirectoriesToPreserve" "PreserveDirectory$R1" ""
		${If} $1 != ""
			${WriteConfig} PRESERVEDIRECTORY$R1 "$1"
		${EndIf}
		IntOp $R1 $R1 + 1
	${LoopUntil} $R1 > 10

	;Remove directories
	StrCpy $R1 1
	${Do}
		${ReadINIStrWithDefault} $1 $InstallerINIFile "DirectoriesToRemove" "RemoveDirectory$R1" ""
		${If} $1 != ""
			${WriteConfig} REMOVEDIRECTORY$R1 "$1"
		${EndIf}
		IntOp $R1 $R1 + 1
	${LoopUntil} $R1 > 10

	;Preserve files
	StrCpy $R1 1
	${Do}
		${ReadINIStrWithDefault} $1 $InstallerINIFile "FilesToPreserve" "PreserveFile$R1" ""
		${If} $1 != ""
			${WriteConfig} PRESERVEFILE$R1 "$1"
		${EndIf}
		IntOp $R1 $R1 + 1
	${LoopUntil} $R1 > 10

	;Remove files
	StrCpy $R1 1
	${Do}
		${ReadINIStrWithDefault} $1 $InstallerINIFile "FilesToRemove" "RemoveFile$R1" ""
		${If} $1 != ""
			${WriteConfig} REMOVEFILE$R1 "$1"
		${EndIf}
		IntOp $R1 $R1 + 1
	${LoopUntil} $R1 > 10

	;Custom code
	${If} $PluginInstaller == "true"
		StrCpy $9 "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerPluginCustom.nsh"
	${Else}
		StrCpy $9 "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerCustom.nsh"
	${EndIf}
	${If} ${FileExists} $9
		${WriteConfig} USESCUSTOMCODE "true"
	${EndIf}

	;Local Files
	${ReadINIStrWithDefault} $1 $InstallerINIFile "CopyLocalFiles" "CopyLocalFiles" "false"
	${If} $1 == "true"
		${WriteConfig} COPYLOCALFILES "true"

		!insertmacro TransferInstallerINIToConfig CopyLocalFiles CopyFromRegPath -
		!insertmacro TransferInstallerINIToConfig CopyLocalFiles CopyFromRegKey -
		!insertmacro TransferInstallerINIToConfig CopyLocalFiles CopyFromRegRemoveDirectories -
		!insertmacro TransferInstallerINIToConfig CopyLocalFiles CopyFromDirectory -
		!insertmacro TransferInstallerINIToConfig CopyLocalFiles CopyToDirectory -
	${EndIf}

	;Download files
	${ReadINIStrWithDefault} $1 $InstallerINIFile "DownloadFiles" "DownloadURL" ""
	${If} $1 != ""
		StrCpy $2 $1 7

		${If} $2 == "http://"
			${WriteConfig} DownloadURL "$1"

			!insertmacro TransferInstallerINIToConfig DownloadFiles DownloadName          required
			!insertmacro TransferInstallerINIToConfig DownloadFiles DownloadFilename      required
			!insertmacro TransferInstallerINIToConfig DownloadFiles DownloadMD5           -
			!insertmacro TransferInstallerINIToConfig DownloadFiles DownloadTo            -
			!insertmacro TransferInstallerINIToConfig DownloadFiles AdditionalInstallSize required

			${For} $R1 1 10
				${ReadINIStrWithDefault} $1 $InstallerINIFile "DownloadFiles" "Extract$R1To" ""
				${If} $1 != ""
					${If} $1 == "<ROOT>"
						StrCpy $1 ""
					${EndIf}
					${WriteConfig} Extract$R1To "$1"
				${EndIf}
			${Next}

			${For} $R1 1 10
				${ReadINIStrWithDefault} $1 $InstallerINIFile "DownloadFiles" "Extract$R1File" ""
				${If} $1 != ""
					${WriteConfig} Extract$R1File "$1"
				${EndIf}
			${Next}

			${For} $R1 1 10
				${ReadINIStrWithDefault} $1 $InstallerINIFile "DownloadFiles" "AdvancedExtract$R1To" ""
				${If} $1 != ""
					${If} $1 == "<ROOT>"
						StrCpy $1 ""
					${EndIf}
					${WriteConfig} AdvancedExtract$R1To "$1"
				${EndIf}
			${Next}

			${For} $R1 1 10
				${ReadINIStrWithDefault} $1 $InstallerINIFile "DownloadFiles" "AdvancedExtract$R1Filter" ""
				${If} $1 != ""
					${WriteConfig} AdvancedExtract$R1Filter "$1"
				${EndIf}
			${Next}

			${ReadINIStrWithDefault} $1 $InstallerINIFile "DownloadFiles" "DoubleExtractFilename" ""
			${If} $1 != ""
				${WriteConfig} DoubleExtractFilename "$1"

				${For} $R1 1 10
					${ReadINIStrWithDefault} $1 $InstallerINIFile "DownloadFiles" "DoubleExtract$R1To" ""
					${If} $1 != ""
						${If} $1 == "<ROOT>"
							StrCpy $1 ""
						${EndIf}
						${WriteConfig} DoubleExtract$R1To "$1"
					${EndIf}
				${Next}

				${For} $R1 1 10
					${ReadINIStrWithDefault} $1 $InstallerINIFile "DownloadFiles" "DoubleExtract$R1Filter" ""
					${If} $1 != ""
						${WriteConfig} DoubleExtract$R1Filter "$1"
					${EndIf}
				${Next}

			${EndIf}
		${Else}
			${WriteErrorToLog} "Installer.ini - DownloadFiles - DownloadURL must begin with http://"
		${EndIf}
	${EndIf}

	FileClose $0

	; If errors have occurred, there's no point in going on to the actual generation of it.
	${If} $ERROROCCURED != "true"
		;Make the installer header
		${If} $USEEXTRACTEDICON == "true"
		${OrIf} $PluginInstaller == "true"
			CopyFiles /SILENT "$EXEDIR\App\default_bits\PortableApps.comInstallerHeader.bmp" "$INSTALLAPPDIRECTORY\Other\Source"
			CopyFiles /SILENT "$EXEDIR\App\default_bits\PortableApps.comInstallerHeaderRTL.bmp" "$INSTALLAPPDIRECTORY\Other\Source"
		${Else}
			ExecWait `"$EXEDIR\App\bin\MakeHeader.exe" "$INSTALLAPPDIRECTORY"`
		${EndIf}

		;Move optional component files
		${If} $OPTIONALCOMPONENTS == "true"
			CreateDirectory "$INSTALLAPPDIRECTORY\Optional1"

			;Move directories
			StrCpy $R1 1
			${Do}
				${ReadINIStrWithDefault} $1 $InstallerINIFile "OptionalComponents" "OptionalDirectory$R1" "\COMPLETED\"
				${If} $1 != ""
				${AndIf} $1 != "\COMPLETED\"
					${GetParent} "$INSTALLAPPDIRECTORY\Optional1\$1" $2
					CreateDirectory $2
					Rename "$INSTALLAPPDIRECTORY\$1" "$INSTALLAPPDIRECTORY\Optional1\$1"
				${EndIf}
				IntOp $R1 $R1 + 1
			${LoopUntil} $1 == "\COMPLETED\"

			;Move files
			StrCpy $R1 1
			${Do}
				${ReadINIStrWithDefault} $1 $InstallerINIFile "OptionalComponents" "OptionalFile$R1" "\COMPLETED\"
				${If} $1 != ""
				${AndIf} $1 != "\COMPLETED\"
					${GetParent} "$INSTALLAPPDIRECTORY\Optional1\$1" $2
					CreateDirectory $2
					${GetParent} "$INSTALLAPPDIRECTORY\$1" $3
					${GetFileName} "$INSTALLAPPDIRECTORY\Optional1\$1" $4
					${MoveFiles} DOS "$4" "$3" "$2"
				${EndIf}
				IntOp $R1 $R1 + 1
			${LoopUntil} $1 == "\COMPLETED\"

		${EndIf}

		;Compile the installer
		SetDetailsPrint ListOnly
		${If} $PluginInstaller == "true"
			DetailPrint "Creating $PLUGINNAME installer..."
		${Else}
			DetailPrint "Creating $PORTABLEAPPNAME installer..."
		${EndIf}
		SetDetailsPrint none
		${TBProgress} 66

		;Delete existing installer if there is one
		${GetParent} $INSTALLAPPDIRECTORY $0
		Delete "$0\$INSTALLERFILENAME.paf.exe"
		${If} ${FileExists} "$0\$INSTALLERFILENAME.paf.exe"
			MessageBox MB_OK|MB_ICONEXCLAMATION "Unable to delete file: $0\$INSTALLERFILENAME.paf.exe.  Please be sure the file is not in use"
			${WriteErrorToLog} "Unable to delete file: $0\$INSTALLERFILENAME.paf.exe.  Please be sure the file is not in use."
		${Else}
			SetOutPath "$EXEDIR\App\nsis"
			${If} $PluginInstaller == "true"
				ExecDos::exec `"$EXEDIR\App\nsis\makensis.exe" /O"$EXEDIR\Data\PortableApps.comInstallerLog.txt" "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerPlugin.nsi"` "" ""
			${Else}
				ExecDos::exec `"$EXEDIR\App\nsis\makensis.exe" /O"$EXEDIR\Data\PortableApps.comInstallerLog.txt" "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstaller.nsi"` "" ""
			${EndIf}
		${EndIf}

		;Move optional component files back
		${If} $OPTIONALCOMPONENTS == "true"
			;Move directories
			StrCpy $R1 1
			${Do}
				${ReadINIStrWithDefault} $1 $InstallerINIFile "OptionalComponents" "OptionalDirectory$R1" "\COMPLETED\"
				${If} $1 != ""
				${AndIf} $1 != "\COMPLETED\"
					Rename "$INSTALLAPPDIRECTORY\Optional1\$1" "$INSTALLAPPDIRECTORY\$1"
				${EndIf}
				IntOp $R1 $R1 + 1
			${LoopUntil} $1 == "\COMPLETED\"

			;Move files
			StrCpy $R1 1
			${Do}
				${ReadINIStrWithDefault} $1 $InstallerINIFile "OptionalComponents" "OptionalFile$R1" "\COMPLETED\"
				${If} $1 != ""
				${AndIf} $1 != "\COMPLETED\"
					${GetParent} "$INSTALLAPPDIRECTORY\Optional1\$1" $2
					${GetParent} "$INSTALLAPPDIRECTORY\$1" $3
					${GetFileName} "$INSTALLAPPDIRECTORY\Optional1\$1" $4
					${MoveFiles} DOS "$4" "$2" "$3"
				${EndIf}
				IntOp $R1 $R1 + 1
			${LoopUntil} $1 == "\COMPLETED\"

			RMDir /r "$INSTALLAPPDIRECTORY\Optional1"
		${EndIf}
	${EndIf}

	; Done
	SetDetailsPrint ListOnly
	DetailPrint " "
	DetailPrint "Processing complete."

	${If} ${FileExists} "$0\$INSTALLERFILENAME.paf.exe"
	${AndIf} $ERROROCCURED != "true"
		StrCpy $FINISHTITLE "Installer Created"
		StrCpy $FINISHTEXT "The installer has been created. Installer location:\r\n$0\r\n\r\nInstaller name:\r\n$INSTALLERFILENAME.paf.exe"
	${Else}
		StrCpy $FINISHTITLE "An Error Occured"
		StrCpy $FINISHTEXT "The installer was not created.  You can view the log file for more information."
		StrCpy $ERROROCCURED "true"
	${EndIf}

	SetDetailsPrint none
	;Remove the installer files if not included
	${If} $INCLUDESOURCE != "true"
	${AndIf} $ERROROCCURED != "true"
		RMDir /r "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerLanguages\"
		Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstaller.bmp"
		Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstaller.ico"
		Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstaller.nsi"
		Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerConfig.nsh"
		Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerDumpLogToFile.nsh"
		Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerHeader.bmp"
		Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerHeaderRTL.bmp"
		Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerMoveFiles.nsh"
		Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerPlugin.nsi"
		Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerPluginConfig.nsh"
		Delete "$INSTALLAPPDIRECTORY\Other\Source\PortableApps.comInstallerTBProgress.nsh"
	${EndIf}

	;Remove the Source and Other directories if empty
	RMDir "$INSTALLAPPDIRECTORY\Other\Source"
	RMDir "$INSTALLAPPDIRECTORY\Other"
	${TBProgress_State} NoProgress 
SectionEnd

Function ShowFinishPage
	${If} $AUTOMATICCOMPILE == "true"
	${AndIf} $ERROROCCURED != "true"
		Abort
	${ElseIf} $ERROROCCURED == "true"
		!insertmacro MUI_INSTALLOPTIONS_WRITE "ioSpecial.ini" "Field 4" "Flags" "DISABLED"
		!insertmacro MUI_INSTALLOPTIONS_WRITE "ioSpecial.ini" "Field 5" "State" "1"
	${EndIf}
FunctionEnd

Function RunOnFinish
	Exec `"$0\$INSTALLERFILENAME.paf.exe"`
FunctionEnd

Function .onGUIEnd
	RealProgress::Unload
FunctionEnd