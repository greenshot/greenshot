#define ExeName "Greenshot"
; Basic build version determined by nerdbank gitversioning, e.g. 1.2.345
#define Version GetEnv('BuildVersionSimple')
; Build version with optional suffix depending on branch, e.g. 1.2.345-g1tc033174ef
#define VersionEnhanced GetEnv('BuildVersionEnhanced')
#define SolutionDir ".."
#define GreenshotProjectDir "..\Greenshot"
#define LanguagesDir "..\Greenshot\Languages"
#define BinDir "bin\Release\net480"
; Greenshot Light (setup-light.iss) installs its own build of Greenshot.exe, see Greenshot.csproj
#ifndef ReleaseDir
  #define ReleaseDir "..\Greenshot\bin\Release\net480"
#endif
#define PluginDir "..\Greenshot\bin\Release\net480\Plugins"
#define CertumThumbprint GetEnv('CertumThumbprint')
#define DefaultInstallFlags "overwritereadonly ignoreversion"

#ifndef AppDisplayName
  #define AppDisplayName "Greenshot"
#endif

#ifndef OutputSuffix
  #define OutputSuffix ""
#endif
