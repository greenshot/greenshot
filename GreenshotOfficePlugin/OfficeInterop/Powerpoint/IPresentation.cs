using Greenshot.Interop;
using GreenshotOfficePlugin.OfficeInterop.Outlook;

namespace GreenshotOfficePlugin.OfficeInterop.Powerpoint
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.presentation_members.aspx
    /// </summary>
    public interface IPresentation : ICommon {
        string Name { get; }
        ISlides Slides { get; }
        IPowerpointApplication Application { get; }
        MsoTriState ReadOnly { get; }
        bool Final { get; set; }
        IPageSetup PageSetup { get; }
    }
}