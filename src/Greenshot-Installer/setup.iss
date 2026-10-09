; Greenshot Full Installer
#define AppDisplayName "Greenshot"
#define OutputSuffix ""

#include "includes\defines.iss"
#include "includes\setup-header.iss"
#include "includes\types.iss"
#include "includes\core-files.iss"
#include "includes\tasks-icons-reg.iss"
#include "includes\cleanup.iss"
; Browser extension and greenshot: URL protocol are not released yet: uncomment the next line to build an installer with them
;#include "includes\browser-extension.iss"
#include "includes\plugins\all-plugins.iss"
#include "includes\languages.iss"
#include "includes\code.iss"
