// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: https://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Runtime.InteropServices;

namespace Greenshot.Plugin.Office.OfficeInterop
{
    /// <summary>
    /// OneNote's IApplication, unlike the other Office applications not used by name over IDispatch:
    /// OneNote answers IDispatch calls with its type library, and a 64-bit Click-to-Run OneNote was seen with it registered
    /// for 32-bit only (no win64 key), which fails with TYPE_E_LIBNOTREGISTERED. Calls through the interface itself worked there.
    /// The slots must be in OneNote's order, the _VtblGap entries skip the methods we don't use.
    /// </summary>
    [ComImport, Guid("452AC71A-B655-4967-A208-A4CC39DD7949"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IOneNoteApplication
    {
        void GetHierarchy(string startNodeId, HierarchyScope scope, out string hierarchyXml, XMLSchema schema);
        void _VtblGap1_3();
        void CreateNewPage(string sectionId, out string pageId, NewPageStyle newPageStyle);
        void _VtblGap2_3();
        void UpdatePageContent(string pageChangesXml, DateTime dateExpectedLastModified, XMLSchema schema, bool force);
        void _VtblGap3_2();
        void NavigateTo(string hierarchyObjectId, string objectId, bool newWindow);
        void _VtblGap4_6();
        void GetSpecialLocation(SpecialLocation specialLocation, out string specialLocationPath);
    }

    /// <summary>
    /// HierarchyScope
    /// </summary>
    public enum HierarchyScope
    {
        hsPages = 4
    }

    /// <summary>
    /// SpecialLocation
    /// </summary>
    public enum SpecialLocation
    {
        slUnfiledNotesSection = 1
    }

    /// <summary>
    /// NewPageStyle
    /// </summary>
    public enum NewPageStyle
    {
        npsDefault = 0
    }

    /// <summary>
    /// XMLSchema
    /// </summary>
    public enum XMLSchema
    {
        xs2010 = 1
    }
}
