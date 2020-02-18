using GreenshotOfficePlugin.OfficeInterop.Outlook;
using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Word
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.inlineshape_members%28v=office.14%29.aspx
    /// </summary>
    public interface IInlineShape : ICommon {
        IHyperlink Hyperlink { get; }
        MsoTriState LockAspectRatio {
            get;
            set;
        }
    }
}