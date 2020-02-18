using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/bb176693%28v=office.12%29.aspx
    /// </summary>
    public interface INameSpace : ICommon {
        IRecipient CurrentUser {
            get;
        }
        IFolder GetDefaultFolder(OlDefaultFolders defaultFolder);
    }
}