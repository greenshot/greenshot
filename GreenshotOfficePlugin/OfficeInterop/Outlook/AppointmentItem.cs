using System;
using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/ff869026.aspx
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook.appointmentitem.aspx
    /// </summary>
    public interface AppointmentItem : IItem, ICommon {
        string Organizer {
            get;
            set;
        }
        string SendUsingAccount {
            get;
        }
        string Categories {
            get;
        }
        DateTime Start {
            get;
        }
        DateTime End {
            get;
        }
        OlReoccurenceState RecurrenceState {
            get;
        }
    }
}