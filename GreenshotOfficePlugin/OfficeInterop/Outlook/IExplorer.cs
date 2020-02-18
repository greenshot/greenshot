using GreenshotOfficePlugin.OfficeInterop.Word;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// Since Outlook 2010, but since 2013 one can edit inside an explorer
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook.explorer_members(v=office.15).aspx
    /// 
    /// </summary>
    public interface IExplorer : ICommonExplorer {
        IItem ActiveInlineResponse {
            get;
        }
        IWordDocument ActiveInlineResponseWordEditor {
            get;
        }
    }
}