;Copyright 2007-2012 John T. Haller of PortableApps.com
;Website: http://PortableApps.com/

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

;EXCEPTION: The PortableApps.com Installer can be used with open source
;applications licensed under OSI-approved licenses as well as freeware provided
;it is unmodified and it adheres to the current PortableApps.com Format Specification
;as published at PortableApps.com/development. It may also be used with commercial
;software by contacting PortableApps.com.

!define PORTABLEAPPSINSTALLERVERSION "3.0.5.0"
!define PORTABLEAPPS.COMFORMATVERSION "3.0.5"

!if ${__FILE__} == "PortableApps.comInstallerPlugin.nsi"
	!include PortableApps.comInstallerPluginConfig.nsh
	!define PLUGININSTALLER
!else
	!include PortableApps.comInstallerConfig.nsh
!endif

!define MAINSECTIONIDX 0
!ifdef MAINSECTIONTITLE
	!define OPTIONALSECTIONIDX 1
!endif

;=== Program Details
Name "${PORTABLEAPPNAME}" "${PORTABLEAPPNAMEDOUBLEDAMPERSANDS}"
OutFile "..\..\..\${FILENAME}.paf.exe"
!ifdef COMMONFILESPLUGIN
	InstallDir "\CommonFiles\${APPID}"
!else
	InstallDir "\${APPID}"
!endif
Caption "${PORTABLEAPPNAME} | PortableApps.com Installer"
VIProductVersion "${VERSION}"
VIAddVersionKey ProductName "${PORTABLEAPPNAME}"
VIAddVersionKey Comments "${INSTALLERCOMMENTS}"
VIAddVersionKey CompanyName "PortableApps.com"
VIAddVersionKey LegalCopyright "PortableApps.com Installer Copyright 2007-2012 PortableApps.com."
VIAddVersionKey FileDescription "${PORTABLEAPPNAME}"
VIAddVersionKey FileVersion "${VERSION}"
VIAddVersionKey ProductVersion "${VERSION}"
VIAddVersionKey InternalName "${PORTABLEAPPNAME}"
VIAddVersionKey LegalTrademarks "${INSTALLERADDITIONALTRADEMARKS}PortableApps.com is a registered trademark of Rare Ideas, LLC."
VIAddVersionKey OriginalFilename "${FILENAME}.paf.exe"
VIAddVersionKey PortableApps.comInstallerVersion "${PORTABLEAPPSINSTALLERVERSION}"
VIAddVersionKey PortableApps.comFormatVersion "${PORTABLEAPPS.COMFORMATVERSION}"
VIAddVersionKey PortableApps.comAppID "${APPID}"
!ifdef DownloadURL ;advertise the needed bits to the PA.c Updater
	VIAddVersionKey PortableApps.comDownloadURL "${DownloadURL}"
	VIAddVersionKey PortableApps.comDownloadName "${DownloadName}"
	VIAddVersionKey PortableApps.comDownloadFileName "${DownloadFileName}"
	VIAddVersionKey PortableApps.comDownloadMD5 "${DownloadMD5}"
!endif

;=== Runtime Switches
SetCompress Auto
SetCompressor /SOLID lzma
SetCompressorDictSize 32
SetDatablockOptimize On
CRCCheck on
AutoCloseWindow True
RequestExecutionLevel user
AllowRootDirInstall true

;=== Include
!include MUI.nsh
!include FileFunc.nsh
!include LogicLib.nsh
!ifdef PRESERVEFILE1
	!include PortableApps.comInstallerMoveFiles.nsh
!endif
!ifdef COPYLOCALFILES
	!include Registry.nsh
!endif
!include TextFunc.nsh
!include WordFunc.nsh
!include PortableApps.comInstallerDumpLogToFile.nsh
!include PortableApps.comInstallerTBProgress.nsh

;=== Program Icon
Icon "PortableApps.comInstaller.ico"
!define MUI_ICON "PortableApps.comInstaller.ico"
!define MUI_UNICON "PortableApps.comInstaller.ico"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "PortableApps.comInstallerHeader.bmp"
!define MUI_HEADERIMAGE_BITMAP_RTL "PortableApps.comInstallerHeaderRTL.bmp"
!define MUI_HEADERIMAGE_RIGHT

;=== Icon & Stye ===
BrandingText "PortableApps.com®"

;=== Pages
!ifdef COPYLOCALFILES
	!define MUI_CUSTOMFUNCTION_ABORT CustomAbortFunction
!endif
!define MUI_LANGDLL_WINDOWTITLE "${PORTABLEAPPNAME}"
!define MUI_LANGDLL_INFO "Please select a language for the installer."
!define MUI_WELCOMEFINISHPAGE_BITMAP "PortableApps.comInstaller.bmp"
!ifdef PLUGINNAME
	!define MUI_WELCOMEPAGE_TITLE "${PORTABLEAPPNAMEDOUBLEDAMPERSANDS}"
!else
	!define MUI_WELCOMEPAGE_TITLE "${PORTABLEAPPNAMEDOUBLEDAMPERSANDS}"
!endif
!define MUI_WELCOMEPAGE_TEXT "$(welcome)"
!define MUI_PAGE_CUSTOMFUNCTION_PRE PreWelcome
!define MUI_COMPONENTSPAGE_SMALLDESC
!insertmacro MUI_PAGE_WELCOME
!ifdef LICENSEAGREEMENT
	;!define MUI_LICENSEPAGE_CHECKBOX
	!define MUI_PAGE_CUSTOMFUNCTION_PRE PreLicense
	!define MUI_PAGE_CUSTOMFUNCTION_SHOW ShowLicense
	!define MUI_PAGE_CUSTOMFUNCTION_LEAVE LeaveLicense
	!insertmacro MUI_PAGE_LICENSE "..\..\App\AppInfo\${LICENSEAGREEMENT}"
!endif
!ifdef MAINSECTIONTITLE
	!define MUI_PAGE_CUSTOMFUNCTION_PRE PreComponents
	!insertmacro MUI_PAGE_COMPONENTS
!endif
!define MUI_DIRECTORYPAGE_VERIFYONLEAVE
!define MUI_PAGE_CUSTOMFUNCTION_PRE PreDirectory
!define MUI_PAGE_CUSTOMFUNCTION_LEAVE LeaveDirectory
!insertmacro MUI_PAGE_DIRECTORY
!define MUI_PAGE_CUSTOMFUNCTION_SHOW ShowInstFiles
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_TEXT "$(finish)"
!define MUI_PAGE_CUSTOMFUNCTION_PRE PreFinish
!define MUI_FINISHPAGE_TITLE_3LINES
!define MUI_FINISHPAGE_CANCEL_ENABLED
!ifndef PLUGINNAME
	!define MUI_FINISHPAGE_RUN_NOTCHECKED
	!define MUI_FINISHPAGE_RUN "$INSTDIR\${FINISHPAGERUN}"
!endif
!insertmacro MUI_PAGE_FINISH

;=== Languages
!ifndef INSTALLERMULTILINGUAL
	!insertmacro MUI_LANGUAGE "${INSTALLERLANGUAGE}"
	!include PortableApps.comInstallerLanguages\${INSTALLERLANGUAGE}.nsh
