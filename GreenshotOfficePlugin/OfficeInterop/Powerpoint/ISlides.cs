using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Powerpoint
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.slides_members.aspx
    /// </summary>
    public interface ISlides : ICommon {
        int Count { get; }
        ISlide Add(int Index, int layout);
    }
}