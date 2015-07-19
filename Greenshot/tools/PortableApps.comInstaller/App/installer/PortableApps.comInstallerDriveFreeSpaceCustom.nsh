;NSIS' built-in DriveSpace function fails for unmapped UNC paths
;This function retrieves the free space on a local, mapped or UNC path in MB

!ifndef DriveFreeSpaceCustom

!define DriveFreeSpaceCustom "!insertmacro DriveFreeSpaceCustom"

!macro DriveFreeSpaceCustom DRIVE_OR_UNC FREE_SPACE
	push `${DRIVE_OR_UNC}`
	call DriveFreeSpaceCustom
	pop `${FREE_SPACE}`
!macroend

Function DriveFreeSpaceCustom
	Exch $0	;DRIVE_OR_UNC
	Push $1	;Free space variable
	
	System::Call 'kernel32::GetDiskFreeSpaceEx(t, *l, *l, *l) i(r0,.r1,.,.)'
	System::Int64Op $1 / 1024
	Pop $1
	System::Int64Op $1 / 1024
	Pop $1
	
	Exch
	Pop $0
	Exch $1
FunctionEnd

!endif