using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/bb176362%28v=office.12%29.aspx
    /// </summary>
    public interface IFolder : ICommon {
        IItems Items {
            get;
        }
    }
}