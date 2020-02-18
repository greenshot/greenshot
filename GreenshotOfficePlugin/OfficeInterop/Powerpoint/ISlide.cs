using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Powerpoint
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.slide_members.aspx
    /// </summary>
    public interface ISlide : ICommon {
        IShapes Shapes { get; }
        void Select();
        int SlideNumber { get; }

    }
}