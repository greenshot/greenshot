using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Excel
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.excel.workbook.aspx
    /// </summary>
    public interface IWorkbook : ICommon {
        IWorksheet ActiveSheet {
            get;
        }
        string Name {
            get;
        }
        void Activate();
        IWorksheets Worksheets {
            get;
        }
    }
}