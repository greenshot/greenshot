#include "extension_relay.h"
#include "framing.h"
#include "pipe_utils.h"
#include "protocol.h"
#include "rt.h"

typedef struct _RELAY_THREAD_ARGS
{
    HANDLE hIn;
    BOOL bInOverlapped;
    HANDLE hOut;
    BOOL bOutOverlapped;
} RELAY_THREAD_ARGS;

static DWORD WINAPI RelayThreadProc(LPVOID lpParam)
{
    RELAY_THREAD_ARGS* pArgs = (RELAY_THREAD_ARGS*)lpParam;
    BYTE* pChunk = (BYTE*)RtAlloc(CHUNK_SIZE);
    if (!pChunk)
    {
        return 1;
    }
    while (CopyFrame(pArgs->hIn, pArgs->bInOverlapped, pArgs->hOut, pArgs->bOutOverlapped, pChunk) > 0)
    {
    }
    RtFree(pChunk);
    return 0;
}

int RunExtensionRelay(LPCWSTR pwszPipeName, LPCWSTR pwszOrigin)
{
    HANDLE hStdIn = GetStdHandle(STD_INPUT_HANDLE);
    HANDLE hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

    /*
     * Probe the pipe without waiting. Overlapped I/O allows the two relay threads to read and write the pipe
     * at the same time without blocking each other.
     */
    HANDLE hPipe = ConnectToGreenshotPipe(pwszPipeName, 0, FILE_FLAG_OVERLAPPED);
    if (hPipe == INVALID_HANDLE_VALUE)
    {
        /* Greenshot is offline: report it to the extension, never start Greenshot from the browser (ADR 003) */
        static const char OfflinePayload[] = OFFLINE_JSON;
        WriteFrame(hStdOut, FALSE, OfflinePayload, sizeof(OfflinePayload) - 1);
        FlushFileBuffers(hStdOut);
        return 0;
    }

    /* Announce the connection before relaying anything from the extension */
    if (!SendHello(hPipe, TRUE, IPC_SOURCE_NATIVE_MESSAGING, IPC_REPLY_JSON, pwszOrigin))
    {
        CloseHandle(hPipe);
        return 1;
    }

    RELAY_THREAD_ARGS toPipe = { hStdIn, FALSE, hPipe, TRUE };
    RELAY_THREAD_ARGS fromPipe = { hPipe, TRUE, hStdOut, FALSE };
    HANDLE hThreads[2];
    hThreads[0] = CreateThread(NULL, 0, RelayThreadProc, &toPipe, 0, NULL);
    hThreads[1] = CreateThread(NULL, 0, RelayThreadProc, &fromPipe, 0, NULL);
    if (!hThreads[0] || !hThreads[1])
    {
        if (hThreads[0]) CloseHandle(hThreads[0]);
        if (hThreads[1]) CloseHandle(hThreads[1]);
        CloseHandle(hPipe);
        return 1;
    }

    /* Either side closing ends the relay */
    WaitForMultipleObjects(2, hThreads, FALSE, INFINITE);

    /* Cancel outstanding I/O so the other thread can finish, then give it a moment */
    CancelIoEx(hPipe, NULL);
    CancelIoEx(hStdIn, NULL);
    WaitForMultipleObjects(2, hThreads, TRUE, 1000);

    CloseHandle(hPipe);
    CloseHandle(hThreads[0]);
    CloseHandle(hThreads[1]);
    return 0;
}
