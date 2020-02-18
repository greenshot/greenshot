using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook.attachment_members.aspx
    /// </summary>
    public interface IAttachment : ICommon {
        string DisplayName {
            get;
            set;
        }
        string FileName {
            get;
        }
        OlAttachmentType Type {
            get;
        }
        IPropertyAccessor PropertyAccessor {
            get;
        }
        object MAPIOBJECT {
            get;
        }
        void SaveAsFile(string path);
    }
}