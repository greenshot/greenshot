# HTTP in Greenshot

How Greenshot, its plugins and new features talk to services on the Internet: downloading, calling a web API, uploading a
capture. Everything goes through one class, `HttpService` in `Greenshot.Base/Http`, so the proxy setting, the certificate
exceptions of the configuration and the logging rules apply everywhere without each caller repeating them.

- [The parts](#the-parts)
- [Using it](#using-it): get JSON, post a form, upload a capture, call an OAuth service, handle errors
- [Rules](#rules): logging, secrets, threads, cancellation
- [Proxy, credentials and certificates](#proxy-credentials-and-certificates)
- [Testing](#testing)

## The parts

| Type | What it is for |
|---|---|
| `HttpService` | `HttpService.Current` is the one HttpClient of Greenshot. `SendAsync` for any request, plus `GetJsonAsync<T>`, `GetBytesAsync`, `GetStatusAsync` and `PostFormAsync`. |
| `HttpResult` | What `SendAsync` and `PostFormAsync` return: `StatusCode`, `ReasonPhrase`, `Headers` (response and content headers, names case insensitive), `RequestUri` (the uri which answered, after redirects) and `Content` (string). The response is read completely, nothing needs to be disposed. |
| `HttpContents` | Request content for a capture: `Upload` (the encoded bytes with their MIME type) and `Multipart` (multipart/form-data with text fields and the file). Both report the upload progress. |
| `CertificateValidator` | The certificate rules (platform validation plus the allowed hosts and thumbprints of the configuration). |
| `ImageDownloader` | Downloads an image from a url (HTML on the clipboard, a drop on the editor). |
| `SiteIcon` | The icon of a web site as 32x32 PNG, e.g. for a destination: `favicon.ico` below the site, else the icon link of its start page (same scheme and host), optionally the same for the parent domain. ICO, PNG, GIF and JPEG by their first bytes, 256 KB and 45 seconds at most, null when there is none. Used by the Jira destination and the custom uploader. |
| `OAuth2Helper` (`Greenshot.Base/Core/OAuth`) | The OAuth 2 authorization (browser, local listener) and the token refresh, on top of `HttpService`. |

## Using it

All methods are async, take a `CancellationToken` and continue on the thread pool (`ConfigureAwait(false)` inside).
Destinations and recipe steps already run on the thread pool; the UI thread can await them too, it isn't blocked.

### Get JSON

```csharp
var feed = await HttpService.Current.GetJsonAsync<UpdateFeed>(new Uri("https://getgreenshot.org/update-feed.json"), cancellationToken);
```

The content is deserialized with Json.NET, so `[JsonProperty]` works. An HTTP error throws (see [Errors](#errors)).

### Any request

```csharp
var request = new HttpRequestMessage(HttpMethod.Delete, "https://api.example.com/files/" + Uri.EscapeDataString(id));
request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
var result = await HttpService.Current.SendAsync(request, cancellationToken);
if (result.StatusCode == HttpStatusCode.NotFound)
{
    return; // already gone
}

result.EnsureSuccess();
```

`SendAsync` disposes the request. It doesn't throw for an HTTP error status, so a caller can treat a 404 or 409 as a normal
answer; `EnsureSuccess()` throws for everything else that isn't 2xx and returns the result, so `.EnsureSuccess().Content`
reads in one line.

### Post a form

```csharp
var result = await HttpService.Current.PostFormAsync(tokenUrl, new Dictionary<string, string>
{
    { "grant_type", "refresh_token" },
    { "refresh_token", refreshToken }
}, cancellationToken);
```

The fields are sent as `application/x-www-form-urlencoded` (spaces as `+`, everything else percent encoded).

### Upload a capture

A destination gets the encoded capture as `EncodedImage` from the export request. Raw body (Dropbox style):

```csharp
var request = new HttpRequestMessage(HttpMethod.Post, "https://content.example.com/upload");
request.Content = HttpContents.Upload(image, progress);
string json = (await HttpService.Current.SendAsync(request, cancellationToken)).EnsureSuccess().Content;
```

Multipart form (Box style), the text fields come before the file:

```csharp
request.Content = HttpContents.Multipart("file", image, filename, new Dictionary<string, string>
{
    { "parent_id", folderId }
}, progress);
```

The file name goes into `filename` in ASCII (other characters become `_`, the extension stays), a name with other characters
is sent completely as `filename*` too. Servers which only read `filename` (PHP) keep the extension that way.

`progress` is an `IProgress<ProgressInfo>`, usually the one the destination gets for the upload, it receives the percentage
as the bytes go out. The bytes of the capture are not copied.

The custom uploader plugin builds both kinds of request from an uploader file of the user, with the import checks a
request to a server someone else chose needs: see [custom-uploader.md](custom-uploader.md#for-developers).

### An OAuth 2 service

`OAuth2Helper.CreateOAuth2RequestAsync` authorizes (once, in the browser) or refreshes the token when needed and returns a
request with the bearer token, or null when the user didn't authorize. Box and Dropbox (`Greenshot.Plugin.Box/Api/BoxUtils.cs`,
`Greenshot.Plugin.Dropbox/Api/DropboxUtils.cs`) show the whole flow, including storing the refresh token encrypted.
A service needs a registered client id; keep it out of the repository (the `*.Credentials.template` files are filled by the build).

### Errors

| What happened | What you get |
|---|---|
| HTTP error status | `SendAsync` / `PostFormAsync`: an `HttpResult` with that status. `EnsureSuccess()`, `GetJsonAsync`, `GetBytesAsync`: `UnauthorizedAccessException` for 401, otherwise `HttpRequestException`. The message is the status only. |
| No connection, DNS, TLS | `HttpRequestException` (from HttpClient) |
| Longer than the timeout (10 minutes for the whole request) | `TimeoutException`, the message names method, scheme and host only |
| The caller canceled | `OperationCanceledException` |

`GetStatusAsync` returns null for no connection and timeouts, for a "is this page there" check (help).

## Rules

- **Never create an HttpClient.** Use `HttpService.Current`; it carries the configuration and keeps the connections.
  Libraries with their own HTTP stack (Dapplo.Jira) get the certificate rules through `CertificateValidator.RegisterGlobally`, called at startup.
- **Nothing that is sent or received goes into the log.** `HttpService` logs method, scheme, host and status, never path,
  query, headers or content: tokens hide in all of them (a webhook url is a secret, an error body can repeat the api key).
  Log the fields of a response you parsed, not the response. The exception messages follow the same rule.
- **Secrets are stored encrypted** for the Windows user, never in clear text in greenshot.ini, and never written to an export
  or a bug report.
- **Escape what goes into a url** (`Uri.EscapeDataString`) and don't put user text into header values unchecked.
- **Requests with your own headers or content don't follow redirects.** `HttpService.Current` follows them, and
  HttpClientHandler sends every header with them to the new host (on .NET Framework even from https to http). Send
  keys, tokens and captures through `HttpService.WithoutRedirects`; a redirect then comes back as the 3xx result.
- **Pass the cancellation token on.** A user who closes the progress dialog cancels the upload.
- **A recipe step or destination which sends data over the network declares it**: implement `IRequiresRecipeAuthorization`
  and return a `RecipeGatedAction(RecipeGateType.NetworkAccess, host)` per host, so the user approves it before a recipe runs it.

## Proxy, credentials and certificates

- The proxy is the Windows proxy (`WebRequest.DefaultWebProxy`) unless `UseProxy` in `[Core]` of greenshot.ini is false.
- The Windows credentials go to the proxy only. A server which asks for Windows authentication doesn't get them.
- Certificates: the platform validation decides; a certificate it rejects is still accepted when its thumbprint is in
  `AllowedCertificateThumbprints` or the host matches `AllowedUntrustedCertificateHosts` (`*.example.com` patterns) in `[Core]`.
  Both are logged as a warning when they are used.
- TLS 1.2 and 1.3 only (`GreenshotMain`).

## Testing

`HttpService` takes a handler, so a test answers the requests itself, without network. `Greenshot.Tests/Http/FakeHttpHandler.cs`
records what was sent (method, uri, content type, body, user agent):

```csharp
var handler = new FakeHttpHandler(request => FakeHttpHandler.Text(HttpStatusCode.OK, "{\"link\":\"https://example.com/a.png\"}"));
var service = new HttpService(handler, TimeSpan.FromSeconds(30));

var result = await service.SendAsync(new HttpRequestMessage(HttpMethod.Post, "https://example.com/upload"), CancellationToken.None);

Assert.Equal(HttpMethod.Post, Assert.Single(handler.Requests).Method);
```

Code which should be testable takes an `HttpService` (or has an internal overload which does, like `ImageDownloader`) and
uses `HttpService.Current` otherwise. See `Greenshot.Tests/Http` for the tests of the layer itself.
