using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using UIpp.Core.Scripting;

namespace UIpp.Windows.Scripting;

// Evaluates a post-substitution VBScript expression string by hosting the Windows IActiveScript
// COM engine (vbscript.dll). WinPE requires the WinPE-Scripting optional component for this to
// work; when the engine is absent Type.GetTypeFromProgID returns null and Evaluate returns false.
public sealed class VBScriptConditionEvaluator : IConditionEvaluator
{
    private const uint ScriptStateStarted     = 1;
    private const uint ScriptTextIsExpression = 0x20;

    public bool Evaluate(string expression, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(expression)) return true;

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
                out var result, out _);

            return AsBool(result);
        }
        catch
        {
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

    private static bool AsBool(object? v) => v switch
    {
        bool b   => b,
        short s  => s != 0,   // VARIANT_BOOL: VARIANT_TRUE = -1, VARIANT_FALSE = 0
        int i    => i != 0,
        double d => d != 0,
        string s => !string.IsNullOrEmpty(s)
                    && s != "0"
                    && !s.Equals("False", StringComparison.OrdinalIgnoreCase),
        null     => false,
        _        => TryConvert(v),
    };

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
