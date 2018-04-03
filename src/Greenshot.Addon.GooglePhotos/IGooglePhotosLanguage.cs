using System.ComponentModel;
using Dapplo.Language;

namespace Greenshot.Addon.GooglePhotos
{
    [Language("GooglePhotos")]
    public interface IGooglePhotosLanguage : ILanguage, INotifyPropertyChanged
    {
        string CommunicationWait { get; }

        string Configure { get; }

        string LabelAfterUpload { get; }

        string LabelAfterUploadLinkToClipBoard { get; }

        string LabelUploadFormat { get; }

        string SettingsTitle { get; }

        string UploadFailure { get; }

        string UploadMenuItem { get; }

        string UploadSuccess { get; }
    }
}
