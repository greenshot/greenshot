using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Powerpoint
{
    public interface ITextRange : ICommon {
        string Text { get; set; }
    }
}