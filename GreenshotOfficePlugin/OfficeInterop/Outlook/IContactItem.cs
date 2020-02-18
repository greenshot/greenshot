using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook.contactitem.aspx
    /// </summary>
    public interface IContactItem : IItem, ICommon {
        bool HasPicture {
            get;
        }
        void SaveBusinessCardImage(string path);
        void AddPicture(string path);
        void RemovePicture();
        string FirstName {
            get;
            set;
        }
        string LastName {
            get;
            set;
        }
    }
}