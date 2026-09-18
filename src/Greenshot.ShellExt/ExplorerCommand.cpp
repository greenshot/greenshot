#include "ExplorerCommand.h"
#include <shlwapi.h>
#include <string>
#include <vector>

extern long g_cRefModule;

static std::wstring EscapeForQuotedCommandLineArgument(const std::wstring& argument)
{
    std::wstring escaped;
    escaped.reserve(argument.size());

    size_t backslashCount = 0;
    for (wchar_t ch : argument)
    {
        if (ch == L'\\')
        {
            ++backslashCount;
            continue;
        }

        if (ch == L'"')
        {
            escaped.append(backslashCount * 2 + 1, L'\\');
            escaped.push_back(L'"');
            backslashCount = 0;
            continue;
        }

        if (backslashCount > 0)
        {
            escaped.append(backslashCount, L'\\');
            backslashCount = 0;
        }

        escaped.push_back(ch);
    }

    if (backslashCount > 0)
    {
        escaped.append(backslashCount * 2, L'\\');
    }

    return escaped;
}

CExplorerCommand::CExplorerCommand() : _cRef(1)
{
    InterlockedIncrement(&g_cRefModule);
}

CExplorerCommand::~CExplorerCommand()
{
    InterlockedDecrement(&g_cRefModule);
}

IFACEMETHODIMP CExplorerCommand::QueryInterface(REFIID riid, void** ppv)
{
    static const QITAB qit[] = {
        QITABENT(CExplorerCommand, IExplorerCommand),
        { 0 },
    };
    return QISearch(this, qit, riid, ppv);
}

IFACEMETHODIMP_(ULONG) CExplorerCommand::AddRef()
{
    return InterlockedIncrement(&_cRef);
}

IFACEMETHODIMP_(ULONG) CExplorerCommand::Release()
{
    long cRef = InterlockedDecrement(&_cRef);
    if (cRef == 0)
    {
        delete this;
    }
    return cRef;
}

extern HINSTANCE g_hinst;

IFACEMETHODIMP CExplorerCommand::GetTitle(IShellItemArray* psiItemArray, LPWSTR* ppszName)
{
    static std::wstring cachedTitle;

    if (cachedTitle.empty())
    {
        cachedTitle = L"Edit with Greenshot"; // Fallback

        WCHAR szModule[MAX_PATH];
        if (GetModuleFileNameW(g_hinst, szModule, MAX_PATH))
        {
            PathRemoveFileSpecW(szModule);
            
            WCHAR szLocale[LOCALE_NAME_MAX_LENGTH];
            if (GetUserDefaultLocaleName(szLocale, LOCALE_NAME_MAX_LENGTH))
            {
                std::wstring langFile = std::wstring(szModule) + L"\\Languages\\language-" + szLocale + L".xml";
                
                // If specific locale file doesn't exist, try language only (e.g. pt-BR -> pt)
                if (GetFileAttributesW(langFile.c_str()) == INVALID_FILE_ATTRIBUTES)
                {
                    std::wstring localeStr(szLocale);
                    size_t dash = localeStr.find(L'-');
                    if (dash != std::wstring::npos)
                    {
                        langFile = std::wstring(szModule) + L"\\Languages\\language-" + localeStr.substr(0, dash) + L".xml";
                    }
                }
                
                // If still doesn't exist, try en-US
                if (GetFileAttributesW(langFile.c_str()) == INVALID_FILE_ATTRIBUTES)
                {
                    langFile = std::wstring(szModule) + L"\\Languages\\language-en-US.xml";
                }

                FILE* f = NULL;
                if (_wfopen_s(&f, langFile.c_str(), L"r, ccs=UTF-8") == 0 && f)
                {
                    WCHAR line[1024];
                    while (fgetws(line, 1024, f))
                    {
                        std::wstring wstr(line);
                        size_t pos = wstr.find(L"name=\"shellext_edit\">");
                        if (pos != std::wstring::npos)
                        {
                            pos += 21; // length of name="shellext_edit">
                            size_t endpos = wstr.find(L"</resource>", pos);
                            if (endpos != std::wstring::npos)
                            {
                                cachedTitle = wstr.substr(pos, endpos - pos);
                                break;
                            }
                        }
                    }
                    fclose(f);
                }
            }
        }
    }

    return SHStrDupW(cachedTitle.c_str(), ppszName);
}

