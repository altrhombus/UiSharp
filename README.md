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

Only `UIpp.Core` is currently implemented. It builds and its full test suite runs on macOS and Linux.

## Building

**Requirements:** .NET 10 SDK

```bash
dotnet build
dotnet test src/UIpp.Core.Tests
```

## Original project

The original C++ source code is included in this repository under its original license. See [UI++](https://github.com/jason4tw/UIPlusPlus) for the upstream project and Jason Sandys's write-up of 15+ years of history behind it.
