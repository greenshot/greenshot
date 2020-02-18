using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Word
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.pane_members.aspx
    /// </summary>
    public interface IPane : ICommon {
        IWordView View { get; }
    }
}