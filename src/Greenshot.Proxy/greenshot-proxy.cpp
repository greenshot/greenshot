#include <windows.h>
#include <shobjidl.h>
#include <shlwapi.h>
#include <sddl.h>
#include <string>
#include <vector>
#include <new>

#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "advapi32.lib")
#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "ole32.lib")

// --- IPC & Launch Helpers ---

std::wstring GetGreenshotInstallDir()
{
    HKEY hKey = NULL;
    const wchar_t* subkey = L"Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Greenshot_is1";
    HKEY roots[] = { HKEY_CURRENT_USER, HKEY_LOCAL_MACHINE };
    for (HKEY root : roots)
    {
        if (RegOpenKeyExW(root, subkey, 0, KEY_READ, &hKey) == ERROR_SUCCESS)
        {
            WCHAR szPath[MAX_PATH];
            DWORD cbData = sizeof(szPath);
            DWORD dwType = 0;
            if (RegQueryValueExW(hKey, L"InstallLocation", NULL, &dwType, (LPBYTE)szPath, &cbData) == ERROR_SUCCESS
                && dwType == REG_SZ && cbData > sizeof(WCHAR))
            {
                RegCloseKey(hKey);
                std::wstring path(szPath);
                while (!path.empty() && path.back() == L'\\') path.pop_back();
                return path;
            }
            RegCloseKey(hKey);
        }
    }
    
    WCHAR szModule[MAX_PATH];
    if (GetModuleFileNameW(NULL, szModule, MAX_PATH))
    {
        PathRemoveFileSpecW(szModule);
        return std::wstring(szModule);
    }
    return L"";
}

std::wstring EscapeJsonString(const std::wstring& input)
{
    std::wstring output;
    for (wchar_t c : input)
    {
        if (c == L'\"') output += L"\\\"";
        else if (c == L'\\') output += L"\\\\";
        else if (c == L'\b') output += L"\\b";
        else if (c == L'\f') output += L"\\f";
        else if (c == L'\n') output += L"\\n";
        else if (c == L'\r') output += L"\\r";
        else if (c == L'\t') output += L"\\t";
        else output += c;
    }
    return output;
}

std::string WStringToStringUTF8(const std::wstring& wstr)
{
    if (wstr.empty()) return std::string();
    int size_needed = WideCharToMultiByte(CP_UTF8, 0, &wstr[0], (int)wstr.size(), NULL, 0, NULL, NULL);
    std::string strTo(size_needed, 0);
    WideCharToMultiByte(CP_UTF8, 0, &wstr[0], (int)wstr.size(), &strTo[0], size_needed, NULL, NULL);
    return strTo;
}

