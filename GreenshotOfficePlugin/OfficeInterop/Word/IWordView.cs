using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Word
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.view_members.aspx
    /// </summary>
    public interface IWordView : ICommon {
        IZoom Zoom { get; }
    }
}