#pragma once

#include "common.h"

/* Returns "\\.\pipe\Greenshot_<UserSID>" (heap allocated, free with RtFree), or NULL */
LPWSTR GetGreenshotPipeName(void);

/*
 * Connects to the Greenshot pipe. If dwTimeoutMs > 0, retries until the pipe exists and is available.
 * dwFlagsAndAttributes is passed to CreateFileW (e.g. FILE_FLAG_OVERLAPPED).
 */
HANDLE ConnectToGreenshotPipe(LPCWSTR pwszPipeName, DWORD dwTimeoutMs, DWORD dwFlagsAndAttributes);

/*
 * Connects to the Greenshot pipe, starting Greenshot.exe (from the proxy's own directory) when it is not running.
 * A per-user mutex prevents several proxies from starting Greenshot at the same time.
 */
HANDLE ConnectOrColdStart(LPCWSTR pwszPipeName);
