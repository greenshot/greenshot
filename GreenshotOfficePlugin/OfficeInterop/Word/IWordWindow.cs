using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Word
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.window_members.aspx
    /// </summary>
    public interface IWordWindow : ICommon {
        IPane ActivePane { get; }
        void Activate();
        string Caption {
            get;
        }

        /// <summary>
        /// Returns an Integer (int in C#) that indicates the window handle of the specified window
        /// </summary>
        int Hwnd { get; }
    }
}