IFACEMETHODIMP CExplorerCommand::GetIcon(IShellItemArray* psiItemArray, LPWSTR* ppszIcon)
{
    WCHAR szModule[MAX_PATH];
    GetModuleFileNameW(g_hinst, szModule, MAX_PATH);
    PathRemoveFileSpecW(szModule);
    std::wstring path = std::wstring(szModule) + L"\\Greenshot.exe,0";

    return SHStrDupW(path.c_str(), ppszIcon);
}

IFACEMETHODIMP CExplorerCommand::GetToolTip(IShellItemArray* psiItemArray, LPWSTR* ppszInfotip)
{
    *ppszInfotip = NULL;
    return E_NOTIMPL;
}

IFACEMETHODIMP CExplorerCommand::GetCanonicalName(GUID* pguidCommandName)
{
    *pguidCommandName = GUID_NULL;
    return S_OK;
}

IFACEMETHODIMP CExplorerCommand::GetState(IShellItemArray* psiItemArray, BOOL fOkToBeSlow, EXPCMDSTATE* pCmdState)
{
    *pCmdState = ECS_ENABLED;
    return S_OK;
}

IFACEMETHODIMP CExplorerCommand::Invoke(IShellItemArray* psiItemArray, IBindCtx* pbc)
{
    if (!psiItemArray) return E_INVALIDARG;

    DWORD count;
    psiItemArray->GetCount(&count);

    WCHAR szModule[MAX_PATH];
    GetModuleFileNameW(g_hinst, szModule, MAX_PATH);
    PathRemoveFileSpecW(szModule);
    std::wstring exePath = std::wstring(szModule) + L"\\Greenshot.exe";

    for (DWORD i = 0; i < count; i++)
    {
        IShellItem* psi;
        if (SUCCEEDED(psiItemArray->GetItemAt(i, &psi)))
        {
            LPWSTR pszName;
            if (SUCCEEDED(psi->GetDisplayName(SIGDN_FILESYSPATH, &pszName)))
            {
                std::wstring args = L"\"" + EscapeForQuotedCommandLineArgument(exePath) + L"\" \"" + EscapeForQuotedCommandLineArgument(pszName) + L"\"";
                
                // CreateProcessW requires a modifiable buffer for the command line
                std::vector<wchar_t> cmdLine(args.begin(), args.end());
                cmdLine.push_back(L'\0');
                
                WCHAR szDir[MAX_PATH];
                wcscpy_s(szDir, exePath.c_str());
                PathRemoveFileSpecW(szDir);
                
                STARTUPINFOW si = { sizeof(si) };
                PROCESS_INFORMATION pi;
                if (CreateProcessW(
                    exePath.c_str(),
                    cmdLine.data(),
                    NULL,
                    NULL,
                    FALSE,
                    0,
                    NULL,
                    szDir,
                    &si,
                    &pi))
                {
                    CloseHandle(pi.hProcess);
                    CloseHandle(pi.hThread);
                }

                CoTaskMemFree(pszName);
            }
            psi->Release();
        }
    }

    return S_OK;
}

IFACEMETHODIMP CExplorerCommand::GetFlags(EXPCMDFLAGS* pFlags)
{
    *pFlags = ECF_DEFAULT;
    return S_OK;
}

IFACEMETHODIMP CExplorerCommand::EnumSubCommands(IEnumExplorerCommand** ppEnum)
{
    *ppEnum = NULL;
    return E_NOTIMPL;
}
