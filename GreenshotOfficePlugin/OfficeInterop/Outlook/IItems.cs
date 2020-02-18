using System.Collections;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/bb208387%28v=office.12%29.aspx
    /// </summary>
    public interface IItems : ICollection, IEnumerable {
        IItem this[object index] {
            get;
        }
        IItem GetFirst();
        IItem GetNext();
        IItem GetLast();
        IItem GetPrevious();

        bool IncludeRecurrences {
            get;
            set;
        }

        IItems Restrict(string filter);
        void Sort(string property, object descending);

        // Actual definition is "object Add( object )", just making it convenient
        object Add(OlItemType type);
    }
}