using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// Is a joined interface of the Explorer an Inspector
    /// </summary>
    public interface ICommonExplorer : ICommon {
        void Activate();
        string Caption {
            get;
        }
        int Height {
            get;
            set;
        }
        int Left {
            get;
            set;
        }
        int Top {
            get;
            set;
        }
        int Width {
            get;
            set;
        }
        OlWindowState WindowState {
            get;
            set;
        }
    }
}