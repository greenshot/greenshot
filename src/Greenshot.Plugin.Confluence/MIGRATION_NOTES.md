# Confluence Plugin Migration Notes

## Overview

The Greenshot Confluence plugin has been migrated from the deprecated SOAP API to the modern REST API using the Dapplo.Confluence library.

## Changes Made

### 1. Dependency Updates

- **Removed**: `System.Web.Services` reference (SOAP client)
- **Removed**: Web References directory containing auto-generated SOAP proxy code
- **Added**: `Dapplo.Confluence` NuGet package (v1.0.17+)

### 2. API Changes

#### Authentication
- **Old (SOAP)**: Used `login()` method with session tokens
- **New (REST)**: Uses HTTP Basic Authentication via `SetBasicAuthentication()`

#### Configuration
- **Old**: Required SOAP endpoint URLs with `/rpc/soap-axis/confluenceservice-v1?wsdl` or `v2` suffix
- **New**: Uses base Confluence URL (e.g., `https://confluence.example.com` or `https://domain.atlassian.net/wiki` for Cloud)

### 3. Code Changes

#### Confluence.cs
- Replaced `ConfluenceSoapServiceService` with `IConfluenceClient`
- Updated all API calls to use async/await pattern with REST endpoints
- Implemented CQL (Confluence Query Language) for search operations
- Updated attachment upload to use streams instead of byte arrays

#### Entities
- Updated `Page.cs` to map from `Dapplo.Confluence.Entities.Content`
- Updated `Space.cs` to map from `Dapplo.Confluence.Entities.Space`

#### Configuration
- Removed SOAP-specific URL constants (`DEFAULT_POSTFIX1`, `DEFAULT_POSTFIX2`)
- Updated URL description to indicate REST API usage

## Confluence Cloud Support

The plugin now supports both **Confluence Server** and **Confluence Cloud**:

### Server
- URL: `https://confluence.example.com`
- Authentication: Username and password

### Cloud
- URL: `https://yourdomain.atlassian.net/wiki`
- Authentication: Email address and API token
- API Token: Generate at https://id.atlassian.com/manage/api-tokens

## API Mapping

| SOAP Method | REST Method | Notes |
|-------------|-------------|-------|
| `login(user, password)` | `SetBasicAuthentication(user, password)` | No session tokens needed |
| `logout(credentials)` | N/A | REST uses stateless auth |
| `getPage(credentials, pageId)` | `Content.GetAsync(pageId)` | Returns Content object |
| `getPage(credentials, space, title)` | `Content.SearchAsync(query)` | Uses CQL query |
| `getSpaces(credentials)` | `Space.GetAllAsync()` | Returns paginated results |
| `getChildren(credentials, pageId)` | `Content.GetChildrenAsync(pageId)` | Returns child pages |
| `search(credentials, query, limit)` | `Content.SearchAsync(whereClause)` | Uses CQL queries |
| `addAttachment(...)` | `Attachment.AttachAsync(...)` | Uses streams |

## Known Limitations

- The REST API does not support session timeout in the same way as SOAP
- Attachment deletion has limitations (see CONF-36015)
- Some operations may have different pagination behavior

## Testing Recommendations

When testing the migrated plugin:

1. Test authentication with both Server and Cloud instances
2. Verify page browsing and searching functionality
3. Test image upload to pages
4. Verify the "current page" detection from browser URLs still works
5. Test personal space filtering

## References

- Dapplo.Confluence: https://github.com/dapplo/Dapplo.Confluence
- Confluence REST API: https://docs.atlassian.com/ConfluenceServer/rest/
- CQL Documentation: https://developer.atlassian.com/cloud/confluence/advanced-searching-using-cql/
