// Which way of calling Word survives a stale type library registration?
// Run once without and once with the stale key (stale-typelib.ps1).
using System;
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
                try { DispatchCall(app, "Quit"); } catch { }
                Marshal.FinalReleaseComObject(app);
            }
        }
    }

    // Only IDispatch.GetIDsOfNames and IDispatch.Invoke, no type information
    private static object DispatchGet(object target, string name) => Invoke(target, name, DISPATCH_PROPERTYGET);
    private static void DispatchCall(object target, string name) => Invoke(target, name, DISPATCH_METHOD);

    private static object Invoke(object target, string name, ushort flags)
    {
        var dispatch = (IDispatch)target;
        var iidNull = Guid.Empty;
        int[] dispIds = new int[1];
        Marshal.ThrowExceptionForHR(dispatch.GetIDsOfNames(ref iidNull, new[] { name }, 1, 0, dispIds));
        var parameters = new DISPPARAMS();
        Marshal.ThrowExceptionForHR(dispatch.Invoke(dispIds[0], ref iidNull, 0, flags, ref parameters, out object result, IntPtr.Zero, IntPtr.Zero));
        return result;
    }

    private const ushort DISPATCH_METHOD = 1;
    private const ushort DISPATCH_PROPERTYGET = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPPARAMS
    {
        public IntPtr rgvarg;
        public IntPtr rgdispidNamedArgs;
        public int cArgs;
        public int cNamedArgs;
    }

    [ComImport, Guid("00020400-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDispatch
    {
        [PreserveSig] int GetTypeInfoCount(out uint count);
        [PreserveSig] int GetTypeInfo(uint index, uint lcid, out IntPtr typeInfo);
        [PreserveSig] int GetIDsOfNames(ref Guid iid, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] names, int count, uint lcid, [Out] int[] dispIds);
        [PreserveSig] int Invoke(int dispId, ref Guid iid, uint lcid, ushort flags, ref DISPPARAMS parameters, out object result, IntPtr exceptionInfo, IntPtr argumentError);
    }
}
