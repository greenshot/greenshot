using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Word
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.hyperlink_members%28v=office.14%29.aspx
    /// </summary>
    public interface IHyperlink : ICommon {
        string Address {
            get;
            set;
        }
    }
}