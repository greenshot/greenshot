using System.Collections;
using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// Since Outlook 2010, but since 2013 one can edit inside an explorer
    /// See: http://msdn.microsoft.com/en-us/library/office/ff867227(v=office.15).aspx
    /// </summary>
    public interface IExplorers : ICommon, ICollection, IEnumerable {
        // Use index + 1!!
        IExplorer this[object Index] {
            get;
        }
    }
}