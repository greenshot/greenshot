using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Word
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.hyperlinks%28v=office.14%29.aspx
    /// </summary>
    public interface IHyperlinks : ICommon, ICollection {
        IHyperlink Add(object Anchor, object Address, object SubAddress, object ScreenTip, object TextToDisplay, object Target);
    }
}