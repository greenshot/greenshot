using GreenshotConfluencePlugin.Web_References.confluence;

namespace GreenshotConfluencePlugin
{
    public class Page
    {
        public Page(RemotePage page)
        {
            Id = page.id;
            Title = page.title;
            SpaceKey = page.space;
            Url = page.url;
            Content = page.content;
        }

        public Page(RemoteSearchResult searchResult, string space)
        {
            Id = searchResult.id;
            Title = searchResult.title;
            SpaceKey = space;
            Url = searchResult.url;
            Content = searchResult.excerpt;
        }

        public Page(RemotePageSummary pageSummary)
        {
            Id = pageSummary.id;
            Title = pageSummary.title;
            SpaceKey = pageSummary.space;
            Url = pageSummary.url;
        }

        public long Id { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public string Content { get; set; }

        public string SpaceKey { get; set; }
    }
}