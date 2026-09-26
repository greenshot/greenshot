#include "cli_launcher.h"
#include "pipe_utils.h"
#include "framing.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <io.h>
#include <fcntl.h>

#define VERSION_STRING L"Greenshot Proxy 1.4.0"

static void PrintOutput(HANDLE hStd, LPCWSTR pwszText)
{
    if (!hStd || !pwszText || hStd == INVALID_HANDLE_VALUE)
    {
        return;
    }

    DWORD dwMode = 0;
    if (GetConsoleMode(hStd, &dwMode))
    {
        WriteConsoleW(hStd, pwszText, (DWORD)wcslen(pwszText), NULL, NULL);
    }
    else
    {
        int utf8Len = WideCharToMultiByte(CP_UTF8, 0, pwszText, -1, NULL, 0, NULL, NULL);
        if (utf8Len > 1)
        {
            char* pUtf8 = (char*)malloc(utf8Len);
            if (pUtf8)
            {
                WideCharToMultiByte(CP_UTF8, 0, pwszText, -1, pUtf8, utf8Len, NULL, NULL);
                DWORD written = 0;
                WriteFile(hStd, pUtf8, (DWORD)(utf8Len - 1), &written, NULL);
                free(pUtf8);
            }
        }
    }
}

static void PrintStdout(LPCWSTR pwszText)
{
    PrintOutput(GetStdHandle(STD_OUTPUT_HANDLE), pwszText);
}

static void PrintStderr(LPCWSTR pwszText)
{
    PrintOutput(GetStdHandle(STD_ERROR_HANDLE), pwszText);
}

static void PrintUsage(void)
{
    LPCWSTR pwszUsage =
        L"Greenshot Proxy CLI\n\n"
        L"Usage:\n"
        L"  greenshot-proxy [options]\n"
        L"  greenshot-proxy <file...>\n\n"
        L"Options:\n"
        L"  --list-recipes, -l            List all recipes configured with a CommandlineTrigger\n"
        L"  --recipe, -r <cmd|id> [k=v]   Execute a recipe by command identifier or recipe ID\n"
        L"  --file, -f <file...>          Open one or more files using configured OpenFile triggers\n"
        L"  --reload                      Reload Greenshot configuration\n"
        L"  --exit                        Exit running Greenshot instance\n"
        L"  --version, -v                 Show version\n"
        L"  --help, -h                    Show this help text\n\n"
        L"Recipe Context Parameters:\n"
        L"  Pass key=value pairs after the recipe name to inject context into the execution flow:\n"
        L"  Example: greenshot-proxy --recipe ocr destination=clipboard\n";

    PrintStdout(pwszUsage);
}

static void EscapeJsonString(LPCWSTR pwszSrc, char* pszDest, size_t cchDest)
{
    if (!pszDest || cchDest == 0)
    {
        return;
    }

    size_t outIdx = 0;
    pszDest[0] = '\0';

    if (!pwszSrc)
    {
        return;
    }

    for (size_t i = 0; pwszSrc[i] != L'\0' && outIdx + 7 < cchDest; ++i)
    {
        wchar_t ch = pwszSrc[i];

        switch (ch)
        {
        case L'\"':
            pszDest[outIdx++] = '\\';
            pszDest[outIdx++] = '\"';
            break;
        case L'\\':
            pszDest[outIdx++] = '\\';
            pszDest[outIdx++] = '\\';
            break;
        case L'\b':
            pszDest[outIdx++] = '\\';
            pszDest[outIdx++] = 'b';
            break;
        case L'\f':
            pszDest[outIdx++] = '\\';
            pszDest[outIdx++] = 'f';
            break;
        case L'\n':
            pszDest[outIdx++] = '\\';
            pszDest[outIdx++] = 'n';
            break;
        case L'\r':
            pszDest[outIdx++] = '\\';
            pszDest[outIdx++] = 'r';
            break;
        case L'\t':
            pszDest[outIdx++] = '\\';
            pszDest[outIdx++] = 't';
            break;
        default:
            if (ch < 0x20)
            {
                int written = snprintf(pszDest + outIdx, cchDest - outIdx, "\\u%04x", (unsigned int)ch);
                if (written > 0)
                {
                    outIdx += written;
                }
            }
            else
            {
                char utf8Char[8] = { 0 };
                int utf8Len = WideCharToMultiByte(CP_UTF8, 0, &ch, 1, utf8Char, sizeof(utf8Char), NULL, NULL);
                if (utf8Len > 0 && outIdx + utf8Len < cchDest)
                {
                    for (int k = 0; k < utf8Len; ++k)
                    {
                        pszDest[outIdx++] = utf8Char[k];
                    }
                }
            }
            break;
        }
    }

    pszDest[outIdx] = '\0';
}

