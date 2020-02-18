using System;
using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.OneNote
{
    [ComProgId("OneNote.Application")]
    public interface IOneNoteApplication : ICommon {
        /// <summary>
        /// Make sure that the out variables are filled with a string, e.g. "", otherwise a type error occurs.
        /// For more info on the methods: http://msdn.microsoft.com/en-us/library/gg649853.aspx
        /// </summary>
        void GetHierarchy(string startNode, HierarchyScope scope, out string notebookXml, XMLSchema schema);
        void GetSpecialLocation(SpecialLocation specialLocation, out string specialLocationPath);
        void UpdatePageContent(string pageChangesXml, DateTime dateExpectedLastModified, XMLSchema schema, bool force);
        void GetPageContent(string pageId, out string pageXml, PageInfo pageInfoToExport, XMLSchema schema);
        void NavigateTo(string hierarchyObjectID, string objectId, bool newWindow);
        void CreateNewPage(string sectionID, out string pageID, NewPageStyle newPageStyle);
    }
}