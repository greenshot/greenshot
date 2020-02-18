namespace GreenshotOfficePlugin.OfficeInterop.OneNote
{
    public enum HierarchyScope {
        hsSelf = 0, // Gets just the start node specified and no descendants.
        hsChildren = 1, //Gets the immediate child nodes of the start node, and no descendants in higher or lower subsection groups.
        hsNotebooks = 2, // Gets all notebooks below the start node, or root.
        hsSections = 3, //Gets all sections below the start node, including sections in section groups and subsection groups.
        hsPages = 4 //Gets all pages below the start node, including all pages in section groups and subsection groups.
    }
}