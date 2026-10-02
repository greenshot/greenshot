#pragma once

/*
 * Shared definitions for greenshot-cli.exe (console CLI) and greenshot-proxy.exe (browser / shell integration).
 *
 * Both executables are built WITHOUT the C runtime: they only use the Win32 API (kernel32, advapi32, shell32),
 * see rt.h for the small replacements. This keeps them tiny and fast to start. All parsing of command lines,
 * formatting of output and validation happens in Greenshot itself; the executables only
 *   1. decide the connection source (cli, open_with, url_scheme, native_messaging) from how they were started,
 *   2. forward the raw arguments / relay the browser messages,
 *   3. print the text frames Greenshot sends back.
 */

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>
#include <sddl.h>
#include <shellapi.h>

typedef unsigned int UINT32_T;

#define MAX_PAYLOAD_SIZE (64u * 1024u * 1024u) /* 64 MB max per frame (ADR 003) */
#define MIN_JSON_PAYLOAD_SIZE 2u               /* Minimum valid JSON e.g. "{}" */
#define CHUNK_SIZE 65536u                      /* Relay copy buffer (heap allocated) */
#define COLD_START_TIMEOUT_MS 8000             /* Max wait for Greenshot to create its pipe after starting it */
#define PIPE_PREFIX L"\\\\.\\pipe\\Greenshot_"
#define STARTUP_MUTEX_PREFIX L"Local\\Greenshot_Startup_"

#define PROXY_CLIENT_VERSION "1.4.0"

/* Offline response written to stdout in Native Messaging mode when Greenshot is not running */
#define OFFLINE_JSON "{\"status\":\"unavailable\",\"greenshot_running\":false,\"retry\":true}"
