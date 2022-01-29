using System.Windows.Forms;

namespace Greenshot.Base.Controls
{
    public class GreenshotDoubleClickButton : Button
    {
        public GreenshotDoubleClickButton()
        {
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);
        }
    }
}
