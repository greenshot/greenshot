#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>
#include <sddl.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdbool.h>
#include <stdint.h>
#include <io.h>
#include <fcntl.h>
#include <strsafe.h>

#define MAX_PAYLOAD_SIZE (64 * 1024 * 1024) /* 64 MB max per ADR 003 */
#define MIN_PAYLOAD_SIZE 2                   /* Minimum valid JSON e.g. "{}" */
#define CHUNK_SIZE 65536                     /* 64 KB chunk for streaming */
#define COLD_START_TIMEOUT_MS 8000           /* 8s max wait for Greenshot startup */
#define PIPE_PREFIX L"\\\\.\\pipe\\Greenshot_"

/* Offline response written to stdout in Native Messaging mode when Greenshot is not running */
#define OFFLINE_JSON "{\"status\":\"unavailable\",\"greenshot_running\":false,\"retry\":true}"
