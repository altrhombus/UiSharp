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
dotnet test src/UIpp.Core.Tests        # 252 tests — pure logic, runs on any OS
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
- [Windows App SDK 2.0.1 runtime](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads) (unpackaged, x64)

```powershell
cd tools/gUISharp
dotnet build -c Debug -p:Platform=x64
```

The output lands in `tools/gUISharp/bin/x64/Debug/net10.0-windows10.0.22621.0/`. Run `gUISharp.exe` directly — no installer required.

> **Note:** Use `-p:Platform=x64` (MSBuild platform property), not `-r win-x64`. The latter changes the output layout and breaks Windows App SDK's native DLL resolution.

### Features

- **Monaco XML editor** — full syntax highlighting, error squiggles, and keyboard shortcuts powered by Monaco Editor 0.52.2 (the VS Code editor engine) hosted in WebView2
- **Guided form** — structured fields for every UI++ action type, kept in bidirectional sync with the XML panel
- **All action types** — TSVar, ExternalCall, DefaultValues, RandomString, FileRead, Vars, FromJson, Rest, SaveItems, ToJson, TSVarList, Preflight, UserInput, UserInfo (normal and full-screen), ErrorInfo, RegRead, RegWrite, AppTree, WmiRead, WmiWrite, UserAuth, SoftwareDiscovery, Switch, TPM
- **Preflight checks** — expandable/collapsible per-check rows with full field editing (text, condition, recheck interval, error text, failed variable, and more)
- **Drag splitter** — resize the guided and XML panels by dragging; collapse either panel entirely
- **Unsaved-changes detection** — title bar marks modified files with `*`; closing with unsaved changes prompts Save / Don't Save / Cancel
- **Open / Save / Save As** — standard file operations via the toolbar

## Original project

The original C++ source code is included in this repository under its original license. See [UI++](https://github.com/jason4tw/UIPlusPlus) for the upstream project and Jason Sandys's write-up of 15+ years of history behind it.
