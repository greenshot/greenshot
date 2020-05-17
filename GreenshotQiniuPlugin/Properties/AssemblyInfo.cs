﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Greenshot.Plugin;
using System.Reflection;
using System.Runtime.InteropServices;
// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Greenshot-Qiniu-Plugin")]
[assembly: AssemblyDescription("A plugin to upload images to qiniu cloud.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Greenshot")]
[assembly: AssemblyProduct("Qiniu Plugin")]
[assembly: AssemblyCopyright("Copyright (C) 2007-2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]


// The PluginAttribute describes the "entryType" and if the plugin is configurable
[assembly: PluginAttribute("GreenshotQiniuPlugin.QiniuPlugin", true)]
// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("635759b5-9673-490f-b373-baab65c07b77")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyInformationalVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]
