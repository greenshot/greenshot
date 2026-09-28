#pragma once

#include "common.h"

/*
 * Native Messaging relay (greenshot-proxy.exe started by the browser):
 * 1. If Greenshot is not running: writes OFFLINE_JSON as a frame to stdout and returns (Greenshot is NOT started).
 * 2. Otherwise sends the HELLO frame (source "native_messaging", JSON replies, the extension's origin)
 *    and then relays frames in both directions until either side closes.
 * pwszOrigin is the extension origin as passed by the browser (may be NULL).
 */
int RunExtensionRelay(LPCWSTR pwszPipeName, LPCWSTR pwszOrigin);
