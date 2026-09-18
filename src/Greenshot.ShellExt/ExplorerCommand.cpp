#include "ExplorerCommand.h"
#include <shlwapi.h>
#include <string>

extern long g_cRefModule;

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

IFACEMETHODIMP CExplorerCommand::GetTitle(IShellItemArray* psiItemArray, LPWSTR* ppszName)
{
    return SHStrDupW(L"Edit with Greenshot", ppszName);
}

IFACEMETHODIMP CExplorerCommand::GetIcon(IShellItemArray* psiItemArray, LPWSTR* ppszIcon)
{
    std::wstring path = L"Greenshot.exe,0";

    HKEY hKey;
    if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Greenshot", 0, KEY_READ | KEY_WOW64_32KEY, &hKey) == ERROR_SUCCESS ||
        RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Greenshot", 0, KEY_READ | KEY_WOW64_64KEY, &hKey) == ERROR_SUCCESS ||
        RegOpenKeyExW(HKEY_CURRENT_USER, L"SOFTWARE\\Greenshot", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
    {
        WCHAR szPath[MAX_PATH];
        DWORD cbData = sizeof(szPath);
        if (RegQueryValueExW(hKey, L"InstallDir", NULL, NULL, (LPBYTE)szPath, &cbData) == ERROR_SUCCESS)
        {
            path = std::wstring(szPath) + L"\\Greenshot.exe,0";
        }
        RegCloseKey(hKey);
    }

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

    std::wstring exePath = L"Greenshot.exe";
    HKEY hKey;
    if (RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Greenshot", 0, KEY_READ | KEY_WOW64_32KEY, &hKey) == ERROR_SUCCESS ||
        RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Greenshot", 0, KEY_READ | KEY_WOW64_64KEY, &hKey) == ERROR_SUCCESS ||
        RegOpenKeyExW(HKEY_CURRENT_USER, L"SOFTWARE\\Greenshot", 0, KEY_READ, &hKey) == ERROR_SUCCESS)
    {
        WCHAR szPath[MAX_PATH];
        DWORD cbData = sizeof(szPath);
        if (RegQueryValueExW(hKey, L"InstallDir", NULL, NULL, (LPBYTE)szPath, &cbData) == ERROR_SUCCESS)
        {
            exePath = std::wstring(szPath) + L"\\Greenshot.exe";
        }
        RegCloseKey(hKey);
    }

    for (DWORD i = 0; i < count; i++)
    {
        IShellItem* psi;
        if (SUCCEEDED(psiItemArray->GetItemAt(i, &psi)))
        {
            LPWSTR pszName;
            if (SUCCEEDED(psi->GetDisplayName(SIGDN_FILESYSPATH, &pszName)))
            {
                std::wstring args = L"\"" + exePath + L"\" --openfile \"" + pszName + L"\"";
                
                STARTUPINFOW si = { sizeof(si) };
                PROCESS_INFORMATION pi;
                if (CreateProcessW(
                    exePath.c_str(),
                    &args[0],
                    NULL,
                    NULL,
                    FALSE,
                    0,
                    NULL,
                    NULL,
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
