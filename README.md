# UiSharp + gUI#

A C# (.NET 10 / WinForms) port of [UI++](https://github.com/jason4tw/UIPlusPlus) — the ConfigMgr task-sequence front-end originally written in C++17/MFC by Jason Sandys.

## What is UiSharp?

UiSharp displays a customizable WinForms UI during OS deployments driven by Microsoft Configuration Manager (SCCM/ConfigMgr). It reads an XML configuration file and presents input dialogs, info screens, and pre-flight checks, then writes the collected values back as task-sequence variables that the rest of the deployment can consume.

## What is gUI#?

gUI# is a WinUI 3 desktop app for creating and editing UiSharp/UI++ XML configuration files. A guided form covers every action type and stays in bidirectional sync with a Monaco-powered XML editor. See [tools/gUISharp/README.md](tools/gUISharp/README.md) for the full feature overview, build instructions, and screenshots.

## Why a C# port?

- Easier to build and maintain without the Visual C++ / MFC / curl toolchain
- Unit-testable pure logic (conditions, variable substitution, XML parsing)
- Single-file self-contained publish with .NET 10
- Retains WinPE compatibility via Windows Forms (.NET 10-windows)

## Solution structure

| Project | Framework | Purpose |
|---|---|---|
| `UiSharp.Core` | net10.0 | Pure logic — parsers, evaluators, action implementations. No Windows dependencies. |
| `UiSharp.Windows` | net10.0-windows | WMI, COM, LDAP, registry integrations |
| `UiSharp.UI` | net10.0-windows | WinForms dialogs and controls |
| `UiSharp.Editing` | net10.0 | Editor-only model and XML round-tripping used by gUI#. Kept out of the runtime so the WinPE executable carries no editing code. |
| `UiSharp.Diagnostics` | net10.0-windows | The `/selftest` checks — see [Self-testing](#self-testing) |
| `UiSharp` | net10.0-windows | Entry point, single-file publish target |
| `tools/gUISharp` | net10.0-windows10.0.22621.0 | WinUI 3 visual XML editor — see [tools/gUISharp/README.md](tools/gUISharp/README.md) |

## Building

**Requirements:** .NET 10 SDK (Windows required for `UiSharp.Windows`, `UiSharp.UI`, and `UiSharp`)

```bash
dotnet build
dotnet test src/UiSharp.Core.Tests        # pure logic, runs on any OS
dotnet test src/UiSharp.Editing.Tests     # editor XML round-tripping, runs on any OS
dotnet test src/UiSharp.Windows.Tests     # registry/WMI + VBScript differential, Windows only
dotnet test src/UiSharp.UI.Tests          # dialog resources, Windows only
dotnet test src/UiSharp.Diagnostics.Tests # the self-test checks, Windows only
```

## Deploying to WinPE

Publish as a single self-contained executable — no .NET runtime or redistributable needed in the WinPE image:

```powershell
dotnet publish src/UiSharp/UiSharp.csproj -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:TrimMode=partial -c Release
```

Copy the output `UiSharp.exe` into your WinPE image in place of the original `UI++.exe`. No other changes to the image are required.

## Command-line arguments

| Argument | Purpose |
|---|---|
| `/Config:<path\|URL>` | XML config file path or HTTP/HTTPS URL. Default: `UI++.xml` in the current directory. |
| `/ConfigFallback:<path>` | Local file to use if the URL download fails after all retries. |
| `/ConfigRetry:<n>` | Number of download attempts before falling back (default: 3). |
| `/DisableTSVarEditor` | Prevents the Ctrl+F2 task-sequence variable editor from opening during dialogs. |
| `/conditionengine:native\|vbscript` | Override the condition evaluator for the whole run. Default: `native`. `vbscript` is a legacy path — see below. |
| `/SelfTest` | Run the diagnostics instead of a configuration — see [Self-testing](#self-testing). |
| `/SelfTestReport:<path>` | Where to write the self-test report. A directory is fine. Implies `/SelfTest`. |

## Self-testing

UiSharp runs where almost nothing can be observed: a WinPE boot image, mid-deployment, with one log file to show for it. Every serious defect found in this port has lived in that path, and none of it is reachable from a unit test.

```
UiSharp.exe /selftest
```

Runs about fifty checks against the live machine and writes a report beside the log, so SMSTS log collection picks it up. Nothing is displayed — it is safe to run unattended inside a real deployment. Exit code `0` if everything passed, `4` if anything failed.

It covers the log location, the task-sequence environment (including *enumerating* variables, the check with the worst track record), variable files, the condition engine as it actually shipped, whether every action type survived trimming, the registry, WMI, and the `X` variables collected by `DefaultValues`. The report says whether the run was inside a task sequence, because a clean run outside one proves much less.

For the dialogs — which need eyes, not assertions — `tools/selftest/UiSharp-SelfTest.xml` shows every screen in turn, each one saying what to look at. See [tools/selftest/README.md](tools/selftest/README.md).

## Differences from the original C++ UI++

These are the only intentional behavioral changes:

| Area | Original UI++ | UiSharp |
|---|---|---|
| `<Action Type="Vars">` save format | MFC `CArchive` binary (`.dat`) | JSON — existing `.dat` files are not readable |
| `<Action Type="RandomString">` output variable | Always writes to `Random` regardless of the `Variable` attribute (C++ bug) | Correctly uses the `Variable` attribute |
| VBScript condition engine | Always used; no way to opt out | Available but not the default, and legacy — see [Condition engines](#condition-engines). `native` is the default and needs no extra WinPE component. |
| WinPE optional components | `WinPE-WMI` + `WinPE-Scripting` required | `WinPE-WMI` always; `WinPE-Scripting` only when using the vbscript engine |
| Log file name | `UI++.log` in `_SMSTSLogPath` (or `%TEMP%`), CMTrace component `UI++` | `UiSharp.log`, CMTrace component `UiSharp` — so it is obvious which tool wrote it. Update any log-collection step that looks for `UI++.log` by name. |
| Unhandled errors | No handler — a crash left nothing behind | Written to the log and to `UiSharp_crash.txt` beside it, exiting with code 3. No dialog, so an unattended task sequence cannot hang on one. |
| `Image` / `InfoImage` sizing | Drawn at native size on the standard dialogs | Scaled down to fit, so an oversized image cannot crowd out the text it is next to. An animated GIF keeps animating: it is scaled as it is drawn rather than resampled. |
| `Icon` | Local `.ico` only, `LoadImage(..., LR_LOADFROMFILE)` | Any image format, and `http(s)` as well as a local path. Also used as the window icon, not only the sidebar icon. |

The XML root element remains `<UIpp>`, as in the original — configuration files need no edit. All other XML attributes, variable names, dialog behavior, and output formats are identical to the C++ original. Existing XML config files work without modification.

Parity is verified two ways, both in the test suite: golden-file snapshots of the original project's own sample configs, and differential tests that run the same expressions through the native engine and the real `vbscript.dll` and assert they agree.

## Images

`<Action Type="Info">`, `ErrorInfo` and `InfoFullScreen` take an `Image` attribute (branding across the top, or centred near the top of a full-screen dialog); `Info` and `ErrorInfo` also take `InfoImage`, centred below the text. The root `<UIpp Icon="...">` sets the icon shown beside the dialog title and in the title bar.

Each of these may be a local path or an `http(s)` URL, which is downloaded before the dialog is shown — the original does this too, and its own sample configuration brands every dialog from a web server.

Anything GDI+ can decode works: PNG, BMP, JPEG, GIF, TIFF. An animated GIF animates. Images larger than the space available are scaled down rather than cropped or allowed to push the text off the dialog.

None of it is load-bearing: an image that is missing, unreachable or in a format the boot image has no codec for is written to the log with the reason, and the dialog is shown without it.

## COM constructs in conditions

Conditions in existing configs often reach for COM, most commonly the file system:

```xml
<Action Type="Info" Condition='CreateObject("Scripting.FileSystemObject").FileExists("C:\marker.txt")'>
```

The native engine handles these itself, so such a config runs unchanged in a WinPE image **without** the `WinPE-Scripting` component. This compatibility shim is a bridge for existing XML rather than the place to stay — each use is noted in the log alongside its native replacement.

| COM member | Native equivalent |
|---|---|
| `CreateObject("Scripting.FileSystemObject").FileExists(p)` | `FileExists(p)` |
| `…FolderExists(p)` / `…DriveExists(d)` | `FolderExists(p)` / `DriveExists(d)` |
| `…GetParentFolderName(p)` | `PathParent(p)` |
| `…GetFileName(p)` / `…GetBaseName(p)` | `PathFileName(p)` / `PathBaseName(p)` |
| `…GetExtensionName(p)` / `…GetDriveName(p)` | `PathExtension(p)` / `PathDrive(p)` |
| `…BuildPath(p, name)` | `PathCombine(p, name)` |
| `CreateObject("WScript.Network").ComputerName` | `ComputerName()` |
| `…UserName` / `…UserDomain` | `UserName()` / `UserDomain()` |
| `CreateObject("WScript.Shell").ExpandEnvironmentStrings(s)` | `ExpandEnvironment(s)` |

The native functions are UiSharp-only: a config using them will not run under the original C++ UI++, which is the trade for dropping the dependency on a deprecated scripting engine. Both forms return the same answer, so migrating is safe to do incrementally.

Still requiring the VBScript engine: `GetObject` (WMI — prefer `<Action Type="WMIRead">`), `Eval`, `Execute`, `WScript.Shell.RegRead` (prefer `<Action Type="RegRead">`), and any other ProgID. These are reported in the log rather than silently evaluating false.

## Condition engines

`ConditionEngine` is a **whole-document** setting on the root `<UIpp>` element, overridable for a run with `/conditionengine`:

```xml
<UIpp ConditionEngine="vbscript">
```

Putting it on an individual `<Action>` does nothing and is reported in the log. It cannot work per-action: an action's own condition and the conditions inside it (a preflight `<Check>`, a `<Choice>`, an input field) would end up on different engines, and the `WinPE-Scripting` dependency belongs to the boot image rather than to any one action.

| Engine | When to use |
|---|---|
| `native` (default) | Everything, unless you hit one of the constructs above. No extra WinPE component. |
| `vbscript` | Legacy. Hosts `IActiveScript`, so the boot image needs `WinPE-Scripting`. Microsoft is deprecating VBScript, so treat this as a bridge and migrate the config. |

Selecting `vbscript` logs a warning naming the native alternatives. Selecting it on a system where the engine is not registered is logged as an **error** — conditions needing a script host then evaluate as false, and each one is reported individually, rather than the run quietly making wrong choices.

None of the original project's own sample configs need the VBScript engine.

### Condition functions

The native engine covers **every function in the VBScript library** — all 92 — along with the comparison, boolean and arithmetic operators at VBScript's precedence, arrays with `arr(0)` indexing, and member access.

Most are implemented outright. A few are deliberately not, and those are reported in the log rather than evaluating to an empty string that would read as a false condition:

| Function | Why not |
|---|---|
| `InputBox`, `MsgBox`, `LoadPicture` | They wait for a person. In an unattended task sequence that stops the deployment rather than answering wrongly. Use an `Input` or `Info` action. |
| `FormatCurrency` | The symbol and its placement *are* the locale, and the runtime carries no locale data. Use `FormatNumber` and add the symbol. |
| `GetObject`, `Eval`, `Execute`, `GetRef`, `GetLocale`, `SetLocale`, `ScriptEngine*` | No native equivalent; these need the script host. |

`Rnd` and `Timer` are implemented but cannot be compared between engines, for the obvious reason.

Everything else is checked against the real engine: the differential suite evaluates the same expressions through both and asserts they agree.

**Locale is the one place they can differ.** The runtime publishes with invariant globalization, so a WinPE image needs no ICU data and results never vary by machine. VBScript instead reads the system's regional settings — including Windows short-date overrides that .NET ignores regardless. `FormatNumber`, `FormatPercent` and `FormatDateTime` are therefore deterministic here rather than identical to VBScript on a machine with customised regional settings.

### UiSharp extensions

UiSharp adds functions VBScript does not have. They are functions rather than an engine mode on purpose: an existing config behaves exactly as before, and a new one opts in visibly, on the line where it matters. A mode would change what `=` means for every condition in a document, decided by an attribute the reader may never see.

Replacements for the COM shim: `FileExists`, `FolderExists`, `DriveExists`, `PathParent`, `PathFileName`, `PathBaseName`, `PathExtension`, `PathDrive`, `PathCombine`, `ComputerName`, `UserName`, `UserDomain`, `ExpandEnvironment`.

Conveniences for what VBScript makes awkward:

| Function | Why |
|---|---|
| `EqualsIgnoreCase(a, b)` | VBScript compares strings case-sensitively, so `"%XHWManufacturer%" = "Lenovo"` is false when WMI reports `LENOVO`. Vendor casing varies by firmware. |
| `IsSet(value)` | True when a value arrived — false when it is empty or still a literal `%Token%` because nothing set it. VBScript cannot tell those apart, which is how a preflight check on missing hardware data can appear to pass. |
| `InList(list, item [, delimiter])` | Membership in a delimited string, ignoring case and surrounding spaces. Use `Filter(Split(…))` when an exact match is wanted. |
| `VersionCompare(a, b)` | Compares dotted versions by number. As text, `"10.0.19041" > "10.0.9600"` is false, because `1` sorts before `9`. |

A config using any of these will not run under the original C++ UI++ or under the `vbscript` engine — the differential tests assert that VBScript rejects them, so the trade is explicit rather than assumed.

## Repository layout

| File / folder | Purpose |
|---|---|
| `UiSharp.slnx` | Modern C# solution — open this in Visual Studio or Rider |
| `src/` | C# projects — runtime (`UiSharp.Core`, `UiSharp.Windows`, `UiSharp.UI`, `UiSharp`), diagnostics (`UiSharp.Diagnostics`) and editor support (`UiSharp.Editing`) |
| `tools/gUISharp/` | WinUI 3 visual XML editor — see [tools/gUISharp/README.md](tools/gUISharp/README.md) |
| `tools/selftest/` | The interactive self-test configuration — see [tools/selftest/README.md](tools/selftest/README.md) |
| `UI++.sln` | Legacy C++ solution for the original UI++ source (not the C# port) |
| `UI++/`, `FTWCMLog/`, `FTWldap/` | Original C++ source, included for historical reference |

## Original project

The original C++ source code is included in this repository under its original license. See [UI++](https://github.com/jason4tw/UIPlusPlus) for the upstream project and Jason Sandys's write-up of 15+ years of history behind it.
