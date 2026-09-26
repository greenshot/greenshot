#include "framing.h"

BOOL ReadExact(HANDLE hIn, void* pBuffer, DWORD dwBytesToRead)
{
    if (!hIn || !pBuffer || dwBytesToRead == 0)
    {
        return FALSE;
    }

    BYTE* pByteBuf = (BYTE*)pBuffer;
    DWORD dwTotalRead = 0;

    while (dwTotalRead < dwBytesToRead)
    {
        DWORD dwRead = 0;
        if (!ReadFile(hIn, pByteBuf + dwTotalRead, dwBytesToRead - dwTotalRead, &dwRead, NULL))
        {
            return FALSE;
        }
        if (dwRead == 0)
        {
            /* Premature EOF */
            return FALSE;
        }
        dwTotalRead += dwRead;
    }

    return TRUE;
}

BOOL WriteExact(HANDLE hOut, const void* pBuffer, DWORD dwBytesToWrite)
{
    if (!hOut || !pBuffer || dwBytesToWrite == 0)
    {
        return FALSE;
    }

    const BYTE* pByteBuf = (const BYTE*)pBuffer;
    DWORD dwTotalWritten = 0;

    while (dwTotalWritten < dwBytesToWrite)
    {
        DWORD dwWritten = 0;
        if (!WriteFile(hOut, pByteBuf + dwTotalWritten, dwBytesToWrite - dwTotalWritten, &dwWritten, NULL))
        {
            return FALSE;
        }
        if (dwWritten == 0)
        {
            return FALSE;
        }
        dwTotalWritten += dwWritten;
    }

    return TRUE;
}

int StreamFramedMessage(HANDLE hIn, HANDLE hOut)
{
    if (!hIn || !hOut)
    {
        return -1;
    }

    uint32_t payloadLength = 0;
    DWORD dwRead = 0;

    /* Read the 4-byte length prefix */
    if (!ReadFile(hIn, &payloadLength, sizeof(payloadLength), &dwRead, NULL))
    {
        DWORD dwErr = GetLastError();
        if (dwErr == ERROR_BROKEN_PIPE || dwErr == ERROR_HANDLE_EOF)
        {
            return 0; /* Clean EOF */
        }
        return -1;
    }

    if (dwRead == 0)
    {
        return 0; /* Clean EOF */
    }

    if (dwRead < sizeof(payloadLength))
    {
        /* Incomplete length prefix read -> framing error */
        return -1;
    }

    /* Validate length bounds strictly */
    if (payloadLength < MIN_PAYLOAD_SIZE || payloadLength > MAX_PAYLOAD_SIZE)
    {
        /* Violation: undersized or exceeds 64 MB cap */
        return -1;
    }

    /* Write 4-byte length prefix to destination */
    if (!WriteExact(hOut, &payloadLength, sizeof(payloadLength)))
    {
        return -1;
    }

    /* Stream the payload in fixed-size chunks without allocating a monolithic buffer */
    BYTE chunk[CHUNK_SIZE];
    uint32_t remaining = payloadLength;

    while (remaining > 0)
    {
        DWORD toRead = (remaining < CHUNK_SIZE) ? (DWORD)remaining : CHUNK_SIZE;
        DWORD bytesRead = 0;

        if (!ReadFile(hIn, chunk, toRead, &bytesRead, NULL) || bytesRead == 0)
        {
            /* Pipe broken or premature EOF before full payload read */
            return -1;
        }

        if (!WriteExact(hOut, chunk, bytesRead))
        {
            return -1;
        }

        remaining -= bytesRead;
    }

    return 1;
}

BOOL ReadExactOverlapped(HANDLE hPipe, void* pBuffer, DWORD dwBytesToRead)
{
    if (!hPipe || !pBuffer || dwBytesToRead == 0)
    {
        return FALSE;
    }

    BYTE* pByteBuf = (BYTE*)pBuffer;
    DWORD dwTotalRead = 0;

    OVERLAPPED ov = { 0 };
    ov.hEvent = CreateEventW(NULL, TRUE, FALSE, NULL);
    if (!ov.hEvent)
    {
        return FALSE;
    }

    while (dwTotalRead < dwBytesToRead)
    {
        DWORD dwRead = 0;
        ResetEvent(ov.hEvent);

        if (!ReadFile(hPipe, pByteBuf + dwTotalRead, dwBytesToRead - dwTotalRead, &dwRead, &ov))
        {
            DWORD dwErr = GetLastError();
            if (dwErr == ERROR_IO_PENDING)
            {
                if (!GetOverlappedResult(hPipe, &ov, &dwRead, TRUE))
                {
                    CloseHandle(ov.hEvent);
                    return FALSE;
                }
            }
            else
            {
                CloseHandle(ov.hEvent);
                return FALSE;
            }
        }

        if (dwRead == 0)
        {
            CloseHandle(ov.hEvent);
            return FALSE;
        }

        dwTotalRead += dwRead;
    }

    CloseHandle(ov.hEvent);
    return TRUE;
}

