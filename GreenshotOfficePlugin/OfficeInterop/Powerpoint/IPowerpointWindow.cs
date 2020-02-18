using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Powerpoint
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.documentwindow.view.aspx
    /// </summary>
    public interface IPowerpointWindow : ICommon {
        void Activate();
        IPowerpointView View { get; }
    }
}