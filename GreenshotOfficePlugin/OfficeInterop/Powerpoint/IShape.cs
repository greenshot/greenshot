using Greenshot.Interop;
using GreenshotOfficePlugin.OfficeInterop.Outlook;

namespace GreenshotOfficePlugin.OfficeInterop.Powerpoint
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.shape_members.aspx
    /// </summary>
    public interface IShape : ICommon {
        float Left { get; set; }
        float Top { get; set; }
        float Width { get; set; }
        float Height { get; set; }
        ITextFrame TextFrame { get; }
        void ScaleWidth(float Factor, MsoTriState RelativeToOriginalSize, MsoScaleFrom fScale);
        void ScaleHeight(float Factor, MsoTriState RelativeToOriginalSize, MsoScaleFrom fScale);
        string AlternativeText { get; set; }
        MsoTriState LockAspectRatio { get; set; }
    }
}