static HANDLE ConnectOrColdStart(LPCWSTR pszPipeName)
{
    /* 1. Fast path: try connecting without waiting if Greenshot is already active */
    HANDLE hPipe = ConnectToGreenshotPipe(pszPipeName, 0);
    if (hPipe != INVALID_HANDLE_VALUE)
    {
        return hPipe;
    }

    /* 2. Acquire user-scoped startup mutex to synchronize concurrent cold-starts */
    wchar_t szMutexName[MAX_PATH];
    HANDLE hStartupMutex = NULL;

    if (GetUserStartupMutexName(szMutexName, _countof(szMutexName)))
    {
        hStartupMutex = CreateMutexW(NULL, FALSE, szMutexName);
    }

    DWORD dwWaitResult = WAIT_OBJECT_0;
    if (hStartupMutex)
    {
        dwWaitResult = WaitForSingleObject(hStartupMutex, COLD_START_TIMEOUT_MS);
    }

    /* 3. Check if pipe became available while waiting for mutex (peer proxy started Greenshot) */
    hPipe = ConnectToGreenshotPipe(pszPipeName, 1000);
    if (hPipe != INVALID_HANDLE_VALUE)
    {
        if (hStartupMutex && (dwWaitResult == WAIT_OBJECT_0 || dwWaitResult == WAIT_ABANDONED))
        {
            ReleaseMutex(hStartupMutex);
            CloseHandle(hStartupMutex);
        }
        return hPipe;
    }

    /* 4. Find Greenshot.exe */
    wchar_t szProxyPath[MAX_PATH];
    if (GetModuleFileNameW(NULL, szProxyPath, MAX_PATH) == 0)
    {
        if (hStartupMutex && (dwWaitResult == WAIT_OBJECT_0 || dwWaitResult == WAIT_ABANDONED))
        {
            ReleaseMutex(hStartupMutex);
            CloseHandle(hStartupMutex);
        }
        return INVALID_HANDLE_VALUE;
    }

    wchar_t* pLastSlash = wcsrchr(szProxyPath, L'\\');
    if (!pLastSlash)
    {
        if (hStartupMutex && (dwWaitResult == WAIT_OBJECT_0 || dwWaitResult == WAIT_ABANDONED))
        {
            ReleaseMutex(hStartupMutex);
            CloseHandle(hStartupMutex);
        }
        return INVALID_HANDLE_VALUE;
    }

    *pLastSlash = L'\0';
    wchar_t szGreenshotDir[MAX_PATH];
    StringCchCopyW(szGreenshotDir, MAX_PATH, szProxyPath);

    wchar_t szGreenshotExe[MAX_PATH];
    StringCchPrintfW(szGreenshotExe, MAX_PATH, L"%s\\Greenshot.exe", szGreenshotDir);

    if (GetFileAttributesW(szGreenshotExe) == INVALID_FILE_ATTRIBUTES)
    {
        if (hStartupMutex && (dwWaitResult == WAIT_OBJECT_0 || dwWaitResult == WAIT_ABANDONED))
        {
            ReleaseMutex(hStartupMutex);
            CloseHandle(hStartupMutex);
        }
        return INVALID_HANDLE_VALUE;
    }

    STARTUPINFOW si;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);

    PROCESS_INFORMATION pi;
    ZeroMemory(&pi, sizeof(pi));

    if (!CreateProcessW(
        szGreenshotExe,
        NULL,
        NULL,
        NULL,
        FALSE,
        0,
        NULL,
        szGreenshotDir,
        &si,
        &pi))
    {
        if (hStartupMutex && (dwWaitResult == WAIT_OBJECT_0 || dwWaitResult == WAIT_ABANDONED))
        {
            ReleaseMutex(hStartupMutex);
            CloseHandle(hStartupMutex);
        }
        return INVALID_HANDLE_VALUE;
    }

    CloseHandle(pi.hThread);
    CloseHandle(pi.hProcess);

    /* 5. Connect to the pipe of the newly spawned Greenshot process */
    hPipe = ConnectToGreenshotPipe(pszPipeName, COLD_START_TIMEOUT_MS);

    if (hStartupMutex && (dwWaitResult == WAIT_OBJECT_0 || dwWaitResult == WAIT_ABANDONED))
    {
        ReleaseMutex(hStartupMutex);
        CloseHandle(hStartupMutex);
    }

    return hPipe;
}

