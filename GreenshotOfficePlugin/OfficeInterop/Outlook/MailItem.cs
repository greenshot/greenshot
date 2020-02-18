using System;
using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/ff861252.aspx
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook.mailitem.aspx
    /// </summary>
    public interface MailItem : IItem, ICommon {
        bool Sent {
            get;
        }
        object MAPIOBJECT {
            get;
        }
        string HTMLBody {
            get;
            set;
        }
        DateTime ExpiryTime {
            get;
            set;
        }
        DateTime ReceivedTime {
            get;
        }
        string SenderName {
            get;
        }
        DateTime SentOn {
            get;
        }
        OlBodyFormat BodyFormat {
            get;
            set;
        }
        string To {
            get;
            set;
        }
        string CC {
            get;
            set;
        }
        string BCC {
            get;
            set;
        }
    }
}