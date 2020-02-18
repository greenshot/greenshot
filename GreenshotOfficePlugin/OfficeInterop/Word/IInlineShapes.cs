using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Word
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/ms263866%28v=office.14%29.aspx
    /// </summary>
    public interface IInlineShapes : ICommon {
        IInlineShape AddPicture(string FileName, object LinkToFile, object SaveWithDocument, object Range);
    }
}