/*
 * Finds a JSON string property: "key": "value"
 * Copies and unescapes the value into pwszDest (UTF-16).
 */
static BOOL JsonGetString(const char* json, const char* key, wchar_t* pwszDest, size_t cchDest)
{
    if (!json || !key || !pwszDest || cchDest == 0)
    {
        return FALSE;
    }

    pwszDest[0] = L'\0';

    char pattern[256];
    snprintf(pattern, sizeof(pattern), "\"%s\"", key);

    const char* pKey = strstr(json, pattern);
    if (!pKey)
    {
        return FALSE;
    }

    const char* pColon = strchr(pKey + strlen(pattern), ':');
    if (!pColon)
    {
        return FALSE;
    }

    const char* pStart = pColon + 1;
    while (*pStart == ' ' || *pStart == '\t' || *pStart == '\r' || *pStart == '\n')
    {
        pStart++;
    }

    if (*pStart != '\"')
    {
        return FALSE;
    }
    pStart++; // skip opening quote

    size_t outIdx = 0;
    while (*pStart != '\0' && *pStart != '\"' && outIdx + 1 < cchDest)
    {
        if (*pStart == '\\' && *(pStart + 1) != '\0')
        {
            pStart++;
            switch (*pStart)
            {
            case '\"': pwszDest[outIdx++] = L'\"'; break;
            case '\\': pwszDest[outIdx++] = L'\\'; break;
            case '/':  pwszDest[outIdx++] = L'/'; break;
            case 'b':  pwszDest[outIdx++] = L'\b'; break;
            case 'f':  pwszDest[outIdx++] = L'\f'; break;
            case 'n':  pwszDest[outIdx++] = L'\n'; break;
            case 'r':  pwszDest[outIdx++] = L'\r'; break;
            case 't':  pwszDest[outIdx++] = L'\t'; break;
            case 'u':
                if (strlen(pStart) >= 5)
                {
                    char hex[5] = { pStart[1], pStart[2], pStart[3], pStart[4], '\0' };
                    pwszDest[outIdx++] = (wchar_t)strtol(hex, NULL, 16);
                    pStart += 4;
                }
                break;
            default:
                pwszDest[outIdx++] = (wchar_t)*pStart;
                break;
            }
            pStart++;
        }
        else
        {
            /* Convert UTF-8 multi-byte sequence to wchar_t */
            unsigned char c = (unsigned char)*pStart;
            int seqLen = 1;
            if ((c & 0xE0) == 0xC0) seqLen = 2;
            else if ((c & 0xF0) == 0xE0) seqLen = 3;
            else if ((c & 0xF8) == 0xF0) seqLen = 4;

            wchar_t wch = L'\0';
            MultiByteToWideChar(CP_UTF8, 0, pStart, seqLen, &wch, 1);
            pwszDest[outIdx++] = wch;
            pStart += seqLen;
        }
    }

    pwszDest[outIdx] = L'\0';
    return TRUE;
}

