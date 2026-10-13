// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: https://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Runtime.InteropServices;

namespace Greenshot.Plugin.Office.OfficeInterop
{
    // The members of Outlook we use, see OfficeApplication for how this works

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOutlookApplication
    {
        IOutlookInspectors Inspectors { get; }
        IOutlookExplorer ActiveExplorer();
        object CreateItem(OlItemType itemType);
        IOutlookNamespace GetNamespace(string type);
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOutlookNamespace
    {
        IOutlookRecipient CurrentUser { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOutlookRecipient
    {
        string Name { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOutlookExplorer
    {
        string Caption { get; }
        object ActiveInlineResponse { get; }
        object ActiveInlineResponseWordEditor { get; }
        void Activate();
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOutlookInspectors
    {
        int Count { get; }
        IOutlookInspector Item(object index);
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOutlookInspector
    {
        string Caption { get; }
        object CurrentItem { get; }
        int EditorType { get; }
        object WordEditor { get; }
        bool IsWordMail();
        void Activate();
    }

    /// <summary>
    /// What every Outlook item has: the class tells what it is
    /// </summary>
    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOutlookItem
    {
        int Class { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOutlookMailItem
    {
        bool Sent { get; }
        string Subject { get; set; }
        string To { get; set; }
        string CC { get; set; }
        string BCC { get; set; }
        int BodyFormat { get; set; }
        string HTMLBody { get; set; }
        IOutlookAttachments Attachments { get; }
        IOutlookInspector GetInspector { get; }
        void Display(object modal);
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOutlookAppointmentItem
    {
        string Organizer { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOutlookAttachments
    {
        IOutlookAttachment Add(object source, object type, object position, object displayName);
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOutlookAttachment
    {
        IOutlookPropertyAccessor PropertyAccessor { get; }
    }

    [ComImport, Guid(OfficeApplication.IDispatchIid), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IOutlookPropertyAccessor
    {
        void SetProperty(string schemaName, object value);
    }

    /// <summary>
    /// OlObjectClass
    /// </summary>
    public enum OlObjectClass
    {
        olAppointment = 26,
        olMail = 43
    }

    /// <summary>
    /// OlItemType
    /// </summary>
    public enum OlItemType
    {
        olMailItem = 0
    }

    /// <summary>
    /// OlBodyFormat
    /// </summary>
    public enum OlBodyFormat
    {
        olFormatPlain = 1,
        olFormatHTML = 2
    }

    /// <summary>
    /// OlEditorType
    /// </summary>
    public enum OlEditorType
    {
        olEditorWord = 4
    }

    /// <summary>
    /// OlAttachmentType
    /// </summary>
    public enum OlAttachmentType
    {
        olByValue = 1
    }
}
