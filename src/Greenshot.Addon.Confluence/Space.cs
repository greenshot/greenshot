using Greenshot.Addon.Confluence.Web_References.confluence;

namespace Greenshot.Addon.Confluence
{
    public class Space
    {
        public Space(RemoteSpaceSummary space)
        {
            Key = space.key;
            Name = space.name;
        }

        public string Key { get; set; }

        public string Name { get; set; }
    }
}