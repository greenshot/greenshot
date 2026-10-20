# Confluence Plugin API Notes

The plugin talks to the Confluence REST API with its own small client (`Api/ConfluenceClient.cs`) over Greenshot's
`HttpService`. It replaced the SOAP API (removed by Atlassian) and later the Dapplo.Confluence library.

## Configuration

- Server/Data Center: the base url, e.g. `https://confluence.example.com`, with user name and password (or a personal access token as password where Basic is allowed).
- Cloud: `https://yourdomain.atlassian.net/wiki`, with the e-mail address and an API token from https://id.atlassian.com/manage/api-tokens

## Calls

Cloud deprecated most v1 calls that have a v2 replacement (RFC-19), so the plugin uses only calls that exist on both:

| Use | Call |
|-----|------|
| Check the login | `GET rest/api/user/current` |
| Page by id (space home page), child pages, text search | `GET rest/api/content/search?cql=...` (CQL `id=`, `parent=`, `text~`) |
| Spaces with their home page | `GET rest/api/space?expand=homepage`; when that is gone (404/410 on Cloud) `GET api/v2/spaces` |
| Upload | `POST rest/api/content/{id}/child/attachment` (multipart `file`, `minorEdit`, `comment`, header `X-Atlassian-Token: no-check`) |

## References

- Confluence Cloud REST API: https://developer.atlassian.com/cloud/confluence/rest/v1/
- Confluence Data Center REST API: https://developer.atlassian.com/server/confluence/rest/
- CQL: https://developer.atlassian.com/cloud/confluence/advanced-searching-using-cql/
- v1 deprecation: https://community.developer.atlassian.com/t/rfc-19-deprecation-of-confluence-cloud-rest-api-v1-endpoints/71752
