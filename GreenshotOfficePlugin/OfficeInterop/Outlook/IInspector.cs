using GreenshotOfficePlugin.OfficeInterop.Word;

namespace GreenshotOfficePlugin.OfficeInterop.Outlook
{
    /// <summary>
    /// See: http://msdn.microsoft.com/en-us/library/microsoft.office.interop.outlook.inspector_members.aspx
    /// </summary>
    public interface IInspector : ICommonExplorer {
        IItem CurrentItem {
            get;
        }
        OlEditorType EditorType {
            get;
        }
        object ModifiedFormPages {
            get;
        }
        void Close(OlInspectorClose SaveMode);
        void Display(object Modal);
        void HideFormPage(string PageName);
        bool IsWordMail();
        void SetCurrentFormPage(string PageName);
        void ShowFormPage(string PageName);
        object HTMLEditor {
            get;
        }
        IWordDocument WordEditor {
            get;
        }
        void SetControlItemProperty(object Control, string PropertyName);
    }
}