# UiSharp + gUI#

A C# (.NET 10 / WinForms) port of [UI++](https://github.com/jason4tw/UIPlusPlus) — the ConfigMgr task-sequence front-end originally written in C++17/MFC by Jason Sandys.

## What is gUI#?
gUI# is a graphical tool for creating and modifying UI++/UiSharp XML files. A guided view shows configuration settings and stays in sync with the built-in XML editor that checks for valid XML and includes IntelliSense-like capabilities for the UI++ actions.

<img width="1482" height="850" alt="image" src="https://github.com/user-attachments/assets/dc0ac82c-d51e-4f9e-af2b-5b73204b5d42" />

## What is UiSharp?

UiSharp displays a customizable WinForms UI during OS deployments driven by Microsoft Configuration Manager (SCCM/ConfigMgr). It reads an XML configuration file and presents input dialogs, info screens, and pre-flight checks, then writes the collected values back as task-sequence variables that the rest of the deployment can consume.

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
| `tools/gUISharp` | net10.0-windows10.0.22621.0 | WinUI 3 visual XML editor for UiSharp configuration files |

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

## Building gUI#

**Requirements:**

- Windows 10 1809 (build 17763) or later; Windows 11 recommended
- .NET 10 SDK
- [Windows App SDK 2.0.1 runtime](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads) (unpackaged; choose the x64, x86, or arm64 installer to match your target platform)
- Microsoft Edge or the [standalone WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) — required for the Monaco XML editor panel (already present on any Windows 11 machine with Edge installed)

```powershell
cd tools/gUISharp
dotnet build -c Debug -p:Platform=x64
```

The output lands in `tools/gUISharp/bin/x64/Debug/net10.0-windows10.0.22621.0/`. Run `gUISharp.exe` directly — no installer required.

> **Note:** Always pass `-p:Platform=x64` (or `arm64`/`x86`). This is the MSBuild platform property, not the .NET runtime identifier. Without it, `WebView2Loader.dll` is not copied to the exe directory and the XML editor panel fails to initialize. Using `-r win-x64` instead changes the output layout and breaks Windows App SDK's native DLL resolution.

### Features

- **Monaco XML editor** — full syntax highlighting, error squiggles, and keyboard shortcuts powered by Monaco Editor 0.52.2 (the VS Code editor engine) hosted in WebView2
- **Guided form** — structured fields for every UI++ action type, kept in bidirectional sync with the XML panel
- **All action types** — TSVar, ExternalCall, DefaultValues, RandomString, FileRead, Vars, FromJson, Rest, SaveItems, ToJson, TSVarList, Preflight, UserInput, UserInfo (normal and full-screen), ErrorInfo, RegRead, RegWrite, AppTree, WmiRead, WmiWrite, UserAuth, SoftwareDiscovery, Switch, TPM
- **Preflight checks** — expandable/collapsible per-check rows with full field editing (display text, description, error/warn description, check condition, warn condition, visibility condition)
- **Drag splitter** — resize the guided and XML panels by dragging; collapse either panel entirely
- **Unsaved-changes detection** — title bar marks modified files with `*`; closing with unsaved changes prompts Save / Don't Save / Cancel
- **Open / Save / Save As** — standard file operations via the toolbar

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

All other XML attributes, variable names, dialog behavior, and output formats are identical to the C++ original. Existing XML config files work without modification.

## Original project

The original C++ source code is included in this repository under its original license. See [UI++](https://github.com/jason4tw/UIPlusPlus) for the upstream project and Jason Sandys's write-up of 15+ years of history behind it.
