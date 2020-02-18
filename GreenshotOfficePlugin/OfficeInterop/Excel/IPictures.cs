namespace GreenshotOfficePlugin.OfficeInterop.Excel
{
    public interface IPictures : ICollection {
        // Use index + 1!!
        //IPicture this[object Index] { get; }
        void Insert(string file);
    }
}