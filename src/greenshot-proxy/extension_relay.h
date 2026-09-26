#pragma once
#include "common.h"

#ifdef __cplusplus
extern "C" {
#endif

/*
 * Executes the Native Messaging relay mode:
 * 1. Checks if Greenshot pipe is available.
 * 2. If OFFLINE: Writes JSON status payload to stdout and exits cleanly without spawning Greenshot.
 * 3. If ONLINE: Sets stdio to binary mode and runs bidirectional stream relay.
 */
int RunExtensionRelay(LPCWSTR pszPipeName);

#ifdef __cplusplus
}
#endif
