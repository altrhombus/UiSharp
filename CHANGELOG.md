# Changelog

## v1.0.0 — 2026-05-19

Initial open source release of UiSharp and gUI#.

### UiSharp (runtime)

- Complete C# (.NET 10 / WinForms) port of [UI++](https://github.com/jason4tw/UIPlusPlus)
- All original action types supported: TSVar, TSVarList, ExternalCall, DefaultValues, RandomString, FileRead, Vars, FromJson, ToJson, Rest, SaveItems, Switch, Preflight, UserInput, UserInfo, UserInfoFullScreen, ErrorInfo, RegRead, RegWrite, AppTree, WmiRead, WmiWrite, UserAuth, SoftwareDiscovery, TPM
- Native condition evaluator — no `WinPE-Scripting` component required by default
- VBScript condition engine available via `ConditionEngine="vbscript"` or `/conditionengine:vbscript`
- Single-file self-contained publish for WinPE (`dotnet publish --self-contained`)
- 254 cross-platform unit tests (UIpp.Core) + 49 Windows integration tests (UIpp.Windows)
- Intentional behavioral differences from C++ UI++ documented in README

### gUI# (visual editor)

- WinUI 3 visual XML editor for UiSharp/UI++ configuration files
- Monaco XML editor (VS Code engine) with syntax highlighting and error squiggles
- Guided form panel for all 24+ action types, kept in bidirectional sync with the XML panel
- Undo / redo with 50-snapshot history and 500 ms debounce
- Drag-and-drop action reordering in the action tree
- Variables catalog with usage tracking and cross-action navigation
- Variable autocomplete (type `%` in any field)
- Cascading variable rename offer when a declared variable name changes
- Regex helper flyout with live testing and IT-deployment presets
- Info markup editor with formatting toolbar and live preview
- Import Applications and Packages from a live ConfigMgr / WMI connection
- Per-section unsaved-changes badges; title bar `•` indicator
- Recent files list (configurable limit)
- New Config wizard with scenario templates
- Preferences page: theme (System / Light / Dark) and recent files limit
- Crash log written to `%TEMP%\guisharp_crash.txt`
