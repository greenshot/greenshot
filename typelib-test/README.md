# Type library test (throwaway, not for main)

Checks which way of calling Office survives a stale type library registration (TYPE_E_CANTLOADLIBRARY, 0x80029C4A):
typed (embedded PIA, today), `dynamic`, and IDispatch by name (Type.InvokeMember). Each run starts and quits its own invisible Word.

Run `powershell -ExecutionPolicy Bypass -File run-test.ps1` from this folder. It builds .NET Framework 4.8 and .NET 10,
64-bit and 32-bit, runs all four without the stale key, adds the key (current user only, stale-typelib.ps1),
runs again, removes the key (always) and runs once more. Report the whole output.

Expected: typed fails with 0x80029C4A with the key (otherwise the reproduction is wrong). The question is whether the other two stay OK.
