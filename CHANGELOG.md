# Changelog

## v1.1.0-rc1 — unreleased

Parity, correctness and architecture work. The theme throughout: several
behaviours differed from C++ UI++ in ways that produced a *wrong answer* rather
than an error, and one bug stopped the runtime from starting at all.

Existing configuration files continue to work unchanged. The XML root element is
still `<UIpp>`.

### Fixed — the runtime could not start in a task sequence

- `UiSharp.exe` threw at startup inside any real task sequence and left nothing
  behind — no dialog, no log. `_SMSTSLogPath` is a *directory*, and it was
  passed straight to `FileStream` as if it were a file. This is very likely why
  the port had never been successfully field-tested.
- Unhandled exceptions are now written to the log and to `UiSharp_crash.txt`
  beside it, exiting with code 3. No dialog, so an unattended task sequence
  cannot hang on one.
- Opening the log can no longer stop a deployment: it falls back to `%TEMP%`,
  then to discarding output.

### Fixed — variable files

- `<Action Type="Vars" Direction="Save">` wrote an **empty file** inside a task
  sequence. Variables set during a task sequence live in the ConfigMgr
  environment object, which was never enumerated.
- The runtime wrote `name=value` where the documentation says JSON; the two
  `ITSEnv` implementations disagreed about the format of the same interface
  method. Both now share one implementation. JSON also survives values
  containing newlines, which a line-per-variable file silently corrupts.
- A damaged variable file is ignored rather than taking the process down.

### Fixed — conditions gave wrong answers

- **Attributes are variable-substituted**, as in C++ `GetXMLAttribute`. Only
  `Condition`, `CheckCondition` and `WarnCondition` are read raw. Previously
  substitution was applied per attribute, so `RegEx=".{3,5}%Suffix%"` never
  matched and the field could not be satisfied.
- **`DontEval` is honoured**, and values are evaluated as expressions by
  default, as the original does. `<Action Type="TSVar">"%Volume%"</Action>` now
  yields `C:` rather than `"C:"` with the quote characters attached.
- **String comparison is case-sensitive**, matching VBScript's binary compare.
  Affects any config that had come to rely on the previous leniency.
- **`True` and `False` are keywords**, not truthy strings — `True AND False` was
  true.
- **Conditions fail closed.** Anything the engine cannot evaluate is false and
  reported, where before the leftovers of a failed parse could read as true. A
  preflight check on a variable that was never set used to *pass*.
- `<Field>` attributes in `UserAuth`, and the `Match` attributes in
  `SoftwareDiscovery` and `WMIWrite` properties, are substituted.
- `UserAuth` domain lists split on `,` and `;` as the original does, not `|`.
- An absent `Namespace` on a WMI action defaults to `root\cimv2` again.

### Added — the native condition engine

- **The whole VBScript function library is accounted for** — all 92. Most are
  implemented, including arrays (`Split`, `Join`, `Filter`, `UBound`, `LBound`,
  `IsArray`, `arr(0)` indexing), date arithmetic, and every function the UI++
  documentation calls common. The rest are refused with a reason in the log
  rather than returning an empty string, which reads as a false condition.
- **`CreateObject` is evaluated natively** for `Scripting.FileSystemObject`,
  `WScript.Network` and `WScript.Shell.ExpandEnvironmentStrings`, so a config
  using them runs in a WinPE image **without** the `WinPE-Scripting` component.
  Each use is noted in the log alongside its native replacement.
- **UiSharp-only functions**: `FileExists`, `FolderExists`, `DriveExists`,
  `PathParent`, `PathFileName`, `PathBaseName`, `PathExtension`, `PathDrive`,
  `PathCombine`, `ComputerName`, `UserName`, `UserDomain`,
  `ExpandEnvironment`, and four conveniences for what VBScript makes awkward:
  `EqualsIgnoreCase`, `IsSet`, `InList`, `VersionCompare`. A config using these
  will not run under the original C++ UI++.
- Conditions the engine cannot evaluate faithfully are reported in the log with
  the reason and a remedy, rather than silently evaluating false.
- `InputBox`, `MsgBox` and `LoadPicture` are refused: they wait for a person,
  which in an unattended task sequence stops the deployment.

### Added — self-test instruments

- **`UiSharp.exe /selftest`** runs about fifty checks against the live machine
  and writes a report beside the log, where SMSTS log collection will pick it up.
  It displays nothing, so it is safe to run unattended inside a real deployment.
  Exit code 0 if everything passed, 4 if anything failed.
  `/selftestreport:<path>` puts the report somewhere else; a directory is fine.
- The checks cover the paths a unit test cannot reach: the task-sequence
  environment (including *enumerating* variables, the one that has shipped
  broken), the log location, variable files, the condition engine as it actually
  shipped, action-type discovery after trimming, the registry, WMI, and the `X`
  variables collected by `DefaultValues`. The report names whether the run was
  inside a task sequence, because a clean run outside one proves much less.
- **`tools/selftest/UiSharp-SelfTest.xml`** is the companion for what only a
  person can judge: it shows every dialog in turn, each screen saying what to
  look at, and records the answers. Meant to be run in the boot image on the
  hardware being deployed to. See `tools/selftest/README.md`.
- The release workflow now runs `/selftest` against the published single-file
  binary and fails the build if it reports a failure, so trimming cannot quietly
  remove an action type between a green test run and a shipped executable.

### Fixed — an apostrophe in a comment broke config loading

- Unescaped `<` in condition attributes is repaired before parsing, and that
  repair treated `<!-- ... -->` as an ordinary tag. An apostrophe inside a
  comment — `the developer's machine` — opened a quoted attribute section that
  never closed, swallowing the rest of the document. The config then failed to
  parse, pointing at a line nowhere near the comment. Comments, CDATA and
  processing instructions are now copied through untouched. Found by writing the
  interactive self-test configuration and having it refuse to load.

### Changed

- The executable is now `UiSharp.exe`, and namespaces are `UiSharp.*`
  (`UiSharp.Editor.*` for gUI#). The XML root element is unchanged.
- The log is `UiSharp.log` with CMTrace component `UiSharp`, where the original
  wrote `UI++.log`. Update any log-collection step that looks for it by name.
- `ConditionEngine` is a whole-document setting on the root element. On an
  individual action it was silently ignored — and its conditions then evaluated
  false — so it is now reported instead.
- Selecting the VBScript engine logs a warning naming the native alternatives;
  requesting it where the engine is not registered is logged as an error rather
  than falling back in silence.
- The executable carries version and product metadata; it previously had no
  version resource at all.

### Internal

- New `UiSharp.Editing` project: editor-only model and XML round-tripping, so
  the WinPE executable carries no editing code.
- gUI#'s XML sync is shared between the Actions and Software panes. Both panes
  had the same defect independently — an item's comment counted towards its line
  range in one direction but not the other, so clicking a comment selected a
  different item depending on which pane was edited last.
- Variable analysis and the cascading-rename offer moved out of the view model.
  The Variables page's reference count and its usage list now cover the same
  ground; references in element content were counted but not listed.
- Shared build settings and version identity in `Directory.Build.props`.

### Verification

- Tests: 303 → 1432.
- Golden-file snapshots of the original project's eight sample configurations,
  covering configuration resolution end to end.
- Differential tests running 257 expressions through both the native engine and
  the real `vbscript.dll`, asserting they agree. This is how most of the
  condition bugs above were found.
- The published single-file executable is smoke-tested against a configuration
  exercising the new functions, and self-tested on every release build,
  confirming reflection-based action discovery survives trimming.

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
