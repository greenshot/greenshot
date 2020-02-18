using System.Collections;
using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook._application.inspectors.aspx
    /// </summary>
    public interface IInspectors : ICommon, ICollection, IEnumerable {
        // Use index + 1!!
        IInspector this[object Index] {
            get;
        }
    }
}