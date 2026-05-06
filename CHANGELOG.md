# Changelog

All notable changes to this package will be documented in this file.

## [1.2.0] - 2026-05-06

### Changed

- **Package renamed**: the UPM identifier moves from `com.barnaff.formulakit` to `com.kobapps.formulakit` to align with the Kobapps namespace. The runtime/editor C# namespaces (`FormulaKit.Runtime`, `FormulaKit.Editor.Tools`, etc.) are unchanged.

### Migration

- Open `Packages/manifest.json`, replace the `com.barnaff.formulakit` key with `com.kobapps.formulakit`, and update the Git URL ref to `#v1.2.0`. Unity will swap packages on next refresh; no source-code changes are required in consuming projects.

## [1.1.1] - 2026-05-06

### Fixed

- Restore the missing `Tests/` and `Tests/Tests/` folder `.meta` files that were dropped during the 1.1.0 layout cleanup. Without them Unity logged "Asset has no meta file, but it's in an immutable folder" warnings on every refresh of a consuming project.
- Register `FormulaKit.Editor.Tests` in `package.json` `testables[]` so the test assembly is picked up by the Test Runner when the package is installed via UPM.

## [1.1.0] - 2026-05-06

### Added

- New `FormulaTokenizer` and `FormulaFunctions` runtime APIs that expose token kinds and the built-in function catalog (signatures, summaries, arities) for editor tooling and future parser work.
- `FormulaParser.TryParse` returning a structured `FormulaParseResult` (line/column/message) without writing to the Console — used by the editor for live diagnostics.
- `return <expr>` keyword in the formula language: short-circuits the formula and yields the given value. Implemented via `ReturnNode` + `FormulaReturnException` caught in `Formula.Evaluate`.
- New UI Toolkit `FormulaCodeEditor` control with native text editing (Enter, selection, copy/paste, IME), tokenizer-driven syntax colors layered behind a transparent `TextField`, snapshot-based undo/redo (`Ctrl/Cmd+Z`, `Ctrl/Cmd+Shift+Z`, `Ctrl/Cmd+Y`), inline diagnostics layer with right-aligned error messages and a column caret marker positioned via `TextElement.MeasureTextSize`, a line-number gutter, and a read-only mode.
- Live input panel in the Formula Builder window: inputs are auto-detected from the expression as you type via the tokenizer; values are preserved across edits when the variable name doesn't change.
- Floating Formula Builder window auto-fits its height to the actual content (capped between 320 and 1200 px).
- New `Tools/Formula Framework/Formula Reference` editor window: left-side category list of every supported function, keyword, and operator; right-side detail panel with signature, summary, description, and an inline read-only `FormulaCodeEditor` showing a working example. The example is also evaluated and the result is shown. About page at the top of the list with the Kobapps logo, package version, and links to the GitHub repo and kobapps.com. All reference content lives in `Editor/Tools/UI/FormulaReference.json`.
- Kobapps editor design system: shared `KobappsEditorTokens.uss` palette/radii/padding tokens consumed by all FormulaKit editor windows.
- FormulaKit icon shown in the header and tab strip of both editor windows.

### Changed

- Move the package manifest and assets to the repository root so the Git URL can be consumed without a `path` query parameter.
- Compile Formula Builder examples into a static script instead of loading them from an external text asset.
- Replace the embedded development project with a Package Manager sample to avoid circular package imports when opening the repository or consuming the Git dependency.
- Rebuild the Formula Builder window on UI Toolkit and replace the custom IMGUI editor with `FormulaCodeEditor`. The "Advanced Editor" toggle is removed — the new editor is the only editor.
- Formulas in the Formula Builder are now anonymous: the formula-ID input is gone, evaluation runs through `FormulaParser` directly with no loader detour.
- Unify all runtime types under the `FormulaKit.Runtime` namespace (previously `FormulaAPI`, `FormulaJsonLoader`, and `IRandomProvider` lived in a stray `FormulaFramework` namespace).
- Drive the parser's built-in function lookups off `FormulaFunctions` so the window, tokenizer, and parser share a single source of truth.

### Fixed

- Restore a standalone Unity development project under `DevelopmentProject` so the repository can be opened directly without import errors.
- Correct the Formula Builder editor namespace so the window loads after the example refactor.
- Repair a stale `GUID:` reference in `FormulaKit.Editor.Tools.asmdef` that prevented the editor assembly from seeing the runtime assembly.
- Stop spamming the Console with `Debug.LogError` while users type partially-valid formulas; the parser now suppresses logging when called via `TryParse`.
- `TextField` no longer selects all text on first click in the code editor.

### Removed

- The custom IMGUI `EditorView` / `ScriptEditorBuffer` editor under `Editor/Tools/Utils` (replaced by `FormulaCodeEditor`).
- Dead syntax-highlight helpers in `FormulaBuilderWindow` (`HighlightFormula`, `EscapeRichText`, the matching regex set, and the duplicated `FunctionSnippet[]` table).
- Manual "Add Input", "Auto-Detect Inputs", and per-row remove buttons from the Inputs section — auto-detection now keeps the list in sync.
- Syntax-help popup ("Help" button); replaced by the new Formula Reference window opened via the "Reference" button.

## [1.0.0] - 2025-10-31

- Initial extraction of Formula Kit into a Unity package that can be installed from a Git URL.
- Includes runtime APIs, editor tooling and optional tests.
