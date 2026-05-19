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
- **Input action field editors** — structured per-field mini-editors for all input types (Text, Choice, Checkbox, Info); each field expands to show its specific controls rather than raw XML
- **Preflight checks** — expandable per-check rows split into two sections: Display Text (title, pass/warn/fail descriptions) and Logic (check condition, warn condition, visibility condition) with an outcome legend
- **Regex helper** — any regex field has a `.*` helper button that opens a flyout with live pattern testing against a sample value, 13 IT-deployment presets (computer names, site code groups, locale codes, GUIDs, etc.), and a quick-reference grid
- **Info markup editor** — message fields for Info actions include a formatting toolbar (Bold, Italic, Color, Line Break), a raw edit box, and a live preview that renders the supported `<b>`, `<i>`, `<color>`, `<br>` tag subset and highlights `%Variable%` references
- **Variable autocomplete** — typing `%` in any condition or value field opens a filtered popup of all variables declared in the current config; Tab or Enter inserts `%VariableName%`
- **Variables catalog** — dedicated page lists every declared variable with usage count, declaring action, and field name; click any usage row to jump directly to that action
- **Add action search** — the Add Action menu has a live search box that filters all action types across every category in real time
- **Human-friendly action names** — the Add menu shows plain-English names (e.g., "Input Dialog", "WMI Read") with the XML type as a secondary caption
- **Recent files** — the Open button is a split button; the dropdown lists the 5 most recently opened files
- **Welcome screen** — opening the app with no file loaded shows a full-page welcome with New Config and Open File cards plus the recent-files list
- **Unsaved-changes badges** — navigation items show dot badges when a section has unsaved edits; badges clear automatically when fields are reverted to their saved state without requiring a save
- **Conditional action badge** — a funnel icon on any tree node whose action has a `Condition` set, so conditionally-executed actions are visible at a glance
- **Action group distinction** — group nodes in the tree use a semi-bold label and a left accent bar to stand out from leaf actions
- **Panel header shows action type** — the guided panel header displays the human-readable action name of the selected action
- **Font face picker** — Global Settings font field is an editable ComboBox pre-populated with fonts reliably present in WinPE; free-text entry is still supported for custom fonts
- **Live dialog preview** — Global Settings shows a live mini-preview that renders the accent color and sidebar text color together as the user types
- **Generate ID button** — the Software page Id field has a one-click button that generates a new GUID
- **Drag splitter** — resize the guided and XML panels by dragging; collapse either panel entirely
- **Keyboard shortcuts** — `Ctrl+N` New, `Ctrl+O` Open, `Ctrl+S` Save, `Ctrl+Shift+S` Save As, `Ctrl+F` focus action search, `Delete` remove selected action, `N` / `S` in the Unsaved Changes prompt
- **Unsaved-changes detection** — title bar marks modified files with `•`; closing with unsaved changes prompts Save / Don't Save / Cancel
- **Import from ConfigMgr** — browse Applications and Packages from a live WMI connection and bulk-import them to the Software catalog

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

## Troubleshooting

If gUI# crashes silently or fails to start, check `%TEMP%\guisharp_crash.txt` — the app writes a full exception report there on any unhandled error. Non-fatal errors (failed file opens, failed saves) are appended to `%TEMP%\guisharp_error.log`.

## Repository layout

| File / folder | Purpose |
|---|---|
| `UiSharp.slnx` | Modern C# solution — open this in Visual Studio or Rider |
| `src/` | C# runtime projects (`UIpp.Core`, `UIpp.Windows`, `UIpp.UI`, `UIpp`) |
| `tools/gUISharp/` | WinUI 3 visual XML editor |
| `UI++.sln` | Legacy C++ solution for the original UI++ source (not the C# port) |
| `UI++/`, `FTWCMLog/`, `FTWldap/` | Original C++ source, included for historical reference |

## Original project

The original C++ source code is included in this repository under its original license. See [UI++](https://github.com/jason4tw/UIPlusPlus) for the upstream project and Jason Sandys's write-up of 15+ years of history behind it.
