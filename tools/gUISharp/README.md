# gUI#

A WinUI 3 desktop app for creating and editing [UiSharp](../../README.md) / UI++ XML configuration files. A guided form covers every action type and stays in bidirectional sync with a Monaco-powered XML editor — no hand-editing required, but the raw XML is always one click away.

<img width="1482" height="850" alt="image" src="https://github.com/user-attachments/assets/dc0ac82c-d51e-4f9e-af2b-5b73204b5d42" />

## Building

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

## Features

### Editor layout

- Two-panel layout: guided form (left) + Monaco XML editor (right); drag the splitter to resize or collapse either panel entirely
- Default panel layout preference — choose Guided + XML, Guided only, or XML only as the startup arrangement
- Panel header always shows the human-readable action type of the selected action

### Monaco XML editor

<!-- SCREENSHOT: full editor window with an action selected — shows two-panel layout, nav tree with badges, and the Monaco editor side-by-side -->

- Powered by Monaco Editor 0.52.2 (the VS Code engine) hosted in WebView2 — full syntax highlighting, error squiggles, and IntelliSense-style autocomplete for UI++ elements
- Bidirectional sync: edits in either panel update the other in real time

### Guided action editors

- Structured per-field forms for all action types — TSVar, ExternalCall, DefaultValues, RandomString, FileRead, Vars, FromJson, Rest, SaveItems, ToJson, TSVarList, Preflight, UserInput, UserInfo (normal and full-screen), ErrorInfo, RegRead, RegWrite, AppTree, WmiRead, WmiWrite, UserAuth, SoftwareDiscovery, Switch, TPM
- Input action fields (Text, Choice, Checkbox, Info) each expand to a type-specific mini-editor rather than raw XML
- Preflight check rows split into two sections: Display Text (title, pass/warn/fail descriptions) and Logic (check, warn, and visibility conditions) with an outcome legend

<!-- SCREENSHOT: info markup editor open — shows formatting toolbar, raw edit box, and live preview with %Variable% highlighting -->

- Info markup editor on message fields: formatting toolbar (Bold, Italic, Color, Line Break), raw edit box, and a live preview that renders `<b>`, `<i>`, `<color>`, `<br>` tags and highlights `%Variable%` references

<!-- SCREENSHOT: regex helper flyout — shows live test field, preset list, and quick-reference grid -->

- Regex helper on any regex field: `.*` button opens a flyout with live pattern testing against a sample value, 13 IT-deployment presets (computer names, site codes, GUIDs, locale codes, etc.), and a quick-reference grid

### Color pickers

- Every color field uses a ColorPickerField: hex text box, flyout WinUI color picker, and a live swatch preview — available in Global Settings and the New Config Wizard

### Variable intelligence

- Typing `%` in any condition or value field opens a filtered autocomplete popup of all variables declared in the current config; Tab or Enter inserts `%VariableName%`
- Variables catalog page: every declared variable listed with usage count, declaring action, and field name; click any usage row to jump directly to that action

### Action tree

- Add Action menu has a live search box that filters all types across every category in real time, with plain-English names (e.g., "Input Dialog", "WMI Read") and the XML type as a secondary caption
- Unsaved-changes dot badges on navigation items; auto-clear when edits are reverted without saving
- Funnel icon on any tree node whose action has a `Condition` set — conditionally-executed actions are visible at a glance
- Group nodes use a semi-bold label and a left accent bar to stand out from leaf actions

### New config wizard

<!-- SCREENSHOT: wizard step 3 (appearance) — shows scenario selection context, ColorPickerField, and sidebar/icons toggles -->

- Three-step flow: choose scenario (Standard OSD, Software Only, User Info, Blank) → configure title, subtitle, and variable bases → set appearance (accent color, sidebar, icons)
- Each scenario seeds a sensible default action set

### Global settings

- Live mini-preview renders the accent color and sidebar text color together as you edit them
- Font face picker: editable ComboBox pre-populated with fonts reliably available in WinPE; free-text entry still supported
- Dialog behavior flags: Show Icons, Show Sidebar, Always On Top, Flat, Allow Back, Allow Refresh, Allow Variable Editor
- Condition engine toggle: native C# evaluator (default, no extra WinPE component) or VBScript via `IActiveScript`

### Software catalog & ConfigMgr import

- Visual AppTree editor for Applications and Packages with groups and conditional references
- One-click GUID generator for Software IDs
- Import wizard: browse live Applications and Packages from a ConfigMgr WMI connection and bulk-import to the catalog

### Source control

- Git page (shown only when a repo is detected): branch display, VS Code-style commit graph with shortened hash, subject, author, and relative date
- Stage-and-commit workflow with a commit message dialog; discard changes reverts to HEAD

### File operations

- Open is a split button; the dropdown lists the 5 most recently opened files
- Welcome screen when no file is loaded: New Config and Open File cards plus the recent-files list
- Title bar marks unsaved files with `•`; closing with unsaved changes prompts Save / Don't Save / Cancel

### Preferences

<!-- SCREENSHOT: preferences page — shows update check result, default layout radio buttons, and ConfigMgr fields -->

- Configurable recent-files limit (1–20)
- Pre-save ConfigMgr server and site code; auto-populated when you connect via the Software page
- Default panel layout selection (persisted across sessions)
- Update check: queries GitHub releases on load, compares against the running version (stable-to-stable / pre-release-to-pre-release), and shows a direct link if a newer release is available

### Keyboard shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl+N` | New config |
| `Ctrl+O` | Open file |
| `Ctrl+S` | Save |
| `Ctrl+Shift+S` | Save As |
| `Ctrl+F` | Focus action search |
| `Delete` | Remove selected action |
| `N` / `S` | Quick-answer keys in the Unsaved Changes prompt |

## Troubleshooting

If gUI# crashes silently or fails to start, check `%TEMP%\guisharp_crash.txt` — the app writes a full exception report there on any unhandled error. Non-fatal errors (failed file opens, failed saves) are appended to `%TEMP%\guisharp_error.log`.
