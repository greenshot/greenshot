namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    public interface IAttachments : ICollection {
        IAttachment Add(object source, object type, object position, object displayName);
        // Use index+1!!!!
        IAttachment this[object index] {
            get;
        }
    }
}