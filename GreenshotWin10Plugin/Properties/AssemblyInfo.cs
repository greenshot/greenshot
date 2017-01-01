using Greenshot.Plugin;
using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("GreenshotWin10Plugin")]
[assembly: AssemblyDescription("A plug-in for Windows 10 only functionality")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Greenshot")]
[assembly: AssemblyProduct("Windown 10 Plug-in")]
[assembly: AssemblyCopyright("Copyright © Greenshot 2007-2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// The PluginAttribute describes the "entryType" and if the plugin is configurable
[assembly: Plugin("GreenshotWin10Plugin.Win10Plugin", false)]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("9801f62c-540f-4bfe-9211-6405dede563b")]

// The assembly version, replaced by build scripts
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyInformationalVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]