void SendFileToGreenshot(const std::wstring& filePath)
{
    // Build Pipe Name
    std::wstring pipeName = L"\\\\.\\pipe\\Greenshot_";
    HANDLE hToken = NULL;
    if (OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken))
    {
        DWORD dwSize = 0;
        GetTokenInformation(hToken, TokenUser, NULL, 0, &dwSize);
        if (dwSize > 0)
        {
            PTOKEN_USER pTokenUser = (PTOKEN_USER)malloc(dwSize);
            if (GetTokenInformation(hToken, TokenUser, pTokenUser, dwSize, &dwSize))
            {
                LPWSTR sidString = NULL;
                if (ConvertSidToStringSidW(pTokenUser->User.Sid, &sidString))
                {
                    pipeName += sidString;
                    LocalFree(sidString);
                }
            }
            free(pTokenUser);
        }
        CloseHandle(hToken);
    }

    // Build JSON Payload
    std::wstring json = L"{\"version\":1,\"source\":\"open_with\",\"raw_input\":\"" + EscapeJsonString(filePath) +
                        L"\",\"parsed\":{\"action\":\"open_file\",\"parameters\":{\"path\":\"" + EscapeJsonString(filePath) + L"\"}}}";
    std::string utf8Json = WStringToStringUTF8(json);

    // Send via Named Pipe
    HANDLE hPipe = CreateFileW(pipeName.c_str(), GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
    if (hPipe == INVALID_HANDLE_VALUE)
    {
        DWORD err = GetLastError();
        if (err == ERROR_FILE_NOT_FOUND || err == ERROR_PIPE_BUSY)
        {
            // Greenshot not running (or busy), spawn it
            std::wstring installDir = GetGreenshotInstallDir();
            std::wstring exePath = installDir + L"\\Greenshot.exe";
            
            SHELLEXECUTEINFOW sei = { sizeof(sei) };
            sei.fMask = SEE_MASK_NOASYNC | SEE_MASK_FLAG_NO_UI;
            sei.lpVerb = L"open";
            sei.lpFile = exePath.c_str();
            sei.lpParameters = L""; // Start without args, we will send via pipe
            sei.lpDirectory = installDir.c_str();
            sei.nShow = SW_SHOWNORMAL;
            
            if (ShellExecuteExW(&sei))
            {
                DWORD start = GetTickCount();
                while (GetTickCount() - start < 8000)
                {
                    if (WaitNamedPipeW(pipeName.c_str(), 250))
                    {
                        hPipe = CreateFileW(pipeName.c_str(), GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
                        if (hPipe != INVALID_HANDLE_VALUE)
                        {
                            break;
                        }
                    }
                    else
                    {
                        hPipe = CreateFileW(pipeName.c_str(), GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
                        if (hPipe != INVALID_HANDLE_VALUE)
                        {
                            break;
                        }
                    }
                    Sleep(100);
                }
            }
        }
    }

    if (hPipe != INVALID_HANDLE_VALUE)
    {
        DWORD cbWritten = 0;
        uint32_t length = (uint32_t)utf8Json.length();
        WriteFile(hPipe, &length, sizeof(length), &cbWritten, NULL);
        WriteFile(hPipe, utf8Json.c_str(), length, &cbWritten, NULL);
        FlushFileBuffers(hPipe);
        CloseHandle(hPipe);
    }
}

// --- COM ExeServer Implementation ---

long g_cRefModule = 0;
DWORD g_dwRegister = 0;
HANDLE g_hEventQuit = NULL;

class CExplorerCommand : public IExplorerCommand
{
public:
    CExplorerCommand() : _cRef(1) { InterlockedIncrement(&g_cRefModule); }
    ~CExplorerCommand()
    {
        if (InterlockedDecrement(&g_cRefModule) == 0 && g_hEventQuit)
        {
            SetEvent(g_hEventQuit);
        }
    }

    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv)
    {
        static const QITAB qit[] = { QITABENT(CExplorerCommand, IExplorerCommand), { 0 } };
        return QISearch(this, qit, riid, ppv);
    }
    IFACEMETHODIMP_(ULONG) AddRef() { return InterlockedIncrement(&_cRef); }
    IFACEMETHODIMP_(ULONG) Release()
    {
        long cRef = InterlockedDecrement(&_cRef);
        if (cRef == 0) delete this;
        return cRef;
    }

    IFACEMETHODIMP GetTitle(IShellItemArray* psiItemArray, LPWSTR* ppszName)
    {
        *ppszName = NULL;
        std::wstring installDir = GetGreenshotInstallDir();
        WCHAR szLocale[LOCALE_NAME_MAX_LENGTH];
        std::wstring cachedTitle = L"Edit with Greenshot";
        
        if (GetUserDefaultLocaleName(szLocale, LOCALE_NAME_MAX_LENGTH))
        {
            std::wstring langFile = installDir + L"\\Languages\\language-" + szLocale + L".xml";
            if (GetFileAttributesW(langFile.c_str()) == INVALID_FILE_ATTRIBUTES)
            {
                std::wstring localeStr(szLocale);
                size_t dash = localeStr.find(L'-');
                if (dash != std::wstring::npos) langFile = installDir + L"\\Languages\\language-" + localeStr.substr(0, dash) + L".xml";
            }
            if (GetFileAttributesW(langFile.c_str()) == INVALID_FILE_ATTRIBUTES)
            {
                langFile = installDir + L"\\Languages\\language-en-US.xml";
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
                        pos += 21;
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
        return SHStrDupW(cachedTitle.c_str(), ppszName);
    }

    IFACEMETHODIMP GetIcon(IShellItemArray* psiItemArray, LPWSTR* ppszIcon)
    {
        std::wstring path = GetGreenshotInstallDir() + L"\\Greenshot.exe,0";
        return SHStrDupW(path.c_str(), ppszIcon);
    }
    
    IFACEMETHODIMP GetToolTip(IShellItemArray*, LPWSTR* ppszInfotip) { *ppszInfotip = NULL; return E_NOTIMPL; }
    IFACEMETHODIMP GetCanonicalName(GUID* pguidCommandName) { *pguidCommandName = GUID_NULL; return S_OK; }
    IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE* pCmdState) { *pCmdState = ECS_ENABLED; return S_OK; }
    
    IFACEMETHODIMP Invoke(IShellItemArray* psiItemArray, IBindCtx* pbc)
    {
        if (!psiItemArray) return E_INVALIDARG;
        DWORD count;
        psiItemArray->GetCount(&count);

        for (DWORD i = 0; i < count; i++)
        {
            IShellItem* psi;
            if (SUCCEEDED(psiItemArray->GetItemAt(i, &psi)))
            {
                LPWSTR pszName;
                if (SUCCEEDED(psi->GetDisplayName(SIGDN_FILESYSPATH, &pszName)))
                {
                    SendFileToGreenshot(pszName);
                    CoTaskMemFree(pszName);
                }
                psi->Release();
            }
        }
        return S_OK;
    }

    IFACEMETHODIMP GetFlags(EXPCMDFLAGS* pFlags) { *pFlags = ECF_DEFAULT; return S_OK; }
    IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** ppEnum) { *ppEnum = NULL; return E_NOTIMPL; }

private:
    long _cRef;
};

class CClassFactory : public IClassFactory
{
public:
    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv)
    {
        static const QITAB qit[] = { QITABENT(CClassFactory, IClassFactory), { 0 } };
        return QISearch(this, qit, riid, ppv);
    }
    IFACEMETHODIMP_(ULONG) AddRef() { return InterlockedIncrement(&_cRef); }
    IFACEMETHODIMP_(ULONG) Release()
    {
        long cRef = InterlockedDecrement(&_cRef);
        if (cRef == 0) delete this;
        return cRef;
    }
    IFACEMETHODIMP CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppv)
    {
        *ppv = NULL;
        if (pUnkOuter) return CLASS_E_NOAGGREGATION;
        CExplorerCommand* pCmd = new (std::nothrow) CExplorerCommand();
        if (!pCmd) return E_OUTOFMEMORY;
        HRESULT hr = pCmd->QueryInterface(riid, ppv);
        pCmd->Release();
        return hr;
    }
    IFACEMETHODIMP LockServer(BOOL fLock)
    {
        if (fLock)
        {
            InterlockedIncrement(&g_cRefModule);
        }
        else
        {
            if (InterlockedDecrement(&g_cRefModule) == 0 && g_hEventQuit)
            {
                SetEvent(g_hEventQuit);
            }
        }
        return S_OK;
    }
    CClassFactory() : _cRef(1) { }
