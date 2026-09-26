#include "common.h"
#include "pipe_utils.h"
#include "extension_relay.h"
#include "cli_launcher.h"

static BOOL IsExtensionInvocation(int argc, wchar_t* argv[])
{
    if (argc < 2)
    {
        return FALSE;
    }

    for (int i = 1; i < argc; ++i)
    {
        LPCWSTR arg = argv[i];

        if (wcsncmp(arg, L"chrome-extension://", 19) == 0 ||
            wcsncmp(arg, L"moz-extension://", 16) == 0 ||
            wcsncmp(arg, L"extension://", 12) == 0 ||
            wcscmp(arg, L"--native-messaging") == 0)
        {
            return TRUE;
        }

        /* Check for Firefox pattern: manifest path + extension id with @ */
        if (wcsstr(arg, L"@") != NULL && argc >= 3)
        {
            return TRUE;
        }
    }

    return FALSE;
}

static int RunMain(int argc, wchar_t* argv[])
{
    wchar_t szPipeName[MAX_PATH];
    if (!GetUserSidPipeName(szPipeName, _countof(szPipeName)))
    {
        return 1;
    }

    if (IsExtensionInvocation(argc, argv))
    {
        return RunExtensionRelay(szPipeName);
    }
    else
    {
        return RunCliOrUrl(szPipeName, argc, argv);
    }
}

int wmain(int argc, wchar_t* argv[])
{
    return RunMain(argc, argv);
}

int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, PWSTR pCmdLine, int nCmdShow)
{
    UNREFERENCED_PARAMETER(hInstance);
    UNREFERENCED_PARAMETER(hPrevInstance);
    UNREFERENCED_PARAMETER(pCmdLine);
    UNREFERENCED_PARAMETER(nCmdShow);

    HANDLE hStdIn = GetStdHandle(STD_INPUT_HANDLE);
    HANDLE hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
    HANDLE hStdErr = GetStdHandle(STD_ERROR_HANDLE);

    BOOL needAttach = (hStdOut == NULL || hStdOut == INVALID_HANDLE_VALUE ||
                       hStdErr == NULL || hStdErr == INVALID_HANDLE_VALUE ||
                       hStdIn == NULL || hStdIn == INVALID_HANDLE_VALUE);

    if (needAttach)
    {
        if (AttachConsole(ATTACH_PARENT_PROCESS))
        {
            if (hStdOut == NULL || hStdOut == INVALID_HANDLE_VALUE)
            {
                HANDLE hConOut = CreateFileW(L"CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
                if (hConOut != INVALID_HANDLE_VALUE)
                {
                    SetStdHandle(STD_OUTPUT_HANDLE, hConOut);
                }
            }

            if (hStdErr == NULL || hStdErr == INVALID_HANDLE_VALUE)
            {
                HANDLE hConErr = CreateFileW(L"CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
                if (hConErr != INVALID_HANDLE_VALUE)
                {
                    SetStdHandle(STD_ERROR_HANDLE, hConErr);
                }
            }

            if (hStdIn == NULL || hStdIn == INVALID_HANDLE_VALUE)
            {
                HANDLE hConIn = CreateFileW(L"CONIN$", GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
                if (hConIn != INVALID_HANDLE_VALUE)
                {
                    SetStdHandle(STD_INPUT_HANDLE, hConIn);
                }
            }
        }
    }

    return RunMain(__argc, __wargv);
}
