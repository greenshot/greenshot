using Greenshot.Interop;
using GreenshotOfficePlugin.OfficeInterop.Outlook;

namespace GreenshotOfficePlugin.OfficeInterop.Powerpoint
{
    public interface ITextFrame : ICommon {
        ITextRange TextRange { get; }
        MsoTriState HasText { get; }
    }
}