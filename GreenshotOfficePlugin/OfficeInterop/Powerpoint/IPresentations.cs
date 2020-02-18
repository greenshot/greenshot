using GreenshotOfficePlugin.OfficeInterop.Outlook;
using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Powerpoint
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.presentations_members.aspx
    /// </summary>
    public interface IPresentations : ICommon, ICollection {
        IPresentation Add(MsoTriState WithWindow);
        IPresentation item(int index);
    }
}