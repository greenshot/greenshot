#include "pipe_utils.h"

static BOOL GetUserSidString(LPWSTR pszSidOut, DWORD cchSidOut)
{
    if (!pszSidOut || cchSidOut == 0)
    {
        return FALSE;
    }

    HANDLE hToken = NULL;
    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken))
    {
        return FALSE;
    }

    DWORD dwSize = 0;
    GetTokenInformation(hToken, TokenUser, NULL, 0, &dwSize);
    if (GetLastError() != ERROR_INSUFFICIENT_BUFFER || dwSize == 0)
    {
        CloseHandle(hToken);
        return FALSE;
    }

    PTOKEN_USER pTokenUser = (PTOKEN_USER)malloc(dwSize);
    if (!pTokenUser)
    {
        CloseHandle(hToken);
        return FALSE;
    }

    BOOL bSuccess = FALSE;
    if (GetTokenInformation(hToken, TokenUser, pTokenUser, dwSize, &dwSize))
    {
        LPWSTR pszSid = NULL;
        if (ConvertSidToStringSidW(pTokenUser->User.Sid, &pszSid))
        {
            HRESULT hr = StringCchCopyW(pszSidOut, cchSidOut, pszSid);
            bSuccess = SUCCEEDED(hr);
            LocalFree(pszSid);
        }
    }

    free(pTokenUser);
    CloseHandle(hToken);
    return bSuccess;
}

BOOL GetUserSidPipeName(LPWSTR pszPipeName, DWORD cchPipeName)
{
    wchar_t szSid[128] = { 0 };
    if (!GetUserSidString(szSid, _countof(szSid)))
    {
        return FALSE;
    }
    return SUCCEEDED(StringCchPrintfW(pszPipeName, cchPipeName, L"%s%s", PIPE_PREFIX, szSid));
}

BOOL GetUserStartupMutexName(LPWSTR pszMutexName, DWORD cchMutexName)
{
    wchar_t szSid[128] = { 0 };
    if (!GetUserSidString(szSid, _countof(szSid)))
    {
        return FALSE;
    }
    return SUCCEEDED(StringCchPrintfW(pszMutexName, cchMutexName, L"Local\\Greenshot_Startup_%s", szSid));
}

HANDLE ConnectToGreenshotPipe(LPCWSTR pszPipeName, DWORD dwTimeoutMs)
{
    return ConnectToGreenshotPipeEx(pszPipeName, dwTimeoutMs, 0);
}

HANDLE ConnectToGreenshotPipeEx(LPCWSTR pszPipeName, DWORD dwTimeoutMs, DWORD dwFlagsAndAttributes)
{
    if (!pszPipeName)
    {
        return INVALID_HANDLE_VALUE;
    }

    DWORD dwStart = GetTickCount();

    while (TRUE)
    {
        HANDLE hPipe = CreateFileW(
            pszPipeName,
            GENERIC_READ | GENERIC_WRITE,
            0,              /* no sharing */
            NULL,           /* default security */
            OPEN_EXISTING,
            dwFlagsAndAttributes,
            NULL);

        if (hPipe != INVALID_HANDLE_VALUE)
        {
            return hPipe;
        }

        if (dwTimeoutMs == 0)
        {
            return INVALID_HANDLE_VALUE;
        }

        DWORD dwErr = GetLastError();
        DWORD dwElapsed = GetTickCount() - dwStart;
        if (dwElapsed >= dwTimeoutMs)
        {
            return INVALID_HANDLE_VALUE;
        }

        DWORD dwRemaining = dwTimeoutMs - dwElapsed;

        if (dwErr == ERROR_PIPE_BUSY)
        {
            WaitNamedPipeW(pszPipeName, dwRemaining > 200 ? 200 : dwRemaining);
        }
        else if (dwErr == ERROR_FILE_NOT_FOUND)
        {
            Sleep(dwRemaining > 100 ? 100 : dwRemaining);
        }
        else
        {
            return INVALID_HANDLE_VALUE;
        }
    }
}

