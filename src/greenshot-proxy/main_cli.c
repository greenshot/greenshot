/*
 * greenshot.com - console command line interface for Greenshot (/SUBSYSTEM:CONSOLE).
 *
 * Forwards the command line unchanged to Greenshot (connection source "cli", text replies) and prints what comes
 * back. Argument parsing, validation, output formatting and --json are all done by Greenshot; only --help and
 * --version are answered here, so they work without Greenshot running.
 */
#include "common.h"
#include "pipe_utils.h"
#include "protocol.h"
#include "rt.h"

static const char Usage[] =
    "Greenshot Proxy CLI\n\n"
    "Usage:\n"
    "  greenshot [options]\n"
    "  greenshot <file...>\n\n"
    "Options:\n"
    "  --list-recipes, -l [--json]             List all recipes configured with a CommandlineTrigger\n"
    "  --info, --describe, -i <id> [--json]    Describe the contract & variables of a recipe\n"
    "  --recipe, -r <cmd|id> [arguments]       Execute a recipe by command identifier or recipe ID\n"
    "  --file, -f <file...>                    Open one or more files using configured OpenFile triggers\n"
    "  --reload                                Reload Greenshot configuration\n"
    "  --exit                                  Exit running Greenshot instance\n"
    "  --version, -v                           Show version\n"
    "  --help, -h                              Show this help text\n\n"
    "Recipe arguments (after --recipe <cmd|id>):\n"
    "  key=value, --key=value, --key value     Pass an argument to the recipe (the value is taken as-is,\n"
    "                                          also when it starts with '-')\n"
    "  --query, -q <expr>                      Print the value of an expression after the recipe finished,\n"
    "                                          instead of the recipe's own output\n"
    "  --json                                  Print the result (status, exit code, output, variables) as JSON\n"
    "  --async, --fire-and-forget              Start the recipe and return immediately\n"
    "  --                                      Stop option parsing; remaining arguments must be key=value\n\n"
    "  Example: greenshot --recipe ocr destination=clipboard\n"
    "  Example: greenshot -r qr --file=invoice.png --query \"${Barcode.Text}\"\n\n"
    "Exit codes:\n"
    "  0 success, 1 failure, 2 invalid command line, 3 Greenshot not available,\n"
    "  or the exit code set by the recipe (Stderr step).\n";

/* Reports an error of the executable itself; with --json, stdout also gets a JSON result document */
static int Fail(BOOL bJson, int exitCode, const char* pszMessage)
{
    RtWriteString(STD_ERROR_HANDLE, pszMessage);
    RtWriteString(STD_ERROR_HANDLE, "\n");
    if (bJson)
    {
        char szExitCode[2] = { (char)('0' + (exitCode % 10)), '\0' };
        RtWriteString(STD_OUTPUT_HANDLE, "{\"status\":\"error\",\"exit_code\":");
        RtWriteString(STD_OUTPUT_HANDLE, szExitCode);
        RtWriteString(STD_OUTPUT_HANDLE, ",\"stderr\":\"");
        RtWriteString(STD_OUTPUT_HANDLE, pszMessage); /* constant messages without characters that need escaping */
        RtWriteString(STD_OUTPUT_HANDLE, "\"}\n");
    }
    return exitCode;
}

static int RunCli(int argc, LPWSTR* argv)
{
    if (argc <= 1 ||
        RtEqualsIgnoreCaseW(argv[1], L"--help") || RtEqualsIgnoreCaseW(argv[1], L"-h") ||
        RtEqualsIgnoreCaseW(argv[1], L"/?") || RtEqualsIgnoreCaseW(argv[1], L"help"))
    {
        RtWriteString(STD_OUTPUT_HANDLE, Usage);
        return PROXY_EXIT_OK;
    }
    if (RtEqualsIgnoreCaseW(argv[1], L"--version") || RtEqualsIgnoreCaseW(argv[1], L"-v") ||
        RtEqualsIgnoreCaseW(argv[1], L"version"))
    {
        RtWriteString(STD_OUTPUT_HANDLE, "Greenshot Proxy " PROXY_CLIENT_VERSION "\n");
        return PROXY_EXIT_OK;
    }

    BOOL bJson = FALSE;
    for (int i = 1; i < argc; ++i)
    {
        if (RtEqualsIgnoreCaseW(argv[i], L"--json"))
        {
            bJson = TRUE;
        }
    }

    LPWSTR pwszPipeName = GetGreenshotPipeName();
    if (!pwszPipeName)
    {
        return Fail(bJson, PROXY_EXIT_FAILURE, "Error: could not determine the Greenshot pipe name.");
    }

    HANDLE hPipe = ConnectOrColdStart(pwszPipeName);
    RtFree(pwszPipeName);
    if (hPipe == INVALID_HANDLE_VALUE)
    {
        return Fail(bJson, PROXY_EXIT_UNAVAILABLE, "Error: Greenshot is not running and could not be started.");
    }

    int exitCode;
    if (!SendHello(hPipe, FALSE, IPC_SOURCE_CLI, IPC_REPLY_TEXT, NULL) || !SendCliRequest(hPipe, argc, argv))
    {
        exitCode = Fail(bJson, PROXY_EXIT_FAILURE, "Error: failed to send the command to Greenshot.");
    }
    else
    {
        exitCode = ReceiveTextReply(hPipe, TRUE);
    }

    CloseHandle(hPipe);
    return exitCode;
}

/* Entry point (no C runtime): see the EntryPointSymbol in greenshot-cli.vcxproj */
void WINAPI ProxyEntry(void)
{
    int argc = 0;
    LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    int exitCode = argv ? RunCli(argc, argv) : PROXY_EXIT_FAILURE;
    if (argv)
    {
        LocalFree(argv);
    }
    ExitProcess((UINT)exitCode);
}
