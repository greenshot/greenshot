namespace GreenshotOfficePlugin.OfficeInterop.OneNote
{
    public enum PageInfo {
        piBasic = 0,				// Returns only basic page content, without selection markup and binary data objects. This is the standard value to pass.
        piBinaryData = 1,			// Returns page content with no selection markup, but with all binary data.
        piSelection = 2,			// Returns page content with selection markup, but no binary data.
        piBinaryDataSelection = 3,	// Returns page content with selection markup and all binary data.
        piAll = 3					// Returns all page content.
    }
}