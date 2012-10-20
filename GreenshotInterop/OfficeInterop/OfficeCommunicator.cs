using System;
using System.Collections.Generic;
using System.Text;
using Greenshot.Interop;
using Greenshot.Interop.Office;

namespace GreenshotInterop.OfficeInterop {
	// See: http://msdn.microsoft.com/en-us/library/bb758788%28v=office.12%29
	[ComProgId("Communicator.UIAutomation")]
	public interface IMessenger : Common {
		void AutoSignin();
		string MyServiceId {
			get;
		}
		IMessengerContact GetContact(string signinName, string serviceId);
		IMessengerWindow InstantMessage(string contact);
	}

	// See: http://msdn.microsoft.com/en-us/library/bb787250%28v=office.12%29
	public interface IMessengerContact : Common {
		string FriendlyName {
			get;
		}
		string ServiceName {
			get;
		}
		string ServiceId {
			get;
		}
		string SigninName {
			get;
		}
		MISTATUS Status {
			get;
		}
	}

	// See: http://msdn.microsoft.com/en-us/library/bb787207%28v=office.12%29
	public enum MISTATUS {
		MISTATUS_UNKNOWN = 0x0000,
		MISTATUS_OFFLINE = 0x0001,
		MISTATUS_ONLINE = 0x0002,
		MISTATUS_INVISIBLE = 0x0006,
		MISTATUS_BUSY = 0x000A,
		MISTATUS_BE_RIGHT_BACK = 0x000E,
		MISTATUS_IDLE = 0x0012,
		MISTATUS_AWAY = 0x0022,
		MISTATUS_ON_THE_PHONE = 0x0032,
		MISTATUS_OUT_TO_LUNCH = 0x0042,
		MISTATUS_IN_A_MEETING = 0x0052,
		MISTATUS_OUT_OF_OFFICE = 0x0062,
		MISTATUS_DO_NOT_DISTURB = 0x0072,
		MISTATUS_IN_A_CONFERENCE = 0x0082,
		MISTATUS_ALLOW_URGENT_INTERRUPTIONS = 0x0092,
		MISTATUS_MAY_BE_AVAILABLE = 0x00A2,
		MISTATUS_CUSTOM = 0x00B2,
		MISTATUS_LOCAL_FINDING_SERVER = 0x0100,
		MISTATUS_LOCAL_CONNECTING_TO_SERVER = 0x0200,
		MISTATUS_LOCAL_SYNCHRONIZING_WITH_SERVER = 0x0300,
		MISTATUS_LOCAL_DISCONNECTING_FROM_SERVER = 0x0400
	} ;

	// See: http://msdn.microsoft.com/en-us/library/bb758816%28v=office.12%29
	public interface IMessengerWindow : Common {
		bool IsClosed {
			get;
		}
		void Show();
	}
	
}
