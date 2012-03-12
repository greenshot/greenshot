using System;
using System.Collections.Generic;
using System.Text;
using Greenshot.Interop;

namespace Greenshot.Interop.Remedy {
	// "Remedy.User.1" is the active remedy, if any
	[ComProgId("Remedy.User.1")]
	public interface IRemedyUserApplication : Common {
		int Login(string a, string b, int c);
		void Logout(int a);
		ICOMFormWnd2 OpenForm(int a, string b, string c, OpenMode d, int e);
		ICOMFormWnd2 GetActiveForm();
		ICOMFormWnd2 LoadForm(int a, string b, string c, string d, OpenMode e, int f);
		object GetServerList(int a);
		object GetFormList(int a, string b);
		int HasDefaultSession();
		void OpenGuide(int a, string b, string c);
		void RunMacro(int a, string b, int c, ref object d);
		object QueryForm(int a, string b, string c, string d, string e, OpenMode f, int g);
		IRemedyUser GetUser(ref string a);
		int Login2(int a);
		void OpenAlertList(string a, string b);
		int Login3(string a, string b, string c, int d);
	}

	public interface IRemedyUser : Common {
	}

	public interface ICOMFormWnd2 : Common {
		string Submit();
		void Modify();
		void Close();
		void MakeVisible();
		ICOMField4 GetField(string a);
		void GiveFieldFocus(string a);
		ICOMField4 GetFieldById(int a);
		void GiveFieldFocusById(int a);
		object Query(string a);
		string GetServerName();
		string GetFormName();
		int HasFieldFocus(string a);
		int HasFieldFocusById(int a);
		int GetVUIId();
		void GetFieldList(ref object a, ref object b);
	}

	public interface ICOMField4 : Common {
		void MakeVisible(int a);
		void MakeReadWrite(int a);
		void Disable();
		int IsVisible();
		int IsHidden();
		int IsReadOnly();
		int IsReadWrite();
		int IsDisabled();
		object GetARDBProp(uint a);
		object GetCurrencyPart(NCOMField3CurrencyPartEnum a);
		void SetCurrencyValue(string a);
		object GetDiaryHistoryList(NCOMField3DiaryHistoryFlagsEnum a);
		string GetFieldLabel();
		string GetFieldFont();
		string GetFieldColor();
		string Value {
			get;
			set;
		}
	}

	public enum NCOMField3CurrencyPartEnum {
	}
	public enum NCOMField3DiaryHistoryFlagsEnum {
	}

	public enum OpenMode {
		ARQuery,
		ARModify
	}
}
