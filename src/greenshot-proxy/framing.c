#include "framing.h"

/* Returns the number of bytes read (> 0), 0 at the end of the stream, or -1 on errors */
static int ReadSome(HANDLE hIn, BOOL bOverlapped, void* pBuffer, DWORD cbCount)
{
    DWORD cbRead = 0;
    BOOL bOk;
    DWORD dwError = ERROR_SUCCESS;

    if (bOverlapped)
    {
        OVERLAPPED overlapped;
        memset(&overlapped, 0, sizeof(overlapped));
        overlapped.hEvent = CreateEventW(NULL, TRUE, FALSE, NULL);
        if (!overlapped.hEvent)
        {
            return -1;
        }
        bOk = ReadFile(hIn, pBuffer, cbCount, &cbRead, &overlapped);
        if (!bOk && GetLastError() == ERROR_IO_PENDING)
        {
            bOk = GetOverlappedResult(hIn, &overlapped, &cbRead, TRUE);
        }
        if (!bOk)
        {
            dwError = GetLastError();
        }
        CloseHandle(overlapped.hEvent);
    }
    else
    {
        bOk = ReadFile(hIn, pBuffer, cbCount, &cbRead, NULL);
        if (!bOk)
        {
            dwError = GetLastError();
        }
    }

    if (!bOk)
    {
        if (dwError == ERROR_BROKEN_PIPE || dwError == ERROR_HANDLE_EOF ||
            dwError == ERROR_PIPE_NOT_CONNECTED || dwError == ERROR_OPERATION_ABORTED)
        {
            return 0;
        }
        return -1;
    }
    return (int)cbRead;
}

BOOL ReadExact(HANDLE hIn, BOOL bOverlapped, void* pBuffer, DWORD cbCount)
{
    BYTE* pBytes = (BYTE*)pBuffer;
    DWORD cbTotal = 0;
    while (cbTotal < cbCount)
    {
        int cbRead = ReadSome(hIn, bOverlapped, pBytes + cbTotal, cbCount - cbTotal);
        if (cbRead <= 0)
        {
            return FALSE;
        }
        cbTotal += (DWORD)cbRead;
    }
    return TRUE;
}

BOOL WriteExact(HANDLE hOut, BOOL bOverlapped, const void* pBuffer, DWORD cbCount)
{
    const BYTE* pBytes = (const BYTE*)pBuffer;
    DWORD cbTotal = 0;
    while (cbTotal < cbCount)
    {
        DWORD cbWritten = 0;
        BOOL bOk;
        if (bOverlapped)
        {
            OVERLAPPED overlapped;
            memset(&overlapped, 0, sizeof(overlapped));
            overlapped.hEvent = CreateEventW(NULL, TRUE, FALSE, NULL);
            if (!overlapped.hEvent)
            {
                return FALSE;
            }
            bOk = WriteFile(hOut, pBytes + cbTotal, cbCount - cbTotal, &cbWritten, &overlapped);
            if (!bOk && GetLastError() == ERROR_IO_PENDING)
            {
                bOk = GetOverlappedResult(hOut, &overlapped, &cbWritten, TRUE);
            }
            CloseHandle(overlapped.hEvent);
        }
        else
        {
            bOk = WriteFile(hOut, pBytes + cbTotal, cbCount - cbTotal, &cbWritten, NULL);
        }
        if (!bOk || cbWritten == 0)
        {
            return FALSE;
        }
        cbTotal += cbWritten;
    }
    return TRUE;
}

BOOL WriteFrame(HANDLE hOut, BOOL bOverlapped, const void* pPayload, DWORD cbPayload)
{
    if (!pPayload || cbPayload == 0 || cbPayload > MAX_PAYLOAD_SIZE)
    {
        return FALSE;
    }
    UINT32_T length = (UINT32_T)cbPayload; /* x86/x64: little endian */
    return WriteExact(hOut, bOverlapped, &length, sizeof(length)) &&
           WriteExact(hOut, bOverlapped, pPayload, cbPayload);
}

int CopyFrame(HANDLE hIn, BOOL bInOverlapped, HANDLE hOut, BOOL bOutOverlapped, BYTE* pChunk)
{
    UINT32_T length = 0;
    BYTE* pLength = (BYTE*)&length;

    /* The first read distinguishes a clean end of stream from a truncated frame */
    int cbRead = ReadSome(hIn, bInOverlapped, pLength, sizeof(length));
    if (cbRead == 0)
    {
        return 0;
    }
    if (cbRead < 0 ||
        ((DWORD)cbRead < sizeof(length) && !ReadExact(hIn, bInOverlapped, pLength + cbRead, sizeof(length) - (DWORD)cbRead)))
    {
        return -1;
    }

    if (length < MIN_JSON_PAYLOAD_SIZE || length > MAX_PAYLOAD_SIZE)
    {
        return -1;
    }
    if (!WriteExact(hOut, bOutOverlapped, &length, sizeof(length)))
    {
        return -1;
    }

    UINT32_T remaining = length;
    while (remaining > 0)
    {
        DWORD cbChunk = remaining < CHUNK_SIZE ? remaining : CHUNK_SIZE;
        if (!ReadExact(hIn, bInOverlapped, pChunk, cbChunk) ||
            !WriteExact(hOut, bOutOverlapped, pChunk, cbChunk))
        {
            return -1;
        }
        remaining -= cbChunk;
    }

    if (!bOutOverlapped)
    {
        FlushFileBuffers(hOut);
    }
    return 1;
}
