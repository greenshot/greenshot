#include "protocol.h"
#include "framing.h"
#include "rt.h"

static BOOL SendJson(HANDLE hPipe, BOOL bOverlapped, JSON_BUILDER* pBuilder)
{
    BOOL bOk = !pBuilder->bFailed && pBuilder->pData &&
               WriteFrame(hPipe, bOverlapped, pBuilder->pData, (DWORD)pBuilder->cbLength);
    JsonFree(pBuilder);
    return bOk;
}

BOOL SendHello(HANDLE hPipe, BOOL bOverlapped, const char* pszSource, const char* pszReplyFormat, LPCWSTR pwszOrigin)
{
    JSON_BUILDER json;
    memset(&json, 0, sizeof(json));

    JsonAppendRaw(&json, "{\"version\":" IPC_PROTOCOL_VERSION ",\"command\":\"HELLO\",\"source\":");
    JsonAppendStringUtf8(&json, pszSource);
    JsonAppendRaw(&json, ",\"reply_format\":");
    JsonAppendStringUtf8(&json, pszReplyFormat);
    JsonAppendRaw(&json, ",\"client\":\"greenshot-proxy\",\"client_version\":\"" PROXY_CLIENT_VERSION "\"");
    if (pwszOrigin && pwszOrigin[0] != L'\0')
    {
        JsonAppendRaw(&json, ",\"origin\":");
        JsonAppendStringW(&json, pwszOrigin);
    }
    JsonAppendRaw(&json, "}");

    return SendJson(hPipe, bOverlapped, &json);
}

BOOL SendCliRequest(HANDLE hPipe, int argc, LPWSTR* argv)
{
    JSON_BUILDER json;
    memset(&json, 0, sizeof(json));

    JsonAppendRaw(&json, "{\"version\":" IPC_PROTOCOL_VERSION ",\"command\":\"CLI\",\"cwd\":");

    DWORD cchCwd = GetCurrentDirectoryW(0, NULL);
    LPWSTR pwszCwd = cchCwd ? (LPWSTR)RtAlloc((SIZE_T)cchCwd * sizeof(WCHAR)) : NULL;
    if (pwszCwd && GetCurrentDirectoryW(cchCwd, pwszCwd) > 0)
    {
        JsonAppendStringW(&json, pwszCwd);
    }
    else
    {
        JsonAppendRaw(&json, "null");
    }
    RtFree(pwszCwd);

    JsonAppendRaw(&json, ",\"argv\":[");
    for (int i = 1; i < argc; ++i)
    {
        if (i > 1)
        {
            JsonAppendRaw(&json, ",");
        }
        JsonAppendStringW(&json, argv[i]);
    }
    JsonAppendRaw(&json, "]}");

    return SendJson(hPipe, FALSE, &json);
}

static int ReplyError(BOOL bPrint, const char* pszMessage)
{
    if (bPrint)
    {
        RtWriteString(STD_ERROR_HANDLE, pszMessage);
    }
    return PROXY_EXIT_FAILURE;
}

int ReceiveTextReply(HANDLE hPipe, BOOL bPrint)
{
    for (;;)
    {
        UINT32_T length = 0;
        if (!ReadExact(hPipe, FALSE, &length, sizeof(length)))
        {
            return ReplyError(bPrint, "Error: the connection to Greenshot was closed before a result was received.\n");
        }
        if (length < 1 || length > MAX_PAYLOAD_SIZE)
        {
            return ReplyError(bPrint, "Error: received an invalid response from Greenshot.\n");
        }

        BYTE* pFrame = (BYTE*)RtAlloc(length);
        if (!pFrame)
        {
            return ReplyError(bPrint, "Error: out of memory.\n");
        }
        if (!ReadExact(hPipe, FALSE, pFrame, length))
        {
            RtFree(pFrame);
            return ReplyError(bPrint, "Error: the connection to Greenshot was closed before a result was received.\n");
        }

        BYTE type = pFrame[0];
        if (type == TEXT_FRAME_EXIT)
        {
            int exitCode = PROXY_EXIT_FAILURE;
            if (length >= 5)
            {
                memcpy(&exitCode, pFrame + 1, sizeof(exitCode));
            }
            RtFree(pFrame);
            return exitCode;
        }

        if (type != TEXT_FRAME_STDOUT && type != TEXT_FRAME_STDERR)
        {
            RtFree(pFrame);
            return ReplyError(bPrint, "Error: received an invalid response from Greenshot.\n");
        }

        if (bPrint)
        {
            RtWriteUtf8(type == TEXT_FRAME_STDOUT ? STD_OUTPUT_HANDLE : STD_ERROR_HANDLE, (const char*)pFrame + 1, length - 1);
        }
        RtFree(pFrame);
    }
}
