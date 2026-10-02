#pragma once

#include "common.h"

/*
 * Framing: every message is a 4-byte little-endian length followed by that many payload bytes.
 * bOverlapped must be TRUE for handles opened with FILE_FLAG_OVERLAPPED (the relay's pipe handle).
 */

/* Reads exactly cbCount bytes. FALSE on error or premature end of stream. */
BOOL ReadExact(HANDLE hIn, BOOL bOverlapped, void* pBuffer, DWORD cbCount);

/* Writes exactly cbCount bytes. FALSE on error. */
BOOL WriteExact(HANDLE hOut, BOOL bOverlapped, const void* pBuffer, DWORD cbCount);

/* Writes one frame (length prefix + payload) */
BOOL WriteFrame(HANDLE hOut, BOOL bOverlapped, const void* pPayload, DWORD cbPayload);

/*
 * Copies one frame from hIn to hOut, validating MIN_JSON_PAYLOAD_SIZE <= length <= MAX_PAYLOAD_SIZE and streaming it
 * through the caller supplied CHUNK_SIZE buffer (no allocation per frame).
 * Returns 1 when a frame was copied, 0 on a clean end of stream before a frame started, -1 on errors.
 */
int CopyFrame(HANDLE hIn, BOOL bInOverlapped, HANDLE hOut, BOOL bOutOverlapped, BYTE* pChunk);
