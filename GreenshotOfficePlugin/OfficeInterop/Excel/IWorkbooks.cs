using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Excel
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.excel.workbooks.aspx
    /// </summary>
    public interface IWorkbooks : ICommon, ICollection {
        IWorkbook Add(object template);
        // Use index + 1!!
        IWorkbook this[object index] {
            get;
        }
    }
}