!else
	!tempfile LangAutoDetectFile
	!macro IncludeLang _LANG_NAME
		; define and filename are all uppercase but both case insensitive
		!ifdef USES_${_LANG_NAME}
			!insertmacro MUI_LANGUAGE "${_LANG_NAME}"
			!include PortableApps.comInstallerLanguages\${_LANG_NAME}.nsh
			!appendfile "${LangAutoDetectFile}" "${Case} ${LANG_${_LANG_NAME}}$\n"
		!endif
	!macroend
	!define IncludeLang "!insertmacro IncludeLang"

	${IncludeLang} English
	${IncludeLang} EnglishGB
	${IncludeLang} Afrikaans
	${IncludeLang} Albanian
	${IncludeLang} Arabic
	${IncludeLang} Armenian
	${IncludeLang} Basque
	${IncludeLang} Belarusian
	${IncludeLang} Bosnian
	${IncludeLang} Breton
	${IncludeLang} Bulgarian
	${IncludeLang} Catalan
	${IncludeLang} Cibemba
	${IncludeLang} Croatian
	${IncludeLang} Czech
	${IncludeLang} Danish
	${IncludeLang} Dutch
	${IncludeLang} Efik
	${IncludeLang} Esperanto
	${IncludeLang} Estonian
	${IncludeLang} Farsi
	${IncludeLang} Finnish
	${IncludeLang} French
	${IncludeLang} Galician
	${IncludeLang} Georgian
	${IncludeLang} German
	${IncludeLang} Greek
	${IncludeLang} Hebrew
	${IncludeLang} Hungarian
	${IncludeLang} Icelandic
	${IncludeLang} Igbo
	${IncludeLang} Indonesian
	${IncludeLang} Irish
	${IncludeLang} Italian
	${IncludeLang} Japanese
	${IncludeLang} Khmer
	${IncludeLang} Korean
	${IncludeLang} Kurdish
	${IncludeLang} Latvian
	${IncludeLang} Lithuanian
	${IncludeLang} Luxembourgish
	${IncludeLang} Macedonian
	${IncludeLang} Malagasy
	${IncludeLang} Malay
	${IncludeLang} Mongolian
	${IncludeLang} Norwegian
	${IncludeLang} NorwegianNynorsk
	${IncludeLang} Pashto
	${IncludeLang} Polish
	${IncludeLang} Portuguese
	${IncludeLang} PortugueseBR
	${IncludeLang} Romanian
	${IncludeLang} Russian
	${IncludeLang} Serbian
	${IncludeLang} SerbianLatin
	${IncludeLang} SimpChinese
	${IncludeLang} Slovak
	${IncludeLang} Slovenian
	${IncludeLang} Spanish
	${IncludeLang} SpanishInternational
	${IncludeLang} Swahili
	${IncludeLang} Swedish
	${IncludeLang} Thai
	${IncludeLang} TradChinese
	${IncludeLang} Turkish
	${IncludeLang} Ukrainian
	${IncludeLang} Uzbek
	${IncludeLang} Valencia
	${IncludeLang} Vietnamese
	${IncludeLang} Welsh
	${IncludeLang} Yoruba

	!insertmacro MUI_RESERVEFILE_LANGDLL
!endif

;=== Macros
!macro !insertmacro1-10 _m
!insertmacro ${_m} 1
!insertmacro ${_m} 2
!insertmacro ${_m} 3
!insertmacro ${_m} 4
!insertmacro ${_m} 5
!insertmacro ${_m} 6
!insertmacro ${_m} 7
!insertmacro ${_m} 8
!insertmacro ${_m} 9
!insertmacro ${_m} 10
!macroend
!define !insertmacro1-10 "!insertmacro !insertmacro1-10"

;=== Variables
Var FOUNDPORTABLEAPPSPATH
!ifdef MAINSECTIONTITLE
	Var OPTIONAL1DONE
!endif
Var AUTOMATEDINSTALL
Var AUTOCLOSE
Var SILENTLANGUAGEMODE
Var HIDEINSTALLER
Var MINIMIZEINSTALLER
!ifdef LICENSEAGREEMENT
	Var EULAVERSIONMATCH
!endif
!ifdef COPYLOCALFILES
	Var CopyLocalFilesFrom
	Var CopyLocalFilesTo
	Var MISSINGFILEORPATH
!endif
!ifdef DOWNLOADURL
	Var MD5MISMATCH
	Var DOWNLOADRESULT
	Var DOWNLOADEDFILE
	Var DOWNLOADALREADYEXISTED
	Var SECONDDOWNLOADATTEMPT
!endif
Var INTERNALEULAVERSION
Var InstallingStatusString
Var bolAppUpgrade
Var bolLogFile

;=== Custom Code
!ifdef USESCUSTOMCODE
	!if ${__FILE__} == "PortableApps.comInstallerPlugin.nsi"
		!include PortableApps.comInstallerPluginCustom.nsh
	!else
		!include PortableApps.comInstallerCustom.nsh
	!endif
!endif

!ifdef INSTALLERMULTILINGUAL
	!macro CaseLang _LANG_NAME _LANG_ID
		!ifdef USES_${_LANG_NAME}
			${Case} ${_LANG_ID}
		!endif
	!macroend
	!define CaseLang "!insertmacro CaseLang"
!endif

