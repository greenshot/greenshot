// Which way of calling Word survives a stale type library registration?
// Run once without and once with the stale key (stale-typelib.ps1).
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Word = Microsoft.Office.Interop.Word;

internal static class Program
{
    private static int Main()
    {
        Console.WriteLine($"{RuntimeInformation.FrameworkDescription}, {(Environment.Is64BitProcess ? "64" : "32")}-bit process");
        int failures = 0;
        failures += Run("typed (embedded PIA, as Greenshot does today)", app => ((Word.Application)app).Documents.Count);
        failures += Run("dynamic", app => (int)((dynamic)app).Documents.Count);
        failures += Run("IDispatch by name (what a DispatchProxy would do)", app => (int)DispatchGet(DispatchGet(app, "Documents"), "Count"));
        failures += Run("own typed interfaces over IDispatch (no library, no type information)", app => ((IWordApplication)app).Documents.Count);
        return failures;
    }

    private static int Run(string name, Func<object, int> call)
    {
        object app = null;
        try
        {
            // A new, invisible Word process, so the user's documents aren't touched
            app = Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application", true));
            int count = call(app);
            Console.WriteLine($"OK    {name}: Documents.Count = {count}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL  {name}: 0x{ex.HResult:X8} {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
        finally
        {
            if (app != null)
            {
                // Quit without saving: wdDoNotSaveChanges = 0
                try { DispatchCall(app, "Quit", 0); } catch (Exception ex) { Console.WriteLine($"      Quit failed: {ex.Message}"); }
                Marshal.FinalReleaseComObject(app);
            }
        }
    }

    // Type.InvokeMember on a COM object calls IDispatch.GetIDsOfNames and IDispatch.Invoke, no type information
    private static object DispatchGet(object target, string name) => target.GetType().InvokeMember(name, BindingFlags.GetProperty, null, target, null);
    private static void DispatchCall(object target, string name, params object[] args) => target.GetType().InvokeMember(name, BindingFlags.InvokeMethod, null, target, args);
}

// Own small interfaces, typed for us, late bound for Office: the IID is IDispatch's, so the cast is a QueryInterface
// for IDispatch only, and each call is IDispatch.GetIDsOfNames (by member name) + IDispatch.Invoke.
[ComImport, Guid("00020400-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IWordApplication
{
    IWordDocuments Documents { get; }
}

[ComImport, Guid("00020400-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
public interface IWordDocuments
{
    int Count { get; }
}
