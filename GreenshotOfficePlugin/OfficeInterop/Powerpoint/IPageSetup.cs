using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Powerpoint
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.pagesetup_members.aspx
    /// </summary>
    public interface IPageSetup : ICommon, ICollection {
        float SlideWidth { get; set; }
        float SlideHeight { get; set; }
    }
}