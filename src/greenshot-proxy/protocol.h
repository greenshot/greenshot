#pragma once

#include "common.h"

/* Version of the framed protocol between the proxy executables and Greenshot */
#define IPC_PROTOCOL_VERSION "1"

/* Connection sources, announced once per connection in the HELLO frame */
#define IPC_SOURCE_CLI "cli"
#define IPC_SOURCE_OPEN_WITH "open_with"
#define IPC_SOURCE_URL_SCHEME "url_scheme"
#define IPC_SOURCE_NATIVE_MESSAGING "native_messaging"

/*
 * Reply formats, announced in the HELLO frame:
 *   "json" - replies are JSON objects (browser extension)
 *   "text" - replies are text frames: payload[0] is the frame type, the rest the frame data:
 *              'O' UTF-8 text for stdout, 'E' UTF-8 text for stderr,
 *              'X' end of the reply, followed by the exit code as 4-byte little-endian signed integer.
 */
#define IPC_REPLY_JSON "json"
#define IPC_REPLY_TEXT "text"

#define TEXT_FRAME_STDOUT 'O'
#define TEXT_FRAME_STDERR 'E'
#define TEXT_FRAME_EXIT 'X'

/* Exit codes produced by the executables themselves (Greenshot supplies all others) */
#define PROXY_EXIT_OK 0
#define PROXY_EXIT_FAILURE 1
#define PROXY_EXIT_UNAVAILABLE 3

/*
 * Sends the HELLO frame, which must be the first frame on every connection. Greenshot binds the connection's
 * source (and origin) from it and ignores any "source" in later messages, so data relayed from a browser can never
 * claim to be the command line. pwszOrigin may be NULL.
 */
BOOL SendHello(HANDLE hPipe, BOOL bOverlapped, const char* pszSource, const char* pszReplyFormat, LPCWSTR pwszOrigin);

/*
 * Sends {"command":"CLI","cwd":...,"argv":[argv[1]..argv[argc-1]]}. The arguments are passed on unchanged;
 * Greenshot parses and validates them (according to the connection source).
 */
BOOL SendCliRequest(HANDLE hPipe, int argc, LPWSTR* argv);

/*
 * Reads text frames until the 'X' frame and returns its exit code. With bPrint, 'O' / 'E' frames are written to
 * stdout / stderr. Protocol and connection errors are reported on stderr (if bPrint) and return PROXY_EXIT_FAILURE.
 */
int ReceiveTextReply(HANDLE hPipe, BOOL bPrint);
