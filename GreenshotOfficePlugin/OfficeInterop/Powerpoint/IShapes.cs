using System.Collections;
using Greenshot.Interop;
using GreenshotOfficePlugin.OfficeInterop.Outlook;

namespace GreenshotOfficePlugin.OfficeInterop.Powerpoint
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.powerpoint.shapes_members.aspx
    /// </summary>
    public interface IShapes : ICommon, IEnumerable {
        int Count { get; }
        IShape item(int index);
        IShape AddPicture(string FileName, MsoTriState LinkToFile, MsoTriState SaveWithDocument, float Left, float Top, float Width, float Height);
    }
}