/*
 * Finds an integer property: "key": 123
 */
static BOOL JsonGetInt(const char* json, const char* key, int* pOut)
{
    if (!json || !key || !pOut)
    {
        return FALSE;
    }

    char pattern[256];
    snprintf(pattern, sizeof(pattern), "\"%s\"", key);

    const char* pKey = strstr(json, pattern);
    if (!pKey)
    {
        return FALSE;
    }

    const char* pColon = strchr(pKey + strlen(pattern), ':');
    if (!pColon)
    {
        return FALSE;
    }

    const char* pStart = pColon + 1;
    while (*pStart == ' ' || *pStart == '\t' || *pStart == '\r' || *pStart == '\n')
    {
        pStart++;
    }

    char* pEnd = NULL;
    long val = strtol(pStart, &pEnd, 10);
    if (pEnd == pStart)
    {
        return FALSE;
    }

    *pOut = (int)val;
    return TRUE;
}

static void PrintRecipeList(const char* json)
{
    const char* pRecipes = strstr(json, "\"recipes\"");
    if (!pRecipes)
    {
        PrintStdout(L"No recipes returned.\n");
        return;
    }

    const char* pArrayStart = strchr(pRecipes, '[');
    if (!pArrayStart)
    {
        PrintStdout(L"No recipes returned.\n");
        return;
    }

    PrintStdout(L"Available recipes with CommandlineTrigger:\n\n");

    const char* pCurrent = pArrayStart + 1;
    int count = 0;

    while (*pCurrent != '\0' && *pCurrent != ']')
    {
        const char* pObjStart = strchr(pCurrent, '{');
        if (!pObjStart)
        {
            break;
        }

        const char* pObjEnd = strchr(pObjStart, '}');
        if (!pObjEnd)
        {
            break;
        }

        size_t objLen = (size_t)(pObjEnd - pObjStart + 1);
        char* pObjJson = (char*)malloc(objLen + 1);
        if (pObjJson)
        {
            memcpy(pObjJson, pObjStart, objLen);
            pObjJson[objLen] = '\0';

            wchar_t szCmd[128] = { 0 };
            wchar_t szId[128] = { 0 };
            wchar_t szName[256] = { 0 };
            wchar_t szDesc[512] = { 0 };

            JsonGetString(pObjJson, "command", szCmd, _countof(szCmd));
            JsonGetString(pObjJson, "id", szId, _countof(szId));
            JsonGetString(pObjJson, "name", szName, _countof(szName));
            JsonGetString(pObjJson, "description", szDesc, _countof(szDesc));

            wchar_t line[1024];
            StringCchPrintfW(line, _countof(line), L"  %-20ls %ls (ID: %ls)\n",
                szCmd[0] != L'\0' ? szCmd : szId,
                szDesc[0] != L'\0' ? szDesc : szName,
                szId);
            PrintStdout(line);

            count++;
            free(pObjJson);
        }

        pCurrent = pObjEnd + 1;
    }

    if (count == 0)
    {
        PrintStdout(L"  (No recipes currently configured with active CommandlineTrigger)\n");
    }
    PrintStdout(L"\n");
}

