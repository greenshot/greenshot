#pragma once
#include "common.h"

#ifdef __cplusplus
extern "C" {
#endif

/*
 * Resolves the current user's SID and formats the pipe name:
 * \\.\pipe\Greenshot_<UserSID>
 */
BOOL GetUserSidPipeName(LPWSTR pszPipeName, DWORD cchPipeName);

/*
 * Resolves the current user's SID and formats the startup mutex name:
 * Local\Greenshot_Startup_<UserSID>
 */
BOOL GetUserStartupMutexName(LPWSTR pszMutexName, DWORD cchMutexName);

/*
 * Attempts to connect to the session-scoped Greenshot named pipe.
 * If dwTimeoutMs > 0, retries until the pipe is created or becomes available.
 */
HANDLE ConnectToGreenshotPipe(LPCWSTR pszPipeName, DWORD dwTimeoutMs);
HANDLE ConnectToGreenshotPipeEx(LPCWSTR pszPipeName, DWORD dwTimeoutMs, DWORD dwFlagsAndAttributes);

#ifdef __cplusplus
}
#endif

