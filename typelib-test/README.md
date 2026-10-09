# Type library test (throwaway, not for main)

Checks which way of calling Office survives a stale type library registration (TYPE_E_CANTLOADLIBRARY, 0x80029C4A):
typed (embedded PIA, today), `dynamic`, and IDispatch by name. Each run starts and quits its own invisible Word.

1. Copy this folder to D:\code\typelib-test (outside the repos).
2. Baseline: `dotnet run -f net48`, `dotnet run -f net48 -p:PlatformTarget=x86`, `dotnet run -f net10.0-windows`, `dotnet run -f net10.0-windows -p:PlatformTarget=x86`. Skip (and report) what can't run because an SDK or runtime is missing.
3. `powershell -ExecutionPolicy Bypass -File stale-typelib.ps1` adds the stale key (current user only).
4. The same four runs again.
5. ALWAYS, even if something failed: `powershell -ExecutionPolicy Bypass -File stale-typelib.ps1 -Remove`, confirm the HKCU key is gone, then one more `dotnet run -f net48`.
6. Report every run's console output verbatim, the "Word's registered version" line, and Office bitness and type (Click-to-Run or MSI). Change nothing else.

Expected: typed fails with 0x80029C4A in step 4 (otherwise the reproduction is wrong). The question is whether the other two stay OK.
