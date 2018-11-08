using System.ComponentModel;
using Dapplo.Config.Language;

namespace Greenshot.Addon.GooglePhotos.Configuration
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