int RunCliOrUrl(LPCWSTR pszPipeName, int argc, wchar_t* argv[])
{
    /* 1. Fast-path offline commands */
    if (argc <= 1)
    {
        PrintUsage();
        return 0;
    }

    LPCWSTR firstArg = argv[1];

    if (_wcsicmp(firstArg, L"--help") == 0 ||
        _wcsicmp(firstArg, L"-h") == 0 ||
        wcscmp(firstArg, L"/?") == 0 ||
        _wcsicmp(firstArg, L"help") == 0)
    {
        PrintUsage();
        return 0;
    }

    if (_wcsicmp(firstArg, L"--version") == 0 ||
        _wcsicmp(firstArg, L"-v") == 0 ||
        _wcsicmp(firstArg, L"version") == 0)
    {
        PrintStdout(VERSION_STRING L"\n");
        return 0;
    }

    /* 2. Formulate JSON payload based on command arguments */
    wchar_t szCwd[MAX_PATH] = { 0 };
    GetCurrentDirectoryW(MAX_PATH, szCwd);
    char szEscapedCwd[MAX_PATH * 4] = { 0 };
    EscapeJsonString(szCwd, szEscapedCwd, sizeof(szEscapedCwd));

    char szJsonEnvelope[65536] = { 0 };
    BOOL isListRecipes = FALSE;

    if (_wcsicmp(firstArg, L"--list-recipes") == 0 ||
        _wcsicmp(firstArg, L"-l") == 0 ||
        _wcsicmp(firstArg, L"list-recipes") == 0)
    {
        isListRecipes = TRUE;
        snprintf(szJsonEnvelope, sizeof(szJsonEnvelope),
            "{\"version\":1,\"source\":\"cli\",\"command\":\"LIST_RECIPES\",\"cwd\":\"%s\"}",
            szEscapedCwd);
    }
    else if (_wcsicmp(firstArg, L"--recipe") == 0 ||
             _wcsicmp(firstArg, L"-r") == 0 ||
             _wcsicmp(firstArg, L"run") == 0)
    {
        if (argc < 3)
        {
            PrintStderr(L"Error: Missing required recipe identifier.\nUsage: greenshot-proxy --recipe <cmd|id> [key=value ...]\n");
            return 1;
        }

        LPCWSTR pwszRecipe = argv[2];
        char szEscapedRecipe[512] = { 0 };
        EscapeJsonString(pwszRecipe, szEscapedRecipe, sizeof(szEscapedRecipe));

        BOOL bAsync = FALSE;
        char szParamsJson[32768] = { 0 };
        size_t paramsIdx = 0;
        szParamsJson[paramsIdx++] = '{';

        BOOL hasParams = FALSE;
        for (int i = 3; i < argc; ++i)
        {
            if (_wcsicmp(argv[i], L"--async") == 0 || _wcsicmp(argv[i], L"--fire-and-forget") == 0)
            {
                bAsync = TRUE;
                continue;
            }

            wchar_t* pEq = wcschr(argv[i], L'=');
            if (pEq)
            {
                *pEq = L'\0';
                LPCWSTR pwszKey = argv[i];
                LPCWSTR pwszVal = pEq + 1;

                char szKey[256] = { 0 };
                char szVal[4096] = { 0 };
                EscapeJsonString(pwszKey, szKey, sizeof(szKey));
                EscapeJsonString(pwszVal, szVal, sizeof(szVal));

                char pairBuf[4608];
                int pairLen = snprintf(pairBuf, sizeof(pairBuf), "%s\"%s\":\"%s\"",
                    hasParams ? "," : "", szKey, szVal);

                if (pairLen > 0 && paramsIdx + pairLen + 2 < sizeof(szParamsJson))
                {
                    memcpy(szParamsJson + paramsIdx, pairBuf, pairLen);
                    paramsIdx += pairLen;
                    hasParams = TRUE;
                }
                *pEq = L'='; // restore
            }
        }
        szParamsJson[paramsIdx++] = '}';
        szParamsJson[paramsIdx] = '\0';

        snprintf(szJsonEnvelope, sizeof(szJsonEnvelope),
            "{\"version\":1,\"source\":\"cli\",\"command\":\"RUN_RECIPE\",\"recipe\":\"%s\",\"async\":%s,\"cwd\":\"%s\",\"parameters\":%s}",
            szEscapedRecipe,
            bAsync ? "true" : "false",
            szEscapedCwd,
            szParamsJson);
    }
    else if (_wcsicmp(firstArg, L"--file") == 0 || _wcsicmp(firstArg, L"-f") == 0)
    {
        if (argc < 3)
        {
            PrintStderr(L"Error: Missing file argument.\nUsage: greenshot-proxy --file <path...>\n");
            return 1;
        }

        char szFilesJson[32768] = { 0 };
        size_t filesIdx = 0;
        szFilesJson[filesIdx++] = '[';

        for (int i = 2; i < argc; ++i)
        {
            char szEscapedPath[4096] = { 0 };
            EscapeJsonString(argv[i], szEscapedPath, sizeof(szEscapedPath));

            char itemBuf[4120];
            int itemLen = snprintf(itemBuf, sizeof(itemBuf), "%s\"%s\"",
                (i > 2) ? "," : "", szEscapedPath);

            if (itemLen > 0 && filesIdx + itemLen + 2 < sizeof(szFilesJson))
            {
                memcpy(szFilesJson + filesIdx, itemBuf, itemLen);
                filesIdx += itemLen;
            }
        }
        szFilesJson[filesIdx++] = ']';
        szFilesJson[filesIdx] = '\0';

        snprintf(szJsonEnvelope, sizeof(szJsonEnvelope),
            "{\"version\":1,\"source\":\"open_with\",\"command\":\"OPEN_FILE\",\"cwd\":\"%s\",\"files\":%s}",
            szEscapedCwd,
            szFilesJson);
    }
    else if (_wcsicmp(firstArg, L"--reload") == 0)
    {
        snprintf(szJsonEnvelope, sizeof(szJsonEnvelope),
            "{\"version\":1,\"source\":\"cli\",\"command\":\"RELOAD_CONFIG\",\"cwd\":\"%s\"}",
            szEscapedCwd);
    }
    else if (_wcsicmp(firstArg, L"--exit") == 0)
    {
        snprintf(szJsonEnvelope, sizeof(szJsonEnvelope),
            "{\"version\":1,\"source\":\"cli\",\"command\":\"EXIT\",\"cwd\":\"%s\"}",
            szEscapedCwd);
    }
    else if (_wcsnicmp(firstArg, L"greenshot:", 10) == 0)
    {
        char szEscapedUrl[4096] = { 0 };
        EscapeJsonString(firstArg, szEscapedUrl, sizeof(szEscapedUrl));

        snprintf(szJsonEnvelope, sizeof(szJsonEnvelope),
            "{\"version\":1,\"source\":\"url_scheme\",\"command\":\"URL_SCHEME\",\"cwd\":\"%s\",\"raw_input\":\"%s\"}",
            szEscapedCwd,
            szEscapedUrl);
    }
    else if (firstArg[0] == L'-')
    {
        wchar_t errMsg[256];
        StringCchPrintfW(errMsg, _countof(errMsg), L"Error: Unrecognized option '%ls'.\nUse 'greenshot-proxy --help' for usage.\n", firstArg);
        PrintStderr(errMsg);
        return 1;
    }
    else
    {
        /* Default: Positional file path(s) */
        char szFilesJson[32768] = { 0 };
        size_t filesIdx = 0;
        szFilesJson[filesIdx++] = '[';

        for (int i = 1; i < argc; ++i)
        {
            char szEscapedPath[4096] = { 0 };
            EscapeJsonString(argv[i], szEscapedPath, sizeof(szEscapedPath));

            char itemBuf[4120];
            int itemLen = snprintf(itemBuf, sizeof(itemBuf), "%s\"%s\"",
                (i > 1) ? "," : "", szEscapedPath);

            if (itemLen > 0 && filesIdx + itemLen + 2 < sizeof(szFilesJson))
            {
                memcpy(szFilesJson + filesIdx, itemBuf, itemLen);
                filesIdx += itemLen;
            }
        }
        szFilesJson[filesIdx++] = ']';
        szFilesJson[filesIdx] = '\0';

        snprintf(szJsonEnvelope, sizeof(szJsonEnvelope),
            "{\"version\":1,\"source\":\"open_with\",\"command\":\"OPEN_FILE\",\"cwd\":\"%s\",\"files\":%s}",
            szEscapedCwd,
            szFilesJson);
    }

    size_t jsonLen = strlen(szJsonEnvelope);
    if (jsonLen == 0 || jsonLen > MAX_PAYLOAD_SIZE)
    {
        PrintStderr(L"Error: Assembled message exceeds maximum payload size.\n");
        return 1;
    }

    /* 3. Connect to Greenshot Named Pipe */
    HANDLE hPipe = ConnectOrColdStart(pszPipeName);

    if (hPipe == INVALID_HANDLE_VALUE)
    {
        PrintStderr(L"Error: Greenshot is not running and cold-start failed.\n");
        return 1;
    }

    /* 4. Write framed length prefix and JSON payload */
    uint32_t payloadLength = (uint32_t)jsonLen;
    if (!WriteExact(hPipe, &payloadLength, sizeof(payloadLength)) ||
        !WriteExact(hPipe, szJsonEnvelope, payloadLength))
    {
        PrintStderr(L"Error: Failed to write command to Greenshot pipe.\n");
        CloseHandle(hPipe);
        return 1;
    }

    FlushFileBuffers(hPipe);

    /* 5. Read framed JSON response */
    uint32_t respLen = 0;
    if (!ReadExact(hPipe, &respLen, sizeof(respLen)))
    {
        /* Greenshot closed pipe without response */
        CloseHandle(hPipe);
        return 0;
    }

    if (respLen < MIN_PAYLOAD_SIZE || respLen > MAX_PAYLOAD_SIZE)
    {
        PrintStderr(L"Error: Received invalid response size from Greenshot.\n");
        CloseHandle(hPipe);
        return 1;
    }

    char* pRespBuffer = (char*)malloc(respLen + 1);
    if (!pRespBuffer)
    {
        PrintStderr(L"Error: Memory allocation failed for response buffer.\n");
        CloseHandle(hPipe);
        return 1;
    }

    if (!ReadExact(hPipe, pRespBuffer, respLen))
    {
        PrintStderr(L"Error: Failed to read complete response from Greenshot.\n");
        free(pRespBuffer);
        CloseHandle(hPipe);
        return 1;
    }

    pRespBuffer[respLen] = '\0';
    CloseHandle(hPipe);

    /* 6. Parse and render response */
    int exitCode = 0;
    JsonGetInt(pRespBuffer, "exit_code", &exitCode);

    wchar_t szStatus[64] = { 0 };
    JsonGetString(pRespBuffer, "status", szStatus, _countof(szStatus));

    if (exitCode == 0 && _wcsicmp(szStatus, L"error") == 0)
    {
        exitCode = 1;
    }

    if (isListRecipes)
    {
        PrintRecipeList(pRespBuffer);
    }
    else
    {
        wchar_t szStdout[32768] = { 0 };
        wchar_t szStderr[32768] = { 0 };

        if (JsonGetString(pRespBuffer, "stdout", szStdout, _countof(szStdout)) && szStdout[0] != L'\0')
        {
            PrintStdout(szStdout);
            PrintStdout(L"\n");
        }

        if (JsonGetString(pRespBuffer, "stderr", szStderr, _countof(szStderr)) && szStderr[0] != L'\0')
        {
            PrintStderr(szStderr);
            PrintStderr(L"\n");
        }
    }

    free(pRespBuffer);
    return exitCode;
}
