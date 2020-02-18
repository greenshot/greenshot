using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Word
{
    /// <summary>
    /// See: http://msdn.microsoft.com/de-de/library/microsoft.office.interop.word.selection_members(v=office.11).aspx 
    /// </summary>
    public interface ISelection : ICommon {
        IInlineShapes InlineShapes { get; }
        void InsertAfter(string text);
        int MoveDown(object Unit, object Count, object Extend);
    }
}