Function .onInit
	SetSilent normal

	!ifdef DownloadURL
	StrCpy $R0 $EXEFILE "" -15
	${If} $R0 != "_online.paf.exe"
	${AndIf} $R0 != "line.paf[1].exe" ;Handle IE's renaming of files when run directly from a download
	${AndIf} $R0 != "line.paf[2].exe"
	${AndIf} $R0 != "line.paf[3].exe"
	${AndIf} $R0 != "line.paf[4].exe"
	${AndIf} $R0 != "line.paf[5].exe"
	${AndIf} $R0 != "line.paf[6].exe"
	${AndIf} $R0 != "line.paf[7].exe"
	${AndIf} $R0 != "line.paf[8].exe"
	${AndIf} $R0 != "line.paf[9].exe"
		MessageBox MB_OK|MB_ICONSTOP `PortableApps.com Installers that download files must end with "_online.paf.exe".  This is to ensure that users always know that an installer downloads files before it is run.  Please rename the file to end in _online.paf.exe before running.`
		Abort
	${EndIf}
	!endif

	InitPluginsDir

	!ifdef INSTALLERMULTILINGUAL
		ReadEnvStr $0 "PortableApps.comLocaleID"
		${Switch} $0
			; Use the Case statements formed earlier.
			!include "${LangAutoDetectFile}"
			!delfile "${LangAutoDetectFile}"
			!undef LangAutoDetectFile
				StrCpy $LANGUAGE $0
				${Break}
			${Default}
				!insertmacro MUI_LANGDLL_DISPLAY
		${EndSwitch}
	!endif

	;=== Check for logging mode
	${GetOptions} $CMDLINE "/LOG=" $0
	
	${IfNot} ${Errors}
	${AndIf} $0 == "true"
		StrCpy $bolLogFile true
	${Else}
		ClearErrors
	${EndIf}
	
	;=== Check for a specified installation directory
	${GetOptions} $CMDLINE "/DESTINATION=" $0

	${IfNot} ${Errors}
		!ifdef COMMONFILESPLUGIN
			StrCpy $INSTDIR "$0CommonFiles\${APPID}"
		!else
			${GetOptions} $CMDLINE "/COPYNUMBER=" $1
			${IfNot} ${Errors}
				StrCpy $INSTDIR "$0${APPID}_Copy_$1"
			${Else}
				StrCpy $INSTDIR "$0${APPID}"
			${EndIf}
		!endif

		!ifdef LICENSEAGREEMENT
			!ifndef EULAVERSION
				StrCpy $INTERNALEULAVERSION "1"
			!else
				StrCpy $INTERNALEULAVERSION ${EULAVERSION}
			!endif
			${If} ${FileExists} "$INSTDIR\Data\PortableApps.comInstaller\license.ini"
				ReadINIStr $0 "$INSTDIR\Data\PortableApps.comInstaller\license.ini" "PortableApps.comInstaller" "EULAVersion"
				ClearErrors
				${If} $0 == $INTERNALEULAVERSION
					StrCpy $EULAVERSIONMATCH "true"
				${EndIf}
			${EndIf}
		!endif

		;=== Check for PortableApps.com Platform
		${GetParent} $INSTDIR $0
		!ifdef COMMONFILESPLUGIN
			${GetParent} $0 $0
		!endif

		;=== Check that it exists at the right location
		DetailPrint '$(checkforplatform)'

		${If} ${FileExists} `$0\PortableApps.com\PortableAppsPlatform.exe`
			;=== Check that it's the real deal
			MoreInfo::GetProductName `$0\PortableApps.com\PortableAppsPlatform.exe`
			Pop $1
			${If} $1 == "PortableApps.com Platform"
				MoreInfo::GetCompanyName `$0\PortableApps.com\PortableAppsPlatform.exe`
				Pop $1
				${If} $1 == "PortableApps.com"
					;=== Check that it's running
					FindProcDLL::FindProc "PortableAppsPlatform.exe"
					${If} $R0 == 1
						;=== Do a partially automated install
						StrCpy $AUTOMATEDINSTALL "true"

						ClearErrors
						${GetOptions} $CMDLINE "/AUTOCLOSE=" $R0
						${IfNot} ${Errors}
						${AndIf} $R0 == "true"
							StrCpy $AUTOCLOSE "true"
						${EndIf}

						ClearErrors
						${GetOptions} $CMDLINE "/HIDEINSTALLER=" $R0
						${IfNot} ${Errors}
						${AndIf} $R0 == "true"
							StrCpy $HIDEINSTALLER "true"
						${EndIf}

						ClearErrors
						${GetOptions} $CMDLINE "/MINIMIZEINSTALLER=" $R0
						${IfNot} ${Errors}
						${AndIf} $R0 == "true"
							StrCpy $MINIMIZEINSTALLER "true"
						${EndIf}

						ClearErrors
						${GetOptions} $CMDLINE "/SILENT=" $R0
						${IfNot} ${Errors}
						${AndIf} $R0 == "true"
							;Duplicate of the size calculation code, to be functionalized later
							SectionGetSize ${MAINSECTIONIDX} $1 ;=== Space Required for App
							!ifdef MAINSECTIONTITLE
								SectionGetFlags ${OPTIONALSECTIONIDX} $9
								IntOp $9 $9 & ${SF_SELECTED}
								${If} $9 >= ${SF_SELECTED}
									SectionGetSize ${OPTIONALSECTIONIDX} $2 ;=== Space Required for App
									IntOp $1 $1 + $2
								${EndIf}
							!endif
							${GetRoot} $INSTDIR $2
							${DriveSpace} `$2\` "/D=F /S=M" $3 ;=== Space Free on Device
							
							IntOp $1 $1 / 1024

							${If} $3 <= $1
								IntOp $1 $1 * 1024
								IntOp $3 $3 * 1024
								!ifndef PLUGININSTALLER ;=== If not a plugin installer, add the current install size to free space
									${If} ${FileExists} $INSTDIR
										${GetSize} `$INSTDIR` "/M=*.* /S=0K /G=0" $4 $5 $6 ;=== Current installation size
										IntOp $3 $3 + $4 ;=== Space Free + Current Root Install Size
										${GetSize} `$INSTDIR\App` "/M=*.* /S=0K /G=1" $4 $5 $6 ;=== Current installation size
										IntOp $3 $3 + $4 ;=== Space Free + Current App Install Size
										${GetSize} `$INSTDIR\Other` "/M=*.* /S=0K /G=1" $4 $5 $6 ;=== Current installation size
										IntOp $3 $3 + $4 ;=== Space Free + Current Other Install Size

										${If} `${ADDONSDIRECTORYPRESERVE}` != "NONE"
										${AndIf} ${FileExists} `$INSTDIR\${ADDONSDIRECTORYPRESERVE}`
												${GetSize} `$INSTDIR\${ADDONSDIRECTORYPRESERVE}` "/M=*.* /S=0K /G=1" $4 $5 $6 ;=== Size of Data directory
												IntOp $3 $3 - $4 ;=== Remove the plugins directory from the free space calculation
										${EndIf}
									${EndIf}
								!else
									!ifdef COMMONFILESPLUGIN ;Duplicate code for now, to do above for CommonFiles as well
										${If} ${FileExists} $INSTDIR
											${GetSize} `$INSTDIR` "/M=*.* /S=0K /G=1" $4 $5 $6 ;=== Current installation size
											IntOp $3 $3 + $4 ;=== Space Free + Current Install Size
										${EndIf}
									!endif
								!endif
								${If} $3 <= $1
									MessageBox MB_OK|MB_ICONEXCLAMATION $(notenoughspace)
									Abort
							${EndIf}
							${EndIf}

							!ifdef LICENSEAGREEMENT
								${If} $EULAVERSIONMATCH == "true"
									SetSilent silent
								${EndIf}
							!else
								SetSilent silent
							!endif
						${EndIf}

						ClearErrors
						${GetOptions} $CMDLINE "/SILENTLANGUAGEMODE=" $R0
						${IfNot} ${Errors}
							${If} $R0 == "auto"
							${OrIf} $R0 == "never"
							${OrIf} $R0 == "always"
								StrCpy $SILENTLANGUAGEMODE $R0
							${Else}
								StrCpy $SILENTLANGUAGEMODE "auto"
							${EndIf}
						${Else}
							StrCpy $SILENTLANGUAGEMODE "auto"
						${EndIf}

					${EndIf}
				${EndIf}
			${EndIf}
		${EndIf}
	${Else}
		ClearErrors
		;=== Check legacy location
		${GetOptions} $CMDLINE "-o" $R0
		${IfNot} ${Errors}
			!ifdef COMMONFILESPLUGIN
				StrCpy $INSTDIR "$R0CommonFiles\${APPID}"
			!else
				StrCpy $INSTDIR "$R0${APPID}"
			!endif
		${Else}
			;=== No installation directory found
			ClearErrors
			${If} ${FileExists}	"$PROFILE\PortableApps\*.*"
				StrCpy $FOUNDPORTABLEAPPSPATH "$Profile\PortableApps"
			${Else}
				${GetDrives} "HDD+FDD" GetDrivesCallBack
			${EndIf}
			${If} $FOUNDPORTABLEAPPSPATH != ""
				!ifdef COMMONFILESPLUGIN
					StrCpy $INSTDIR "$FOUNDPORTABLEAPPSPATH\CommonFiles\${APPID}"
				!else
					StrCpy $INSTDIR "$FOUNDPORTABLEAPPSPATH\${APPID}"
				!endif
			${Else}
				!ifdef COMMONFILESPLUGIN
					StrCpy $INSTDIR "$EXEDIR\CommonFiles\${APPID}"
				!else
					StrCpy $INSTDIR "$EXEDIR\${APPID}"
				!endif
			${EndIf}
		${EndIf}
	${EndIf}

	!ifdef MAINSECTIONTITLE
		!ifdef OPTIONALSECTIONPRESELECTEDIFNONENGLISHINSTALL
			;=== If it's not English, select the optional component (languages) by default
			${IfThen} $LANGUAGE != 1033 ${|} SectionSetFlags 1 ${OPTIONALSECTIONIDX} ${|}
		!endif
		${If} ${Silent}
			${If} "${OPTIONALSECTIONINSTALLEDWHENSILENT}" == "true"
				SectionSetFlags 1 ${OPTIONALSECTIONIDX}
			${ElseIf} "${OptionalSectionSelectedInstallType}" == "Multilingual"
				${If} $SILENTLANGUAGEMODE != "never"
					${If} $SILENTLANGUAGEMODE == "always"
						SectionSetFlags 1 ${OPTIONALSECTIONIDX}
					${Else}
						${IfThen} $LANGUAGE != 1033 ${|} SectionSetFlags 1 ${OPTIONALSECTIONIDX} ${|}
					${EndIf}
				${EndIf}
			${EndIf}
		${EndIf}

	!endif

	!ifdef COPYLOCALFILES
		StrCpy $CopyLocalFilesFrom ""

		${If} "${CopyFromRegPath}" != ""
			${registry::Read} "${CopyFromRegPath}" "${CopyFromRegKey}" $R0 $R1
			${If} $R0 != ""
				;Strip trailing slash if there
				StrCpy $1 $R0 "" -1
				${If} $1 == "\"
					StrCpy $R0 $R0 -1
				${EndIf}

				;Go up directories if needed
				${If} "${CopyFromRegRemoveDirectories}" != ""
					StrCpy $1 1
					${Do}
						${GetParent} $R0 $R0
						IntOp $1 $1 + 1
					${LoopUntil} $1 > "${CopyFromRegRemoveDirectories}"
				${EndIf}

				;Check for existence
				${If} ${FileExists} "$R0\*.*"
					StrCpy $CopyLocalFilesFrom $R0
				${EndIf}
			${EndIf}
		${EndIf}

		;Fallback to direct entry
		${If} $CopyLocalFilesFrom == ""
		${AndIf} "${CopyFromDirectory}" != ""
			StrCpy $CopyLocalFilesFrom "${CopyFromDirectory}"
			${WordReplace} $CopyLocalFilesFrom "%PROGRAMFILES%" $PROGRAMFILES + $CopyLocalFilesFrom
			${WordReplace} $CopyLocalFilesFrom "%PROGRAMFILES32%" $PROGRAMFILES32 + $CopyLocalFilesFrom
			${WordReplace} $CopyLocalFilesFrom "%PROGRAMFILES64%" $PROGRAMFILES64 + $CopyLocalFilesFrom
			${WordReplace} $CopyLocalFilesFrom "%COMMONFILES%" $COMMONFILES + $CopyLocalFilesFrom
			${WordReplace} $CopyLocalFilesFrom "%COMMONFILES32%" $COMMONFILES32 + $CopyLocalFilesFrom
			${WordReplace} $CopyLocalFilesFrom "%COMMONFILES64%" $COMMONFILES64 + $CopyLocalFilesFrom
			${WordReplace} $CopyLocalFilesFrom "%DESKTOP%" $DESKTOP + $CopyLocalFilesFrom
			${WordReplace} $CopyLocalFilesFrom "%WINDIR%" $WINDIR + $CopyLocalFilesFrom
			${WordReplace} $CopyLocalFilesFrom "%SYSDIR%" $SYSDIR + $CopyLocalFilesFrom
			${WordReplace} $CopyLocalFilesFrom "%APPDATA%" $APPDATA + $CopyLocalFilesFrom
			${WordReplace} $CopyLocalFilesFrom "%LOCALAPPDATA%" $LOCALAPPDATA + $CopyLocalFilesFrom
			${WordReplace} $CopyLocalFilesFrom "%TEMP%" $TEMP + $CopyLocalFilesFrom
		${EndIf}
		${If} ${FileExists} "$CopyLocalFilesFrom\*.*"
			SectionGetSize ${MAINSECTIONIDX} $0
			${GetSize} $CopyLocalFilesFrom "/M=*.* /S=0K /G=1" $1 $2 $3
			IntOp $0 $0 + $1
			SectionSetSize ${MAINSECTIONIDX} $0
		${EndIf}
	!endif
	!ifdef AdditionalInstallSize
		SectionGetSize ${MAINSECTIONIDX} $0
		IntOp $0 $0 + ${AdditionalInstallSize}
		SectionSetSize ${MAINSECTIONIDX} $0
	!endif

	${If} "${CHECKRUNNING}" != "NONE"
		;=== Check if app is running?
		RunningTryAgain:
		FindProcDLL::FindProc "${CHECKRUNNING}"
		${If} $R0 == 1
			MessageBox MB_OKCANCEL|MB_ICONINFORMATION $(runwarning) IDOK RunningTryAgain IDCANCEL RunningCancel
			
			RunningCancel:
				Abort
		${EndIf}
	${EndIf}
FunctionEnd

Function PreWelcome
	${IfThen} $AUTOMATEDINSTALL == "true" ${|} Abort ${|}
FunctionEnd

!ifdef LICENSEAGREEMENT
Function PreLicense
	${If} $AUTOMATEDINSTALL == "true"
	${AndIf} $EULAVERSIONMATCH == "true"
		Abort
	${EndIf}

	!ifndef EULAVERSION
		StrCpy $INTERNALEULAVERSION "1"
	!else
		StrCpy $INTERNALEULAVERSION "${EULAVERSION}"
	!endif
	${If} ${FileExists} "$INSTDIR\Data\PortableApps.comInstaller\license.ini"
		ReadINIStr $0 "$INSTDIR\Data\PortableApps.comInstaller\license.ini" "PortableApps.comInstaller" "EULAVersion"
		ClearErrors
		${If} $0 == $INTERNALEULAVERSION
		${AndIf} $AUTOMATEDINSTALL == "true"
			Abort
		${EndIf}
	${EndIf}
FunctionEnd
Function ShowLicense
	${If} $AUTOMATEDINSTALL == "true"
		${TBProgress} 20
		${TBProgress_State} Paused
	${EndIf}
FunctionEnd
Function LeaveLicense
	${If} $AUTOMATEDINSTALL == "true"
		${TBProgress_State} NoProgress
	${EndIf}
FunctionEnd
!endif

Function ShowInstFiles
	w7tbp::Start
FunctionEnd

!ifdef MAINSECTIONTITLE
	Function PreComponents
		${If} $AUTOCLOSE != "true"
		${OrIfNot} ${FileExists} "$INSTDIR\App\AppInfo\appinfo.ini"
			Return
		${EndIf}

		ReadINIStr $0 "$INSTDIR\App\AppInfo\appinfo.ini" "Details" "InstallType"
		ClearErrors
		${If} $0 == "${OPTIONALSECTIONSELECTEDINSTALLTYPE}"
			SectionSetFlags 1 ${OPTIONALSECTIONIDX}
			Abort
		${EndIf}

		;=== Check not selected
		${If} $0 == "${OPTIONALSECTIONNOTSELECTEDINSTALLTYPE}"
			SectionSetFlags 0 ${OPTIONALSECTIONIDX}
			Abort
		${EndIf}
	FunctionEnd
!endif

Function PreDirectory
	${IfThen} $AUTOMATEDINSTALL != "true" ${|} Return ${|}

	SectionGetSize ${MAINSECTIONIDX} $1 ;=== Space Required for App
	!ifdef MAINSECTIONTITLE
		SectionGetFlags ${OPTIONALSECTIONIDX} $9
		IntOp $9 $9 & ${SF_SELECTED}
		${If} $9 >= ${SF_SELECTED}
			SectionGetSize ${OPTIONALSECTIONIDX} $2 ;=== Space Required for App
			IntOp $1 $1 + $2
		${EndIf}
	!endif
	${GetRoot} $INSTDIR $2
	${DriveSpace} `$2\` "/D=F /S=M" $3 ;=== Space Free on Device
							
	IntOp $1 $1 / 1024

	${If} $3 <= $1
		IntOp $1 $1 * 1024
		IntOp $3 $3 * 1024

		!ifndef PLUGININSTALLER ;=== If not a plugin installer, add the current install size to free space
			${If} ${FileExists} $INSTDIR
				${GetSize} $INSTDIR "/M=*.* /S=0K /G=1" $4 $5 $6 ;=== Current installation size
				IntOp $3 $3 + $4 ;=== Space Free + Current Install Size

				${If} ${FileExists} `$INSTDIR\Data`
					${GetSize} `$INSTDIR\Data` "/M=*.* /S=0K /G=1" $4 $5 $6 ;=== Size of Data directory
					IntOp $3 $3 - $4 ;=== Remove the data directory from the free space calculation
				${EndIf}

				${If} `${ADDONSDIRECTORYPRESERVE}` != "NONE"
				${AndIf} ${FileExists} `$INSTDIR\${ADDONSDIRECTORYPRESERVE}`
						${GetSize} `$INSTDIR\${ADDONSDIRECTORYPRESERVE}` "/M=*.* /S=0K /G=1" $4 $5 $6 ;=== Size of Data directory
						IntOp $3 $3 - $4 ;=== Remove the plugins directory from the free space calculation
				${EndIf}
			${EndIf}
		!else
			!ifdef COMMONFILESPLUGIN ;Duplicate code for now, to do above for CommonFiles as well
				${If} ${FileExists} $INSTDIR
					${GetSize} `$INSTDIR` "/M=*.* /S=0K /G=1" $4 $5 $6 ;=== Current installation size
					IntOp $3 $3 + $4 ;=== Space Free + Current Install Size
				${EndIf}
			!endif
		!endif

		${If} $3 <= $1
			MessageBox MB_OK|MB_ICONEXCLAMATION "$(notenoughspace)"
			Return
		${EndIf}
	${EndIf}

	;=== Check if app is running?
	${IfThen} "${CHECKRUNNING}" == "NONE" ${|} Abort ${|}
	FindProcDLL::FindProc "${CHECKRUNNING}"
	${IfThen} $R0 != "1" ${|} Abort ${|}
	MessageBox MB_OK|MB_ICONINFORMATION $(runwarning)
FunctionEnd

Function LeaveDirectory
	GetInstDirError $0

	;=== Does it already exist? (upgrade)
	${If} ${FileExists} $INSTDIR
	${AndIf} "${CHECKRUNNING}" != "NONE"
		;=== Check if app is running?
		FindProcDLL::FindProc "${CHECKRUNNING}"
		${If} $R0 = 1
			MessageBox MB_OK|MB_ICONINFORMATION $(runwarning)
			Abort
		${EndIf}
	${EndIf}

	; 0 is valid, enough space, all fine
	${Select} $0
		${Case} 1
			MessageBox MB_OK|MB_ICONINFORMATION $(invaliddirectory)
			Abort

		${Case} 2
			${IfNot} ${FileExists} $INSTDIR ;=== Is upgrade
				MessageBox MB_OK|MB_ICONEXCLAMATION $(notenoughspace)
				Abort
			${EndIf}

			SectionGetSize ${MAINSECTIONIDX} $1 ;=== Space Required for App
			!ifdef MAINSECTIONTITLE
					SectionGetFlags ${OPTIONALSECTIONIDX} $9
					IntOp $9 $9 & ${SF_SELECTED}
					${If} $9 >= ${SF_SELECTED}
						SectionGetSize ${OPTIONALSECTIONIDX} $2 ;=== Space Required for App
						IntOp $1 $1 + $2
					${EndIf}
			!endif
			${GetRoot} $INSTDIR $2
			${DriveSpace} `$2\` "/D=F /S=K" $3 ;=== Space Free on Device

			
			!ifndef PLUGININSTALLER ;=== If not a plugin installer, add the current install size to free space
				${GetSize} `$INSTDIR` "/M=*.* /S=0K /G=0" $4 $5 $6 ;=== Current installation size
				IntOp $3 $3 + $4 ;=== Space Free + Current Root Install Size
				${GetSize} `$INSTDIR\App` "/M=*.* /S=0K /G=1" $4 $5 $6 ;=== Current installation size
				IntOp $3 $3 + $4 ;=== Space Free + Current App Install Size
				${GetSize} `$INSTDIR\Other` "/M=*.* /S=0K /G=1" $4 $5 $6 ;=== Current installation size
				IntOp $3 $3 + $4 ;=== Space Free + Current Other Install Size

				${If} `${ADDONSDIRECTORYPRESERVE}` != "NONE"
				${AndIf} ${FileExists} `$INSTDIR\${ADDONSDIRECTORYPRESERVE}`
						${GetSize} `$INSTDIR\${ADDONSDIRECTORYPRESERVE}` "/M=*.* /S=0K /G=1" $4 $5 $6 ;=== Size of Data directory
						IntOp $3 $3 - $4 ;=== Remove the plugins directory from the free space calculation
				${EndIf}
			!else
				!ifdef COMMONFILESPLUGIN ;Duplicate code for now, to do above for CommonFiles as well
					${GetSize} `$INSTDIR` "/M=*.* /S=0K /G=1" $4 $5 $6 ;=== Current installation size
					IntOp $3 $3 + $4 ;=== Space Free + Current Install Size
				!endif
			!endif

			${If} $3 <= $1
				MessageBox MB_OK|MB_ICONEXCLAMATION "$(notenoughspace)"
				Abort
			${EndIf}
	${EndSelect}
	
	;Check for Program Files
	ReadEnvStr $0 IPromiseNotToComplainWhenPortableAppsDontWorkRightInProgramFiles
	${If} $0 != "I understand that this may not work and that I can not ask for help with any of my apps when operating in this fashion."
		${WordFind} "$INSTDIR\" "$PROGRAMFILES\" "*" $R0
		${If} $R0 > 0
			MessageBox MB_OK|MB_ICONINFORMATION "$(invaliddirectory) [$PROGRAMFILES or sub-directories]"
			Abort
		${EndIf}
		${WordFind} "$INSTDIR\" "$PROGRAMFILES64\" "*" $R0
		${If} $R0 > 0
			MessageBox MB_OK|MB_ICONINFORMATION "$(invaliddirectory) [$PROGRAMFILES64 or sub-directories]"
			Abort
		${EndIf}
	${EndIf}
FunctionEnd

Function PreFinish
	${IfThen} $AUTOCLOSE == "true" ${|} Abort ${|}
FunctionEnd

Function GetDrivesCallBack
	;=== Skip usual floppy letters
	${If} $8 == "FDD"
		${If} $9 == "A:\"
		${OrIf} $9 == "B:\"
			Push $0
			Return
		${EndIf}
	${EndIf}

	${If} ${FileExists} $9PortableApps
		StrCpy $FOUNDPORTABLEAPPSPATH $9PortableApps
	${EndIf}

	Push $0
FunctionEnd

!ifdef MAINSECTIONTITLE
	Section "${MAINSECTIONTITLE}"
!else
	Section "App Portable (required)"
!endif

	${If} $MINIMIZEINSTALLER == "true"
		ShowWindow $HWNDPARENT ${SW_MINIMIZE}
	${EndIf}
	${If} $HIDEINSTALLER == "true"
		ShowWindow $HWNDPARENT ${SW_HIDE}
	${EndIf}

	${If} ${FileExists} "$INSTDIR\*.*"
		StrCpy $bolAppUpgrade true
	${EndIf}

	${If} $(installingstatus) != ""
		StrCpy $InstallingStatusString "$(installingstatus)"
	${Else}
		StrCpy $InstallingStatusString "$(MUI_TEXT_INSTALLING_TITLE)"
	${EndIf}

	SectionIn RO
	SetOutPath $INSTDIR

	${If} $bolAppUpgrade == true
		${If} $(prepareupgrade) == ""
			DetailPrint $InstallingStatusString
		${Else}
			DetailPrint $(prepareupgrade)
		${EndIf}
	${Else}
		DetailPrint $InstallingStatusString
	${EndIf}
	SetDetailsPrint ListOnly
	
	;=== Download Files
!ifdef DownloadURL
	${If} ${FileExists} `$EXEDIR\${DownloadFileName}`
		!ifdef DownloadMD5
			md5dll::GetMD5File "$EXEDIR\${DownloadFileName}"
			Pop $R0
			${If} $R0 == ${DownloadMD5}
				StrCpy $DOWNLOADALREADYEXISTED "true"
				StrCpy $DOWNLOADRESULT "OK"
			${EndIf}
		!else
			StrCpy $DOWNLOADALREADYEXISTED "true"
			StrCpy $DOWNLOADRESULT "OK"
		!endif
	${EndIf}
	
	${If} $DOWNLOADALREADYEXISTED == "true"
		StrCpy $DOWNLOADEDFILE "$EXEDIR\${DownloadFileName}"
	${Else}
		DownloadTheFile:
		CreateDirectory `$PLUGINSDIR\Downloaded`
		SetDetailsPrint both
		${If} $(downloading) != ""
			DetailPrint $(downloading)
		${Else}
			DetailPrint "Downloading ${DownloadName}..."
		${EndIf}

		SetDetailsPrint none
		Delete "$PLUGINSDIR\Downloaded\${DownloadName}"
		Delete "$PLUGINSDIR\Downloaded\${DownloadFilename}"
		
		${If} $(downloading) != ""
			inetc::get /CONNECTTIMEOUT 30 /NOCOOKIES /TRANSLATE $(downloading) $(downloadconnecting) $(downloadsecond) $(downloadminute) $(downloadhour) $(downloadplural) "%dkB (%d%%) of %dkB @ %d.%01dkB/s" " (%d %s%s $(downloadremaining))" "${DownloadURL}" "$PLUGINSDIR\Downloaded\${DownloadName}" /END
		${Else}
			inetc::get /CONNECTTIMEOUT 30 /NOCOOKIES /TRANSLATE "Downloading %s..." "Connecting..." second minute hour s "%dkB (%d%%) of %dkB @ %d.%01dkB/s" " (%d %s%s remaining)" "${DownloadURL}" "$PLUGINSDIR\Downloaded\${DownloadName}" /END
		${EndIf}
		SetDetailsPrint both
		DetailPrint $InstallingStatusString
		SetDetailsPrint ListOnly
		Pop $DOWNLOADRESULT
		${If} $DOWNLOADRESULT == "OK"
			Rename "$PLUGINSDIR\Downloaded\${DownloadName}" "$PLUGINSDIR\Downloaded\${DownloadFilename}"
			StrCpy $DOWNLOADEDFILE "$PLUGINSDIR\Downloaded\${DownloadFilename}"
			!ifdef DownloadMD5
				md5dll::GetMD5File "$DOWNLOADEDFILE"
				Pop $R0
				${If} $R0 != ${DownloadMD5}
					${If} $SECONDDOWNLOADATTEMPT != true
						StrCpy $SECONDDOWNLOADATTEMPT true
						Goto DownloadTheFile
					${EndIf}
					StrCpy $MD5MISMATCH "true"

					Delete "$INTERNET_CACHE\${DownloadFileName}"
					Delete "$PLUGINSDIR\Downloaded\${DownloadFilename}"
					SetDetailsPrint textonly
					DetailPrint ""
					SetDetailsPrint listonly
					${TBProgress_State} Error
					${If} $(downloadfilemismatch) != ""
						MessageBox MB_OK|MB_ICONEXCLAMATION $(downloadfilemismatch)
						DetailPrint $(downloadfilemismatch)
					${Else}
						MessageBox MB_OK|MB_ICONEXCLAMATION `The downloaded copy of ${DownloadName} is not valid and can not be installed.  Please try installing again.`
						DetailPrint `The downloaded copy of ${DownloadName} is not valid and can not be installed.  Please try installing again.`
					${EndIf}
					${TBProgress_State} NoProgress
					Abort
				${EndIf}
			!endif
		${Else}
			${If} $SECONDDOWNLOADATTEMPT != true
			${AndIf} $DOWNLOADRESULT != "Cancelled"
				StrCpy $SECONDDOWNLOADATTEMPT true
				Goto DownloadTheFile
			${EndIf}
			Delete "$INTERNET_CACHE\${DownloadFileName}"
			Delete "$PLUGINSDIR\Downloaded\${DownloadFilename}"
			SetDetailsPrint textonly
				DetailPrint ""
			SetDetailsPrint listonly
			${TBProgress_State} Error
			${If} $(downloadfailed) != ""
				MessageBox MB_OK|MB_ICONEXCLAMATION $(downloadfailed)
				DetailPrint $(downloadfailed)
			${Else}
				MessageBox MB_OK|MB_ICONEXCLAMATION `The installer was unable to download ${DownloadName}.  The installation of the portable app will be incomplete without it. Please try installing again. (ERROR: $DOWNLOADRESULT)`
				DetailPrint `The installer was unable to download ${DownloadName}.  The installation of the portable app will be incomplete without it. Please try installing again. (ERROR: $DOWNLOADRESULT)`
			${EndIf}
			${TBProgress_State} NoProgress
			Abort
		${EndIf}
	${EndIf}
!endif

!ifdef MAINSECTIONTITLE
	SectionGetFlags 1 $0
	IntOp $0 $0 & ${SF_SELECTED}
	${If} $0 != ${SF_SELECTED}
		;=== BEGIN: OPTIONAL NOT SELECTED CLEANUP CODE ===
		;This will be executed before install if the optional section (additional languages, etc) is not selected
		!ifmacrodef CustomCodeOptionalCleanup
			!insertmacro CustomCodeOptionalCleanup
		!endif
		;=== END: OPTIONAL NOT SELECTED CLEANUP CODE ===
	${EndIf}
!endif

	;=== BEGIN: PRE-INSTALL CODE ===
	;This will be executed before the app is installed.  Useful for cleaning up files no longer used.
	!ifmacrodef CustomCodePreInstall
		!insertmacro CustomCodePreInstall
	!endif
	;=== END: PRE-INSTALL CODE ===

	;=== Remove specific files
	!macro RemoveFile _n
		!ifdef REMOVEFILE${_n}
			Delete `$INSTDIR\${REMOVEFILE${_n}}`
		!endif
	!macroend
	${!insertmacro1-10} RemoveFile

	;=== Rename the preserved files so they're not deleted in the next part
	!macro PreserveFilePre _n
		!ifdef PRESERVEFILE${_n}
			${GetFileName} `$INSTDIR\${PRESERVEFILE${_n}}` $1
			${GetParent} `$INSTDIR\${PRESERVEFILE${_n}}` $2
			CreateDirectory `$INSTDIR\~PRESERVEFILE${_n}`
			${MoveFiles} DOS $1 $2 `$INSTDIR\~PRESERVEFILE${_n}`
		!endif
	!macroend
	${!insertmacro1-10} PreserveFilePre

	;=== Remove specific directories
	!macro RemoveDirectory _n
		!ifdef REMOVEDIRECTORY${_n}
			RMDir /r `$INSTDIR\${REMOVEDIRECTORY${_n}}`
		!endif
	!macroend
	${!insertmacro1-10} RemoveDirectory

	;=== Rename the preserved directories so they're not deleted in the next part
	!macro PreserveDirectoryPre _n
		!ifdef PRESERVEDIRECTORY${_n}
			Rename `$INSTDIR\${PRESERVEDIRECTORY${_n}}\` `$INSTDIR\~PRESERVEDIRECTORY${_n}\`
		!endif
	!macroend
	${!insertmacro1-10} PreserveDirectoryPre

	;=== Remove main directories if necessary
	!ifdef REMOVEAPPDIRECTORY
		!ifdef COMMONFILESPLUGIN
			${GetParent} $INSTDIR $0
			${For} $1 1 10
				Rename `$INSTDIR\~PRESERVEFILE$1\` `$0\~PRESERVEFILE$1\`
				Rename `$INSTDIR\~PRESERVEDIRECTORY$1\` `$0\~PRESERVEDIRECTORY$1\`
			${Next}
			RMDir /r $INSTDIR
			CreateDirectory $INSTDIR
			${For} $1 1 10
				Rename `$0\~PRESERVEFILE$1\` `$INSTDIR\~PRESERVEFILE$1\`
				Rename `$0\~PRESERVEDIRECTORY$1\` `$INSTDIR\~PRESERVEDIRECTORY$1\`
			${Next}
		!else
			RMDir /r `$INSTDIR\App`
		!endif
	!endif
	!ifdef REMOVEDATADIRECTORY
		RMDir /r `$INSTDIR\Data`
	!endif
	!ifdef REMOVEOTHERDIRECTORY
		RMDir /r `$INSTDIR\Other`
	!endif

	;=== Rename the preserved directories back to their proper names
	!macro PreserveDirectoryPost _n
		!ifdef PRESERVEDIRECTORY${_n}
			${GetParent} `$INSTDIR\${PRESERVEDIRECTORY${_n}}\` $R0
			CreateDirectory $R0
			Rename `$INSTDIR\~PRESERVEDIRECTORY${_n}\` `$INSTDIR\${PRESERVEDIRECTORY${_n}}\`
		!endif
	!macroend
	${!insertmacro1-10} PreserveDirectoryPost

	;=== Rename the preserved files back to their proper names
	!macro PreserveFilePost _n
		!ifdef PRESERVEFILE${_n}
			${GetFileName} `$INSTDIR\${PRESERVEFILE${_n}}` $1
			${GetParent} `$INSTDIR\${PRESERVEFILE${_n}}` $2
			CreateDirectory $2
			${MoveFiles} DOS $1 `$INSTDIR\~PRESERVEFILE${_n}` $2
			RMDir `$INSTDIR\~PRESERVEFILE${_n}`
		!endif
	!macroend
	${!insertmacro1-10} PreserveFilePost

	${If} $bolAppUpgrade == true
		SetDetailsPrint both
		DetailPrint $InstallingStatusString
		SetDetailsPrint ListOnly
	${EndIf}

	!ifndef PLUGININSTALLER
		File /x thumbs.db "..\..\*.exe"
		File /x thumbs.db "..\..\*.html"
		SetOutPath $INSTDIR\App
		File /r /x thumbs.db "..\..\App\*.*"
	!else ifdef COMMONFILESPLUGIN
		SetOutPath $INSTDIR
		File /r /x thumbs.db /x PortableApps.comInstaller*.* "..\..\*.*"
	!else ; non-CommonFiles plugin installer
		SetOutPath $INSTDIR\Data
		File /nonfatal /r /x thumbs.db "..\..\Data\*.*"
		SetOutPath $INSTDIR\App
		File /nonfatal /r /x thumbs.db "..\..\App\*.*"
	!endif

	SetOutPath $INSTDIR\Other
	File /nonfatal /r /x thumbs.db /x PortableApps.comInstaller*.* "..\..\Other\*.*"

	SetOutPath $INSTDIR\Other\Source
	!ifdef USESCUSTOMCODE
		!if ${__FILE__} == "PortableApps.comInstallerPlugin.nsi"
			File "..\..\Other\Source\PortableApps.comInstallerPluginCustom.nsh"
		!else
			File "..\..\Other\Source\PortableApps.comInstallerCustom.nsh"
		!endif
	!endif
	!ifndef PLUGININSTALLER
		CreateDirectory "$INSTDIR\Data"
	!endif

	!ifdef INCLUDEINSTALLERSOURCE
		File /r /x PortableApps.comInstallerCustom.nsh /x PortableApps.comInstallerPluginCustom.nsh "..\..\Other\Source\PortableApps.comInstaller*.*"
	!endif

	;=== Extract Download Files
	!ifdef DownloadURL
		!ifdef DownloadTo
			;Just copy the file
			CopyFiles /SILENT "$DOWNLOADEDFILE" "$INSTDIR\${DownloadTo}"
		!else
		;Process the file
			!ifdef Extract1To
				;Standard extract

				!macro ExtractTo _n
					!ifdef Extract${_n}To
						CreateDirectory "$INSTDIR\${Extract${_n}To}"
						nsisunz::UnzipToLog /file "${Extract${_n}File}" "$DOWNLOADEDFILE" "$INSTDIR\${Extract${_n}To}"
						Pop $R0
						${If} $R0 <> "OK"
							DetailPrint "ERROR: $R0 (${DownloadFilename} - ${Extract${_n}File})"
							Abort
						${EndIf}
					!endif
				!macroend
				${!insertmacro1-10} ExtractTo
			!endif
			!ifdef AdvancedExtract1To
				;Advanced extract with 7zip
				CreateDirectory "$INSTDIR\7zTemp"
				SetOutPath "$INSTDIR\7zTemp"
				File "${NSISDIR}\..\7zip\7z.exe"
				File "${NSISDIR}\..\7zip\7z.dll"
				SetOutPath $INSTDIR

				; The original code didn't have a !ifdef for 1, but we
				; know it will be defined, and it doesn't matter if we
				; check if it is because it will be.
				!macro AdvancedExtractFilter _n
					!ifdef AdvancedExtract${_n}To
						CreateDirectory "$INSTDIR\${AdvancedExtract${_n}To}"
						${If} "${AdvancedExtract${_n}Filter}" == "**"
							ExecDOS::exec `"$INSTDIR\7zTemp\7z.exe" x -r "$DOWNLOADEDFILE" -o"$INSTDIR\${AdvancedExtract${_n}To}" * -aoa -y` "" ""
						${Else}
							ExecDOS::exec `"$INSTDIR\7zTemp\7z.exe" x "$DOWNLOADEDFILE" -o"$INSTDIR\${AdvancedExtract${_n}To}" "${AdvancedExtract${_n}Filter}" -aoa -y` "" ""
						${EndIf}
						Pop $R0
						${If} $R0 <> 0
							DetailPrint "ERROR: (${DownloadFilename} > ${AdvancedExtract${_n}To})"
							Abort
						${EndIf}
					!endif
				!macroend
				${!insertmacro1-10} AdvancedExtractFilter

				Delete "$INSTDIR\7zTemp\7z.dll"
				Delete "$INSTDIR\7zTemp\7z.exe"
				RMDir "$INSTDIR\7zTemp"
			!endif
			!ifdef DoubleExtractFilename
				;Double extract using 7zip
				CreateDirectory "$INSTDIR\7zTemp"
				SetOutPath "$INSTDIR\7zTemp"
				File "${NSISDIR}\..\7zip\7z.exe"
				File "${NSISDIR}\..\7zip\7z.dll"
				SetOutPath $INSTDIR

				CreateDirectory "$PLUGINSDIR\Downloaded2"
				ExecDOS::exec `"$INSTDIR\7zTemp\7z.exe" x "$DOWNLOADEDFILE" -o"$PLUGINSDIR\Downloaded2" "${DoubleExtractFilename}" -aoa -y` "" ""
				Pop $R0
				${If} $R0 <> 0
					DetailPrint "ERROR: (${DownloadFilename} > ${DoubleExtractFilename})"
					Abort
				${EndIf}

				; The original code didn't have a !ifdef for 1, but we
				; know it will be defined, and it doesn't matter if we
				; check if it is because it will be.
				!macro DoubleExtractTo _n
					!ifdef DoubleExtract${_n}To
						CreateDirectory "$INSTDIR\${DoubleExtract${_n}To}"
						${If} "${DoubleExtract${_n}Filter}" == "**"
							ExecDOS::exec `"$INSTDIR\7zTemp\7z.exe" x -r "$PLUGINSDIR\Downloaded2\${DoubleExtractFilename}" -o"$INSTDIR\${DoubleExtract${_n}To}" * -aoa -y` "" ""
						${Else}
							ExecDOS::exec `"$INSTDIR\7zTemp\7z.exe" x "$PLUGINSDIR\Downloaded2\${DoubleExtractFilename}" -o"$INSTDIR\${DoubleExtract${_n}To}" "${DoubleExtract${_n}Filter}" -aoa -y` "" ""
						${EndIf}
						Pop $R0
						${If} $R0 <> 0
							DetailPrint "ERROR: (${DoubleExtractFilename} > ${DoubleExtract${_n}To})"
							Abort
						${EndIf}
					!endif
				!macroend
				${!insertmacro1-10} DoubleExtractTo

				Delete "$INSTDIR\7zTemp\7z.exe"
				Delete "$INSTDIR\7zTemp\7z.dll"
				RMDir "$INSTDIR\7zTemp"
			!endif
		!endif
	!endif

	;=== Copy Local Files
	!ifdef COPYLOCALFILES
		${If} ${FileExists} "$CopyLocalFilesFrom\*.*"
			CreateDirectory "$INSTDIR\${CopyToDirectory}"
			CopyFiles /SILENT "$CopyLocalFilesFrom\*.*" "$INSTDIR\${CopyToDirectory}"
		${Else}
			StrCpy $MISSINGFILEORPATH $CopyLocalFilesFrom
			${If} $(copylocalfilesnotfound) != ""
				MessageBox MB_OK|MB_ICONINFORMATION $(copylocalfilesnotfound)
			${Else}
				MessageBox MB_OK|MB_ICONINFORMATION `This installer copies a local version of the application and makes it portable.  Unfortunately, a local copy of the application was not found.  You may reinstall or copy the files yourself to complete the installation at a later time.  (ERROR: $MISSINGFILEORPATH could not be found.)`
			${EndIf}
		${EndIf}
	!endif

	;=== BEGIN: POST-INSTALL CODE ===
	;This will be executed after the app is installed.  Useful for updating configuration files.
	!ifmacrodef CustomCodePostInstall
		!insertmacro CustomCodePostInstall
	!endif
	;=== END: POST-INSTALL CODE ===

	!ifndef PLUGININSTALLER
		;=== Refresh PortableApps.com Menu (not final version)
		${GetParent} $INSTDIR $0
		;=== Check that it exists at the right location
		SetDetailsPrint both
		DetailPrint '$(checkforplatform)'
		${If} ${FileExists} `$0\PortableApps.com\PortableAppsPlatform.exe`
			;=== Check that it's the real deal so we aren't hanging with no response
			MoreInfo::GetProductName `$0\PortableApps.com\PortableAppsPlatform.exe`
			Pop $1
			${If} $1 == "PortableApps.com Platform"
				MoreInfo::GetCompanyName `$0\PortableApps.com\PortableAppsPlatform.exe`
				Pop $1
				${If} $1 == "PortableApps.com"

					;=== Check that it's running
					FindProcDLL::FindProc "PortableAppsPlatform.exe"
					${If} $R0 == "1"

						;=== Send message for the Menu to refresh
						CreateDirectory "$0\PortableApps.com\Data"
						WriteINIStr "$0\PortableApps.com\Data\NewApp.ini" "NewApp" "AppID" "${APPID}"

						DetailPrint '$(refreshmenu)'
						${IfNot} ${FileExists} `$0\PortableApps.com\App\PortableAppsPlatform.exe`
							StrCpy $2 'PortableApps.comPlatformWindowMessageToRefresh$0\PortableApps.com\PortableAppsPlatform.exe'
							System::Call "user32::RegisterWindowMessage(t r2) i .r3"
							SendMessage 65535 $3 0 0 /TIMEOUT=1
						${Else} ; old message
							StrCpy $2 'PortableApps.comPlatformWindowMessageToRefresh$0\PortableApps.com\App\PortableAppsPlatform.exe'
							System::Call "user32::RegisterWindowMessage(t r2) i .r3"
							SendMessage 65535 $3 0 0 /TIMEOUT=1
						${EndIf}
					${EndIf}
				${EndIf}
			${EndIf}
		${EndIf}
	!endif
		DetailPrint $InstallingStatusString
		SetDetailsPrint listonly
		Delete "$INSTDIR\7zTemp\7z.exe"
		Delete "$INSTDIR\7zTemp\7z.dll"
		RMDir "$INSTDIR\7zTemp"

!ifdef LICENSEAGREEMENT
	CreateDirectory "$INSTDIR\Data\PortableApps.comInstaller"
	WriteINIStr "$INSTDIR\Data\PortableApps.comInstaller\license.ini" "PortableApps.comInstaller" "EULAVersion" $INTERNALEULAVERSION
	ClearErrors
!endif

!ifdef DownloadURL
	Delete "$INTERNET_CACHE\${DownloadFileName}"
!endif
	${If} $bolLogFile == true
		${DumpLogToFile} "$EXEDIR\$EXEFILE.log"
	${EndIf}
	SetOutPath $INSTDIR
SectionEnd

!ifdef MAINSECTIONTITLE
	Section /o "${OPTIONALSECTIONTITLE}"
		SetOutPath $INSTDIR
		File /r "..\..\Optional1\*.*"
		StrCpy $OPTIONAL1DONE "true"
	SectionEnd

	Section "-UpdateAppInfo" SecUpdateAppInfo
	!ifndef PLUGININSTALLER
		${If} $OPTIONAL1DONE != "true"
		${AndIf} "${OPTIONALSECTIONNOTSELECTEDINSTALLTYPE}" != ""
			WriteINIStr "$INSTDIR\App\AppInfo\appinfo.ini" "Details" "InstallType" "${OPTIONALSECTIONNOTSELECTEDINSTALLTYPE}"
		${ElseIf} "${OPTIONALSECTIONSELECTEDINSTALLTYPE}" != ""
			WriteINIStr "$INSTDIR\App\AppInfo\appinfo.ini" "Details" "InstallType" "${OPTIONALSECTIONSELECTEDINSTALLTYPE}"
		${EndIf}
	!endif
	SectionEnd

	!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
		!insertmacro MUI_DESCRIPTION_TEXT ${MAINSECTIONIDX} "${MAINSECTIONDESCRIPTION}"
		!insertmacro MUI_DESCRIPTION_TEXT ${OPTIONALSECTIONIDX} "${OPTIONALSECTIONDESCRIPTION}"
	!insertmacro MUI_FUNCTION_DESCRIPTION_END
!endif

Function .onInstFailed
	!ifdef COPYLOCALFILES
		${registry::Unload}
	!endif
	RMDir $INSTDIR ;remove directory if empty
FunctionEnd

!ifdef COPYLOCALFILES
	Function .onInstSuccess
		${registry::Unload}
	FunctionEnd
	Function CustomAbortFunction
		${registry::Unload}
	FunctionEnd
!endif