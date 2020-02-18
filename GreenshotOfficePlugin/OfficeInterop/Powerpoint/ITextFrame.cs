using GreenshotOfficePlugin.OfficeInterop.Outlook;
using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Powerpoint
{
    public interface ITextFrame : ICommon {
        ITextRange TextRange { get; }
        MsoTriState HasText { get; }
    }
}