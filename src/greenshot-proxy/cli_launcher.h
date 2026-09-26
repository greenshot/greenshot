#pragma once
#include "common.h"

#ifdef __cplusplus
extern "C" {
#endif

/*
 * Executes the CLI / Custom URL Scheme / Open-With mode:
 * 1. Safely escapes command line argument into minimal JSON envelope.
 * 2. Connects to Greenshot named pipe (performing cold-start if offline).
 * 3. Writes 4-byte length prefixed JSON to the pipe and terminates.
 */
int RunCliOrUrl(LPCWSTR pszPipeName, int argc, wchar_t* argv[]);

#ifdef __cplusplus
}
#endif
