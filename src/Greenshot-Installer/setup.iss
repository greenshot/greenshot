; Greenshot Full Installer
#define AppDisplayName "Greenshot"
#define OutputSuffix ""

#include "includes\defines.iss"
#include "includes\setup-header.iss"
#include "includes\types.iss"
#include "includes\core-files.iss"
#include "includes\tasks-icons-reg.iss"
#include "includes\cleanup.iss"
#include "includes\url-protocol.iss"
; The browser extension is not released yet: uncomment the next line to build an installer with it
;#include "includes\browser-extension.iss"
#include "includes\plugins\all-plugins.iss"
#include "includes\languages.iss"
#include "includes\code.iss"
