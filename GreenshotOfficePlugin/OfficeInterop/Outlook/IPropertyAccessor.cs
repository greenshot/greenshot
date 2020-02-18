using Greenshot.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook.propertyaccessor_members.aspx
    /// </summary>
    public interface IPropertyAccessor : ICommon {
        void SetProperty(string SchemaName, object Value);
        object GetProperty(string SchemaName);
    }
}