using GreenshotConfluencePlugin.Web_References.confluence;

namespace GreenshotConfluencePlugin
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