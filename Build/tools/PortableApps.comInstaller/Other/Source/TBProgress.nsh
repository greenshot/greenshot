!include "LogicLib.nsh"
 
!ifndef CLSCTX_INPROC_SERVER
    !define CLSCTX_INPROC_SERVER  1
!endif
 
!define CLSID_ITaskbarList    {56fdf344-fd6d-11d0-958a-006097c9a090}
 
!define IID_ITaskbarList3     {ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf}
!define ITaskbarList3->SetProgressState      $ITaskbarList3->10
!define ITaskbarList3->SetProgressValue      $ITaskbarList3->9
 
!define TBPF_NOPROGRESS       0x00000000 ; Normal state / no progress bar
!define TBPF_INDETERMINATE    0x00000001 ; Marquee style progress bar
!define TBPF_NORMAL           0x00000002 ; Standard progress bar
!define TBPF_ERROR            0x00000004 ; Red taskbar button to indicate an error occurred
!define TBPF_PAUSED           0x00000008 ; Yellow taskbar button to indicate user attention
                                         ; (input) is required to resume progress
 
Var ITaskbarList3
 
!macro TBProgress_Init
  !ifndef TBProgressInitialized
    !define TBProgressInitialized
    ${Unless} ${Silent}
      System::Call "ole32::CoCreateInstance( \
        g '${CLSID_ITaskbarList}', \
        i 0, \
        i ${CLSCTX_INPROC_SERVER}, \
        g '${IID_ITaskbarList3}', \
        *i .s)"
      Pop $ITaskbarList3
    ${Else}
      StrCpy $ITaskbarList3 0
    ${EndIf}
  !endif
!macroend
!define TBProgress_Init `!insertmacro TBProgress_Init`
 
!macro TBProgress_Progress Val Max
  ${TBProgress_Init}
  ${If} $ITaskbarList3 <> 0
      System::Call "${ITaskbarList3->SetProgressValue}(i$HWNDPARENT, l${Val}, l${Max})"
  ${EndIf}
!macroend
!define TBProgress_Progress `!insertmacro TBProgress_Progress`
 
!macro TBProgress Val
  ${TBProgress_Progress} ${Val} 100
!macroend
!define TBProgress `!insertmacro TBProgress`
 
!macro TBProgress_State State
  ${TBProgress_Init}
  ${If} $ITaskbarList3 <> 0
      System::Call "${ITaskbarList3->SetProgressState}(i$HWNDPARENT, i${TBPF_${State}})"
  ${EndIf}
!macroend
!define TBProgress_State `!insertmacro TBProgress_State`