private:
    ~CClassFactory() { }
    long _cRef;
};

// --- Entry Point ---

int APIENTRY wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR lpCmdLine, int nCmdShow)
{
    // If run with -Embedding, host the COM server
    std::wstring cmdLine(lpCmdLine);
    if (cmdLine.find(L"-Embedding") != std::wstring::npos || cmdLine.find(L"/Embedding") != std::wstring::npos)
    {
        HRESULT hr = CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
        if (SUCCEEDED(hr))
        {
            g_hEventQuit = CreateEvent(NULL, FALSE, FALSE, NULL);
            CClassFactory* pcf = new (std::nothrow) CClassFactory();
            if (pcf)
            {
                // {3D1E6BB3-7033-4D9A-BF6D-F18A32CA11B2}
                const GUID CLSID_GreenshotContextMenu = 
                { 0x3d1e6bb3, 0x7033, 0x4d9a, { 0xbf, 0x6d, 0xf1, 0x8a, 0x32, 0xca, 0x11, 0xb2 } };
                
                hr = CoRegisterClassObject(CLSID_GreenshotContextMenu, pcf, CLSCTX_LOCAL_SERVER, REGCLS_MULTIPLEUSE, &g_dwRegister);
                if (SUCCEEDED(hr))
                {
                    MSG msg;
                    while (true)
                    {
                        DWORD dwWait = MsgWaitForMultipleObjects(1, &g_hEventQuit, FALSE, INFINITE, QS_ALLINPUT);
                        if (dwWait == WAIT_OBJECT_0) break; // Quit event
                        while (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE))
                        {
                            TranslateMessage(&msg);
                            DispatchMessage(&msg);
                        }
                    }
                    CoRevokeClassObject(g_dwRegister);
                }
                pcf->Release();
            }
            CloseHandle(g_hEventQuit);
            CoUninitialize();
        }
        return 0;
    }

    // Otherwise, it was called directly via command line (Legacy context menu / File Association)
    // E.g. greenshot-proxy.exe --file "C:\path\to\image.png"
    int argc;
    LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (argv)
    {
        for (int i = 1; i < argc; i++)
        {
            std::wstring arg = argv[i];
            if ((arg == L"--file" || arg == L"-f") && i + 1 < argc)
            {
                SendFileToGreenshot(argv[++i]);
            }
            else if (arg[0] != L'-')
            {
                // Assume it's a file path
                SendFileToGreenshot(arg);
            }
        }
        LocalFree(argv);
    }

    return 0;
}
