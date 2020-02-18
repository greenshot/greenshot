using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Word
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.zoom_members.aspx
    /// </summary>
    public interface IZoom : ICommon {
        int Percentage { get; set; }
    }
}