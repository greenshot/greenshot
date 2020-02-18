using Greenshot.Interop;
using GreenshotOfficePlugin.OfficeInterop.Powerpoint;

namespace GreenshotOfficePlugin.OfficeInterop.Excel
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.excel._worksheet_members.aspx
    /// </summary>
    public interface IWorksheet : ICommon {
        IPictures Pictures {
            get;
        }
        IShapes Shapes {
            get;
        }
        string Name {
            get;
        }
    }
}