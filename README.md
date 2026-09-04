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
| `UIpp.Core` | net10.0 | Pure logic — parsers, evaluators, action implementations. No Windows dependencies. |
| `UIpp.Windows` | net10.0-windows | WMI, COM, LDAP, registry integrations |
| `UIpp.UI` | net10.0-windows | WinForms dialogs and controls |
| `UIpp` | net10.0-windows | Entry point, single-file publish target |
| `tools/gUISharp` | net10.0-windows10.0.22621.0 | WinUI 3 visual XML editor — see [tools/gUISharp/README.md](tools/gUISharp/README.md) |

## Building

**Requirements:** .NET 10 SDK (Windows required for `UIpp.Windows`, `UIpp.UI`, and `UIpp`)

```bash
dotnet build
dotnet test src/UIpp.Core.Tests        # 254 tests — pure logic, runs on any OS
dotnet test src/UIpp.Windows.Tests     # 49 tests  — registry/WMI, Windows only
```

## Deploying to WinPE

Publish as a single self-contained executable — no .NET runtime or redistributable needed in the WinPE image:

```powershell
dotnet publish src/UIpp/UIpp.csproj -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:TrimMode=partial -c Release
```

Copy the output `UIpp.exe` into your WinPE image in place of the original `UI++.exe`. No other changes to the image are required.

## Command-line arguments

| Argument | Purpose |
|---|---|
| `/Config:<path\|URL>` | XML config file path or HTTP/HTTPS URL. Default: `UI++.xml` in the current directory. |
| `/ConfigFallback:<path>` | Local file to use if the URL download fails after all retries. |
| `/ConfigRetry:<n>` | Number of download attempts before falling back (default: 3). |
| `/DisableTSVarEditor` | Prevents the Ctrl+F2 task-sequence variable editor from opening during dialogs. |
| `/conditionengine:native\|vbscript` | Override the condition evaluator. Default: `native`. `vbscript` uses the Windows `IActiveScript` COM host; requires `WinPE-Scripting` in WinPE. |

## Differences from the original C++ UI++

These are the only intentional behavioral changes:

| Area | Original UI++ | UiSharp |
|---|---|---|
| `<Action Type="Vars">` save format | MFC `CArchive` binary (`.dat`) | JSON — existing `.dat` files are not readable |
| `<Action Type="RandomString">` output variable | Always writes to `Random` regardless of the `Variable` attribute (C++ bug) | Correctly uses the `Variable` attribute |
| VBScript condition engine | Fully supported via `IActiveScript` COM | Supported — select with `ConditionEngine="vbscript"` or `/conditionengine:vbscript`. Requires `WinPE-Scripting` in WinPE; default engine is `native` which needs no extra component. |
| WinPE optional components | `WinPE-WMI` + `WinPE-Scripting` required | `WinPE-WMI` always; `WinPE-Scripting` only when using the vbscript engine |
| Log file name | `UI++.log` in `_SMSTSLogPath` (or `%TEMP%`), CMTrace component `UI++` | `UiSharp.log`, CMTrace component `UiSharp` — so it is obvious which tool wrote it. Update any log-collection step that looks for `UI++.log` by name. |
| Unhandled errors | No handler — a crash left nothing behind | Written to the log and to `UiSharp_crash.txt` beside it, exiting with code 3. No dialog, so an unattended task sequence cannot hang on one. |

All other XML attributes, variable names, dialog behavior, and output formats are identical to the C++ original. Existing XML config files work without modification.

Parity is verified two ways, both in the test suite: golden-file snapshots of the original project's own sample configs, and differential tests that run the same expressions through the native engine and the real `vbscript.dll` and assert they agree.

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

Still requiring `ConditionEngine="vbscript"`: `GetObject` (WMI — prefer `<Action Type="WMIRead">`), `Eval`, `Execute`, `Split`, `WScript.Shell.RegRead` (prefer `<Action Type="RegRead">`), and any other ProgID. These are reported in the log rather than silently evaluating false.

## Repository layout

| File / folder | Purpose |
|---|---|
| `UiSharp.slnx` | Modern C# solution — open this in Visual Studio or Rider |
| `src/` | C# runtime projects (`UIpp.Core`, `UIpp.Windows`, `UIpp.UI`, `UIpp`) |
| `tools/gUISharp/` | WinUI 3 visual XML editor — see [tools/gUISharp/README.md](tools/gUISharp/README.md) |
| `UI++.sln` | Legacy C++ solution for the original UI++ source (not the C# port) |
| `UI++/`, `FTWCMLog/`, `FTWldap/` | Original C++ source, included for historical reference |

## Original project

The original C++ source code is included in this repository under its original license. See [UI++](https://github.com/jason4tw/UIPlusPlus) for the upstream project and Jason Sandys's write-up of 15+ years of history behind it.
