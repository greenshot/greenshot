using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Word
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.word.document%28v=office.14%29.aspx 
    /// </summary>
    public interface IWordDocument : ICommon {
        void Activate();
        IWordApplication Application { get; }
        IWordWindow ActiveWindow { get; }
        bool ReadOnly { get; }
        IHyperlinks Hyperlinks { get; }

        // Only 2007 and later!
        bool Final { get; set; }
    }
}