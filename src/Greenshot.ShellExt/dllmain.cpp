#include <windows.h>
#include <unknwn.h>
#include <new>
#include <shlwapi.h>
#include "ExplorerCommand.h"

long g_cRefModule = 0;

HINSTANCE g_hinst = NULL;

extern "C" BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        g_hinst = hInstance;
        DisableThreadLibraryCalls(hInstance);
        break;
    case DLL_PROCESS_DETACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;
    }
    return TRUE;
}

class CClassFactory : public IClassFactory
{
public:
    // IUnknown
    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv)
    {
        static const QITAB qit[] = {
            QITABENT(CClassFactory, IClassFactory),
            { 0 },
        };
        return QISearch(this, qit, riid, ppv);
    }

    IFACEMETHODIMP_(ULONG) AddRef()
    {
        return InterlockedIncrement(&_cRef);
    }

    IFACEMETHODIMP_(ULONG) Release()
    {
        long cRef = InterlockedDecrement(&_cRef);
        if (cRef == 0)
        {
            delete this;
        }
        return cRef;
    }

    // IClassFactory
    IFACEMETHODIMP CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppv)
    {
        *ppv = NULL;
        if (pUnkOuter)
        {
            return CLASS_E_NOAGGREGATION;
        }

        CExplorerCommand* pCmd = new (std::nothrow) CExplorerCommand();
        if (pCmd == NULL)
        {
            return E_OUTOFMEMORY;
        }

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
            InterlockedDecrement(&g_cRefModule);
        }
        return S_OK;
    }

    CClassFactory() : _cRef(1)
    {
        InterlockedIncrement(&g_cRefModule);
    }

private:
    ~CClassFactory()
    {
        InterlockedDecrement(&g_cRefModule);
    }
    long _cRef;
};

// Exported functions
STDAPI DllGetClassObject(REFIID rclsid, REFIID riid, void** ppv)
{
    *ppv = NULL;

    // {3D1E6BB3-7033-4D9A-BF6D-F18A32CA11B2}
    static const GUID CLSID_GreenshotContextMenu = 
    { 0x3d1e6bb3, 0x7033, 0x4d9a, { 0xbf, 0x6d, 0xf1, 0x8a, 0x32, 0xca, 0x11, 0xb2 } };

    if (rclsid == CLSID_GreenshotContextMenu)
    {
        CClassFactory* pcf = new (std::nothrow) CClassFactory();
        if (pcf)
        {
            HRESULT hr = pcf->QueryInterface(riid, ppv);
            pcf->Release();
            return hr;
        }
        return E_OUTOFMEMORY;
    }
    return CLASS_E_CLASSNOTAVAILABLE;
}

STDAPI DllCanUnloadNow()
{
    return g_cRefModule == 0 ? S_OK : S_FALSE;
}
