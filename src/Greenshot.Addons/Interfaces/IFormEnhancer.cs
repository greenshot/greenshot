using System.Windows.Forms;

namespace Greenshot.Addons.Interfaces
{
    /// <summary>
    /// This interface makes it possible to enhance forms by e.g. injecting components
    /// </summary>
    public interface IFormEnhancer
    {
        /// <summary>
        /// This is called during the InitializeComponent of the form
        /// </summary>
        /// <param name="target">Form</param>
        void InitializeComponent(Form target);
    }
}
