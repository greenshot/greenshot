using GreenshotPlugin.Interop;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// Wrapper for Outlook.Application, see: http://msdn.microsoft.com/en-us/library/aa210897%28v=office.11%29.aspx
    /// This is the initial COM-Object which is created/retrieved
    /// </summary>
    [ComProgId("Outlook.Application")]
    public interface IOutlookApplication : ICommon {
        string Name {
            get;
        }
        string Version {
            get;
        }
        IItem CreateItem(OlItemType ItemType);
        object CreateItemFromTemplate(string TemplatePath, object InFolder);
        object CreateObject(string ObjectName);
        IInspector ActiveInspector();
        IInspectors Inspectors {
            get;
        }
        INameSpace GetNameSpace(string type);
        IExplorer ActiveExplorer();
        IExplorers Explorers {
            get;
        }
    }
}