#include "pipe_utils.h"
#include "rt.h"

/* Returns the current user's SID as string (LocalAlloc'ed, free with LocalFree), or NULL */
static LPWSTR GetUserSidString(void)
{
    HANDLE hToken = NULL;
    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken))
    {
        return NULL;
    }

    DWORD cbSize = 0;
    GetTokenInformation(hToken, TokenUser, NULL, 0, &cbSize);
    if (GetLastError() != ERROR_INSUFFICIENT_BUFFER || cbSize == 0)
    {
        CloseHandle(hToken);
        return NULL;
    }

    PTOKEN_USER pTokenUser = (PTOKEN_USER)RtAlloc(cbSize);
    if (!pTokenUser)
    {
        CloseHandle(hToken);
        return NULL;
    }

    LPWSTR pwszSid = NULL;
    if (!GetTokenInformation(hToken, TokenUser, pTokenUser, cbSize, &cbSize) ||
        !ConvertSidToStringSidW(pTokenUser->User.Sid, &pwszSid))
    {
        pwszSid = NULL;
    }

    RtFree(pTokenUser);
    CloseHandle(hToken);
    return pwszSid;
}

static LPWSTR BuildUserScopedName(LPCWSTR pwszPrefix)
{
    LPWSTR pwszSid = GetUserSidString();
    if (!pwszSid)
    {
        return NULL;
    }
    LPWSTR pwszName = RtConcatW(pwszPrefix, pwszSid);
    LocalFree(pwszSid);
    return pwszName;
}

LPWSTR GetGreenshotPipeName(void)
{
    return BuildUserScopedName(PIPE_PREFIX);
}

HANDLE ConnectToGreenshotPipe(LPCWSTR pwszPipeName, DWORD dwTimeoutMs, DWORD dwFlagsAndAttributes)
{
    if (!pwszPipeName)
    {
        return INVALID_HANDLE_VALUE;
    }

    DWORD dwStart = GetTickCount();
    for (;;)
    {
        HANDLE hPipe = CreateFileW(pwszPipeName, GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, dwFlagsAndAttributes, NULL);
        if (hPipe != INVALID_HANDLE_VALUE)
        {
            return hPipe;
        }

        DWORD dwError = GetLastError();
        DWORD dwElapsed = GetTickCount() - dwStart;
        if (dwTimeoutMs == 0 || dwElapsed >= dwTimeoutMs)
        {
            return INVALID_HANDLE_VALUE;
        }

        DWORD dwRemaining = dwTimeoutMs - dwElapsed;
        if (dwError == ERROR_PIPE_BUSY)
        {
            WaitNamedPipeW(pwszPipeName, dwRemaining > 200 ? 200 : dwRemaining);
        }
        else if (dwError == ERROR_FILE_NOT_FOUND)
        {
            Sleep(dwRemaining > 100 ? 100 : dwRemaining);
        }
        else
        {
            return INVALID_HANDLE_VALUE;
        }
    }
}

/* Starts Greenshot.exe located next to this executable */
static BOOL StartGreenshot(void)
{
    const DWORD cchPath = 32768;
    LPWSTR pwszPath = (LPWSTR)RtAlloc(cchPath * sizeof(WCHAR));
    if (!pwszPath)
    {
        return FALSE;
    }

    BOOL bStarted = FALSE;
    DWORD cchModule = GetModuleFileNameW(NULL, pwszPath, cchPath);
    if (cchModule > 0 && cchModule < cchPath)
    {
        /* Cut off the file name, keep the directory */
        DWORD i = cchModule;
        while (i > 0 && pwszPath[i - 1] != L'\\')
        {
            i--;
        }
        if (i > 0)
        {
            pwszPath[i - 1] = L'\0';
            LPWSTR pwszExe = RtConcatW(pwszPath, L"\\Greenshot.exe");
            if (pwszExe && GetFileAttributesW(pwszExe) != INVALID_FILE_ATTRIBUTES)
            {
                STARTUPINFOW startupInfo;
                PROCESS_INFORMATION processInfo;
                memset(&startupInfo, 0, sizeof(startupInfo));
                memset(&processInfo, 0, sizeof(processInfo));
                startupInfo.cb = sizeof(startupInfo);

                if (CreateProcessW(pwszExe, NULL, NULL, NULL, FALSE, 0, NULL, pwszPath, &startupInfo, &processInfo))
                {
                    CloseHandle(processInfo.hThread);
                    CloseHandle(processInfo.hProcess);
                    bStarted = TRUE;
                }
            }
            RtFree(pwszExe);
        }
    }

    RtFree(pwszPath);
    return bStarted;
}

HANDLE ConnectOrColdStart(LPCWSTR pwszPipeName)
{
    /* 1. Fast path: Greenshot is already running */
    HANDLE hPipe = ConnectToGreenshotPipe(pwszPipeName, 0, 0);
    if (hPipe != INVALID_HANDLE_VALUE)
    {
        return hPipe;
    }

    /* 2. Serialize concurrent cold starts (several proxies started at the same time) */
    HANDLE hMutex = NULL;
    BOOL bOwnsMutex = FALSE;
    LPWSTR pwszMutexName = BuildUserScopedName(STARTUP_MUTEX_PREFIX);
    if (pwszMutexName)
    {
        hMutex = CreateMutexW(NULL, FALSE, pwszMutexName);
        RtFree(pwszMutexName);
    }
    if (hMutex)
    {
        DWORD dwWait = WaitForSingleObject(hMutex, COLD_START_TIMEOUT_MS);
        bOwnsMutex = (dwWait == WAIT_OBJECT_0 || dwWait == WAIT_ABANDONED);
    }

    /* 3. A peer proxy may have started Greenshot while we waited */
    hPipe = ConnectToGreenshotPipe(pwszPipeName, 1000, 0);

    /* 4. Start Greenshot and wait for its pipe */
    if (hPipe == INVALID_HANDLE_VALUE && StartGreenshot())
    {
        hPipe = ConnectToGreenshotPipe(pwszPipeName, COLD_START_TIMEOUT_MS, 0);
    }

    if (hMutex)
    {
        if (bOwnsMutex)
        {
            ReleaseMutex(hMutex);
        }
        CloseHandle(hMutex);
    }
    return hPipe;
}
