using System;
using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// Common attributes of all the Items (MailItem, AppointmentItem)
    /// See: http://msdn.microsoft.com/en-us/library/ff861252.aspx
    /// </summary>
    public interface IItem : ICommon {
        IAttachments Attachments {
            get;
        }
        string Body {
            get;
            set;
        }
        OlObjectClass Class {
            get;
        }
        DateTime CreationTime {
            get;
        }
        string EntryID {
            get;
        }
        DateTime LastModificationTime {
            get;
        }
        string MessageClass {
            get;
            set;
        }
        bool NoAging {
            get;
            set;
        }
        int OutlookInternalVersion {
            get;
        }
        string OutlookVersion {
            get;
        }
        bool Saved {
            get;
        }
        OlSensitivity Sensitivity {
            get;
            set;
        }
        int Size {
            get;
        }
        string Subject {
            get;
            set;
        }
        bool UnRead {
            get;
            set;
        }
        object Copy();
        void Display(bool Modal);
        void Save();
        IPropertyAccessor PropertyAccessor {
            get;
        }
        IInspector GetInspector();
    }
}