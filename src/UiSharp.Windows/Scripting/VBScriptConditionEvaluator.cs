using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using UiSharp.Core.Scripting;

namespace UiSharp.Windows.Scripting;

// Evaluates a post-substitution VBScript expression string by hosting the Windows IActiveScript
// COM engine (vbscript.dll). WinPE requires the WinPE-Scripting optional component for this to
// work; when the engine is absent Type.GetTypeFromProgID returns null and Evaluate returns false.
public sealed class VBScriptConditionEvaluator : IConditionEvaluator
{
    private const uint ScriptStateStarted     = 1;
    private const uint ScriptTextIsExpression = 0x20;

    /// <summary>True when the VBScript engine is present on this machine.</summary>
    public static bool IsAvailable => Type.GetTypeFromProgID("VBScript") is not null;

    public bool Evaluate(string expression, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(expression)) return true;
        return TryRun(expression, out var result) && AsBool(result);
    }

    /// <summary>
    /// Evaluates an expression for its value. Declines when the engine errors or
    /// returns nothing, matching the HRESULT and VARIANT checks the original
    /// applies to CScriptHost::Eval (Actions.cpp:393).
    /// </summary>
    public bool TryEvaluateValue(string expression, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(expression)) return false;

        if (!TryRun(expression, out var result) || result is null) return false;

        var text = AsScriptString(result);
        if (text.Length == 0) return false;   // C++ requires a non-empty result

        value = text;
        return true;
    }

    // Runs the expression through a fresh engine instance. Returns false if the
    // engine is missing or the expression raised an error, which is the
    // equivalent of a failed HRESULT in the original.
    private static bool TryRun(string expression, out object? result)
    {
        result = null;

        var engineType = Type.GetTypeFromProgID("VBScript");
        if (engineType is null) return false;

        IActiveScript? engine = null;
        try
        {
            engine = (IActiveScript)Activator.CreateInstance(engineType)!;
            var parse = (IActiveScriptParse)engine;

            engine.SetScriptSite(new NullScriptSite());
            parse.InitNew();
            engine.SetScriptState(ScriptStateStarted);

            parse.ParseScriptText(
                expression, null, null, null,
                IntPtr.Zero, 0, ScriptTextIsExpression,
                out result, out _);

            return true;
        }
        catch
        {
            result = null;
            return false;
        }
        finally
        {
            if (engine is not null)
            {
                try { engine.Close(); } catch { }
                Marshal.ReleaseComObject(engine);
            }
        }
    }

    // The C++ original converts the VARIANT with (_bstr_t), which is a locale
    // conversion. Invariant culture is used here so results do not vary by
    // machine; UI++ expressions are ASCII arithmetic and text.
    private static string AsScriptString(object v) => v switch
    {
        string s  => s,
        bool b    => b ? "True" : "False",
        short sh  => sh.ToString(CultureInfo.InvariantCulture),
        int i     => i.ToString(CultureInfo.InvariantCulture),
        long l    => l.ToString(CultureInfo.InvariantCulture),
        float f   => f.ToString("R", CultureInfo.InvariantCulture),
        double d  => d.ToString("R", CultureInfo.InvariantCulture),
        decimal m => m.ToString(CultureInfo.InvariantCulture),

        // A Date VARIANT renders the way VBScript's CStr does. .NET's default
        // ("01/03/2020 00:00:00") is not what a config author sees, and the
        // original converts with (_bstr_t), not ToString().
        //
        // A Date carries both halves, so VBScript shows only the half that is
        // set: no time at midnight, and no date when the date part is the zero
        // date of 30 December 1899, which is what TimeValue and TimeSerial
        // return.
        DateTime t => FormatVariantDate(t),
        _         => Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    private static bool AsBool(object? v) => v switch
    {
        bool b   => b,
        short s  => s != 0,   // VARIANT_BOOL: VARIANT_TRUE = -1, VARIANT_FALSE = 0
        int i    => i != 0,
        double d => d != 0,
        string s => !string.IsNullOrEmpty(s)
                    && s != "0"
                    && !s.Equals("False", StringComparison.OrdinalIgnoreCase),
        // A Date coerces to a non-zero serial number, so it is truthy.
        // Convert.ToBoolean throws on DateTime, which would have read as false.
        DateTime  => true,
        null     => false,
        _        => TryConvert(v),
    };

    private static readonly DateTime VariantZeroDate = new(1899, 12, 30);

    private static string FormatVariantDate(DateTime value)
    {
        if (value.Date == VariantZeroDate)
            return value.ToString("h:mm:ss tt", CultureInfo.InvariantCulture);

        return value.TimeOfDay == TimeSpan.Zero
            ? value.ToString("M/d/yyyy", CultureInfo.InvariantCulture)
            : value.ToString("M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
    }

    private static bool TryConvert(object v)
    {
        try   { return Convert.ToBoolean(v); }
        catch { return false; }
    }

    // Minimal IActiveScriptSite — no named items; errors silently swallowed.
    private sealed class NullScriptSite : IActiveScriptSite
    {
        public void GetLCID(out uint lcid) => lcid = 0;

        public void GetItemInfo(string name, uint returnMask, out IntPtr item, out IntPtr typeInfo)
        {
            item     = IntPtr.Zero;
            typeInfo = IntPtr.Zero;
            // TYPE_E_ELEMENTNOTFOUND — our expressions don't reference named host objects.
            throw new COMException("Named items not supported", unchecked((int)0x8002802B));
        }

        public void GetDocVersionString(out string version) => version = "1.0";
        public void OnScriptTerminate(IntPtr pvarResult, IntPtr pexcepinfo) { }
        public void OnStateChange(uint state)                               { }
        public void OnScriptError(IActiveScriptError error)                 { }
        public void OnEnterScript()                                         { }
        public void OnLeaveScript()                                         { }
    }
}

// ─── COM interface declarations ───────────────────────────────────────────────

[ComImport]
[Guid("BB1A2AE1-A4F9-11CF-8F20-00805F2CD064")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IActiveScript
{
    void SetScriptSite([In, MarshalAs(UnmanagedType.Interface)] IActiveScriptSite site);
    void GetScriptSite(ref Guid riid, out IntPtr ppvObject);
    void SetScriptState(uint state);
    void GetScriptState(out uint state);
    void Close();
    void AddNamedItem([MarshalAs(UnmanagedType.BStr)] string name, uint flags);
    void AddTypeLib(ref Guid typeLib, uint major, uint minor, uint flags);
    void GetScriptDispatch([MarshalAs(UnmanagedType.BStr)] string? itemName, out IntPtr dispatch);
    void GetCurrentScriptThreadID(out uint threadId);
    void GetScriptThreadID(uint win32ThreadId, out uint scriptThreadId);
    void GetScriptThreadState(uint threadId, out uint state);
    void InterruptScriptThread(uint threadId, ref EXCEPINFO excepinfo, uint flags);
    void Clone(out IActiveScript script);
}

// 64-bit IActiveScriptParse — source context cookie is DWORD_PTR (IntPtr on 64-bit Windows).
[ComImport]
[Guid("C7EF7658-E1EE-480E-97EA-D52CB4D76D17")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IActiveScriptParse
{
    void InitNew();

    void AddScriptlet(
        [MarshalAs(UnmanagedType.BStr)] string?  defaultName,
        [MarshalAs(UnmanagedType.BStr)] string   code,
        [MarshalAs(UnmanagedType.BStr)] string?  itemName,
        [MarshalAs(UnmanagedType.BStr)] string?  subItemName,
        [MarshalAs(UnmanagedType.BStr)] string?  eventName,
        [MarshalAs(UnmanagedType.BStr)] string?  delimiter,
        IntPtr  sourceContext,
        uint    startingLine,
        uint    flags,
        [MarshalAs(UnmanagedType.BStr)] out string name,
        out EXCEPINFO excepinfo);

    void ParseScriptText(
        [MarshalAs(UnmanagedType.BStr)] string   code,
        [MarshalAs(UnmanagedType.BStr)] string?  itemName,
        [MarshalAs(UnmanagedType.IUnknown)] object? context,
        [MarshalAs(UnmanagedType.BStr)] string?  delimiter,
        IntPtr  sourceContext,
        uint    startingLine,
        uint    flags,
        [MarshalAs(UnmanagedType.Struct)] out object? result,
        out EXCEPINFO excepinfo);
}

[ComImport]
[Guid("DB01A1E3-A42B-11CF-8F20-00805F2CD064")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IActiveScriptSite
{
    void GetLCID(out uint lcid);
    void GetItemInfo(
        [MarshalAs(UnmanagedType.BStr)] string name,
        uint returnMask,
        out IntPtr item,
        out IntPtr typeInfo);
    void GetDocVersionString([MarshalAs(UnmanagedType.BStr)] out string version);
    // pvarResult and pexcepinfo are const pointers; use IntPtr to skip VARIANT/EXCEPINFO marshaling.
    void OnScriptTerminate(IntPtr pvarResult, IntPtr pexcepinfo);
    void OnStateChange(uint state);
    void OnScriptError([MarshalAs(UnmanagedType.Interface)] IActiveScriptError error);
    void OnEnterScript();
    void OnLeaveScript();
}

[ComImport]
[Guid("EAE1BA61-A4ED-11CF-8F20-00805F2CD064")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IActiveScriptError
{
    void GetExceptionInfo(out EXCEPINFO excepinfo);
    void GetSourcePosition(out uint sourceContext, out uint lineNumber, out int characterPosition);
    void GetSourceLineText([MarshalAs(UnmanagedType.BStr)] out string sourceLine);
}
