using System;
using System.Windows;
using System.Windows.Threading;
using System.Data;
using System.Xml;
using System.Configuration;

namespace GreenshotLanguageEditor {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		public App() :base () {
			this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
		}
		void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
		    new ErrorWindow(e.Exception).ShowDialog();
            e.Handled = true;
        }
	}
}