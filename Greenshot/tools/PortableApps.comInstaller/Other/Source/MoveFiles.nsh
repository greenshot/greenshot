; Copyright (c) 2008, Harold E Austin Jr
; All rights reserved.
;
; Redistribution and use in source and binary forms, with or without
; modification, are permitted provided that the following conditions are met:
;     * Redistributions of source code must retain the above copyright
;       notice, this list of conditions and the following disclaimer.
;     * Redistributions in binary form must reproduce the above copyright
;       notice, this list of conditions and the following disclaimer in the
;       documentation and/or other materials provided with the distribution.
;     * Neither the name of the organization nor the
;       names of its contributors may be used to endorse or promote products
;       derived from this software without specific prior written permission.
;
; THIS SOFTWARE IS PROVIDED BY Harold E Austin Jr ``AS IS'' AND ANY
; EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
; WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
; DISCLAIMED. IN NO EVENT SHALL Harold E Austin Jr BE LIABLE FOR ANY
; DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
; (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
; ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
; (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
; SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

/*
	MoveFiles.nsh -- version 1.0  (May 5, 2008)
		move files matching "filespec" from "source-directory" to "destination-directory"

	usage: 
		!include MoveFiles.nsh
		
		${MoveFiles} mode "filespec" "source-directory" "destination-directory"

	where:
		mode can be DOS, DIR, FORCE or DIR+FORCE (anything else = DOS):
			DOS		means act like the DOS MOVE command (move only files)
			DIR		means move files AND directories
			FORCE 	means overwrite destination files (like MOVE/Y)

	example:
		CreateDirectory "C:\NEW\DIR"
		DetailPrint "Moving files and directories..."
		${MoveFiles} DIR+FORCE "*" "C:\OLD\DIR" "C:\NEW\DIR"
		DetailPrint `"Processing"...`
		Sleep 2000
		DetailPrint "Moving only the files back..."
		${MoveFiles} DOS "*" "C:\NEW\DIR" "C:\OLD\DIR"
		DetailPrint "Moving the directories back..."
		${MoveFiles} DIR "*" "C:\NEW\DIR" "C:\OLD\DIR"
*/
!ifndef MoveFiles
!define MoveFiles "!insertmacro MoveFiles"
!macro MoveFiles mode filespec sourcedir destdir
	push `${destdir}`
	push `${sourcedir}`
	push `${filespec}`
	push `${mode}`
	call MoveFiles
!macroend

Function MoveFiles ; mode filespec sourcedir destdir
	Exch $0					; mode, directory mode
	Exch 
	Exch $1					; filespec, force mode
	Exch 2
	Exch $2					; source directory
	Exch 3
	Exch $3					; destination directory
	Push $4					; FindFirst/FindNext search handle
	Push $5					; current filename matching filespec in sourcedir
    FindFirst $4 $5 "$2\$1"
	StrCpy $1 ""			; FORCE mode disabled by default
	StrCmp $0 FORCE 0 +2
		StrCpy $1 FORCE
	StrCmp $0 DIR+FORCE 0 +3
		StrCpy $0 DIR
		StrCpy $1 FORCE
	loop:                    
		StrCmp $5 "" done	; $5 == "", if no more matching files
		StrCmp $5 . next
		StrCmp $5 .. next
		StrCmp $0 DIR +2
			; DIR mode disabled: ignore directories that match ${filespec}
			IfFileExists "$2\$5\*.*" next
		StrCmp $1 FORCE 0 +4
			; FORCE mode: make sure destination doesn't exist
			Delete "$3\$5"
			StrCmp $0 DIR 0 +2
				RMDir /R "$3\$5"
		Rename "$2\$5" "$3\$5"
	next:
		FindNext $4 $5
		Goto loop
	done:
		FindClose $4        ; finished with this search; close handle
		Pop $5
		Pop $4
		Pop $3
		Pop $0
		Pop $1
		Pop $2
FunctionEnd
!endif
