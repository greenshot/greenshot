﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
[assembly: AssemblyTitle("GreenshotWebDavPlugin")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Kai Schröer")]
[assembly: AssemblyProduct("WebDAV Plugin")]
[assembly: AssemblyCopyright("Copyright (C) 2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
// The PluginAttribute describes the "entryType" and if the plugin is configurable
[assembly: PluginAttribute("GreenshotWebDavPlugin.WebDavPlugin", true)]

// This sets the default COM visibility of types in the assembly to invisible.
// If you need to expose a type to COM, use [ComVisible(true)] on that type.
[assembly: ComVisible(false)]

// The assembly version, replaced by build scripts
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyInformationalVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]
