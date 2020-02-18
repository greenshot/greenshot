using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    public interface IRecipient : ICommon {
        string Name {
            get;
        }
    }
}