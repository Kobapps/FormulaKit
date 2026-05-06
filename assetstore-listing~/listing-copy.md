# Formula Kit — Asset Store Listing Copy

Drafts for the Unity Publisher Portal form. Paste into the matching fields when submitting `com.kobapps.formulakit` for review.

---

## Title (max 50 chars)

```
Formula Kit — Runtime Formula Builder
```

Alternative shorter form:
```
Formula Kit
```

---

## Subtitle / Short description (≤ 200 chars)

```
Author readable text formulas — arithmetic, conditionals, let-bindings, return — and evaluate them at runtime. Includes a UI Toolkit Formula Builder window for live authoring with syntax highlighting, undo, and inline error markers.
```

---

## Full description (rich text)

> Format the headers / bullets in the Publisher Portal's rich-text editor. The body below is the source content.

### Author once, evaluate everywhere

Formula Kit lets designers and engineers express game math as plain text strings — `baseDamage * (1 + strength * 0.1)` — and evaluate them at runtime against a dictionary of inputs. Source the strings from anywhere: ScriptableObjects, TextAssets, remote configs, save files, even runtime player input. The runtime parses each formula once, caches the parsed AST, and evaluates it as fast as a pure-C# expression tree.

### Two editor windows that pay for themselves

**Formula Builder** is a fully-featured UI Toolkit code editor for prototyping formulas without leaving Unity:
- Live syntax highlighting driven by the same tokenizer the runtime uses.
- Snapshot-based undo / redo.
- Live input auto-detection — every variable referenced shows up as an input row; values you've set persist across edits.
- Inline parse-error markers with line / column information.
- One-click **Evaluate** with the current inputs.

**Formula Reference** is a built-in language guide — left-side category list of every supported function, keyword, and operator; right-side detail panel with signature, summary, description, and a runnable example whose result is shown live.

### Expression language

- Arithmetic: `+ - * / %` and exponentiation `^`
- Comparisons: `< <= > >= == !=`
- Logical: `&& || !`
- Ternary: `condition ? a : b`
- Statements: `let` declarations, `=` / `+=` / `-=` / `*=` / `/=` assignments, `if` / `else if` / `else`, block scopes, and `return <expr>` for early exit.
- Math helpers: `abs`, `acos`, `asin`, `atan`, `ceil`, `clamp`, `clamp01`, `cos`, `exp`, `floor`, `lerp`, `log`, `max`, `min`, `negative`, `pow`, `round`, `sign`, `sin`, `sqrt`, `tan`.
- Random helpers: `random()`, `rand(maxExclusive)`, `randf(maxExclusive)` — pluggable via `IRandomProvider` for deterministic / testable runs.

### Runtime API

```csharp
using FormulaKit.Runtime;

float damage = FormulaAPI.Run(
    "baseDamage * (1 + strength * 0.1)",
    new Dictionary<string, float>
    {
        ["baseDamage"] = 25f,
        ["strength"]   = 12f,
    });
```

For repeated evaluations against the same formula, use `FormulaLoader` + `FormulaRunner` with input pooling. For tokenization, function catalog enumeration, and silent parse validation, use `FormulaTokenizer`, `FormulaFunctions`, and `FormulaParser.TryParse`.

### What's included

- Runtime evaluator (pure C#, no Unity-specific dependencies in the core nodes)
- Static `FormulaAPI` for one-line evaluation
- `FormulaLoader` + `FormulaRunner` with caching and input-pool reuse
- `FormulaTokenizer` and `FormulaFunctions` catalogs for editor tools
- Two UI Toolkit editor windows (Builder + Reference) with shared design tokens
- Edit-mode test suite (`FormulaKit.Editor.Tests`) covering runtime, loader, and runner

### Technical details

- **Unity:** 6000.1 or newer (UI Toolkit-based editor windows).
- **Render pipeline:** any (no rendering dependencies).
- **Platform:** any (runtime is pure C#; editor windows are editor-only).
- **License:** MIT.
- **Language:** C# (no DLLs, full source).

---

## Category

Tools → Utilities

(Alternative if the Publisher Portal asks for two: Tools → Utilities AND Tools → Integration)

---

## Tags / keywords

```
formula, expression, math, parser, evaluator, scripting, runtime, editor tool,
designer-friendly, ui-toolkit, syntax highlighting, undo, dsl
```

---

## Compatibility

- Unity 6000.1 or newer
- Render pipelines: Built-in, URP, HDRP (no rendering used)
- All build targets supported

---

## Documentation URL

https://github.com/Kobapps/FormulaKit/blob/main/README.md

## Changelog URL

https://github.com/Kobapps/FormulaKit/blob/main/CHANGELOG.md

## Source / issue tracker

https://github.com/Kobapps/FormulaKit

---

## Submission notes for reviewer (private)

> Internal-only field; helps the reviewer if anything is non-obvious. Optional.

Formula Kit ships entirely as a UPM package (`com.kobapps.formulakit`). The package compiles cleanly on Unity 6000.1; both editor windows live under **Tools → Formula Framework**. The included edit-mode tests are registered via `package.json` `testables[]` and pass under the Test Runner (Window → General → Test Runner → Run All).
