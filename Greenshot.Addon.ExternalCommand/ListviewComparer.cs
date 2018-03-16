using System;
using System.Collections;
using System.Windows.Forms;

namespace GreenshotExternalCommandPlugin
{
	/// <summary>
	/// An IComparer for a listview, to allow sorting
	/// </summary>
	public class ListviewComparer : IComparer
	{
		public int Compare(object x, object y)
		{
			if (!(x is ListViewItem))
			{
				return 0;
			}
			if (!(y is ListViewItem))
			{
				return 0;
			}

			var l1 = (ListViewItem) x;
			var l2 = (ListViewItem) y;
			return string.Compare(l1.Text, l2.Text, StringComparison.Ordinal);
		}
	}
}