BOOL WriteExactOverlapped(HANDLE hPipe, const void* pBuffer, DWORD dwBytesToWrite)
{
    if (!hPipe || !pBuffer || dwBytesToWrite == 0)
    {
        return FALSE;
    }

    const BYTE* pByteBuf = (const BYTE*)pBuffer;
    DWORD dwTotalWritten = 0;

    OVERLAPPED ov = { 0 };
    ov.hEvent = CreateEventW(NULL, TRUE, FALSE, NULL);
    if (!ov.hEvent)
    {
        return FALSE;
    }

    while (dwTotalWritten < dwBytesToWrite)
    {
        DWORD dwWritten = 0;
        ResetEvent(ov.hEvent);

        if (!WriteFile(hPipe, pByteBuf + dwTotalWritten, dwBytesToWrite - dwTotalWritten, &dwWritten, &ov))
        {
            DWORD dwErr = GetLastError();
            if (dwErr == ERROR_IO_PENDING)
            {
                if (!GetOverlappedResult(hPipe, &ov, &dwWritten, TRUE))
                {
                    CloseHandle(ov.hEvent);
                    return FALSE;
                }
            }
            else
            {
                CloseHandle(ov.hEvent);
                return FALSE;
            }
        }

        if (dwWritten == 0)
        {
            CloseHandle(ov.hEvent);
            return FALSE;
        }

        dwTotalWritten += dwWritten;
    }

    CloseHandle(ov.hEvent);
    return TRUE;
}

int StreamFramedToOverlapped(HANDLE hIn, HANDLE hOut)
{
    if (!hIn || !hOut)
    {
        return -1;
    }

    uint32_t payloadLength = 0;
    DWORD dwRead = 0;

    /* Read the 4-byte length prefix from synchronous hIn (hStdIn) */
    if (!ReadFile(hIn, &payloadLength, sizeof(payloadLength), &dwRead, NULL))
    {
        DWORD dwErr = GetLastError();
        if (dwErr == ERROR_BROKEN_PIPE || dwErr == ERROR_HANDLE_EOF)
        {
            return 0; /* Clean EOF */
        }
        return -1;
    }

    if (dwRead == 0)
    {
        return 0; /* Clean EOF */
    }

    if (dwRead < sizeof(payloadLength))
    {
        return -1;
    }

    if (payloadLength < MIN_PAYLOAD_SIZE || payloadLength > MAX_PAYLOAD_SIZE)
    {
        return -1;
    }

    /* Write 4-byte length prefix to overlapped hOut (hPipe) */
    if (!WriteExactOverlapped(hOut, &payloadLength, sizeof(payloadLength)))
    {
        return -1;
    }

    BYTE chunk[CHUNK_SIZE];
    uint32_t remaining = payloadLength;

    while (remaining > 0)
    {
        DWORD toRead = (remaining < CHUNK_SIZE) ? (DWORD)remaining : CHUNK_SIZE;
        DWORD bytesRead = 0;

        if (!ReadFile(hIn, chunk, toRead, &bytesRead, NULL) || bytesRead == 0)
        {
            return -1;
        }

        if (!WriteExactOverlapped(hOut, chunk, bytesRead))
        {
            return -1;
        }

        remaining -= bytesRead;
    }

    return 1;
}

int StreamFramedFromOverlapped(HANDLE hIn, HANDLE hOut)
{
    if (!hIn || !hOut)
    {
        return -1;
    }

    uint32_t payloadLength = 0;
    DWORD dwRead = 0;
    OVERLAPPED ov = { 0 };
    ov.hEvent = CreateEventW(NULL, TRUE, FALSE, NULL);
    if (!ov.hEvent)
    {
        return -1;
    }

    /* Read the 4-byte length prefix from overlapped hIn (hPipe) */
    if (!ReadFile(hIn, &payloadLength, sizeof(payloadLength), &dwRead, &ov))
    {
        DWORD dwErr = GetLastError();
        if (dwErr == ERROR_IO_PENDING)
        {
            if (!GetOverlappedResult(hIn, &ov, &dwRead, TRUE))
            {
                dwErr = GetLastError();
                CloseHandle(ov.hEvent);
                if (dwErr == ERROR_BROKEN_PIPE || dwErr == ERROR_PIPE_NOT_CONNECTED || dwErr == ERROR_HANDLE_EOF || dwErr == ERROR_OPERATION_ABORTED)
                {
                    return 0; /* Clean EOF */
                }
                return -1;
            }
        }
        else
        {
            CloseHandle(ov.hEvent);
            if (dwErr == ERROR_BROKEN_PIPE || dwErr == ERROR_PIPE_NOT_CONNECTED || dwErr == ERROR_HANDLE_EOF)
            {
                return 0; /* Clean EOF */
            }
            return -1;
        }
    }

    CloseHandle(ov.hEvent);

    if (dwRead == 0)
    {
        return 0; /* Clean EOF */
    }

    if (dwRead < sizeof(payloadLength))
    {
        return -1;
    }

    if (payloadLength < MIN_PAYLOAD_SIZE || payloadLength > MAX_PAYLOAD_SIZE)
    {
        return -1;
    }

    /* Write 4-byte length prefix to synchronous hOut (hStdOut) */
    if (!WriteExact(hOut, &payloadLength, sizeof(payloadLength)))
    {
        return -1;
    }

    BYTE chunk[CHUNK_SIZE];
    uint32_t remaining = payloadLength;

    while (remaining > 0)
    {
        DWORD toRead = (remaining < CHUNK_SIZE) ? (DWORD)remaining : CHUNK_SIZE;
        if (!ReadExactOverlapped(hIn, chunk, toRead))
        {
            return -1;
        }

        if (!WriteExact(hOut, chunk, toRead))
        {
            return -1;
        }

        remaining -= toRead;
    }

    FlushFileBuffers(hOut);
    return 1;
}
