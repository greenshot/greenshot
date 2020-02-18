namespace GreenshotOfficePlugin.OfficeInterop.Excel
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.excel.iworksheets_members.aspx
    /// </summary>
    public interface IWorksheets : ICollection {
        // Use index + 1!!
        IWorksheet this[object index] {
            get;
        }
    }
}