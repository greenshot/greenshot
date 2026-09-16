# Greenshot 1.3 – Technical Changelog

## Security

- Fixed process-wide TLS certificate validation bypass.
- Root cause: a static constructor in `NetworkHelper` set `ServicePointManager.ServerCertificateValidationCallback` to always return `true`.
- Impact in affected versions: every HTTPS request from Greenshot accepted any certificate, enabling man-in-the-middle attacks in hostile network positions.
- Potentially exposed/alterable traffic included OAuth authorization-code and refresh-token flows for Imgur, Box, Dropbox, Flickr, Confluence and Jira.
- Fix scope: certificate validation is no longer globally disabled for the process.

## Rollup Policy

- This entry is prepared for the stable 1.3 release.
- Intermediate/continuous build changes are rolled up and do not get separate stable changelog entries.
