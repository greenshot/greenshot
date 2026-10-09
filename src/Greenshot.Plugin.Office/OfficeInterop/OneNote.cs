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
    // The members of OneNote we use, see OfficeApplication for how this works

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOneNoteApplication
    {
        void GetHierarchy(string startNodeId, HierarchyScope scope, out string hierarchyXml, XMLSchema schema);
        void GetSpecialLocation(SpecialLocation specialLocation, out string specialLocationPath);
        void CreateNewPage(string sectionId, out string pageId, NewPageStyle newPageStyle);
        void UpdatePageContent(string pageChangesXml, DateTime dateExpectedLastModified, XMLSchema schema, bool force);
        void NavigateTo(string hierarchyObjectId, string objectId, bool newWindow);
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
