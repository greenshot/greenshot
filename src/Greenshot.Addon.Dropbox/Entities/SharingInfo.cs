using Newtonsoft.Json;

namespace Greenshot.Addon.Dropbox.Entities
{
    internal class SharingInfo
    {
        [JsonProperty("modified_by")]
        public string ModifiedBy { get; set; }

        [JsonProperty("parent_shared_folder_id")]
        public string ParentSharedFolderId { get; set; }

        [JsonProperty("read_only")]
        public bool ReadOnly { get; set; }
    }
}