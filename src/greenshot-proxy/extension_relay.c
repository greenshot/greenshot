#include "extension_relay.h"
#include "pipe_utils.h"
#include "framing.h"

typedef struct _RELAY_THREAD_ARGS
{
    HANDLE hIn;
    HANDLE hOut;
} RELAY_THREAD_ARGS;

static DWORD WINAPI InToPipeThreadProc(LPVOID lpParam)
{
    RELAY_THREAD_ARGS* pArgs = (RELAY_THREAD_ARGS*)lpParam;
    HANDLE hIn = pArgs->hIn;
    HANDLE hOut = pArgs->hOut;

    while (TRUE)
    {
        int res = StreamFramedToOverlapped(hIn, hOut);
        if (res <= 0)
        {
            break;
        }
    }

    return 0;
}

static DWORD WINAPI PipeToOutThreadProc(LPVOID lpParam)
{
    RELAY_THREAD_ARGS* pArgs = (RELAY_THREAD_ARGS*)lpParam;
    HANDLE hIn = pArgs->hIn;
    HANDLE hOut = pArgs->hOut;

    while (TRUE)
    {
        int res = StreamFramedFromOverlapped(hIn, hOut);
        if (res <= 0)
        {
            break;
        }
    }

    return 0;
}

int RunExtensionRelay(LPCWSTR pszPipeName)
{
    /* Set stdin and stdout to binary mode for raw framing */
    _setmode(_fileno(stdin), _O_BINARY);
    _setmode(_fileno(stdout), _O_BINARY);

    HANDLE hStdIn = GetStdHandle(STD_INPUT_HANDLE);
    HANDLE hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

    /*
     * Probe Greenshot named pipe with zero timeout (instant check).
     * Open with FILE_FLAG_OVERLAPPED to prevent synchronous handle locking deadlock
     * between simultaneous ReadFile and WriteFile operations across relay threads.
     */
    HANDLE hPipe = ConnectToGreenshotPipeEx(pszPipeName, 0, FILE_FLAG_OVERLAPPED);

    if (hPipe == INVALID_HANDLE_VALUE)
    {
        /*
         * Greenshot is OFFLINE:
         * Strict behavior per ADR 003: Do NOT launch Greenshot.exe.
         * Write offline status payload to stdout and exit cleanly.
         */
        const char szOfflinePayload[] = OFFLINE_JSON;
        uint32_t payloadLen = (uint32_t)strlen(szOfflinePayload);

        WriteExact(hStdOut, &payloadLen, sizeof(payloadLen));
        WriteExact(hStdOut, szOfflinePayload, payloadLen);
        FlushFileBuffers(hStdOut);
        return 0;
    }

    /*
     * Greenshot is ONLINE:
     * Run full-duplex transparent relay between stdio and Named Pipe.
     */
    RELAY_THREAD_ARGS inToPipeArgs = { hStdIn, hPipe };
    RELAY_THREAD_ARGS pipeToOutArgs = { hPipe, hStdOut };

    HANDLE hThreads[2];
    hThreads[0] = CreateThread(NULL, 0, InToPipeThreadProc, &inToPipeArgs, 0, NULL);
    hThreads[1] = CreateThread(NULL, 0, PipeToOutThreadProc, &pipeToOutArgs, 0, NULL);

    if (!hThreads[0] || !hThreads[1])
    {
        if (hThreads[0]) CloseHandle(hThreads[0]);
        if (hThreads[1]) CloseHandle(hThreads[1]);
        CloseHandle(hPipe);
        return 1;
    }

    /* Wait for either thread to terminate (connection closed on one end) */
    WaitForMultipleObjects(2, hThreads, FALSE, INFINITE);

    /* Close pipe to unblock the other thread if it's waiting on I/O */
    CloseHandle(hPipe);

    /* Wait up to 1 second for the remaining thread to wind down */
    WaitForMultipleObjects(2, hThreads, TRUE, 1000);

    CloseHandle(hThreads[0]);
    CloseHandle(hThreads[1]);

    return 0;
}
