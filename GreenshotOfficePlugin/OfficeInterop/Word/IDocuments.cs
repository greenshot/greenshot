using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Word
{
    /// <summary>
    /// See: http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.documents_members(v=office.11).aspx
    /// </summary>
    public interface IDocuments : ICommon, ICollection {
        IWordDocument Add(ref object Template, ref object NewTemplate, ref object DocumentType, ref object Visible);
        IWordDocument item(int index);
    }
}