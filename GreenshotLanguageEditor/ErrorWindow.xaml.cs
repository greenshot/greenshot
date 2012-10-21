/*
 * Created by SharpDevelop.
 * User: jens
 * Date: 02.10.2012
 * Time: 22:52
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Greenshot.Helpers;

namespace GreenshotLanguageEditor
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class ErrorWindow : Window
	{
		public ErrorWindow(Exception e)
		{
			InitializeComponent();
			this.Title = "Unexpected Error in GreenshotLanguageEditor";

			this.textBox.Text = "Sorry, an unexpected error occurred :(\n\n"
				+ EnvironmentInfo.BuildReport(e);
			
		}
	}
}