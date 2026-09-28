/*
 * greenshot-proxy.exe - browser and Windows shell integration for Greenshot (/SUBSYSTEM:WINDOWS, no console).
 *
 * Started by:
 *   - the browser (Native Messaging): relays frames between the extension and Greenshot,
 *   - the greenshot: URL protocol:     forwards the URL (connection source "url_scheme"),
 *   - Explorer (file association / Open with): forwards the file arguments (connection source "open_with").
 * The source decides what Greenshot allows; it is determined here, from how this process was started, and cannot
 * be chosen by the data that is passed on.
 */
#include "common.h"
#include "extension_relay.h"
#include "pipe_utils.h"
#include "protocol.h"
#include "rt.h"

/*
 * Detects a Native Messaging launch by the browser. Only the exact positions browsers use are checked:
 *   Chrome / Edge: greenshot-proxy.exe chrome-extension://<id>/ [--parent-window=<hwnd>]
 *   Firefox:       greenshot-proxy.exe <path-to-host-manifest.json> <extension-id>
 */
static BOOL IsExtensionInvocation(int argc, LPWSTR* argv)
{
    if (argc < 2)
    {
        return FALSE;
    }
    if (RtStartsWithW(argv[1], L"chrome-extension://", FALSE) ||
        RtStartsWithW(argv[1], L"moz-extension://", FALSE) ||
        RtStartsWithW(argv[1], L"extension://", FALSE) ||
        RtEqualsIgnoreCaseW(argv[1], L"--native-messaging"))
    {
        return TRUE;
    }
    /* Firefox: exactly two arguments, the host manifest (.json) followed by the extension id */
    return argc == 3 && RtEndsWithIgnoreCaseW(argv[1], L".json") &&
           (RtContainsCharW(argv[2], L'@') || argv[2][0] == L'{');
}

/* The calling extension as passed by the browser: Chrome/Edge pass the origin first, Firefox the extension id second */
static LPCWSTR GetExtensionOrigin(int argc, LPWSTR* argv)
{
    if (argc == 3 && RtEndsWithIgnoreCaseW(argv[1], L".json"))
    {
        return argv[2];
    }
    return (argc >= 2 && RtStartsWithW(argv[1], L"chrome-extension://", FALSE)) ||
           (argc >= 2 && RtStartsWithW(argv[1], L"moz-extension://", FALSE)) ||
           (argc >= 2 && RtStartsWithW(argv[1], L"extension://", FALSE))
        ? argv[1]
        : NULL;
}

static int RunProxy(int argc, LPWSTR* argv)
{
    LPWSTR pwszPipeName = GetGreenshotPipeName();
    if (!pwszPipeName)
    {
        return PROXY_EXIT_FAILURE;
    }

    int exitCode;
    if (IsExtensionInvocation(argc, argv))
    {
        exitCode = RunExtensionRelay(pwszPipeName, GetExtensionOrigin(argc, argv));
    }
    else if (argc < 2)
    {
        exitCode = PROXY_EXIT_OK;
    }
    else
    {
        const char* pszSource = RtStartsWithW(argv[1], L"greenshot:", TRUE) ? IPC_SOURCE_URL_SCHEME : IPC_SOURCE_OPEN_WITH;
        HANDLE hPipe = ConnectOrColdStart(pwszPipeName);
        if (hPipe == INVALID_HANDLE_VALUE)
        {
            exitCode = PROXY_EXIT_UNAVAILABLE;
        }
        else
        {
            exitCode = SendHello(hPipe, FALSE, pszSource, IPC_REPLY_TEXT, NULL) && SendCliRequest(hPipe, argc, argv)
                ? ReceiveTextReply(hPipe, FALSE)
                : PROXY_EXIT_FAILURE;
            CloseHandle(hPipe);
        }
    }

    RtFree(pwszPipeName);
    return exitCode;
}

/* Entry point (no C runtime): see the EntryPointSymbol in greenshot-proxy.vcxproj */
void WINAPI ProxyEntry(void)
{
    int argc = 0;
    LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    int exitCode = argv ? RunProxy(argc, argv) : PROXY_EXIT_FAILURE;
    if (argv)
    {
        LocalFree(argv);
    }
    ExitProcess((UINT)exitCode);
}
