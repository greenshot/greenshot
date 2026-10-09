; Greenshot Light Installer: the basics only, no plugins, no AI tools and no browser extension. It installs the Light build of
; Greenshot.exe (built with /p:GreenshotEdition=Light, which compiles that code out, see Greenshot.csproj).
#define IsLightEdition 1
#define ReleaseDir "..\Greenshot\bin\Release-Light\net480"
#define AppDisplayName "Greenshot Light"
#define OutputSuffix "-Light"

#include "includes\defines.iss"
#include "includes\setup-header.iss"
#include "includes\types.iss"
#include "includes\core-files.iss"
#include "includes\tasks-icons-reg.iss"
#include "includes\cleanup.iss"
; Browser extension and greenshot: URL protocol are not released yet: uncomment the next line to build an installer with them
;#include "includes\browser-extension.iss"
#include "includes\languages.iss"
#include "includes\code.iss"
