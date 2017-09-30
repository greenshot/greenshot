using System.Windows.Forms;

namespace Greenshot.Forms
{
	public partial class TitleDialog : Form {
		public TitleDialog() {
			InitializeComponent();
		}

		public string Title {
			get {
				return textboxTitle.Text;
			}
			set {
				textboxTitle.Text = value;
			}
		}

		private void textboxTitle_KeyPress(object sender, KeyPressEventArgs e) {
			if (e.KeyChar == (char)Keys.Return) {
				DialogResult = DialogResult.OK;
				Close();
			}
		}
	}
}
