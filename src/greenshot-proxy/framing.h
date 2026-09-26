#pragma once
#include "common.h"

#ifdef __cplusplus
extern "C" {
#endif

/*
 * Reads exactly dwBytesToRead from hIn.
 * Returns TRUE on success, FALSE if EOF is reached or an error occurs.
 */
BOOL ReadExact(HANDLE hIn, void* pBuffer, DWORD dwBytesToRead);

/*
 * Writes exactly dwBytesToWrite to hOut.
 * Returns TRUE on success, FALSE on error.
 */
BOOL WriteExact(HANDLE hOut, const void* pBuffer, DWORD dwBytesToWrite);

/*
 * Reads a 4-byte length prefixed message from hIn and streams it to hOut.
 * Validates that MIN_PAYLOAD_SIZE <= payload_length <= MAX_PAYLOAD_SIZE.
 * Uses a fixed chunk buffer to prevent large heap allocations and buffer overruns.
 * Returns:
 *   1 on full successful transfer
 *   0 on clean EOF (when reading length prefix)
 *  -1 on framing violation / out-of-bounds size / write error
 */
int StreamFramedMessage(HANDLE hIn, HANDLE hOut);

/*
 * Reads exactly dwBytesToRead from an overlapped handle using an internal event.
 * Returns TRUE on success, FALSE on error or premature EOF.
 */
BOOL ReadExactOverlapped(HANDLE hFile, void* pBuffer, DWORD dwBytesToRead);

/*
 * Writes exactly dwBytesToWrite to an overlapped handle using an internal event.
 * Returns TRUE on success, FALSE on error.
 */
BOOL WriteExactOverlapped(HANDLE hFile, const void* pBuffer, DWORD dwBytesToWrite);

/*
 * Reads a framed message from synchronous handle hIn (e.g. hStdIn)
 * and streams it to overlapped handle hOut (e.g. hPipe).
 */
int StreamFramedToOverlapped(HANDLE hIn, HANDLE hOut);

/*
 * Reads a framed message from overlapped handle hIn (e.g. hPipe)
 * and streams it to synchronous handle hOut (e.g. hStdOut).
 */
int StreamFramedFromOverlapped(HANDLE hIn, HANDLE hOut);

#ifdef __cplusplus
}
#endif
